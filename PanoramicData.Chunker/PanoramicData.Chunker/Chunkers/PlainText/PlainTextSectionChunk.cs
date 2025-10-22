using PanoramicData.Chunker.Models;

namespace PanoramicData.Chunker.Chunkers.PlainText;

/// <summary>
/// Represents a section chunk from a plain text document with detected heading.
/// </summary>
public class PlainTextSectionChunk : StructuralChunk
{
	/// <summary>
	/// The type of heuristic used to detect this heading.
	/// </summary>
	public HeadingHeuristic HeadingType { get; set; }

	/// <summary>
	/// Confidence score for the heading detection (0.0 - 1.0).
	/// </summary>
	public double Confidence { get; set; }

	/// <summary>
	/// The original heading text.
	/// </summary>
	public string HeadingText { get; set; } = string.Empty;

	/// <summary>
	/// The heading level (1-6).
	/// </summary>
	public int HeadingLevel { get; set; }
}

/// <summary>
/// Heuristic methods for detecting headings in plain text.
/// </summary>
public enum HeadingHeuristic
{
	/// <summary>
	/// Heading detected by ALL CAPS text.
	/// </summary>
	AllCaps,

	/// <summary>
	/// Heading detected by underline (=== or ---).
	/// </summary>
	Underlined,

	/// <summary>
	/// Heading detected by section numbering (1., 1.1, 1.1.1).
	/// </summary>
	Numbered,

	/// <summary>
	/// Heading detected by prefix markers (#, ##, ###).
	/// </summary>
	Prefixed
}
