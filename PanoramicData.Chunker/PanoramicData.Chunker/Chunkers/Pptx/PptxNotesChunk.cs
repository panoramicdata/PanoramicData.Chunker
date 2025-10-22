using PanoramicData.Chunker.Models;

namespace PanoramicData.Chunker.Chunkers.Pptx;

/// <summary>
/// Represents speaker notes from a PPTX slide.
/// </summary>
public class PptxNotesChunk : ContentChunk
{
	/// <summary>
	/// The slide number these notes belong to.
	/// </summary>
	public int SlideNumber { get; set; }

	/// <summary>
	/// Whether the notes have formatting applied.
	/// </summary>
	public bool HasFormatting { get; set; }

	/// <summary>
	/// The length of the notes in characters.
	/// </summary>
	public int NotesLength { get; set; }
}
