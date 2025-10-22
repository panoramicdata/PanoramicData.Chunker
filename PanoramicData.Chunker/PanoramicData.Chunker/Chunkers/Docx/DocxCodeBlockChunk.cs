using PanoramicData.Chunker.Models;

namespace PanoramicData.Chunker.Chunkers.Docx;

/// <summary>
/// Represents a DOCX code block chunk.
/// Corresponds to paragraphs with code or preformatted text styles.
/// </summary>
public class DocxCodeBlockChunk : ContentChunk
{
	/// <summary>
	/// The programming language if detected (optional).
	/// </summary>
	public string? Language { get; set; }

	/// <summary>
	/// The paragraph style name (e.g., "Code", "HTMLPreformatted").
	/// </summary>
	public string? StyleName { get; set; }

	/// <summary>
	/// Whether this code block uses a monospace font.
	/// </summary>
	public bool IsMonospace { get; set; }

	/// <summary>
	/// Whether syntax highlighting information is available.
	/// </summary>
	public bool HasSyntaxHighlighting { get; set; }
}
