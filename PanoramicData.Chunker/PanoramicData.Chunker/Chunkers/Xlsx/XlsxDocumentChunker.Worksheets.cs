using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using PanoramicData.Chunker.Models;
using PanoramicData.Chunker.Utilities;

namespace PanoramicData.Chunker.Chunkers.Xlsx;

public partial class XlsxDocumentChunker
{
	private static ChunkMetadata CreateMetadata(string sheetName, string hierarchy, params string[] tags) => new()
	{
		DocumentType = "XLSX",
		SourceId = string.Empty,
		Hierarchy = hierarchy,
		Tags = [.. tags],
		SheetName = sheetName,
		CreatedAt = DateTimeOffset.UtcNow
	};

	private void ExtractWorksheetChunks(WorksheetPart worksheetPart, Sheet sheet, int sheetIndex)
	{
		var rows = worksheetPart.Worksheet?.GetFirstChild<SheetData>()?.Elements<Row>().ToList();
		if (rows is not { Count: > 0 })
		{
			return;
		}

		var sheetName = sheet.Name?.Value ?? $"Sheet{sheetIndex + 1}";
		var worksheetChunk = CreateWorksheetChunk(rows, sheet, sheetName, sheetIndex);
		_chunks.Add(worksheetChunk);

		if (ShouldTreatAsTable(rows))
		{
			ExtractTableChunks(rows, sheetName, worksheetChunk.Id);
		}
		else
		{
			ExtractRowChunks(rows, sheetName, worksheetChunk.Id);
		}

		ExtractFormulaChunks(rows, sheetName, worksheetChunk.Id);
	}

	private XlsxWorksheetChunk CreateWorksheetChunk(List<Row> rows, Sheet sheet, string sheetName, int sheetIndex)
	{
		var firstRowIndex = rows.First().RowIndex?.Value ?? 1;
		var lastRowIndex = rows.Last().RowIndex?.Value ?? (uint)rows.Count;
		var maxColumns = rows.SelectMany(r => r.Elements<Cell>()).Select(c => GetColumnIndex(c.CellReference?.Value ?? "A1")).Max();

		return new XlsxWorksheetChunk
		{
			SheetName = sheetName,
			SheetIndex = sheetIndex,
			RowCount = rows.Count,
			ColumnCount = maxColumns,
			IsHidden = sheet.State?.Value == SheetStateValues.Hidden,
			UsedRange = $"A{firstRowIndex}:{GetLastColumnReference(rows)}{lastRowIndex}",
			ParentId = null,
			SequenceNumber = _sequenceNumber++,
			SpecificType = "worksheet",
			Metadata = CreateMetadata(sheetName, sheetName, "worksheet", sheetName),
			QualityMetrics = new ChunkQualityMetrics
			{
				TokenCount = 0,
				CharacterCount = 0,
				WordCount = 0,
				SemanticCompleteness = 1.0
			}
		};
	}

	private bool ShouldTreatAsTable(List<Row> rows)
	{
		if (rows.Count < 2)
		{
			return false;
		}

		var firstRowCells = rows[0].Elements<Cell>().ToList();
		if (firstRowCells.Count == 0)
		{
			return false;
		}

		// Mostly text values in the first row suggests headers
		var textCount = firstRowCells
			.Select(GetCellValue)
			.Count(value => !string.IsNullOrWhiteSpace(value) && !double.TryParse(value, out _));

		return textCount >= firstRowCells.Count * 0.6;
	}

	private void ExtractTableChunks(List<Row> rows, string sheetName, Guid worksheetId)
	{
		if (rows.Count < 2)
		{
			return;
		}

		var headerRow = rows[0];
		var headers = ReadRowValues(headerRow);
		var tableData = rows.Skip(1).Select(row => PadRow(ReadRowValues(row), headers.Count)).ToList();
		var serializedTable = MarkdownTableSerializer.Serialize(headers, tableData);

		var firstCellRef = headerRow.Elements<Cell>().FirstOrDefault()?.CellReference?.Value ?? "A1";
		var lastCellRef = rows.Last().Elements<Cell>().LastOrDefault()?.CellReference?.Value ?? $"A{rows.Count}";

		var tableChunk = new XlsxTableChunk
		{
			Content = serializedTable,
			SheetName = sheetName,
			Range = $"{firstCellRef}:{lastCellRef}",
			IsFormattedTable = false,
			ParentId = worksheetId,
			SequenceNumber = _sequenceNumber++,
			SpecificType = "table",
			SerializedTable = serializedTable,
			SerializationFormat = TableSerializationFormat.Markdown,
			TableInfo = new TableMetadata
			{
				RowCount = tableData.Count,
				ColumnCount = headers.Count,
				Headers = [.. headers],
				HasHeaderRow = true,
				HasMergedCells = false,
				PreferredFormat = TableSerializationFormat.Markdown
			},
			Metadata = CreateMetadata(sheetName, $"{sheetName}/table", "table", sheetName)
		};

		var allText = string.Join(" ", headers) + " " + string.Join(" ", tableData.SelectMany(r => r));
		tableChunk.QualityMetrics = CalculateQualityMetrics(allText);

		_chunks.Add(tableChunk);
	}

	private List<string> ReadRowValues(Row row)
		=> [.. row.Elements<Cell>().Select(cell => GetCellValue(cell) ?? string.Empty)];

	private static List<string> PadRow(List<string> rowData, int columnCount)
	{
		while (rowData.Count < columnCount)
		{
			rowData.Add(string.Empty);
		}

		return rowData;
	}
}
