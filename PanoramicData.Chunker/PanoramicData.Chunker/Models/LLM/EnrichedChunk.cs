namespace PanoramicData.Chunker.Models.Llm;

/// <summary>
/// Represents an enriched chunk with LLM-generated metadata.
/// </summary>
public record EnrichedChunk
{
	/// <summary>
	/// ID of the original chunk.
	/// </summary>
	public required Guid ChunkId { get; init; }

	/// <summary>
	/// The original content of the chunk.
	/// </summary>
	public required string OriginalContent { get; init; }

	/// <summary>
	/// LLM-generated summary of the chunk.
	/// </summary>
	public string? Summary { get; init; }

	/// <summary>
	/// Extracted keywords from the chunk.
	/// </summary>
	public List<string> Keywords { get; init; } = [];

	/// <summary>
	/// Preliminary named entities extracted from the chunk.
	/// </summary>
	public List<PreliminaryEntity> PreliminaryEntities { get; init; } = [];

	/// <summary>
	/// Total tokens used for enrichment.
	/// </summary>
	public int TokensUsed { get; init; }

	/// <summary>
	/// Time taken to enrich the chunk.
	/// </summary>
	public TimeSpan EnrichmentDuration { get; init; }

	/// <summary>
	/// Timestamp when enrichment was performed.
	/// </summary>
	public DateTimeOffset EnrichedAt { get; init; } = DateTimeOffset.UtcNow;
}
