using PanoramicData.Chunker.Models;

namespace PanoramicData.Chunker.Chunkers.Pdf;

/// <summary>
/// Represents a PDF document as a structural container.
/// </summary>
public class PdfDocumentChunk : StructuralChunk
{
	/// <summary>
	/// Gets or sets the PDF version (e.g., "1.7").
	/// </summary>
	public string? PdfVersion { get; set; }

	/// <summary>
	/// Gets or sets the total number of pages.
	/// </summary>
	public int PageCount { get; set; }

	/// <summary>
	/// Gets or sets the document title from metadata.
	/// </summary>
	public string? Title { get; set; }

	/// <summary>
	/// Gets or sets the document author from metadata.
	/// </summary>
	public string? Author { get; set; }

	/// <summary>
	/// Gets or sets the document subject from metadata.
	/// </summary>
	public string? Subject { get; set; }

	/// <summary>
	/// Gets or sets the document creation date.
	/// </summary>
	public DateTime? CreationDate { get; set; }

	/// <summary>
	/// Gets or sets the document modification date.
	/// </summary>
	public DateTime? ModificationDate { get; set; }

	/// <summary>
	/// Gets or sets whether the PDF is encrypted.
	/// </summary>
	public bool IsEncrypted { get; set; }
}
