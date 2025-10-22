using PanoramicData.Chunker.Models;

namespace PanoramicData.Chunker.Chunkers.Docx;

/// <summary>
/// Represents a DOCX image chunk.
/// Corresponds to inline or floating images in the document.
/// </summary>
public class DocxImageChunk : VisualChunk
{
	/// <summary>
	/// The image description from alt text.
	/// </summary>
	public string? Description { get; set; }

	/// <summary>
	/// The image title or name.
	/// </summary>
	public string? Title { get; set; }

	/// <summary>
	/// The image's position in the document (inline or floating).
	/// </summary>
	public string? Position { get; set; }

	/// <summary>
	/// The wrapping style for floating images (square, tight, through, etc.).
	/// </summary>
	public string? WrappingStyle { get; set; }

	/// <summary>
	/// The image width in EMUs (English Metric Units) from the document.
	/// </summary>
	public long? WidthEmu { get; set; }

	/// <summary>
	/// The image height in EMUs.
	/// </summary>
	public long? HeightEmu { get; set; }

	/// <summary>
	/// The relationship ID for the image part.
	/// </summary>
	public string? RelationshipId { get; set; }
}
