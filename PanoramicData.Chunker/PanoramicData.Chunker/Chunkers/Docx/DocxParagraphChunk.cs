using PanoramicData.Chunker.Models;

namespace PanoramicData.Chunker.Chunkers.Docx;

/// <summary>
/// Represents a DOCX paragraph chunk (normal text paragraph).
/// Corresponds to paragraphs with Normal or other non-heading styles.
/// </summary>
public class DocxParagraphChunk : ContentChunk
{
	/// <summary>
	/// The paragraph style name from the DOCX document (e.g., "Normal", "BodyText").
	/// </summary>
	public string? StyleName { get; set; }

	/// <summary>
	/// The alignment of the paragraph (left, center, right, justified).
	/// </summary>
	public string? Alignment { get; set; }

	/// <summary>
	/// Whether this paragraph has formatting applied (bold, italic, etc.).
	/// </summary>
	public bool HasFormatting { get; set; }

	/// <summary>
	/// The indentation level of the paragraph (0 for no indentation).
	/// </summary>
	public int IndentationLevel { get; set; }
}
