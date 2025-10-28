namespace PanoramicData.Chunker.Models.KnowledgeGraph;

/// <summary>
/// Represents the result of knowledge graph extraction from document chunks.
/// </summary>
public class KnowledgeGraphResult
{
	/// <summary>
	/// Gets or sets whether the knowledge graph extraction was successful.
	/// </summary>
	public bool Success { get; set; }

	/// <summary>
	/// Gets or sets the extracted knowledge graph.
	/// </summary>
	public Graph? Graph { get; set; }

	/// <summary>
	/// Gets or sets statistics about the extraction process.
	/// </summary>
	public GraphExtractionStatistics Statistics { get; set; } = new();

	/// <summary>
	/// Gets or sets any errors that occurred during extraction.
	/// </summary>
	public List<string> Errors { get; set; } = [];

	/// <summary>
	/// Gets or sets any warnings that occurred during extraction.
	/// </summary>
	public List<string> Warnings { get; set; } = [];

	/// <summary>
	/// Gets or sets the timestamp when extraction started.
	/// </summary>
	public DateTimeOffset StartedAt { get; set; }

	/// <summary>
	/// Gets or sets the timestamp when extraction completed.
	/// </summary>
	public DateTimeOffset? CompletedAt { get; set; }

	/// <summary>
	/// Gets the duration of the extraction process.
	/// </summary>
	public TimeSpan? Duration => CompletedAt.HasValue ? CompletedAt.Value - StartedAt : null;

	/// <summary>
	/// Initializes a new instance of the <see cref="KnowledgeGraphResult"/> class.
	/// </summary>
	public KnowledgeGraphResult()
	{
		StartedAt = DateTimeOffset.UtcNow;
	}

	/// <summary>
	/// Marks the extraction as complete and records the completion time.
	/// </summary>
	public void Complete() => CompletedAt = DateTimeOffset.UtcNow;

	/// <summary>
	/// Adds an error message to the result.
	/// </summary>
	/// <param name="error">The error message.</param>
	public void AddError(string error)
	{
		Errors.Add(error);
		Success = false;
	}

	/// <summary>
	/// Adds a warning message to the result.
	/// </summary>
	/// <param name="warning">The warning message.</param>
	public void AddWarning(string warning) => Warnings.Add(warning);

	/// <summary>
	/// Returns a string representation of this result.
	/// </summary>
	public override string ToString()
	{
		var status = Success ? "Success" : "Failed";
		var entityCount = Graph?.Entities.Count ?? 0;
		var relationshipCount = Graph?.Relationships.Count ?? 0;
		return $"KnowledgeGraphResult: {status} - {entityCount} entities, {relationshipCount} relationships in {Duration?.TotalSeconds:F2}s";
	}
}
