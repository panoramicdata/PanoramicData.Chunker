namespace PanoramicData.Chunker.Models;

/// <summary>
/// Represents small, vector-embeddable units of text (Paragraphs, List Items, Cells).
/// </summary>
public abstract class ContentChunk : ChunkerBase
{
	/// <summary>
	/// The raw text content of the chunk (plain text).
	/// </summary>
	public string Content { get; set; } = string.Empty;

	/// <summary>
	/// HTML representation of the content, preserving formatting.
	/// Populated when ChunkingOptions.PreserveFormatting is enabled.
	/// </summary>
	public string? HtmlContent { get; set; }

	/// <summary>
	/// Markdown representation of the content.
	/// Populated when ChunkingOptions.GenerateMarkdown is enabled.
	/// </summary>
	public string? MarkdownContent { get; set; }

	/// <summary>
	/// Rich text annotations (bold, italic, links, code, etc.).
	/// </summary>
	public List<ContentAnnotation>? Annotations { get; set; }

	/// <summary>
	/// Keywords extracted from this chunk.
	/// Generated when ChunkingOptions.ExtractKeywords is enabled.
	/// </summary>
	public List<string>? Keywords { get; set; }
}
