using PanoramicData.Chunker.Models;
using PanoramicData.Chunker.Models.KnowledgeGraph;

namespace PanoramicData.Chunker.Interfaces.KnowledgeGraph;

/// <summary>
/// Interface for extracting relationships between entities.
/// </summary>
public interface IRelationshipExtractor
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
	/// Gets the relationship types that this extractor can identify.
	/// </summary>
	IReadOnlyList<RelationshipType> SupportedRelationshipTypes { get; }

	/// <summary>
	/// Extracts relationships between entities from the given chunks.
	/// </summary>
	/// <param name="entities">The entities to analyze.</param>
	/// <param name="chunks">The source chunks.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>List of extracted relationships.</returns>
	Task<List<Relationship>> ExtractRelationshipsAsync(
		IEnumerable<Entity> entities,
		IEnumerable<ChunkerBase> chunks,
		CancellationToken cancellationToken = default);
}
