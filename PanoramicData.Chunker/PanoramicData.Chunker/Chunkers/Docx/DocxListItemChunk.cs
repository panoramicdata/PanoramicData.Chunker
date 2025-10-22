using PanoramicData.Chunker.Models;

namespace PanoramicData.Chunker.Chunkers.Docx;

/// <summary>
/// Represents a DOCX list item chunk.
/// Corresponds to paragraphs with list numbering or bullets applied.
/// </summary>
public class DocxListItemChunk : ContentChunk
{
	/// <summary>
	/// Whether this is a numbered list (true) or bullet list (false).
	/// </summary>
	public bool IsNumbered { get; set; }

	/// <summary>
	/// The list level (nesting depth, 0-based).
	/// </summary>
	public int ListLevel { get; set; }

	/// <summary>
	/// The numbering or bullet character (e.g., "1.", "a)", "•").
	/// </summary>
	public string? NumberingText { get; set; }

	/// <summary>
	/// The numbering format (e.g., "decimal", "bullet", "lowerLetter").
	/// </summary>
	public string? NumberingFormat { get; set; }

	/// <summary>
	/// The paragraph style name if any.
	/// </summary>
	public string? StyleName { get; set; }
}
