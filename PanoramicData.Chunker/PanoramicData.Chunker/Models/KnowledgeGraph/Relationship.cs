namespace PanoramicData.Chunker.Models.KnowledgeGraph;

/// <summary>
/// Represents a relationship between two entities in the knowledge graph.
/// </summary>
public class Relationship
{
	/// <summary>
	/// Gets or sets the unique identifier for this relationship.
	/// </summary>
	public Guid Id { get; set; } = Guid.NewGuid();

	/// <summary>
	/// Gets or sets the type of this relationship.
	/// </summary>
	public RelationshipType Type { get; set; }

	/// <summary>
	/// Gets or sets the ID of the source entity (from).
	/// </summary>
	public Guid FromEntityId { get; set; }

	/// <summary>
	/// Gets or sets the ID of the target entity (to).
	/// </summary>
	public Guid ToEntityId { get; set; }

	/// <summary>
	/// Gets or sets the weight or strength of this relationship (0.0 to 1.0).
	/// Higher values indicate stronger relationships.
	/// </summary>
	public double Weight { get; set; } = 1.0;

	/// <summary>
	/// Gets or sets the confidence score for this relationship (0.0 to 1.0).
	/// </summary>
	public double Confidence { get; set; } = 1.0;

	/// <summary>
	/// Gets or sets whether this relationship is bidirectional.
	/// </summary>
	public bool Bidirectional { get; set; }

	/// <summary>
	/// Gets or sets custom properties for this relationship.
	/// </summary>
	public Dictionary<string, object> Properties { get; set; } = [];

	/// <summary>
	/// Gets or sets metadata about this relationship's extraction.
	/// </summary>
	public RelationshipMetadata Metadata { get; set; } = new();

	/// <summary>
	/// Gets or sets evidence supporting this relationship from source chunks.
	/// </summary>
	public List<RelationshipEvidence> Evidence { get; set; } = [];

	/// <summary>
	/// Initializes a new instance of the <see cref="Relationship"/> class.
	/// </summary>
	public Relationship()
	{
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="Relationship"/> class with the specified entities and type.
	/// </summary>
	/// <param name="fromEntityId">The source entity ID.</param>
	/// <param name="toEntityId">The target entity ID.</param>
	/// <param name="type">The relationship type.</param>
	/// <param name="weight">The relationship weight (default: 1.0).</param>
	/// <param name="confidence">The confidence score (default: 1.0).</param>
	public Relationship(Guid fromEntityId, Guid toEntityId, RelationshipType type, double weight = 1.0, double confidence = 1.0)
	{
		FromEntityId = fromEntityId;
		ToEntityId = toEntityId;
		Type = type;
		Weight = weight;
		Confidence = confidence;
	}

	/// <summary>
	/// Adds evidence supporting this relationship.
	/// </summary>
	/// <param name="chunkId">The chunk ID where evidence was found.</param>
	/// <param name="context">The context text supporting the relationship.</param>
	/// <param name="confidence">The confidence score for this evidence.</param>
	public void AddEvidence(Guid chunkId, string context, double confidence = 1.0) => Evidence.Add(new RelationshipEvidence
	{
		ChunkId = chunkId,
		Context = context,
		Confidence = confidence,
		Timestamp = DateTimeOffset.UtcNow
	});

	/// <summary>
	/// Merges another relationship into this one (for consolidation).
	/// </summary>
	/// <param name="other">The relationship to merge.</param>
	public void Merge(Relationship other)
	{
		if (other == null || other.Id == Id)
		{
			return;
		}

		// Sum weights (accumulate strength)
		Weight += other.Weight;

		// Take the higher confidence
		Confidence = Math.Max(Confidence, other.Confidence);

		// Merge evidence
		foreach (var evidence in other.Evidence)
		{
			if (!Evidence.Any(e => e.ChunkId == evidence.ChunkId && e.Context == evidence.Context))
			{
				Evidence.Add(evidence);
			}
		}

		// Merge properties (newer values overwrite existing)
		foreach (var prop in other.Properties)
		{
			Properties[prop.Key] = prop.Value;
		}
	}

	/// <summary>
	/// Returns a string representation of this relationship.
	/// </summary>
	public override string ToString() => $"{FromEntityId} --[{Type}]--> {ToEntityId} (Weight: {Weight:F2}, Confidence: {Confidence:F2})";

	/// <summary>
	/// Determines if this relationship is equivalent to another (same entities and type).
	/// </summary>
	/// <param name="other">The other relationship.</param>
	/// <param name="ignoreDirection">If true, considers bidirectional equivalence.</param>
	/// <returns>True if relationships are equivalent.</returns>
	public bool IsEquivalentTo(Relationship other, bool ignoreDirection = false)
	{
		if (other == null)
		{
			return false;
		}

		if (FromEntityId == other.FromEntityId && ToEntityId == other.ToEntityId && Type == other.Type)
		{
			return true;
		}

		if (ignoreDirection && FromEntityId == other.ToEntityId && ToEntityId == other.FromEntityId && Type == other.Type)
		{
			return true;
		}

		return false;
	}
}
