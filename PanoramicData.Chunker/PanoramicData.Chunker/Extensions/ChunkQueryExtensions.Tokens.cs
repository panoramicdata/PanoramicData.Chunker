using PanoramicData.Chunker.Models;

namespace PanoramicData.Chunker.Extensions;

public static partial class ChunkQueryExtensions
{
	/// <summary>
	/// Filters chunks with a minimum token count.
	/// </summary>
	/// <param name="chunks">The chunks to filter.</param>
	/// <param name="minTokens">Minimum number of tokens.</param>
	public static IEnumerable<ChunkerBase> WithMinTokens(this IEnumerable<ChunkerBase> chunks, int minTokens)
		=> chunks.Where(c => (c.QualityMetrics?.TokenCount ?? 0) >= minTokens);

	/// <summary>
	/// Filters chunks with a maximum token count.
	/// </summary>
	/// <param name="chunks">The chunks to filter.</param>
	/// <param name="maxTokens">Maximum number of tokens.</param>
	public static IEnumerable<ChunkerBase> WithMaxTokens(this IEnumerable<ChunkerBase> chunks, int maxTokens)
		=> chunks.Where(c => (c.QualityMetrics?.TokenCount ?? 0) <= maxTokens);

	/// <summary>
	/// Filters chunks within a token count range.
	/// </summary>
	/// <param name="chunks">The chunks to filter.</param>
	/// <param name="minTokens">Minimum tokens (inclusive).</param>
	/// <param name="maxTokens">Maximum tokens (inclusive).</param>
	public static IEnumerable<ChunkerBase> WithTokenRange(this IEnumerable<ChunkerBase> chunks, int minTokens, int maxTokens)
		=> chunks.Where(c =>
		{
			var tokenCount = c.QualityMetrics?.TokenCount ?? 0;
			return tokenCount >= minTokens && tokenCount <= maxTokens;
		});

	/// <summary>
	/// Filters chunks with a minimum semantic completeness score.
	/// </summary>
	/// <param name="chunks">The chunks to filter.</param>
	/// <param name="minScore">Minimum completeness score (0.0 to 1.0).</param>
	public static IEnumerable<ChunkerBase> WithMinSemanticCompleteness(this IEnumerable<ChunkerBase> chunks, double minScore)
		=> chunks.Where(c => (c.QualityMetrics?.SemanticCompleteness ?? 0) >= minScore);

	/// <summary>
	/// Excludes chunks that were split from larger chunks.
	/// </summary>
	public static IEnumerable<ChunkerBase> ExcludeSplitChunks(this IEnumerable<ChunkerBase> chunks)
		=> chunks.Where(c => c.QualityMetrics?.WasSplit != true);

	/// <summary>
	/// Filters chunks that were split from larger chunks.
	/// </summary>
	public static IEnumerable<ChunkerBase> OnlySplitChunks(this IEnumerable<ChunkerBase> chunks)
		=> chunks.Where(c => c.QualityMetrics?.WasSplit == true);
}
