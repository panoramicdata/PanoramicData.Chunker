using PanoramicData.Chunker.Models;

namespace PanoramicData.Chunker.Chunkers.Markdown;

/// <summary>
/// Represents a Markdown list item chunk.
/// </summary>
public class MarkdownListItemChunk : ContentChunk
{
	/// <summary>
	/// Indicates if this is an ordered list item.
	/// </summary>
	public bool IsOrdered { get; set; }

	/// <summary>
	/// The list item number (for ordered lists).
	/// </summary>
	public int? ItemNumber { get; set; }

	/// <summary>
	/// Nesting level of the list item (0 = top level).
	/// </summary>
	public int NestingLevel { get; set; }

	/// <summary>
	/// Initializes a new instance of the <see cref="MarkdownListItemChunk"/> class.
	/// </summary>
	public MarkdownListItemChunk()
	{
		SpecificType = "ListItem";
	}
}
