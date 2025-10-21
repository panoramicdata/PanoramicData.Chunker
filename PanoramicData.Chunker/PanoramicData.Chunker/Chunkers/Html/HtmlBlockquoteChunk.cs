using PanoramicData.Chunker.Models;

namespace PanoramicData.Chunker.Chunkers.Html;

/// <summary>
/// Represents an HTML blockquote element (&lt;blockquote&gt;).
/// </summary>
public class HtmlBlockquoteChunk : ContentChunk
{
	/// <summary>
	/// Gets or sets the cite attribute (URL) of the blockquote, if present.
	/// </summary>
	public string? CiteUrl { get; set; }

	/// <summary>
	/// Gets or sets the CSS class names applied to this blockquote.
	/// </summary>
	public List<string> CssClasses { get; set; } = [];

	/// <summary>
	/// Gets or sets the ID attribute of this blockquote element.
	/// </summary>
	public string? ElementId { get; set; }
}
