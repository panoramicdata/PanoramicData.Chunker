using PanoramicData.Chunker.Models;

namespace PanoramicData.Chunker.Chunkers.PlainText;

/// <summary>
/// Represents a paragraph chunk from a plain text document.
/// </summary>
public class PlainTextParagraphChunk : ContentChunk
{
	/// <summary>
	/// The method used to detect this paragraph.
	/// </summary>
	public ParagraphDetection DetectionMethod { get; set; }
}

/// <summary>
/// Methods for detecting paragraphs in plain text.
/// </summary>
public enum ParagraphDetection
{
	/// <summary>
	/// Paragraph detected by double newline.
	/// </summary>
	DoubleNewline,

	/// <summary>
	/// Paragraph detected by indentation change.
	/// </summary>
	Indentation,

	/// <summary>
	/// Paragraph detected by sentence completion.
	/// </summary>
	SentenceCompletion
}
