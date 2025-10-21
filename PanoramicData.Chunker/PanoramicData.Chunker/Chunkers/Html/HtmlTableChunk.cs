using PanoramicData.Chunker.Models;

namespace PanoramicData.Chunker.Chunkers.Html;

/// <summary>
/// Represents an HTML table (&lt;table&gt;) extracted as a chunk.
/// </summary>
public class HtmlTableChunk : TableChunk
{
	/// <summary>
	/// Gets or sets the CSS class names applied to this table.
	/// </summary>
	public List<string> CssClasses { get; set; } = [];

	/// <summary>
	/// Gets or sets the ID attribute of this table element.
	/// </summary>
	public string? ElementId { get; set; }

	/// <summary>
	/// Gets or sets the caption text from the &lt;caption&gt; element, if present.
	/// </summary>
	public string? Caption { get; set; }

	/// <summary>
	/// Gets or sets the summary attribute or aria-label of the table, if present.
	/// </summary>
	public string? Summary { get; set; }
}
