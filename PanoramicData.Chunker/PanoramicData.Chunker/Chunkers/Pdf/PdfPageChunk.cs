using PanoramicData.Chunker.Models;

namespace PanoramicData.Chunker.Chunkers.Pdf;

/// <summary>
/// Represents a single page in a PDF document.
/// </summary>
public class PdfPageChunk : StructuralChunk
{
	/// <summary>
	/// Gets or sets the page number (1-based).
	/// </summary>
	public int PageNumber { get; set; }

	/// <summary>
	/// Gets or sets the page width in points.
	/// </summary>
	public double Width { get; set; }

	/// <summary>
	/// Gets or sets the page height in points.
	/// </summary>
	public double Height { get; set; }

	/// <summary>
	/// Gets or sets the page rotation (0, 90, 180, 270 degrees).
	/// </summary>
	public int Rotation { get; set; }

	/// <summary>
	/// Gets or sets the extracted text from the page.
	/// </summary>
	public string? ExtractedText { get; set; }

	/// <summary>
	/// Gets or sets the number of words on the page.
	/// </summary>
	public int WordCount { get; set; }
}
