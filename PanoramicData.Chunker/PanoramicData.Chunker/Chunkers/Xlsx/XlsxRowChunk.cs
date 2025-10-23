using PanoramicData.Chunker.Models;

namespace PanoramicData.Chunker.Chunkers.Xlsx;

/// <summary>
/// Represents a single row in an Excel spreadsheet.
/// </summary>
public class XlsxRowChunk : ContentChunk
{
	/// <summary>
	/// Gets or sets the row number (1-based).
	/// </summary>
	public int RowNumber { get; set; }

	/// <summary>
	/// Gets or sets the worksheet name.
	/// </summary>
	public string SheetName { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets the cell values in this row.
	/// </summary>
	public List<XlsxCellData> Cells { get; set; } = [];

	/// <summary>
	/// Gets or sets whether this row is a header row.
	/// </summary>
	public bool IsHeaderRow { get; set; }

	/// <summary>
	/// Gets or sets the row height (if specified).
	/// </summary>
	public double? Height { get; set; }

	/// <summary>
	/// Gets or sets whether this row is hidden.
	/// </summary>
	public bool IsHidden { get; set; }
}

/// <summary>
/// Represents data in a single cell.
/// </summary>
public class XlsxCellData
{
	/// <summary>
	/// Gets or sets the cell reference (e.g., "A1").
	/// </summary>
	public string CellReference { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets the column name (e.g., "A").
	/// </summary>
	public string ColumnName { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets the cell value as text.
	/// </summary>
	public string Value { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets the cell data type.
	/// </summary>
	public XlsxCellType CellType { get; set; }

	/// <summary>
	/// Gets or sets the formula (if any).
	/// </summary>
	public string? Formula { get; set; }

	/// <summary>
	/// Gets or sets the number format (if any).
	/// </summary>
	public string? NumberFormat { get; set; }

	/// <summary>
	/// Gets or sets hyperlink URL (if any).
	/// </summary>
	public string? Hyperlink { get; set; }
}

/// <summary>
/// Excel cell data types.
/// </summary>
public enum XlsxCellType
{
	/// <summary>Text/string value.</summary>
	Text,

	/// <summary>Numeric value.</summary>
	Number,

	/// <summary>Date/time value.</summary>
	Date,

	/// <summary>Boolean value.</summary>
	Boolean,

	/// <summary>Formula.</summary>
	Formula,

	/// <summary>Empty cell.</summary>
	Empty,

	/// <summary>Error value.</summary>
	Error
}
