using PanoramicData.Chunker.Models;

namespace PanoramicData.Chunker.Chunkers.Pdf;

/// <summary>
/// Represents a paragraph or text block in a PDF.
/// </summary>
public class PdfParagraphChunk : ContentChunk
{
	/// <summary>
	/// Gets or sets the page number where this paragraph appears.
	/// </summary>
	public int PageNumber { get; set; }

	/// <summary>
	/// Gets or sets the paragraph index within the page (0-based).
	/// </summary>
	public int ParagraphIndex { get; set; }

	/// <summary>
	/// Gets or sets the Y-position on the page (from top).
	/// </summary>
	public double YPosition { get; set; }

	/// <summary>
	/// Gets or sets the font size (if detected).
	/// </summary>
	public double? FontSize { get; set; }

	/// <summary>
	/// Gets or sets whether this appears to be a heading based on font size.
	/// </summary>
	public bool IsLikelyHeading { get; set; }
}
