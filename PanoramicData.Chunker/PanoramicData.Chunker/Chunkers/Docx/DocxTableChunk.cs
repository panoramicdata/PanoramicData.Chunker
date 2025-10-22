using PanoramicData.Chunker.Models;

namespace PanoramicData.Chunker.Chunkers.Docx;

/// <summary>
/// Represents a DOCX table chunk.
/// Corresponds to Word tables with rows, columns, and optional headers.
/// </summary>
public class DocxTableChunk : TableChunk
{
	/// <summary>
	/// The table style name from the DOCX document (e.g., "TableGrid", "LightShading").
	/// </summary>
	public string? TableStyle { get; set; }

	/// <summary>
	/// The table caption or title if specified.
	/// </summary>
	public string? Caption { get; set; }

	/// <summary>
	/// Whether the table has banded rows (alternating row colors).
	/// </summary>
	public bool HasBandedRows { get; set; }

	/// <summary>
	/// Whether the table has banded columns.
	/// </summary>
	public bool HasBandedColumns { get; set; }

	/// <summary>
	/// The table's position in the document (inline or floating).
	/// </summary>
	public string? Position { get; set; }

	/// <summary>
	/// The table description from alt text if available.
	/// </summary>
	public string? Description { get; set; }
}
