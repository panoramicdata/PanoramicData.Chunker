using PanoramicData.Chunker.Models;

namespace PanoramicData.Chunker.Extensions;

public static partial class ChunkQueryExtensions
{
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
