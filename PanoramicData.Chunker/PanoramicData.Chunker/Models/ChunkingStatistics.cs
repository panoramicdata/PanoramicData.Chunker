namespace PanoramicData.Chunker.Models;

/// <summary>
/// Statistics about a chunking operation.
/// </summary>
public class ChunkingStatistics
{
	/// <summary>
	/// Total number of chunks created.
	/// </summary>
	public int TotalChunks { get; set; }

	/// <summary>
	/// Number of structural chunks.
	/// </summary>
	public int StructuralChunks { get; set; }

	/// <summary>
	/// Number of content chunks.
	/// </summary>
	public int ContentChunks { get; set; }

	/// <summary>
	/// Number of visual chunks.
	/// </summary>
	public int VisualChunks { get; set; }

	/// <summary>
	/// Number of table chunks.
	/// </summary>
	public int TableChunks { get; set; }

	/// <summary>
	/// Maximum hierarchical depth.
	/// </summary>
	public int MaxDepth { get; set; }

	/// <summary>
	/// Total processing time.
	/// </summary>
	public TimeSpan ProcessingTime { get; set; }

	/// <summary>
	/// Distribution of chunk types by SpecificType.
	/// </summary>
	public Dictionary<string, int> ChunkTypeDistribution { get; set; } = [];

	/// <summary>
	/// Total number of tokens across all chunks.
	/// </summary>
	public int TotalTokens { get; set; }

	/// <summary>
	/// Average tokens per chunk.
	/// </summary>
	public int AverageTokensPerChunk { get; set; }

	/// <summary>
	/// Maximum tokens in a single chunk.
	/// </summary>
	public int MaxTokensInChunk { get; set; }

	/// <summary>
	/// Minimum tokens in a single chunk.
	/// </summary>
	public int MinTokensInChunk { get; set; }
}
