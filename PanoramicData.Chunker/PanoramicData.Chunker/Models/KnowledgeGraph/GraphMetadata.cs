namespace PanoramicData.Chunker.Models.KnowledgeGraph;

/// <summary>
/// Metadata about a knowledge graph.
/// </summary>
public class GraphMetadata
{
	/// <summary>
	/// Gets or sets the timestamp when this graph was created.
	/// </summary>
	public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

	/// <summary>
	/// Gets or sets the timestamp of the last update to this graph.
	/// </summary>
	public DateTimeOffset? LastUpdatedAt { get; set; }

	/// <summary>
	/// Gets or sets the version of this graph.
	/// </summary>
	public string Version { get; set; } = "1.0";

	/// <summary>
	/// Gets or sets the creator or author of this graph.
	/// </summary>
	public string? Creator { get; set; }

	/// <summary>
	/// Gets or sets a description of this graph's purpose or content.
	/// </summary>
	public string? Description { get; set; }

	/// <summary>
	/// Gets or sets the source documents from which this graph was extracted.
	/// </summary>
	public List<string> SourceDocuments { get; set; } = [];

	/// <summary>
	/// Gets or sets tags for categorizing this graph.
	/// </summary>
	public List<string> Tags { get; set; } = [];

	/// <summary>
	/// Gets or sets custom metadata properties.
	/// </summary>
	public Dictionary<string, object> Properties { get; set; } = [];
}
