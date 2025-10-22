using PanoramicData.Chunker.Models;

namespace PanoramicData.Chunker.Chunkers.PlainText;

/// <summary>
/// Represents a list item chunk from a plain text document.
/// </summary>
public class PlainTextListItemChunk : ContentChunk
{
	/// <summary>
	/// The type of list (bullet, numbered, alphabetic).
	/// </summary>
	public string ListType { get; set; } = string.Empty;

	/// <summary>
	/// The nesting level of this list item (0-based).
	/// </summary>
	public int NestingLevel { get; set; }

	/// <summary>
	/// The list marker used (e.g., "-", "1.", "a)").
	/// </summary>
	public string Marker { get; set; } = string.Empty;

	/// <summary>
	/// Indicates if this is an ordered (numbered) list.
	/// </summary>
	public bool IsOrdered { get; set; }
}
