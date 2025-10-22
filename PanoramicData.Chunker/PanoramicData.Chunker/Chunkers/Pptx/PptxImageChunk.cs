using PanoramicData.Chunker.Models;

namespace PanoramicData.Chunker.Chunkers.Pptx;

/// <summary>
/// Represents an image or visual element from a PPTX slide.
/// </summary>
public class PptxImageChunk : VisualChunk
{
	/// <summary>
	/// The slide number this image belongs to.
	/// </summary>
	public int SlideNumber { get; set; }

	/// <summary>
	/// The type of visual element (e.g., "image", "chart", "smartArt", "shape").
	/// </summary>
	public string? VisualType { get; set; }

	/// <summary>
	/// The name of the shape containing the visual.
	/// </summary>
	public string? ShapeName { get; set; }

	/// <summary>
	/// Whether this is a chart.
	/// </summary>
	public bool IsChart { get; set; }

	/// <summary>
	/// Chart type (if this is a chart).
	/// </summary>
	public string? ChartType { get; set; }

	/// <summary>
	/// Whether this is SmartArt.
	/// </summary>
	public bool IsSmartArt { get; set; }

	/// <summary>
	/// Position on the slide (x, y coordinates).
	/// </summary>
	public string? Position { get; set; }

	/// <summary>
	/// Whether the image has a hyperlink.
	/// </summary>
	public bool HasHyperlink { get; set; }
}
