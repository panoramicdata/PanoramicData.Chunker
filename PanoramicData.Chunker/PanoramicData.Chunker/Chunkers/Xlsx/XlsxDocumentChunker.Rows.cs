using DocumentFormat.OpenXml.Spreadsheet;
using PanoramicData.Chunker.Models;
using System.Text;
using System.Text.RegularExpressions;

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
		var rowNumber = (int)(row.RowIndex?.Value ?? 0);
		if (rowNumber == 0)
		{
			return null;
		}

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

		if (cells.Count == 0 || rowText.Length == 0)
		{
			return null;
		}

		var content = rowText.ToString().Trim();

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

	private string? GetCellValue(Cell cell)
	{
		if (cell.CellValue == null)
		{
			return cell.InnerText;
		}

		var value = cell.CellValue.Text;
		if (cell.DataType?.Value != CellValues.SharedString || _sharedStringTable == null || !int.TryParse(value, out var index))
		{
			return value;
		}

		var sharedStringItem = _sharedStringTable.SharedStringTable?.Elements<SharedStringItem>().ElementAtOrDefault(index);
		return sharedStringItem?.InnerText ?? value;
	}

	private static XlsxCellType DetermineCellType(Cell cell)
	{
		if (cell.CellFormula != null)
		{
			return XlsxCellType.Formula;
		}

		var dataType = cell.DataType?.Value;
		foreach (var (type, result) in CellTypeMap)
		{
			if (dataType == type)
			{
				return result;
			}
		}

		return cell.CellValue != null ? XlsxCellType.Number : XlsxCellType.Empty;
	}

	private static string GetColumnName(string cellReference)
		=> string.IsNullOrEmpty(cellReference) ? "A" : ColumnNameRegex().Match(cellReference).Value;

	private static int GetColumnIndex(string cellReference)
		=> GetColumnName(cellReference).Aggregate(0, (index, c) => (index * 26) + (c - 'A' + 1));

	private static string GetLastColumnReference(List<Row> rows)
	{
		var maxColumnIndex = rows
			.SelectMany(row => row.Elements<Cell>())
			.Select(cell => cell.CellReference?.Value)
			.Where(cellRef => !string.IsNullOrEmpty(cellRef))
			.Select(cellRef => GetColumnIndex(cellRef!))
			.Prepend(0)
			.Max();

		return GetColumnNameFromIndex(maxColumnIndex);
	}

	private static string GetColumnNameFromIndex(int index)
	{
		var letters = new Stack<char>();

		while (index > 0)
		{
			var modulo = (index - 1) % 26;
			letters.Push(Convert.ToChar('A' + modulo));
			index = (index - modulo) / 26;
		}

		return letters.Count == 0 ? "A" : new string([.. letters]);
	}

	private static string? ExtractFormulaType(string formula)
	{
		var match = FormulaTypeRegex().Match(formula);
		return match.Success ? match.Groups[1].Value.ToUpperInvariant() : null;
	}

	private static List<string> ExtractReferencedCells(string formula)
		=> [.. CellReferenceRegex().Matches(formula).Select(match => match.Value)];

	private static string SerializeTableAsMarkdown(List<string> headers, List<List<string>> rows)
	{
		var sb = new StringBuilder();

		if (headers.Count > 0)
		{
			AppendMarkdownRow(sb, headers);
			AppendMarkdownRow(sb, headers.Select(_ => "---"));
		}

		foreach (var row in rows)
		{
			AppendMarkdownRow(sb, row);
		}

		return sb.ToString();
	}

	private static void AppendMarkdownRow(StringBuilder sb, IEnumerable<string> cells)
		=> sb.Append("| ").AppendJoin(" | ", cells).AppendLine(" |");

	private ChunkQualityMetrics CalculateQualityMetrics(string text) => new()
	{
		TokenCount = _tokenCounter.CountTokens(text),
		CharacterCount = text.Length,
		WordCount = text.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length,
		SemanticCompleteness = 1.0
	};

	[GeneratedRegex(@"[A-Z]+", RegexOptions.None)]
	private static partial Regex ColumnNameRegex();

	[GeneratedRegex(@"^=?([A-Z]+)\(", RegexOptions.None)]
	private static partial Regex FormulaTypeRegex();

	[GeneratedRegex(@"[A-Z]+\d+(?::[A-Z]+\d+)?", RegexOptions.None)]
	private static partial Regex CellReferenceRegex();
}
