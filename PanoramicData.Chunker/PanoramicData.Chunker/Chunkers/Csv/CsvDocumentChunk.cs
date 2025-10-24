using PanoramicData.Chunker.Models;

namespace PanoramicData.Chunker.Chunkers.Csv;

/// <summary>
/// Represents a CSV document as a structural container.
/// </summary>
public class CsvDocumentChunk : StructuralChunk
{
	/// <summary>
	/// Gets or sets the detected delimiter character.
	/// </summary>
	public char Delimiter { get; set; }

	/// <summary>
	/// Gets or sets the total number of rows (including header).
	/// </summary>
	public int TotalRows { get; set; }

	/// <summary>
	/// Gets or sets the number of columns.
	/// </summary>
	public int ColumnCount { get; set; }

	/// <summary>
	/// Gets or sets whether a header row was detected.
	/// </summary>
	public bool HasHeaderRow { get; set; }

	/// <summary>
	/// Gets or sets the header column names.
	/// </summary>
	public List<string> Headers { get; set; } = [];

	/// <summary>
	/// Gets or sets the file encoding detected.
	/// </summary>
	public string? Encoding { get; set; }
}
