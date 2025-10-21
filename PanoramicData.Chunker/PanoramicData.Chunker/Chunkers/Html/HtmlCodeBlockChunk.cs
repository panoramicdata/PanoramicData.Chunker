using PanoramicData.Chunker.Models;

namespace PanoramicData.Chunker.Chunkers.Html;

/// <summary>
/// Represents an HTML code block (&lt;pre&gt;&lt;code&gt; or &lt;pre&gt;).
/// </summary>
public class HtmlCodeBlockChunk : ContentChunk
{
	/// <summary>
	/// Gets or sets the programming language of the code block, if specified (e.g., from class="language-csharp").
	/// </summary>
	public string? Language { get; set; }

	/// <summary>
	/// Gets or sets the CSS class names applied to this code block.
	/// </summary>
	public List<string> CssClasses { get; set; } = [];

	/// <summary>
	/// Gets or sets the ID attribute of this code block element.
	/// </summary>
	public string? ElementId { get; set; }

	/// <summary>
	/// Gets or sets whether this code block uses the &lt;pre&gt;&lt;code&gt; pattern (true) or just &lt;pre&gt; (false).
	/// </summary>
	public bool HasCodeElement { get; set; }
}
