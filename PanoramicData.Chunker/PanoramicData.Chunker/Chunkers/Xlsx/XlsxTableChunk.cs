using PanoramicData.Chunker.Models;

namespace PanoramicData.Chunker.Chunkers.Xlsx;

/// <summary>
/// Represents a table in an Excel spreadsheet.
/// Contains structured data with headers and rows.
/// </summary>
public class XlsxTableChunk : TableChunk
{
	/// <summary>
	/// Gets or sets the table name (if defined).
	/// </summary>
	public string? TableName { get; set; }

	/// <summary>
	/// Gets or sets the worksheet name containing this table.
	/// </summary>
	public string SheetName { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets the table range (e.g., "A1:D10").
	/// </summary>
	public string Range { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets whether this is an Excel Table object or just a data range.
	/// </summary>
	public bool IsFormattedTable { get; set; }

	/// <summary>
	/// Gets or sets the table style name (if any).
	/// </summary>
	public string? StyleName { get; set; }
}
