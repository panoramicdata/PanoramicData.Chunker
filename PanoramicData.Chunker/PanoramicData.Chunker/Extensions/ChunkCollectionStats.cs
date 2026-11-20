using PanoramicData.Chunker.Models;

namespace PanoramicData.Chunker.Extensions;

/// <summary>
/// Statistics for a chunk collection.
/// </summary>
public class ChunkCollectionStats
{
	/// <summary>
	/// Total number of chunks.
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
	/// Maximum depth in the hierarchy.
	/// </summary>
	public int MaxDepth { get; set; }

	/// <summary>
	/// Average tokens per chunk.
	/// </summary>
	public double AverageTokens { get; set; }

	/// <summary>
	/// Total tokens across all chunks.
	/// </summary>
	public int TotalTokens { get; set; }

	/// <summary>
	/// Average semantic completeness score.
	/// </summary>
	public double AverageSemanticCompleteness { get; set; }
}
