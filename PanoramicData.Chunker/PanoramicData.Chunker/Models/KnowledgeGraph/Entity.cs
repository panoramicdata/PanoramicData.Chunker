namespace PanoramicData.Chunker.Models.KnowledgeGraph;

/// <summary>
/// Represents an entity extracted from document content in the knowledge graph.
/// </summary>
public class Entity
{
	/// <summary>
	/// Gets or sets the unique identifier for this entity.
	/// </summary>
	public Guid Id { get; set; } = Guid.NewGuid();

	/// <summary>
	/// Gets or sets the type of this entity.
	/// </summary>
	public EntityType Type { get; set; }

	/// <summary>
	/// Gets or sets the display name of the entity as it appears in the source text.
	/// </summary>
	public string Name { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets the normalized form of the entity name (lowercased, trimmed).
	/// Used for deduplication and matching.
	/// </summary>
	public string NormalizedName { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets the confidence score for this entity extraction (0.0 to 1.0).
	/// Higher values indicate greater confidence in the entity identification.
	/// </summary>
	public double Confidence { get; set; }

	/// <summary>
	/// Gets or sets the number of times this entity appears across all chunks.
	/// </summary>
	public int Frequency { get; set; }

	/// <summary>
	/// Gets or sets custom properties specific to this entity type.
	/// For example, a Person entity might have properties like "title" or "email".
	/// </summary>
	public Dictionary<string, object> Properties { get; set; } = [];

	/// <summary>
	/// Gets or sets metadata about this entity's extraction and sources.
	/// </summary>
	public EntityMetadata Metadata { get; set; } = new();

	/// <summary>
	/// Gets or sets the list of source chunks from which this entity was extracted.
	/// </summary>
	public List<EntitySource> Sources { get; set; } = [];

	/// <summary>
	/// Gets or sets aliases or alternative names for this entity.
	/// </summary>
	public List<string> Aliases { get; set; } = [];

	/// <summary>
	/// Gets or sets the IDs of related entities.
	/// Populated during relationship resolution.
	/// </summary>
	public List<Guid> RelatedEntityIds { get; set; } = [];

	/// <summary>
	/// Initializes a new instance of the <see cref="Entity"/> class.
	/// </summary>
	public Entity()
	{
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="Entity"/> class with the specified type and name.
	/// </summary>
	/// <param name="type">The entity type.</param>
	/// <param name="name">The entity name.</param>
	/// <param name="confidence">The confidence score (default: 1.0).</param>
	public Entity(EntityType type, string name, double confidence = 1.0)
	{
		Type = type;
		Name = name;
		NormalizedName = name.ToLowerInvariant().Trim();
		Confidence = confidence;
	}

	/// <summary>
	/// Adds a source chunk reference to this entity.
	/// </summary>
	/// <param name="chunkId">The chunk ID.</param>
	/// <param name="position">The position in the chunk where the entity appears.</param>
	/// <param name="context">The surrounding context text.</param>
	public void AddSource(Guid chunkId, int position, string context = "") => Sources.Add(new EntitySource
	{
		ChunkId = chunkId,
		Position = position,
		Context = context,
		Timestamp = DateTimeOffset.UtcNow
	});

	/// <summary>
	/// Adds an alias for this entity.
	/// </summary>
	/// <param name="alias">The alias to add.</param>
	public void AddAlias(string alias)
	{
		if (!string.IsNullOrWhiteSpace(alias) && !Aliases.Contains(alias))
		{
			Aliases.Add(alias);
		}
	}

	/// <summary>
	/// Merges another entity into this one (for deduplication).
	/// </summary>
	/// <param name="other">The entity to merge.</param>
	public void Merge(Entity other)
	{
		if (other == null || other.Id == Id)
		{
			return;
		}

		// Combine frequencies
		Frequency += other.Frequency;

		// Take the higher confidence score
		Confidence = Math.Max(Confidence, other.Confidence);

		// Merge sources
		foreach (var source in other.Sources)
		{
			if (!Sources.Any(s => s.ChunkId == source.ChunkId && s.Position == source.Position))
			{
				Sources.Add(source);
			}
		}

		// Merge aliases
		foreach (var alias in other.Aliases)
		{
			AddAlias(alias);
		}

		// Add the other entity's name as an alias if different
		if (other.Name != Name)
		{
			AddAlias(other.Name);
		}

		// Merge properties (prefer existing values)
		foreach (var prop in other.Properties)
		{
			if (!Properties.ContainsKey(prop.Key))
			{
				Properties[prop.Key] = prop.Value;
			}
		}

		// Merge related entity IDs
		foreach (var relatedId in other.RelatedEntityIds)
		{
			if (!RelatedEntityIds.Contains(relatedId))
			{
				RelatedEntityIds.Add(relatedId);
			}
		}
	}

	/// <summary>
	/// Returns a string representation of this entity.
	/// </summary>
	public override string ToString() => $"{Type}: {Name} (Confidence: {Confidence:F2}, Frequency: {Frequency})";
}
