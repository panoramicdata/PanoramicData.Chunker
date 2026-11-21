using DocumentFormat.OpenXml.Spreadsheet;
using PanoramicData.Chunker.Utilities;
using System.Text;

namespace PanoramicData.Chunker.Chunkers.Xlsx;

public partial class XlsxDocumentChunker
{
	private static readonly (CellValues DataType, XlsxCellType Result)[] CellTypeMap =
	[
		(CellValues.SharedString, XlsxCellType.Text),
		(CellValues.InlineString, XlsxCellType.Text),
		(CellValues.Boolean, XlsxCellType.Boolean),
		(CellValues.Error, XlsxCellType.Error),
		(CellValues.Date, XlsxCellType.Date),
		(CellValues.Number, XlsxCellType.Number)
	];

	private void ExtractRowChunks(List<Row> rows, string sheetName, Guid worksheetId)
	{
		foreach (var row in rows)
		{
			var rowChunk = CreateRowChunk(row, sheetName, worksheetId);
			if (rowChunk != null)
			{
				_chunks.Add(rowChunk);
			}
		}
	}

	private XlsxRowChunk? CreateRowChunk(Row row, string sheetName, Guid worksheetId)
	{
		var rowNumber = GetRowNumber(row);
		if (rowNumber == 0)
		{
			return null;
		}

		var (cells, rowText) = ReadCells(row);
		return cells.Count == 0 || rowText.Length == 0
			? null
			: BuildRowChunk(row, rowNumber, cells, rowText.ToString().Trim(), sheetName, worksheetId);
	}

	private static int GetRowNumber(Row row) => (int)(row.RowIndex?.Value ?? 0);

	private XlsxRowChunk BuildRowChunk(Row row, int rowNumber, List<XlsxCellData> cells, string content, string sheetName, Guid worksheetId)
	{
		return new XlsxRowChunk
		{
			Content = content,
			RowNumber = rowNumber,
			SheetName = sheetName,
			Cells = cells,
			IsHeaderRow = rowNumber == 1,
			IsHidden = row.Hidden?.Value ?? false,
			Height = row.Height?.Value,
			ParentId = worksheetId,
			SequenceNumber = _sequenceNumber++,
			SpecificType = "row",
			Metadata = CreateMetadata(sheetName, $"{sheetName}/row{rowNumber}", "row", sheetName),
			QualityMetrics = CalculateQualityMetrics(content)
		};
	}

	private (List<XlsxCellData> Cells, StringBuilder Text) ReadCells(Row row)
	{
		var cells = new List<XlsxCellData>();
		var rowText = new StringBuilder();

		foreach (var cell in row.Elements<Cell>())
		{
			var value = GetCellValue(cell);
			cells.Add(CreateCellData(cell, value));

			if (!string.IsNullOrWhiteSpace(value))
			{
				_ = rowText.Append(value).Append(' ');
			}
		}

		return (cells, rowText);
	}

	private static XlsxCellData CreateCellData(Cell cell, string? value)
	{
		var cellRef = cell.CellReference?.Value ?? string.Empty;

		return new XlsxCellData
		{
			CellReference = cellRef,
			ColumnName = GetColumnName(cellRef),
			Value = value ?? string.Empty,
			CellType = DetermineCellType(cell),
			Formula = cell.CellFormula?.Text
		};
	}

	private void ExtractFormulaChunks(List<Row> rows, string sheetName, Guid worksheetId)
	{
		var formulaCells = rows
			.SelectMany(row => row.Elements<Cell>())
			.Where(cell => !string.IsNullOrWhiteSpace(cell.CellFormula?.Text));

		foreach (var cell in formulaCells)
		{
			_chunks.Add(CreateFormulaChunk(cell, sheetName, worksheetId));
		}
	}

	private XlsxFormulaChunk CreateFormulaChunk(Cell cell, string sheetName, Guid worksheetId)
	{
		var cellRef = cell.CellReference?.Value ?? string.Empty;
		var formulaText = cell.CellFormula!.Text;
		var formulaType = ExtractFormulaType(formulaText);

		return new XlsxFormulaChunk
		{
			Content = formulaText,
			CellReference = cellRef,
			SheetName = sheetName,
			Formula = formulaText,
			CalculatedValue = GetCellValue(cell),
			FormulaType = formulaType,
			ReferencedCells = ExtractReferencedCells(formulaText),
			IsArrayFormula = false,
			ParentId = worksheetId,
			SequenceNumber = _sequenceNumber++,
			SpecificType = "formula",
			Metadata = CreateMetadata(sheetName, $"{sheetName}/formula/{cellRef}", "formula", formulaType ?? "unknown", sheetName),
			QualityMetrics = CalculateQualityMetrics(formulaText)
		};
	}
}
