namespace PanoramicData.Chunker.Models;

/// <summary>
/// Table metadata with structure information.
/// </summary>
public class TableMetadata
{
	/// <summary>
	/// Number of rows in the table.
	/// </summary>
	public int RowCount { get; set; }

	/// <summary>
	/// Number of columns in the table.
	/// </summary>
	public int ColumnCount { get; set; }

	/// <summary>
	/// Table header column names.
	/// </summary>
	public string[] Headers { get; set; } = [];

	/// <summary>
	/// Indicates if the table has merged cells.
	/// </summary>
	public bool HasMergedCells { get; set; }

	/// <summary>
	/// Indicates if the table has a header row.
	/// </summary>
	public bool HasHeaderRow { get; set; }

	/// <summary>
	/// Preferred serialization format for this table.
	/// </summary>
	public TableSerializationFormat PreferredFormat { get; set; }
}
