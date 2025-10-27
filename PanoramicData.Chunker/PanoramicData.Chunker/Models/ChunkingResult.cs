using PanoramicData.Chunker.Models.Llm;

namespace PanoramicData.Chunker.Models;

/// <summary>
/// Result of a chunking operation.
/// </summary>
public class ChunkingResult
{
	/// <summary>
	/// The generated chunks.
	/// </summary>
	public IReadOnlyList<ChunkerBase> Chunks { get; set; } = [];

	/// <summary>
	/// Enriched chunks with LLM-generated metadata (if enrichment was enabled).
	/// </summary>
	public IReadOnlyList<EnrichedChunk>? EnrichedChunks { get; set; }

	/// <summary>
	/// Statistical information about the chunking process.
	/// </summary>
	public ChunkingStatistics Statistics { get; set; } = new();

	/// <summary>
	/// Warnings or issues encountered during chunking.
	/// </summary>
	public List<ChunkingWarning> Warnings { get; set; } = [];

	/// <summary>
	/// Validation result for the chunking operation.
	/// </summary>
	public ValidationResult? ValidationResult { get; set; }

	/// <summary>
	/// Indicates if the chunking operation completed successfully.
	/// </summary>
	public bool Success { get; set; } = true;
}
