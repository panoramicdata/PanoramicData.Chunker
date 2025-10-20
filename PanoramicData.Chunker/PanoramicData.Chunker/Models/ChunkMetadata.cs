namespace PanoramicData.Chunker.Models;

/// <summary>
/// Strongly-typed metadata for chunks.
/// </summary>
public class ChunkMetadata
{
	/// <summary>
	/// Document type (DOCX, PPTX, XLSX, HTML, PDF, etc.).
	/// </summary>
	public string DocumentType { get; set; } = string.Empty;

	/// <summary>
	/// Original path/name of the file.
	/// </summary>
	public string? FilePath { get; set; }

	/// <summary>
	/// Unique document identifier (e.g., SHA256 hash or internal ID).
	/// </summary>
	public string SourceId { get; set; } = string.Empty;

	/// <summary>
	/// Internal structural path (e.g., "Section 1.2 > Introduction").
	/// </summary>
	public string Hierarchy { get; set; } = string.Empty;

	/// <summary>
	/// External context path (e.g., "Folder/Subfolder/ProjectX" or "https://example.com/docs/api").
	/// </summary>
	public string? ExternalHierarchy { get; set; }

	/// <summary>
	/// Page number (if applicable).
	/// </summary>
	public int? PageNumber { get; set; }

	/// <summary>
	/// Sheet name (for XLSX documents).
	/// </summary>
	public string? SheetName { get; set; }

	/// <summary>
	/// Classification tags for the chunk.
	/// </summary>
	public List<string> Tags { get; set; } = [];

	/// <summary>
	/// Bounding box coordinates (PDF-specific): "x,y,width,height".
	/// </summary>
	public string? BoundingBox { get; set; }

	/// <summary>
	/// Indicates if this chunk's content block contains an image (PDF-specific).
	/// </summary>
	public bool HasImage { get; set; }

	/// <summary>
	/// Language code (ISO 639-1) detected or specified for this chunk.
	/// </summary>
	public string? Language { get; set; }

	/// <summary>
	/// Timestamp when the chunk was created.
	/// </summary>
	public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

	/// <summary>
	/// Custom metadata key-value pairs.
	/// </summary>
	public Dictionary<string, object>? CustomMetadata { get; set; }
}
