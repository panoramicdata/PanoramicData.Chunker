using PanoramicData.Chunker.Interfaces;
using Microsoft.Extensions.Logging;

namespace PanoramicData.Chunker.Configuration;

/// <summary>
/// Configuration options for chunking operations.
/// </summary>
public class ChunkingOptions
{
	// Size and Token Control
	/// <summary>
	/// Maximum number of tokens per chunk.
	/// </summary>
	public int MaxTokens { get; set; } = 512;

	/// <summary>
	/// Number of tokens to overlap between consecutive chunks.
	/// </summary>
	public int OverlapTokens { get; set; } = 50;

	/// <summary>
	/// Custom token counter implementation.
	/// </summary>
	public ITokenCounter? TokenCounter { get; set; }

	/// <summary>
	/// Token counting method to use.
	/// </summary>
	public TokenCountingMethod TokenCountingMethod { get; set; } = TokenCountingMethod.CharacterBased;

	// Chunking Strategy
	/// <summary>
	/// Strategy for determining chunk boundaries.
	/// </summary>
	public ChunkingStrategy Strategy { get; set; } = ChunkingStrategy.Structural;

	// Content Processing
	/// <summary>
	/// Preserve formatting information in chunks.
	/// </summary>
	public bool PreserveFormatting { get; set; } = false;

	/// <summary>
	/// Generate Markdown representation of content.
	/// </summary>
	public bool GenerateMarkdown { get; set; } = false;

	/// <summary>
	/// Extract keywords from content.
	/// </summary>
	public bool ExtractKeywords { get; set; } = false;

	/// <summary>
	/// Generate summaries for structural chunks.
	/// </summary>
	public bool GenerateSummaries { get; set; } = false;

	// Images
	/// <summary>
	/// Extract images from documents.
	/// </summary>
	public bool ExtractImages { get; set; } = true;

	/// <summary>
	/// Generate AI descriptions for images.
	/// </summary>
	public bool GenerateImageDescriptions { get; set; } = false;

	/// <summary>
	/// Image description provider.
	/// </summary>
	public IImageDescriptionProvider? ImageDescriptionProvider { get; set; }

	/// <summary>
	/// Maximum image size in bytes (default 10MB).
	/// </summary>
	public int MaxImageSizeBytes { get; set; } = 10 * 1024 * 1024;

	// Tables
	/// <summary>
	/// Preferred format for table serialization.
	/// </summary>
	public Models.TableSerializationFormat TableFormat { get; set; } = Models.TableSerializationFormat.Markdown;

	/// <summary>
	/// Include table headers in serialized output.
	/// </summary>
	public bool IncludeTableHeaders { get; set; } = true;

	// Metadata
	/// <summary>
	/// External hierarchy path for all chunks.
	/// </summary>
	public string? ExternalHierarchy { get; set; }

	/// <summary>
	/// Tags to apply to all chunks.
	/// </summary>
	public List<string> Tags { get; set; } = [];

	/// <summary>
	/// Source identifier for the document.
	/// </summary>
	public string? SourceId { get; set; }

	/// <summary>
	/// Custom metadata to include in all chunks.
	/// </summary>
	public Dictionary<string, object>? CustomMetadata { get; set; }

	// Output
	/// <summary>
	/// Output format (Flat, Hierarchical, or LeavesOnly).
	/// </summary>
	public OutputFormat OutputFormat { get; set; } = OutputFormat.Flat;

	/// <summary>
	/// Calculate and include quality metrics.
	/// </summary>
	public bool IncludeQualityMetrics { get; set; } = true;

	/// <summary>
	/// Validate chunks after creation.
	/// </summary>
	public bool ValidateChunks { get; set; } = true;

	// Performance
	/// <summary>
	/// Enable streaming for large documents.
	/// </summary>
	public bool EnableStreaming { get; set; } = false;

	/// <summary>
	/// Batch size for processing.
	/// </summary>
	public int BatchSize { get; set; } = 100;

	/// <summary>
	/// Maximum degree of parallelism.
	/// </summary>
	public int MaxDegreeOfParallelism { get; set; } = 1;

	/// <summary>
	/// Enable caching of chunking results.
	/// </summary>
	public bool EnableCaching { get; set; } = false;

	/// <summary>
	/// Cache provider implementation.
	/// </summary>
	public ICacheProvider? CacheProvider { get; set; }

	// Logging
	/// <summary>
	/// Logger instance for chunking operations.
	/// </summary>
	public ILogger? Logger { get; set; }

	/// <summary>
	/// Enable detailed logging.
	/// </summary>
	public bool EnableDetailedLogging { get; set; } = false;

	// LLM Provider
	/// <summary>
	/// LLM provider for summaries and keywords.
	/// </summary>
	public ILlmProvider? LlmProvider { get; set; }
}
