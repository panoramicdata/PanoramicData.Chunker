namespace PanoramicData.Chunker.Models;

/// <summary>
/// Content annotation for rich text formatting.
/// </summary>
public class ContentAnnotation
{
	/// <summary>
	/// Starting character index in the Content string.
	/// </summary>
	public int StartIndex { get; set; }

	/// <summary>
	/// Length of the annotated text.
	/// </summary>
	public int Length { get; set; }

	/// <summary>
	/// Type of annotation.
	/// </summary>
	public AnnotationType Type { get; set; }

	/// <summary>
	/// Additional attributes (e.g., href for links, language for code).
	/// </summary>
	public Dictionary<string, string>? Attributes { get; set; }
}
