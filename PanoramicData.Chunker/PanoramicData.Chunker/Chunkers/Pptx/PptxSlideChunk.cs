using PanoramicData.Chunker.Models;

namespace PanoramicData.Chunker.Chunkers.Pptx;

/// <summary>
/// Represents a PPTX slide chunk (structural element).
/// Each slide in a presentation becomes a separate structural chunk.
/// </summary>
public class PptxSlideChunk : StructuralChunk
{
	/// <summary>
	/// The slide number (1-based index).
	/// </summary>
	public int SlideNumber { get; set; }

	/// <summary>
	/// The slide title (if present).
	/// </summary>
	public string? Title { get; set; }

	/// <summary>
	/// The layout name used by this slide.
	/// </summary>
	public string? LayoutName { get; set; }

	/// <summary>
	/// Whether this slide has speaker notes.
	/// </summary>
	public bool HasNotes { get; set; }

	/// <summary>
	/// Number of shapes on this slide.
	/// </summary>
	public int ShapeCount { get; set; }

	/// <summary>
	/// Whether this slide contains animations.
	/// </summary>
	public bool HasAnimations { get; set; }

	/// <summary>
	/// Whether this slide contains transitions.
	/// </summary>
	public bool HasTransitions { get; set; }
}
