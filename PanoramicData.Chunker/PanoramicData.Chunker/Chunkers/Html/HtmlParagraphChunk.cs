using PanoramicData.Chunker.Models;

namespace PanoramicData.Chunker.Chunkers.Html;

/// <summary>
/// Represents an HTML paragraph element (&lt;p&gt;).
/// </summary>
public class HtmlParagraphChunk : ContentChunk
{
	/// <summary>
	/// Gets or sets the CSS class names applied to this paragraph.
	/// </summary>
	public List<string> CssClasses { get; set; } = [];

	/// <summary>
	/// Gets or sets the ID attribute of this paragraph element.
	/// </summary>
	public string? ElementId { get; set; }
}
