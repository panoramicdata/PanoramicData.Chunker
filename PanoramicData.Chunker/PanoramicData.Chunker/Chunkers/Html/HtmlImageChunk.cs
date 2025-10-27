using PanoramicData.Chunker.Models;

namespace PanoramicData.Chunker.Chunkers.Html;

/// <summary>
/// Represents an HTML image (&lt;img&gt;) extracted as a visual chunk.
/// </summary>
public class HtmlImageChunk : VisualChunk
{
	/// <summary>
	/// Gets or sets the CSS class names applied to this image.
	/// </summary>
	public List<string> CssClasses { get; set; } = [];

	/// <summary>
	/// Gets or sets the ID attribute of this image element.
	/// </summary>
	public string? ElementId { get; set; }

	/// <summary>
	/// Gets or sets the title attribute of the image, if present.
	/// </summary>
	public string? Title { get; set; }
}
