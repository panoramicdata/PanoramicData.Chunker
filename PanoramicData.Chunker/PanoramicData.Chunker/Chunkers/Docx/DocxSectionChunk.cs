using PanoramicData.Chunker.Models;

namespace PanoramicData.Chunker.Chunkers.Docx;

/// <summary>
/// Represents a DOCX section chunk (heading-based structural element).
/// Corresponds to paragraphs with heading styles (Heading1-Heading6).
/// </summary>
public class DocxSectionChunk : StructuralChunk
{
	/// <summary>
	/// The heading level (1-6) corresponding to Heading1-Heading6 styles.
	/// </summary>
	public int HeadingLevel { get; set; }

	/// <summary>
	/// The paragraph style name from the DOCX document (e.g., "Heading1", "Title").
	/// </summary>
	public string? StyleName { get; set; }

	/// <summary>
	/// The text content of the heading.
	/// </summary>
	public string Content { get; set; } = string.Empty;

	/// <summary>
	/// Whether this heading has numbering applied.
	/// </summary>
	public bool HasNumbering { get; set; }

	/// <summary>
	/// The numbering text if present (e.g., "1.1", "a)", "i.").
	/// </summary>
	public string? NumberingText { get; set; }
}
