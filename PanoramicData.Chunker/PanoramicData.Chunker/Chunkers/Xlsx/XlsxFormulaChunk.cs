using PanoramicData.Chunker.Models;

namespace PanoramicData.Chunker.Chunkers.Xlsx;

/// <summary>
/// Represents a formula cell in an Excel spreadsheet.
/// Contains formula text and calculated value.
/// </summary>
public class XlsxFormulaChunk : ContentChunk
{
	/// <summary>
	/// Gets or sets the cell reference (e.g., "D2").
	/// </summary>
	public string CellReference { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets the worksheet name.
	/// </summary>
	public string SheetName { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets the formula text (e.g., "=SUM(A1:A10)").
	/// </summary>
	public string Formula { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets the calculated value (if available).
	/// </summary>
	public string? CalculatedValue { get; set; }

	/// <summary>
	/// Gets or sets the formula type (e.g., SUM, AVERAGE, etc.).
	/// </summary>
	public string? FormulaType { get; set; }

	/// <summary>
	/// Gets or sets the cells referenced by this formula.
	/// </summary>
	public List<string> ReferencedCells { get; set; } = [];

	/// <summary>
	/// Gets or sets whether this is an array formula.
	/// </summary>
	public bool IsArrayFormula { get; set; }
}
