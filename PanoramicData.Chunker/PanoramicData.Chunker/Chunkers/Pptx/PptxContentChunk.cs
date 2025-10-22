using PanoramicData.Chunker.Models;

namespace PanoramicData.Chunker.Chunkers.Pptx;

/// <summary>
/// Represents a PPTX content chunk from a slide's text content.
/// This includes content from body placeholders, text boxes, and shapes.
/// </summary>
public class PptxContentChunk : ContentChunk
{
	/// <summary>
	/// The slide number this content belongs to.
	/// </summary>
	public int SlideNumber { get; set; }

	/// <summary>
	/// The type of content source (e.g., "body", "textBox", "shape").
	/// </summary>
	public string? SourceType { get; set; }

	/// <summary>
	/// The placeholder type (if from a placeholder).
	/// </summary>
	public string? PlaceholderType { get; set; }

	/// <summary>
	/// Whether this content is a list item.
	/// </summary>
	public bool IsListItem { get; set; }

	/// <summary>
	/// The list level (0-based) if this is a list item.
	/// </summary>
	public int ListLevel { get; set; }

	/// <summary>
	/// Whether this content has formatting applied.
	/// </summary>
	public bool HasFormatting { get; set; }

	/// <summary>
	/// The shape name (if from a named shape).
	/// </summary>
	public string? ShapeName { get; set; }
}
