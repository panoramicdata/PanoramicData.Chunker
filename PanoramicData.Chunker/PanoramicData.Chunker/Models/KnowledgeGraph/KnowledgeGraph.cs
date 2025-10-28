namespace PanoramicData.Chunker.Models.KnowledgeGraph;

/// <summary>
/// Represents a knowledge graph containing entities and their relationships.
/// </summary>
public class Graph
{
	/// <summary>
	/// Gets or sets the unique identifier for this knowledge graph.
	/// </summary>
	public Guid Id { get; set; } = Guid.NewGuid();

	/// <summary>
	/// Gets or sets the name or title of this knowledge graph.
	/// </summary>
	public string Name { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets the entities in this knowledge graph.
	/// </summary>
	public List<Entity> Entities { get; set; } = [];

	/// <summary>
	/// Gets or sets the relationships between entities in this knowledge graph.
	/// </summary>
	public List<Relationship> Relationships { get; set; } = [];

	/// <summary>
	/// Gets or sets metadata about this knowledge graph.
	/// </summary>
	public GraphMetadata Metadata { get; set; } = new();

	/// <summary>
	/// Gets or sets the schema definitions for this graph.
	/// </summary>
	public GraphSchema Schema { get; set; } = new();

	/// <summary>
	/// Gets or sets computed statistics about this knowledge graph.
	/// </summary>
	public GraphStatistics Statistics { get; set; } = new();

	// Index structures for fast lookup
	private Dictionary<Guid, Entity>? _entityIndex;
	private Dictionary<string, List<Entity>>? _entityNameIndex;
	private Dictionary<Guid, List<Relationship>>? _entityRelationshipIndex;

	/// <summary>
	/// Initializes a new instance of the <see cref="Graph"/> class.
	/// </summary>
	public Graph()
	{
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="Graph"/> class with the specified name.
	/// </summary>
	/// <param name="name">The name of the knowledge graph.</param>
	public Graph(string name)
	{
		Name = name;
	}

	/// <summary>
	/// Adds an entity to the knowledge graph.
	/// </summary>
	/// <param name="entity">The entity to add.</param>
	public void AddEntity(Entity entity)
	{
		ArgumentNullException.ThrowIfNull(entity);

		Entities.Add(entity);
		InvalidateIndexes();
	}

	/// <summary>
	/// Adds a relationship to the knowledge graph.
	/// </summary>
	/// <param name="relationship">The relationship to add.</param>
	public void AddRelationship(Relationship relationship)
	{
		ArgumentNullException.ThrowIfNull(relationship);

		Relationships.Add(relationship);
		InvalidateIndexes();
	}

	/// <summary>
	/// Builds indexes for fast entity and relationship lookup.
	/// </summary>
	public void BuildIndexes()
	{
		// Entity ID index
		_entityIndex = Entities.ToDictionary(e => e.Id);

		// Entity name index (normalized)
		_entityNameIndex = Entities
			.GroupBy(e => e.NormalizedName)
			.ToDictionary(g => g.Key, g => g.ToList());

		// Entity relationship index (outgoing relationships)
		_entityRelationshipIndex = Relationships
			.GroupBy(r => r.FromEntityId)
			.ToDictionary(g => g.Key, g => g.ToList());
	}

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
	public List<Relationship> GetRelationships(Guid entityId, bool includeIncoming = true)
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

	/// <summary>
	/// Computes statistics for this knowledge graph.
	/// </summary>
	public void ComputeStatistics() => Statistics = new GraphStatistics
	{
		TotalEntities = Entities.Count,
		TotalRelationships = Relationships.Count,
		EntityTypeDistribution = Entities
				.GroupBy(e => e.Type)
				.ToDictionary(g => g.Key, g => g.Count()),
		RelationshipTypeDistribution = Relationships
				.GroupBy(r => r.Type)
				.ToDictionary(g => g.Key, g => g.Count()),
		AverageEntityConfidence = Entities.Count != 0 ? Entities.Average(e => e.Confidence) : 0.0,
		AverageRelationshipConfidence = Relationships.Count != 0 ? Relationships.Average(r => r.Confidence) : 0.0,
		AverageEntityFrequency = Entities.Count != 0 ? Entities.Average(e => e.Frequency) : 0.0,
		TotalEntitySources = Entities.Sum(e => e.Sources.Count),
		TotalRelationshipEvidence = Relationships.Sum(r => r.Evidence.Count),
		ComputedAt = DateTimeOffset.UtcNow
	};

	/// <summary>
	/// Validates the integrity of the knowledge graph.
	/// </summary>
	/// <returns>True if the graph is valid.</returns>
	public bool Validate(out List<string> errors)
	{
		errors = [];

		// Check for duplicate entity IDs
		var entityIds = new HashSet<Guid>();
		foreach (var entity in Entities)
		{
			if (!entityIds.Add(entity.Id))
			{
				errors.Add($"Duplicate entity ID: {entity.Id}");
			}
		}

		// Check for duplicate relationship IDs
		var relationshipIds = new HashSet<Guid>();
		foreach (var relationship in Relationships)
		{
			if (!relationshipIds.Add(relationship.Id))
			{
				errors.Add($"Duplicate relationship ID: {relationship.Id}");
			}
		}

		// Check for relationships with invalid entity references
		foreach (var relationship in Relationships)
		{
			if (!entityIds.Contains(relationship.FromEntityId))
			{
				errors.Add($"Relationship {relationship.Id} references non-existent entity: {relationship.FromEntityId}");
			}
			if (!entityIds.Contains(relationship.ToEntityId))
			{
				errors.Add($"Relationship {relationship.Id} references non-existent entity: {relationship.ToEntityId}");
			}
		}

		return errors.Count == 0;
	}

	/// <summary>
	/// Clears all entities and relationships from the graph.
	/// </summary>
	public void Clear()
	{
		Entities.Clear();
		Relationships.Clear();
		InvalidateIndexes();
		Statistics = new GraphStatistics();
	}

	/// <summary>
	/// Returns a string representation of this knowledge graph.
	/// </summary>
	public override string ToString() => $"Graph '{Name}': {Entities.Count} entities, {Relationships.Count} relationships";

	private void EnsureIndexes()
	{
		if (_entityIndex == null)
		{
			BuildIndexes();
		}
	}

	private void InvalidateIndexes()
	{
		_entityIndex = null;
		_entityNameIndex = null;
		_entityRelationshipIndex = null;
	}
}
