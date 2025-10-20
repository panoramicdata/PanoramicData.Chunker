namespace PanoramicData.Chunker.Models;

/// <summary>
/// Table serialization formats.
/// </summary>
public enum TableSerializationFormat
{
	/// <summary>
	/// Markdown table format.
	/// </summary>
	Markdown,

	/// <summary>
	/// CSV (Comma-Separated Values) format.
	/// </summary>
	Csv,

	/// <summary>
	/// JSON format.
	/// </summary>
	Json,

	/// <summary>
	/// HTML table format.
	/// </summary>
	Html
}
