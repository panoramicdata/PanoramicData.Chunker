namespace PanoramicData.Chunker.Models.KnowledgeGraph;

/// <summary>
/// Statistics about the knowledge graph extraction process.
/// </summary>
public class GraphExtractionStatistics
{
	/// <summary>
	/// Gets or sets the number of chunks processed.
	/// </summary>
	public int ChunksProcessed { get; set; }

	/// <summary>
	/// Gets or sets the number of entities extracted before deduplication.
	/// </summary>
	public int EntitiesExtracted { get; set; }

	/// <summary>
	/// Gets or sets the number of entities after deduplication.
	/// </summary>
	public int EntitiesAfterDeduplication { get; set; }

	/// <summary>
	/// Gets or sets the number of relationships extracted before consolidation.
	/// </summary>
	public int RelationshipsExtracted { get; set; }

	/// <summary>
	/// Gets or sets the number of relationships after consolidation.
	/// </summary>
	public int RelationshipsAfterConsolidation { get; set; }

	/// <summary>
	/// Gets or sets the number of entities that were merged during deduplication.
	/// </summary>
	public int EntitiesMerged { get; set; }

	/// <summary>
	/// Gets or sets the number of relationships that were merged during consolidation.
	/// </summary>
	public int RelationshipsMerged { get; set; }

	/// <summary>
	/// Gets or sets the total time spent on entity extraction.
	/// </summary>
	public TimeSpan EntityExtractionTime { get; set; }

	/// <summary>
	/// Gets or sets the total time spent on relationship extraction.
	/// </summary>
	public TimeSpan RelationshipExtractionTime { get; set; }

	/// <summary>
	/// Gets or sets the total time spent on graph building.
	/// </summary>
	public TimeSpan GraphBuildingTime { get; set; }

	/// <summary>
	/// Gets or sets the names of extractors that were used.
	/// </summary>
	public List<string> ExtractorsUsed { get; set; } = [];

	/// <summary>
	/// Gets or sets the extraction rate (entities per second).
	/// </summary>
	public double EntityExtractionRate => EntityExtractionTime.TotalSeconds > 0
				? EntitiesExtracted / EntityExtractionTime.TotalSeconds
				: 0.0;

	/// <summary>
	/// Gets or sets the relationship extraction rate (relationships per second).
	/// </summary>
	public double RelationshipExtractionRate => RelationshipExtractionTime.TotalSeconds > 0
				? RelationshipsExtracted / RelationshipExtractionTime.TotalSeconds
				: 0.0;

	/// <summary>
	/// Gets the total extraction time.
	/// </summary>
	public TimeSpan TotalTime => EntityExtractionTime + RelationshipExtractionTime + GraphBuildingTime;

	/// <summary>
	/// Gets the deduplication ratio (1.0 means no duplicates).
	/// </summary>
	public double DeduplicationRatio => EntitiesExtracted > 0
				? (double)EntitiesAfterDeduplication / EntitiesExtracted
				: 1.0;

	/// <summary>
	/// Gets the consolidation ratio (1.0 means no consolidation).
	/// </summary>
	public double ConsolidationRatio => RelationshipsExtracted > 0
				? (double)RelationshipsAfterConsolidation / RelationshipsExtracted
				: 1.0;

	/// <summary>
	/// Returns a string representation of these statistics.
	/// </summary>
	public override string ToString() => $"Extracted {EntitiesExtracted} entities ({EntitiesAfterDeduplication} after dedup), " +
			   $"{RelationshipsExtracted} relationships ({RelationshipsAfterConsolidation} after consolidation) " +
			   $"in {TotalTime.TotalSeconds:F2}s";
}
