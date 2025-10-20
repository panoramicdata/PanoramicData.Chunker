using PanoramicData.Chunker.Models;

namespace PanoramicData.Chunker.Chunkers.Markdown;

/// <summary>
/// Represents a Markdown image chunk.
/// </summary>
public class MarkdownImageChunk : VisualChunk
{
	/// <summary>
	/// The image URL or path from the Markdown syntax.
	/// </summary>
	public string ImageUrl { get; set; } = string.Empty;

	/// <summary>
	/// Alt text from the Markdown syntax.
	/// </summary>
	public string AltText { get; set; } = string.Empty;

	/// <summary>
	/// Optional title attribute.
	/// </summary>
	public string? Title { get; set; }

	/// <summary>
	/// Indicates if this is an inline image or reference-style image.
	/// </summary>
	public bool IsReferenceStyle { get; set; }

	/// <summary>
	/// Initializes a new instance of the <see cref="MarkdownImageChunk"/> class.
	/// </summary>
	public MarkdownImageChunk()
	{
		SpecificType = "Image";
		MimeType = "image/unknown"; // Will be determined from URL extension
		BinaryReference = string.Empty; // Will be set during processing
	}
}
