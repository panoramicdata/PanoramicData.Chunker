using PanoramicData.Chunker.Models;

namespace PanoramicData.Chunker.Chunkers.Markdown;

/// <summary>
/// Represents a Markdown table chunk with Markdown-specific metadata.
/// </summary>
public class MarkdownTableChunk : TableChunk
{
	/// <summary>
	/// Table alignment information for each column.
	/// </summary>
	public List<TableColumnAlignment> ColumnAlignments { get; set; } = [];

	/// <summary>
	/// Initializes a new instance of the <see cref="MarkdownTableChunk"/> class.
	/// </summary>
	public MarkdownTableChunk()
	{
		SpecificType = "Table";
		SerializationFormat = TableSerializationFormat.Markdown;
	}
}

/// <summary>
/// Table column alignment options.
/// </summary>
public enum TableColumnAlignment
{
	/// <summary>
	/// No specific alignment (default).
	/// </summary>
	None,

	/// <summary>
	/// Left-aligned column.
	/// </summary>
	Left,

	/// <summary>
	/// Center-aligned column.
	/// </summary>
	Center,

	/// <summary>
	/// Right-aligned column.
	/// </summary>
	Right
}
