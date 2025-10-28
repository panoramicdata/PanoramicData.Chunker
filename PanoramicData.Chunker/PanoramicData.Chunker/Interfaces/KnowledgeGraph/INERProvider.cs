using PanoramicData.Chunker.Models.KnowledgeGraph;

namespace PanoramicData.Chunker.Interfaces.KnowledgeGraph;

/// <summary>
/// Interface for Named Entity Recognition (NER) providers.
/// This will be fully implemented in Phase 12.
/// </summary>
public interface INERProvider
{
	/// <summary>
	/// Gets the name of this NER provider.
	/// </summary>
	string Name { get; }

	/// <summary>
	/// Gets the version of this NER provider.
	/// </summary>
	string Version { get; }

	/// <summary>
	/// Gets the entity types that this provider can recognize.
	/// </summary>
	IReadOnlyList<EntityType> SupportedEntityTypes { get; }

	/// <summary>
	/// Extracts named entities from text.
	/// </summary>
	/// <param name="text">The text to analyze.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>List of extracted entities.</returns>
	Task<List<Entity>> ExtractEntitiesAsync(string text, CancellationToken cancellationToken);

	/// <summary>
	/// Extracts named entities from multiple texts in batch.
	/// </summary>
	/// <param name="texts">The texts to analyze.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>List of lists of extracted entities (one per input text).</returns>
	Task<List<List<Entity>>> ExtractEntitiesBatchAsync(IEnumerable<string> texts, CancellationToken cancellationToken);
}
