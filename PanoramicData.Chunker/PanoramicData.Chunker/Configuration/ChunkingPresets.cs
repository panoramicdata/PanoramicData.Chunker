namespace PanoramicData.Chunker.Configuration;

/// <summary>
/// Predefined chunking configurations for common scenarios.
/// </summary>
public static class ChunkingPresets
{
	/// <summary>
	/// Optimized for OpenAI embeddings (text-embedding-ada-002, text-embedding-3-small/large).
	/// </summary>
	public static ChunkingOptions ForOpenAIEmbeddings() => new()
	{
		MaxTokens = 512,
		OverlapTokens = 50,
		TokenCountingMethod = TokenCountingMethod.OpenAI_CL100K,
		Strategy = ChunkingStrategy.Structural,
		ExtractImages = true,
		IncludeQualityMetrics = true
	};

	/// <summary>
	/// Optimized for Cohere embeddings.
	/// </summary>
	public static ChunkingOptions ForCohereEmbeddings() => new()
	{
		MaxTokens = 256,
		OverlapTokens = 25,
		TokenCountingMethod = TokenCountingMethod.Cohere,
		Strategy = ChunkingStrategy.Structural
	};

	/// <summary>
	/// Optimized for Azure AI Search with semantic ranking.
	/// </summary>
	public static ChunkingOptions ForAzureAISearch() => new()
	{
		MaxTokens = 1024,
		OverlapTokens = 100,
		Strategy = ChunkingStrategy.Structural,
		PreserveFormatting = true,
		ExtractKeywords = true
	};

	/// <summary>
	/// Optimized for processing large documents with streaming.
	/// </summary>
	public static ChunkingOptions ForLargeDocuments() => new()
	{
		EnableStreaming = true,
		BatchSize = 50,
		MaxDegreeOfParallelism = 4,
		EnableCaching = true
	};

	/// <summary>
	/// Semantic chunking with embedding-based boundary detection.
	/// </summary>
	public static ChunkingOptions ForSemanticChunking() => new()
	{
		Strategy = ChunkingStrategy.Semantic,
		MaxTokens = 512,
		IncludeQualityMetrics = true
	};

	/// <summary>
	/// Maximum context preservation for RAG systems.
	/// </summary>
	public static ChunkingOptions ForRAG() => new()
	{
		MaxTokens = 512,
		OverlapTokens = 100,
		Strategy = ChunkingStrategy.Structural,
		ExtractImages = true,
		GenerateImageDescriptions = true,
		ExtractKeywords = true,
		PreserveFormatting = true,
		IncludeQualityMetrics = true
	};
}
