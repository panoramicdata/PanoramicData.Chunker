using PanoramicData.Chunker.Models;

namespace PanoramicData.Chunker.Chunkers.Xlsx;

/// <summary>
/// Represents a worksheet in an Excel spreadsheet.
/// Acts as a structural container for rows, tables, and other content.
/// </summary>
public class XlsxWorksheetChunk : StructuralChunk
{
	/// <summary>
	/// Gets or sets the worksheet name.
	/// </summary>
	public string SheetName { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets the worksheet index (0-based).
	/// </summary>
	public int SheetIndex { get; set; }

	/// <summary>
	/// Gets or sets the number of rows with data.
	/// </summary>
	public int RowCount { get; set; }

	/// <summary>
	/// Gets or sets the number of columns with data.
	/// </summary>
	public int ColumnCount { get; set; }

	/// <summary>
	/// Gets or sets whether this worksheet is hidden.
	/// </summary>
	public bool IsHidden { get; set; }

	/// <summary>
	/// Gets or sets the used range (e.g., "A1:D100").
	/// </summary>
	public string? UsedRange { get; set; }
}
