namespace PanoramicData.Chunker.Models;

/// <summary>
/// Specialized chunk type for tables with enhanced metadata.
/// </summary>
public class TableChunk : ContentChunk
{
	/// <summary>
	/// Detailed metadata about the table structure.
	/// </summary>
	public TableMetadata TableInfo { get; set; } = new();

	/// <summary>
	/// Serialized representation of the table in the preferred format.
	/// </summary>
	public string SerializedTable { get; set; } = string.Empty;

	/// <summary>
	/// Format of the serialized table (Markdown, CSV, JSON).
	/// </summary>
	public TableSerializationFormat SerializationFormat { get; set; }
}
