namespace PanoramicData.Chunker.Interfaces;

/// <summary>
/// Interface for generating descriptions of visual content.
/// </summary>
public interface IImageDescriptionProvider
{
	/// <summary>
	/// Generate a description of the image.
	/// </summary>
	/// <param name="imageData">The image binary data.</param>
	/// <param name="mimeType">The MIME type of the image.</param>
	/// <param name="existingCaption">Existing caption or alt-text if available.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>Image description information.</returns>
	Task<ImageDescription> GenerateDescriptionAsync(
		byte[] imageData,
		string mimeType,
		string? existingCaption,
		CancellationToken cancellationToken);
}

/// <summary>
/// Image description result.
/// </summary>
public class ImageDescription
{
	/// <summary>
	/// Generated description of the image.
	/// </summary>
	public string Description { get; set; } = string.Empty;

	/// <summary>
	/// Confidence score (0-1).
	/// </summary>
	public double Confidence { get; set; }

	/// <summary>
	/// Detected objects in the image.
	/// </summary>
	public List<string>? DetectedObjects { get; set; }

	/// <summary>
	/// Detected text (OCR results).
	/// </summary>
	public List<string>? DetectedText { get; set; }
}
