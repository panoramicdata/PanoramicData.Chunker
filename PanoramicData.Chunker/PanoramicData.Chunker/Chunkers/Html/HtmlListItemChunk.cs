using PanoramicData.Chunker.Models;

namespace PanoramicData.Chunker.Chunkers.Html;

/// <summary>
/// Represents an HTML list item (&lt;li&gt;) element.
/// </summary>
public class HtmlListItemChunk : ContentChunk
{
	/// <summary>
	/// Gets or sets the type of list this item belongs to ("ul" for unordered, "ol" for ordered).
	/// </summary>
	public string ListType { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets the nesting level of this list item (0 for top-level, 1 for first nested level, etc.).
	/// </summary>
	public int NestingLevel { get; set; }

	/// <summary>
	/// Gets or sets the CSS class names applied to this list item.
	/// </summary>
	public List<string> CssClasses { get; set; } = [];

	/// <summary>
	/// Gets or sets the ID attribute of this list item element.
	/// </summary>
	public string? ElementId { get; set; }
}
