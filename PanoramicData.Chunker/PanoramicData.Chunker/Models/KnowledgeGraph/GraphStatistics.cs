namespace PanoramicData.Chunker.Models.KnowledgeGraph;

/// <summary>
/// Statistical information about a knowledge graph.
/// </summary>
public class GraphStatistics
{
	/// <summary>
	/// Gets or sets the total number of entities in the graph.
	/// </summary>
	public int TotalEntities { get; set; }

	/// <summary>
	/// Gets or sets the total number of relationships in the graph.
	/// </summary>
	public int TotalRelationships { get; set; }

	/// <summary>
	/// Gets or sets the distribution of entity types (type -> count).
	/// </summary>
	public Dictionary<EntityType, int> EntityTypeDistribution { get; set; } = [];

	/// <summary>
	/// Gets or sets the distribution of relationship types (type -> count).
	/// </summary>
	public Dictionary<RelationshipType, int> RelationshipTypeDistribution { get; set; } = [];

	/// <summary>
	/// Gets or sets the average confidence score across all entities.
	/// </summary>
	public double AverageEntityConfidence { get; set; }

	/// <summary>
	/// Gets or sets the average confidence score across all relationships.
	/// </summary>
	public double AverageRelationshipConfidence { get; set; }

	/// <summary>
	/// Gets or sets the average frequency of entities across chunks.
	/// </summary>
	public double AverageEntityFrequency { get; set; }

	/// <summary>
	/// Gets or sets the total number of source references across all entities.
	/// </summary>
	public int TotalEntitySources { get; set; }

	/// <summary>
	/// Gets or sets the total number of evidence records across all relationships.
	/// </summary>
	public int TotalRelationshipEvidence { get; set; }

	/// <summary>
	/// Gets or sets the timestamp when these statistics were computed.
	/// </summary>
	public DateTimeOffset ComputedAt { get; set; } = DateTimeOffset.UtcNow;

	/// <summary>
	/// Gets the average degree (number of relationships per entity).
	/// </summary>
	public double AverageDegree => TotalEntities > 0 ? (double)TotalRelationships / TotalEntities : 0.0;

	/// <summary>
	/// Gets the graph density (ratio of actual to possible edges).
	/// </summary>
	public double Density
	{
		get
		{
			if (TotalEntities < 2)
			{
				return 0.0;
			}
			var possibleEdges = TotalEntities * (TotalEntities - 1);
			return possibleEdges > 0 ? (double)TotalRelationships / possibleEdges : 0.0;
		}
	}

	/// <summary>
	/// Returns a string representation of these statistics.
	/// </summary>
	public override string ToString() => $"Entities: {TotalEntities}, Relationships: {TotalRelationships}, " +
			   $"Avg Confidence: {AverageEntityConfidence:F2}, Density: {Density:F4}";
}
