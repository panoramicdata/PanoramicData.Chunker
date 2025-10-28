using PanoramicData.Chunker.Models.KnowledgeGraph;

namespace PanoramicData.Chunker.Interfaces.KnowledgeGraph;

/// <summary>
/// Interface for resolving and deduplicating entities.
/// </summary>
public interface IEntityResolver
{
	/// <summary>
	/// Resolves and deduplicates a list of entities.
	/// </summary>
	/// <param name="entities">The entities to resolve.</param>
	/// <returns>Deduplicated list of entities.</returns>
	List<Entity> Resolve(IEnumerable<Entity> entities);

	/// <summary>
	/// Merges entities that are determined to be duplicates.
	/// </summary>
	/// <param name="entities">The entities to merge.</param>
	/// <returns>The merged entity.</returns>
	Entity MergeEntities(IEnumerable<Entity> entities);

	/// <summary>
	/// Determines if two entities are duplicates.
	/// </summary>
	/// <param name="entity1">The first entity.</param>
	/// <param name="entity2">The second entity.</param>
	/// <returns>True if the entities are duplicates.</returns>
	bool AreDuplicates(Entity entity1, Entity entity2);
}
