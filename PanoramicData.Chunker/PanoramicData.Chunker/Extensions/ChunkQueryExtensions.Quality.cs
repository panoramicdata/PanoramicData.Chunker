using PanoramicData.Chunker.Models;

namespace PanoramicData.Chunker.Extensions;

public static partial class ChunkQueryExtensions
{
	/// <summary>
	/// Filters chunks with at least the specified word count.
	/// </summary>
	/// <param name="chunks">The chunks to filter.</param>
	/// <param name="minWords">Minimum word count.</param>
	public static IEnumerable<ChunkerBase> WithMinWords(this IEnumerable<ChunkerBase> chunks, int minWords)
		=> chunks.Where(c => c.QualityMetrics?.WordCount >= minWords);

	/// <summary>
	/// Filters chunks with at least the specified character count.
	/// </summary>
	/// <param name="chunks">The chunks to filter.</param>
	/// <param name="minCharacters">Minimum character count.</param>
	public static IEnumerable<ChunkerBase> WithMinCharacters(this IEnumerable<ChunkerBase> chunks, int minCharacters)
		=> chunks.Where(c => c.QualityMetrics?.CharacterCount >= minCharacters);

	/// <summary>
	/// Filters chunks that have complete sentences (no truncation).
	/// </summary>
	/// <param name="chunks">The chunks to filter.</param>
	public static IEnumerable<ChunkerBase> WithCompleteSentences(this IEnumerable<ChunkerBase> chunks)
		=> chunks.Where(c => c.QualityMetrics?.HasTruncatedSentence != true);

	/// <summary>
	/// Filters chunks that have complete tables (no truncation).
	/// </summary>
	/// <param name="chunks">The chunks to filter.</param>
	public static IEnumerable<ChunkerBase> WithCompleteTables(this IEnumerable<ChunkerBase> chunks)
		=> chunks.Where(c => c.QualityMetrics?.HasIncompleteTable != true);

	/// <summary>
	/// Filters chunks by language.
	/// </summary>
	/// <param name="chunks">The chunks to filter.</param>
	/// <param name="languageCode">The ISO 639-1 language code (e.g., "en", "fr").</param>
	public static IEnumerable<ChunkerBase> InLanguage(this IEnumerable<ChunkerBase> chunks, string languageCode)
		=> chunks.Where(c => c.Metadata?.Language?.Equals(languageCode, StringComparison.OrdinalIgnoreCase) == true);

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

	/// <summary>
	/// Filters chunks containing specific text (case-insensitive).
	/// </summary>
	/// <param name="chunks">The chunks to filter.</param>
	/// <param name="searchText">The text to search for.</param>
	public static IEnumerable<ChunkerBase> ContainingText(this IEnumerable<ChunkerBase> chunks, string searchText)
		=> chunks.OfType<ContentChunk>()
			.Where(c => c.Content?.Contains(searchText, StringComparison.OrdinalIgnoreCase) == true)
			.Cast<ChunkerBase>();

	/// <summary>
	/// Filters chunks whose content contains the specified text with case option.
	/// </summary>
	/// <param name="chunks">The chunks to filter.</param>
	/// <param name="searchText">The text to search for.</param>
	/// <param name="ignoreCase">Whether to ignore case (default: true).</param>
	public static IEnumerable<ChunkerBase> ContainingText(
		this IEnumerable<ChunkerBase> chunks,
		string searchText,
		bool ignoreCase)
	{
		var comparison = ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
		return chunks.Where(c => MatchesText(c, searchText, comparison));
	}

	private static bool MatchesText(ChunkerBase chunk, string searchText, StringComparison comparison) => chunk switch
	{
		ContentChunk content => TextContains(content.Content, searchText, comparison),
		StructuralChunk structural => TextContains(structural.Summary, searchText, comparison),
		VisualChunk visual => TextContains(visual.Caption, searchText, comparison)
			|| TextContains(visual.GeneratedDescription, searchText, comparison),
		_ => false
	};

	private static bool TextContains(string? text, string searchText, StringComparison comparison)
		=> text?.Contains(searchText, comparison) == true;

	/// <summary>
	/// Filters chunks that contain any of the specified keywords.
	/// </summary>
	/// <param name="chunks">The chunks to filter.</param>
	/// <param name="keywords">The keywords to search for.</param>
	public static IEnumerable<ChunkerBase> WithKeywords(this IEnumerable<ChunkerBase> chunks, params string[] keywords)
		=> chunks.Where(c =>
			c is ContentChunk contentChunk &&
			contentChunk.Keywords != null &&
			keywords.Any(k => contentChunk.Keywords.Contains(k, StringComparer.OrdinalIgnoreCase)));

	/// <summary>
	/// Filters chunks that have annotations.
	/// </summary>
	/// <param name="chunks">The chunks to filter.</param>
	public static IEnumerable<ChunkerBase> WithAnnotations(this IEnumerable<ChunkerBase> chunks)
		=> chunks.Where(c => c is ContentChunk contentChunk && contentChunk.Annotations?.Count > 0);

	/// <summary>
	/// Filters chunks that have annotations of a specific type.
	/// </summary>
	/// <param name="chunks">The chunks to filter.</param>
	/// <param name="annotationType">The annotation type to match.</param>
	public static IEnumerable<ChunkerBase> WithAnnotationType(
		this IEnumerable<ChunkerBase> chunks,
		AnnotationType annotationType)
		=> chunks.Where(c => c is ContentChunk contentChunk &&
			contentChunk.Annotations?.Any(a => a.Type == annotationType) == true);
}
