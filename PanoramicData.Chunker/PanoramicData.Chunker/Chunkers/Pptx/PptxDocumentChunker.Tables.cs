using PanoramicData.Chunker.Models;
using PanoramicData.Chunker.Utilities;
using A = DocumentFormat.OpenXml.Drawing;

namespace PanoramicData.Chunker.Chunkers.Pptx;

public partial class PptxDocumentChunker
{
	private PptxTableChunk? CreateTableChunk(A.Table table, int slideNumber, Guid slideId)
	{
		var rows = table.Elements<A.TableRow>().ToList();
		if (rows.Count == 0)
		{
			return null;
		}

		// The first row is treated as the header (simplified check)
		var headers = ExtractTableRowCells(rows[0]);
		var tableData = rows
			.Skip(1)
			.Select(ExtractTableRowCells)
			.Where(cells => cells.Count > 0)
			.ToList();

		return headers.Count == 0 && tableData.Count == 0
			? null
			: BuildTableChunk(slideNumber, slideId, headers, tableData);
	}

	private PptxTableChunk BuildTableChunk(int slideNumber, Guid slideId, List<string> headers, List<List<string>> tableData)
	{
		var serializedTable = MarkdownTableSerializer.Serialize(headers, tableData);

		return new PptxTableChunk
		{
			Content = serializedTable,
			SlideNumber = slideNumber,
			HasFirstRowHeader = true,
			ParentId = slideId,
			SequenceNumber = _sequenceNumber++,
			SpecificType = "table",
			SerializedTable = serializedTable,
			SerializationFormat = TableSerializationFormat.Markdown,
			TableInfo = TableChunkSupport.CreateInfo(headers, tableData, TableChunkSupport.GetColumnCount(headers, tableData), true, false),
			Metadata = CreateMetadata(slideNumber, $"slide{slideNumber}/table", "table", $"slide{slideNumber}"),
			QualityMetrics = CalculateQualityMetrics(TableChunkSupport.GetAllText(headers, tableData))
		};
	}

	private static List<string> ExtractTableRowCells(A.TableRow row)
		=> [.. row.Elements<A.TableCell>().Select(cell => WhitespaceRegex().Replace(GetCellText(cell), " ").Trim())];

	private static string GetCellText(A.TableCell cell)
		=> cell.TextBody == null
			? string.Empty
			: string.Concat(cell.TextBody.Elements<A.Paragraph>().Select(GetRunsText));
}
