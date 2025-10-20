namespace PanoramicData.Chunker.Models;

/// <summary>
/// Quality and size metrics for chunks.
/// </summary>
public class ChunkQualityMetrics
{
	/// <summary>
	/// Number of tokens (using the configured token counter).
	/// </summary>
	public int TokenCount { get; set; }

	/// <summary>
	/// Number of characters.
	/// </summary>
	public int CharacterCount { get; set; }

	/// <summary>
	/// Number of words.
	/// </summary>
	public int WordCount { get; set; }

	/// <summary>
	/// Semantic completeness score (0-1).
	/// 1.0 = chunk contains complete sentences/thoughts.
	/// </summary>
	public double SemanticCompleteness { get; set; } = 1.0;

	/// <summary>
	/// Indicates if this chunk contains an incomplete table (truncated due to size limits).
	/// </summary>
	public bool HasIncompleteTable { get; set; }

	/// <summary>
	/// Indicates if the chunk ends with a truncated sentence.
	/// </summary>
	public bool HasTruncatedSentence { get; set; }

	/// <summary>
	/// Indicates if this chunk was split from a larger parent due to size constraints.
	/// </summary>
	public bool WasSplit { get; set; }
}
