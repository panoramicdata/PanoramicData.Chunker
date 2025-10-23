using PanoramicData.Chunker.Models;

namespace PanoramicData.Chunker.Chunkers.Xlsx;

/// <summary>
/// Represents an image embedded in an Excel spreadsheet.
/// </summary>
public class XlsxImageChunk : VisualChunk
{
	/// <summary>
	/// Gets or sets the worksheet name containing this image.
	/// </summary>
	public string SheetName { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets the image name or title.
	/// </summary>
	public string? ImageName { get; set; }

	/// <summary>
	/// Gets or sets the image description or alt text.
	/// </summary>
	public string? Description { get; set; }

	/// <summary>
	/// Gets or sets the cell reference where the image is anchored (e.g., "A1").
	/// </summary>
	public string? AnchorCell { get; set; }

	/// <summary>
	/// Gets or sets the image format (e.g., "png", "jpeg").
	/// </summary>
	public string? ImageFormat { get; set; }
}
