using PanoramicData.Chunker.Models;
using PanoramicData.Chunker.Models.KnowledgeGraph;

namespace PanoramicData.Chunker.Interfaces.KnowledgeGraph;

/// <summary>
/// Interface for extracting entities from document chunks.
/// </summary>
public interface IEntityExtractor
{
	/// <summary>
	/// Gets the name of this extractor.
	/// </summary>
	string Name { get; }

	/// <summary>
	/// Gets the version of this extractor.
	/// </summary>
	string Version { get; }

	/// <summary>
	/// Gets the entity types that this extractor can identify.
	/// </summary>
	IReadOnlyList<EntityType> SupportedEntityTypes { get; }

	/// <summary>
	/// Extracts entities from the given chunks.
	/// </summary>
	/// <param name="chunks">The chunks to process.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>List of extracted entities.</returns>
	Task<List<Entity>> ExtractEntitiesAsync(
		IEnumerable<ChunkerBase> chunks,
		CancellationToken cancellationToken = default);

	/// <summary>
	/// Extracts entities from a single chunk.
	/// </summary>
	/// <param name="chunk">The chunk to process.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>List of extracted entities.</returns>
	Task<List<Entity>> ExtractEntitiesAsync(
		ChunkerBase chunk,
		CancellationToken cancellationToken = default);
}
