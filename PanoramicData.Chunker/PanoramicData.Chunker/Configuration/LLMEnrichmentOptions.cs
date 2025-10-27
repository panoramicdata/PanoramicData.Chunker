namespace PanoramicData.Chunker.Configuration;

/// <summary>
/// Configuration options for LLM-based chunk enrichment.
/// </summary>
public record LLMEnrichmentOptions
{
	/// <summary>
	/// Enable summarization of chunks.
	/// </summary>
	public bool EnableSummarization { get; init; } = true;

	/// <summary>
	/// Enable keyword extraction from chunks.
	/// </summary>
	public bool EnableKeywordExtraction { get; init; } = true;

	/// <summary>
	/// Enable preliminary named entity recognition.
	/// </summary>
	public bool EnablePreliminaryNER { get; init; } = false;

	/// <summary>
	/// Model to use for enrichment.
	/// </summary>
	public string Model { get; init; } = "llama3";

	/// <summary>
	/// Maximum words for summaries.
	/// </summary>
	public int SummaryMaxWords { get; init; } = 50;

	/// <summary>
	/// Maximum tokens for summary generation.
	/// </summary>
	public int SummaryMaxTokens { get; init; } = 200;

	/// <summary>
	/// Maximum number of keywords to extract.
	/// </summary>
	public int MaxKeywords { get; init; } = 10;

	/// <summary>
	/// Enable caching of enrichment results.
	/// </summary>
	public bool EnableCaching { get; init; } = true;

	/// <summary>
	/// Duration to cache enrichment results.
	/// </summary>
	public TimeSpan CacheDuration { get; init; } = TimeSpan.FromHours(24);

	/// <summary>
	/// Maximum number of concurrent enrichment requests.
	/// </summary>
	public int MaxConcurrency { get; init; } = 5;
}
