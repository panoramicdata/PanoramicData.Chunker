namespace PanoramicData.Chunker.Models;

/// <summary>
/// Base class for all chunk types.
/// </summary>
public abstract class ChunkerBase
{
	/// <summary>
	/// Unique identifier for this specific chunk.
	/// </summary>
	public Guid Id { get; set; } = Guid.NewGuid();

	/// <summary>
	/// Unique identifier for the immediate parent of this chunk.
	/// </summary>
	public Guid? ParentId { get; set; }

	/// <summary>
	/// The document-specific role (e.g., "Heading1", "TableRow", "Image", "Section").
	/// </summary>
	public string SpecificType { get; set; } = string.Empty;

	/// <summary>
	/// Strongly-typed metadata about this chunk.
	/// </summary>
	public ChunkMetadata Metadata { get; set; } = new();

	/// <summary>
	/// The hierarchical depth of this chunk (0 = root level).
	/// </summary>
	public int Depth { get; set; }

	/// <summary>
	/// Sequential order of this chunk in the document (0-based).
	/// </summary>
	public int SequenceNumber { get; set; }

	/// <summary>
	/// Array of ancestor chunk IDs from root to immediate parent.
	/// </summary>
	public Guid[] AncestorIds { get; set; } = [];

	/// <summary>
	/// Quality and size metrics for this chunk.
	/// </summary>
	public ChunkQualityMetrics? QualityMetrics { get; set; }
}
