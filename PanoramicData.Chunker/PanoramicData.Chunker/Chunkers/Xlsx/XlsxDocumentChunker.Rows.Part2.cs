using DocumentFormat.OpenXml.Spreadsheet;
using PanoramicData.Chunker.Utilities;
using System.Text.RegularExpressions;

namespace PanoramicData.Chunker.Chunkers.Xlsx;

public partial class XlsxDocumentChunker
{
	private string? GetCellValue(Cell cell)
	{
		if (cell.CellValue == null)
		{
			return cell.InnerText;
		}

		var value = cell.CellValue.Text;
		return cell.DataType?.Value == CellValues.SharedString ? ResolveSharedString(value) ?? value : value;
	}

	private string? ResolveSharedString(string value)
		=> _sharedStringTable != null && int.TryParse(value, out var index)
			? _sharedStringTable.SharedStringTable?.Elements<SharedStringItem>().ElementAtOrDefault(index)?.InnerText
			: null;

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
}
