using PanoramicData.Chunker.Models;

namespace PanoramicData.Chunker.Chunkers.Pptx;

/// <summary>
/// Represents a PPTX slide title chunk.
/// </summary>
public class PptxTitleChunk : ContentChunk
{
	/// <summary>
	/// The slide number this title belongs to.
	/// </summary>
	public int SlideNumber { get; set; }

	/// <summary>
	/// Whether this is the main title or a subtitle.
	/// </summary>
	public bool IsSubtitle { get; set; }

	/// <summary>
	/// The placeholder type (e.g., "title", "ctrTitle").
	/// </summary>
	public string? PlaceholderType { get; set; }

	/// <summary>
	/// Font size in points (if available).
	/// </summary>
	public double? FontSize { get; set; }

	/// <summary>
	/// Whether the title has formatting applied.
	/// </summary>
	public bool HasFormatting { get; set; }
}
