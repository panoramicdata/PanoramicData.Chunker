namespace PanoramicData.Chunker.Configuration;

/// <summary>
/// Configuration options for knowledge graph extraction.
/// </summary>
public class KnowledgeGraphOptions
{
	/// <summary>
	/// Gets or sets a value indicating whether keyword extraction is enabled.
	/// </summary>
	public bool EnableKeywordExtraction { get; set; } = true;

	/// <summary>
	/// Gets or sets a value indicating whether relationship extraction is enabled.
	/// </summary>
	public bool EnableRelationshipExtraction { get; set; } = true;

	/// <summary>
	/// Gets or sets a value indicating whether entity normalization is enabled.
	/// </summary>
	public bool EnableEntityNormalization { get; set; } = true;

	/// <summary>
	/// Gets or sets a value indicating whether entity resolution (deduplication) is enabled.
	/// </summary>
	public bool EnableEntityResolution { get; set; } = true;

	/// <summary>
	/// Gets or sets the minimum confidence score for entity extraction (0.0 to 1.0).
	/// Entities with confidence below this threshold will be filtered out.
	/// </summary>
	public double MinEntityConfidence { get; set; } = 0.3;

	/// <summary>
	/// Gets or sets the minimum confidence score for relationship extraction (0.0 to 1.0).
	/// Relationships with confidence below this threshold will be filtered out.
	/// </summary>
	public double MinRelationshipConfidence { get; set; } = 0.3;

	/// <summary>
	/// Gets or sets the maximum number of keywords to extract per chunk.
	/// </summary>
	public int MaxKeywordsPerChunk { get; set; } = 10;

	/// <summary>
	/// Gets or sets the minimum word length for keyword extraction.
	/// Words shorter than this will be ignored.
	/// </summary>
	public int MinWordLength { get; set; } = 3;

	/// <summary>
	/// Gets or sets the maximum character distance between entities to consider them related.
	/// </summary>
	public int MaxCooccurrenceDistance { get; set; } = 500;

	/// <summary>
	/// Gets or sets the similarity threshold for entity deduplication (0.0 to 1.0).
	/// Entities with similarity above this threshold will be merged.
	/// </summary>
	public double EntitySimilarityThreshold { get; set; } = 0.85;

	/// <summary>
	/// Gets or sets a value indicating whether to store the graph in PostgreSQL + Apache AGE.
	/// </summary>
	public bool EnableGraphStorage { get; set; }

	/// <summary>
	/// Gets or sets the PostgreSQL connection string for graph storage.
	/// Required when EnableGraphStorage is true.
	/// </summary>
	public string? PostgresConnectionString { get; set; }

	/// <summary>
	/// Gets or sets the name of the graph in Apache AGE.
	/// </summary>
	public string GraphName { get; set; } = "document_graph";

	/// <summary>
	/// Gets or sets a value indicating whether to build indexes for the graph.
	/// </summary>
	public bool BuildIndexes { get; set; } = true;

	/// <summary>
	/// Gets or sets a value indicating whether to compute graph statistics.
	/// </summary>
	public bool ComputeStatistics { get; set; } = true;

	/// <summary>
	/// Gets or sets a value indicating whether to validate the graph after extraction.
	/// </summary>
	public bool ValidateGraph { get; set; } = true;

	/// <summary>
	/// Gets or sets custom entity extractors to use in addition to the default keyword extractor.
	/// </summary>
	public List<string> CustomExtractorTypes { get; set; } = new();

	/// <summary>
	/// Gets or sets custom relationship extractors to use in addition to the default co-occurrence extractor.
	/// </summary>
	public List<string> CustomRelationshipExtractorTypes { get; set; } = new();

	/// <summary>
	/// Validates the configuration options.
	/// </summary>
	/// <returns>A list of validation errors, or an empty list if valid.</returns>
	public List<string> Validate()
	{
		(bool IsInvalid, string Message)[] checks =
		[
			(MinEntityConfidence is < 0.0 or > 1.0, "MinEntityConfidence must be between 0.0 and 1.0."),
			(MinRelationshipConfidence is < 0.0 or > 1.0, "MinRelationshipConfidence must be between 0.0 and 1.0."),
			(MaxKeywordsPerChunk < 1, "MaxKeywordsPerChunk must be at least 1."),
			(MinWordLength < 1, "MinWordLength must be at least 1."),
			(MaxCooccurrenceDistance < 1, "MaxCooccurrenceDistance must be at least 1."),
			(EntitySimilarityThreshold is < 0.0 or > 1.0, "EntitySimilarityThreshold must be between 0.0 and 1.0."),
			(EnableGraphStorage && string.IsNullOrWhiteSpace(PostgresConnectionString), "PostgresConnectionString is required when EnableGraphStorage is true."),
			(string.IsNullOrWhiteSpace(GraphName), "GraphName cannot be empty.")
		];

		return [.. checks.Where(check => check.IsInvalid).Select(check => check.Message)];
	}
}
