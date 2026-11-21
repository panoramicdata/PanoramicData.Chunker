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
}
