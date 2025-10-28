namespace PanoramicData.Chunker.Models.KnowledgeGraph;

/// <summary>
/// Represents a source chunk from which an entity was extracted.
/// </summary>
public class EntitySource
{
	/// <summary>
	/// Gets or sets the unique identifier of the chunk from which this entity was extracted.
	/// </summary>
	public Guid ChunkId { get; set; }

	/// <summary>
	/// Gets or sets the position (character offset) where the entity appears in the chunk.
	/// </summary>
	public int Position { get; set; }

	/// <summary>
	/// Gets or sets the length of the entity mention in characters.
	/// </summary>
	public int Length { get; set; }

	/// <summary>
	/// Gets or sets the surrounding context text (e.g., sentence or paragraph).
	/// </summary>
	public string Context { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets the timestamp when this source was recorded.
	/// </summary>
	public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;

	/// <summary>
	/// Gets or sets the confidence score for this particular mention (0.0 to 1.0).
	/// </summary>
	public double Confidence { get; set; } = 1.0;

	/// <summary>
	/// Gets or sets additional source-specific properties.
	/// </summary>
	public Dictionary<string, object> Properties { get; set; } = [];
}
