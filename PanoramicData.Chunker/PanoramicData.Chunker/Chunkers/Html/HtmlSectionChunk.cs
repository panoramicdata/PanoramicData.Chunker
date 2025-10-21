using PanoramicData.Chunker.Models;

namespace PanoramicData.Chunker.Chunkers.Html;

/// <summary>
/// Represents a semantic HTML section element (e.g., &lt;article&gt;, &lt;section&gt;, &lt;main&gt;, &lt;aside&gt;, &lt;header&gt;, &lt;footer&gt;, or heading elements).
/// This chunk type provides hierarchical structure for HTML documents.
/// </summary>
public class HtmlSectionChunk : StructuralChunk
{
	/// <summary>
	/// Gets or sets the HTML tag name that represents this section (e.g., "article", "section", "h1", "h2").
	/// </summary>
	public string TagName { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets the heading level if this section is a heading element (1-6 for H1-H6).
	/// Null if this is not a heading element.
	/// </summary>
	public int? HeadingLevel { get; set; }

	/// <summary>
	/// Gets or sets the CSS class names applied to this section element.
	/// </summary>
	public List<string> CssClasses { get; set; } = [];

	/// <summary>
	/// Gets or sets the ID attribute of this section element.
	/// </summary>
	public string? ElementId { get; set; }

	/// <summary>
	/// Gets or sets the role attribute (ARIA role) of this section element.
	/// </summary>
	public string? Role { get; set; }
}
