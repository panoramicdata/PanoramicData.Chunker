using DocumentFormat.OpenXml.Wordprocessing;
using PanoramicData.Chunker.Models;
using PanoramicData.Chunker.Utilities;

namespace PanoramicData.Chunker.Chunkers.Docx;

public partial class DocxDocumentChunker
{
	private DocxTableChunk? ProcessTable(Table table, Guid? parentId)
	{
		var rows = table.Elements<TableRow>().ToList();
		if (rows.Count == 0)
		{
			return null;
		}

		var hasHeaderRow = IsHeaderRow(rows[0]);
		var headers = hasHeaderRow ? ExtractRowCells(rows[0]) : [];
		var tableData = rows
			.Skip(hasHeaderRow ? 1 : 0)
			.Select(ExtractRowCells)
			.Where(cells => cells.Count > 0)
			.ToList();

		return headers.Count == 0 && tableData.Count == 0
			? null
			: CreateTableChunk(table, parentId, hasHeaderRow, headers, tableData);
	}

	private DocxTableChunk CreateTableChunk(
		Table table,
		Guid? parentId,
		bool hasHeaderRow,
		List<string> headers,
		List<List<string>> tableData)
	{
		var properties = table.GetFirstChild<TableProperties>();
		var serializedTable = MarkdownTableSerializer.Serialize(headers, tableData);
		var allText = string.Join(" ", headers) + " " + string.Join(" ", tableData.SelectMany(r => r));

		return new DocxTableChunk
		{
			Content = serializedTable,
			TableStyle = GetTableStyle(properties),
			Caption = GetTableCaption(properties),
			Description = GetTableDescription(properties),
			HasBandedRows = false,
			HasBandedColumns = false,
			Position = "inline",
			ParentId = parentId,
			SequenceNumber = _sequenceNumber++,
			SpecificType = "table",
			SerializedTable = serializedTable,
			SerializationFormat = TableSerializationFormat.Markdown,
			TableInfo = CreateTableInfo(table, hasHeaderRow, headers, tableData),
			Metadata = CreateMetadata("table", "table"),
			QualityMetrics = CalculateQualityMetrics(allText)
		};
	}

	private static string? GetTableStyle(TableProperties? properties) => properties?.TableStyle?.Val?.Value;

	private static string? GetTableCaption(TableProperties? properties) => properties?.TableCaption?.Val?.Value;

	private static string? GetTableDescription(TableProperties? properties) => properties?.TableDescription?.Val?.Value;

	private static TableMetadata CreateTableInfo(Table table, bool hasHeaderRow, List<string> headers, List<List<string>> tableData) => new()
	{
		RowCount = tableData.Count,
		ColumnCount = GetColumnCount(headers, tableData),
		Headers = [.. headers],
		HasHeaderRow = hasHeaderRow,
		HasMergedCells = HasMergedCells(table),
		PreferredFormat = TableSerializationFormat.Markdown
	};

	private static int GetColumnCount(List<string> headers, List<List<string>> tableData)
	{
		if (headers.Count > 0)
		{
			return headers.Count;
		}

		return tableData.Count > 0 ? tableData[0].Count : 0;
	}

	// Header rows typically have bold text
	private static bool IsHeaderRow(TableRow row)
		=> row.Elements<TableCell>().Any(cell => cell.Descendants<RunProperties>().FirstOrDefault()?.Bold != null);

	private static List<string> ExtractRowCells(TableRow row)
		=> [.. row.Elements<TableCell>().Select(cell => WhitespaceRegex().Replace(cell.InnerText, " ").Trim())];

	private static bool HasMergedCells(Table table)
		=> table.Elements<TableRow>()
			.SelectMany(row => row.Elements<TableCell>())
			.Select(cell => cell.GetFirstChild<TableCellProperties>())
			.Any(properties => properties?.GridSpan != null || properties?.VerticalMerge != null);
}
