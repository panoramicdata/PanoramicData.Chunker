namespace PanoramicData.Chunker.Models.KnowledgeGraph;

/// <summary>
/// Metadata about an entity's extraction and characteristics.
/// </summary>
public class EntityMetadata
{
	/// <summary>
	/// Gets or sets the timestamp when this entity was first extracted.
	/// </summary>
	public DateTimeOffset ExtractedAt { get; set; } = DateTimeOffset.UtcNow;

	/// <summary>
	/// Gets or sets the name of the extractor that identified this entity.
	/// </summary>
	public string ExtractorName { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets the version of the extractor.
	/// </summary>
	public string ExtractorVersion { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets the model name (if extracted using an LLM or ML model).
	/// </summary>
	public string? ModelName { get; set; }

	/// <summary>
	/// Gets or sets additional extraction details or debugging information.
	/// </summary>
	public Dictionary<string, object> ExtractionDetails { get; set; } = [];

	/// <summary>
	/// Gets or sets tags associated with this entity.
	/// </summary>
	public List<string> Tags { get; set; } = [];

	/// <summary>
	/// Gets or sets whether this entity was manually verified or corrected.
	/// </summary>
	public bool ManuallyVerified { get; set; }

	/// <summary>
	/// Gets or sets the timestamp of the last update to this entity.
	/// </summary>
	public DateTimeOffset? LastUpdatedAt { get; set; }

	/// <summary>
	/// Gets or sets notes or comments about this entity.
	/// </summary>
	public string? Notes { get; set; }
}
