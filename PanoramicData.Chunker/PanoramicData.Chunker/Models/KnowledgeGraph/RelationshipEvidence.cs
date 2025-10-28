namespace PanoramicData.Chunker.Models.KnowledgeGraph;

/// <summary>
/// Evidence supporting a relationship from source chunks.
/// </summary>
public class RelationshipEvidence
{
	/// <summary>
	/// Gets or sets the unique identifier of the chunk containing this evidence.
	/// </summary>
	public Guid ChunkId { get; set; }

	/// <summary>
	/// Gets or sets the context text that supports this relationship.
	/// Typically a sentence or paragraph containing both entities.
	/// </summary>
	public string Context { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets the confidence score for this evidence (0.0 to 1.0).
	/// </summary>
	public double Confidence { get; set; } = 1.0;

	/// <summary>
	/// Gets or sets the timestamp when this evidence was recorded.
	/// </summary>
	public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;

	/// <summary>
	/// Gets or sets the pattern or rule that identified this relationship.
	/// </summary>
	public string? Pattern { get; set; }

	/// <summary>
	/// Gets or sets the distance between entities in the text (e.g., word count, character count).
	/// </summary>
	public int? Distance { get; set; }

	/// <summary>
	/// Gets or sets additional evidence-specific properties.
	/// </summary>
	public Dictionary<string, object> Properties { get; set; } = [];
}
