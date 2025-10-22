using PanoramicData.Chunker.Models;

namespace PanoramicData.Chunker.Chunkers.Pptx;

/// <summary>
/// Represents a table chunk from a PPTX slide.
/// </summary>
public class PptxTableChunk : TableChunk
{
	/// <summary>
	/// The slide number this table belongs to.
	/// </summary>
	public int SlideNumber { get; set; }

	/// <summary>
	/// The table style name (if available).
	/// </summary>
	public string? TableStyle { get; set; }

	/// <summary>
	/// Whether the table has banded rows.
	/// </summary>
	public bool HasBandedRows { get; set; }

	/// <summary>
	/// Whether the table has banded columns.
	/// </summary>
	public bool HasBandedColumns { get; set; }

	/// <summary>
	/// Whether the first row is styled as a header.
	/// </summary>
	public bool HasFirstRowHeader { get; set; }

	/// <summary>
	/// Whether the first column is styled as a header.
	/// </summary>
	public bool HasFirstColumnHeader { get; set; }
}
