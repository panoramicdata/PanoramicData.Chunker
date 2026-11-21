using System.Runtime.InteropServices;

namespace PanoramicData.Chunker.Models.KnowledgeGraph;

public partial class Graph
{
	/// <summary>
	/// Gets an entity by its ID.
	/// </summary>
	/// <param name="id">The entity ID.</param>
	/// <returns>The entity, or null if not found.</returns>
	public Entity? GetEntity(Guid id)
	{
		EnsureIndexes();
		return _entityIndex!.TryGetValue(id, out var entity) ? entity : null;
	}

	/// <summary>
	/// Gets entities by name (case-insensitive).
	/// </summary>
	/// <param name="name">The entity name.</param>
	/// <returns>List of matching entities.</returns>
	public List<Entity> GetEntitiesByName(string name)
	{
		EnsureIndexes();
		var normalizedName = name.ToLowerInvariant().Trim();
		return _entityNameIndex!.TryGetValue(normalizedName, out var entities) ? entities : [];
	}

	/// <summary>
	/// Gets entities by type.
	/// </summary>
	/// <param name="type">The entity type.</param>
	/// <returns>List of matching entities.</returns>
	public List<Entity> GetEntitiesByType(EntityType type) => [.. Entities.Where(e => e.Type == type)];

	/// <summary>
	/// Gets all relationships involving a specific entity.
	/// </summary>
	/// <param name="entityId">The entity ID.</param>
	/// <param name="includeIncoming">If true, includes relationships where the entity is the target.</param>
	/// <returns>List of relationships.</returns>
	public List<Relationship> GetRelationships(Guid entityId, [Optional][DefaultParameterValue(true)] bool includeIncoming)
	{
		EnsureIndexes();
		var relationships = new List<Relationship>();

		// Outgoing relationships
		if (_entityRelationshipIndex!.TryGetValue(entityId, out var outgoing))
		{
			relationships.AddRange(outgoing);
		}

		// Incoming relationships
		if (includeIncoming)
		{
			relationships.AddRange(Relationships.Where(r => r.ToEntityId == entityId));
		}

		return relationships;
	}

	/// <summary>
	/// Gets relationships of a specific type.
	/// </summary>
	/// <param name="type">The relationship type.</param>
	/// <returns>List of matching relationships.</returns>
	public List<Relationship> GetRelationshipsByType(RelationshipType type) => [.. Relationships.Where(r => r.Type == type)];
}
