using PanoramicData.Chunker.Models;

namespace PanoramicData.Chunker.Chunkers.Markdown;

/// <summary>
/// Represents a Markdown section (header-based structural element).
/// </summary>
public class MarkdownSectionChunk : StructuralChunk
{
	/// <summary>
	/// Heading level (1-6 for H1-H6).
	/// </summary>
	public int HeadingLevel { get; set; }

	/// <summary>
	/// The heading text content.
	/// </summary>
	public string HeadingText { get; set; } = string.Empty;

	/// <summary>
	/// Original Markdown syntax for this heading.
	/// </summary>
	public string MarkdownSyntax { get; set; } = string.Empty;
}
