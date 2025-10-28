using PanoramicData.Chunker.Models;
using PanoramicData.Chunker.Models.Llm;

namespace PanoramicData.Chunker.Interfaces;

/// <summary>
/// Interface for enriching chunks with LLM-generated metadata.
/// </summary>
public interface IChunkEnricher
{
	/// <summary>
	/// Enriches a single chunk with LLM-generated metadata.
	/// </summary>
	/// <param name="chunk">The chunk to enrich.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>The enriched chunk.</returns>
	Task<EnrichedChunk> EnrichAsync(
		ChunkerBase chunk,
		CancellationToken cancellationToken);

	/// <summary>
	/// Enriches multiple chunks with LLM-generated metadata.
	/// </summary>
	/// <param name="chunks">The chunks to enrich.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>The enriched chunks.</returns>
	Task<IEnumerable<EnrichedChunk>> EnrichBatchAsync(
		IEnumerable<ChunkerBase> chunks,
		CancellationToken cancellationToken);
}
