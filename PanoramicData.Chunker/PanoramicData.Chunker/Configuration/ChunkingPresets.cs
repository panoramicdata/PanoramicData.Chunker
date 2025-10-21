using PanoramicData.Chunker.Infrastructure;

namespace PanoramicData.Chunker.Configuration;

/// <summary>
/// Predefined chunking configurations for common scenarios.
/// </summary>
public static class ChunkingPresets
{
	/// <summary>
	/// Optimized for OpenAI embeddings (text-embedding-ada-002, text-embedding-3-small/large).
	/// Uses accurate OpenAI CL100K token counting for optimal chunk sizing.
	/// </summary>
	public static ChunkingOptions ForOpenAIEmbeddings() => new()
	{
		MaxTokens = 512,
		OverlapTokens = 50,
		TokenCountingMethod = TokenCountingMethod.OpenAI_CL100K,
		TokenCounter = TokenCounterFactory.Create(TokenCountingMethod.OpenAI_CL100K),
		Strategy = ChunkingStrategy.Structural,
		ExtractImages = true,
		IncludeQualityMetrics = true
	};

	/// <summary>
	/// Optimized for Cohere embeddings.
	/// Note: Cohere tokenization not yet implemented, uses character-based estimation.
	/// </summary>
	public static ChunkingOptions ForCohereEmbeddings() => new()
	{
		MaxTokens = 256,
		OverlapTokens = 25,
		TokenCountingMethod = TokenCountingMethod.CharacterBased,
		TokenCounter = TokenCounterFactory.Create(TokenCountingMethod.CharacterBased),
		Strategy = ChunkingStrategy.Structural
	};

	/// <summary>
	/// Optimized for Azure AI Search with semantic ranking.
	/// Uses OpenAI token counting for consistent sizing.
	/// </summary>
	public static ChunkingOptions ForAzureAISearch() => new()
	{
		MaxTokens = 1024,
		OverlapTokens = 100,
		TokenCountingMethod = TokenCountingMethod.OpenAI_CL100K,
		TokenCounter = TokenCounterFactory.Create(TokenCountingMethod.OpenAI_CL100K),
		Strategy = ChunkingStrategy.Structural,
		PreserveFormatting = true,
		ExtractKeywords = true
	};

	/// <summary>
	/// Optimized for processing large documents with streaming.
	/// </summary>
	public static ChunkingOptions ForLargeDocuments() => new()
	{
		MaxTokens = 512,
		OverlapTokens = 50,
		TokenCountingMethod = TokenCountingMethod.OpenAI_CL100K,
		TokenCounter = TokenCounterFactory.Create(TokenCountingMethod.OpenAI_CL100K),
		EnableStreaming = true,
		BatchSize = 50,
		MaxDegreeOfParallelism = 4,
		EnableCaching = true
	};

	/// <summary>
	/// Semantic chunking with embedding-based boundary detection.
	/// Uses OpenAI token counting for accurate sizing.
	/// </summary>
	public static ChunkingOptions ForSemanticChunking() => new()
	{
		Strategy = ChunkingStrategy.Semantic,
		MaxTokens = 512,
		OverlapTokens = 50,
		TokenCountingMethod = TokenCountingMethod.OpenAI_CL100K,
		TokenCounter = TokenCounterFactory.Create(TokenCountingMethod.OpenAI_CL100K),
		IncludeQualityMetrics = true
	};

	/// <summary>
	/// Maximum context preservation for RAG systems.
	/// Optimized for OpenAI embeddings with accurate token counting.
	/// </summary>
	public static ChunkingOptions ForRAG() => new()
	{
		MaxTokens = 512,
		OverlapTokens = 100,
		TokenCountingMethod = TokenCountingMethod.OpenAI_CL100K,
		TokenCounter = TokenCounterFactory.Create(TokenCountingMethod.OpenAI_CL100K),
		Strategy = ChunkingStrategy.Structural,
		ExtractImages = true,
		GenerateImageDescriptions = true,
		ExtractKeywords = true,
		PreserveFormatting = true,
		IncludeQualityMetrics = true
	};

	/// <summary>
	/// Fast chunking with character-based token estimation.
	/// Use when performance is more important than accuracy.
	/// </summary>
	public static ChunkingOptions ForFastProcessing() => new()
	{
		MaxTokens = 512,
		OverlapTokens = 50,
		TokenCountingMethod = TokenCountingMethod.CharacterBased,
		TokenCounter = TokenCounterFactory.Create(TokenCountingMethod.CharacterBased),
		Strategy = ChunkingStrategy.Structural,
		IncludeQualityMetrics = false,
		ValidateChunks = false
	};

	/// <summary>
	/// GPT-3 optimized chunking with P50K tokenization.
	/// </summary>
	public static ChunkingOptions ForGPT3() => new()
	{
		MaxTokens = 512,
		OverlapTokens = 50,
		TokenCountingMethod = TokenCountingMethod.OpenAI_P50K,
		TokenCounter = TokenCounterFactory.Create(TokenCountingMethod.OpenAI_P50K),
		Strategy = ChunkingStrategy.Structural,
		IncludeQualityMetrics = true
	};
}
