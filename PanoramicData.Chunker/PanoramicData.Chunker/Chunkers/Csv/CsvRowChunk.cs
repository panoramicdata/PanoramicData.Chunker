using PanoramicData.Chunker.Models;

namespace PanoramicData.Chunker.Chunkers.Csv;

/// <summary>
/// Represents a single row in a CSV file with its associated header context.
/// </summary>
public class CsvRowChunk : TableChunk
{
	/// <summary>
	/// Gets or sets the row number (1-based, excluding header).
	/// </summary>
	public int RowNumber { get; set; }

	/// <summary>
	/// Gets or sets the raw CSV row text.
	/// </summary>
	public string RawRow { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets the parsed field values.
	/// </summary>
	public List<string> Fields { get; set; } = [];

	/// <summary>
	/// Gets or sets the header names for this row.
	/// </summary>
	public List<string> HeaderNames { get; set; } = [];

	/// <summary>
	/// Gets or sets whether this row contains quoted fields.
	/// </summary>
	public bool HasQuotedFields { get; set; }

	/// <summary>
	/// Gets or sets the delimiter used in this row.
	/// </summary>
	public char Delimiter { get; set; }
}
