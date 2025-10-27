using PanoramicData.Chunker.Chunkers.Markdown;
using PanoramicData.Chunker.Models;

namespace PanoramicData.Chunker.Extensions;

/// <summary>
/// LINQ-style extension methods for querying and filtering chunk collections.
/// </summary>
public static class ChunkQueryExtensions
{
	/// <summary>
	/// Filters chunks to only content chunks.
	/// </summary>
	public static IEnumerable<ContentChunk> ContentChunks(this IEnumerable<ChunkerBase> chunks)
		=> chunks.OfType<ContentChunk>();

	/// <summary>
	/// Filters chunks to only structural chunks.
	/// </summary>
	public static IEnumerable<StructuralChunk> StructuralChunks(this IEnumerable<ChunkerBase> chunks)
		=> chunks.OfType<StructuralChunk>();

	/// <summary>
	/// Filters chunks to only visual chunks.
	/// </summary>
	public static IEnumerable<VisualChunk> VisualChunks(this IEnumerable<ChunkerBase> chunks)
		=> chunks.OfType<VisualChunk>();

	/// <summary>
	/// Filters chunks to only table chunks.
	/// </summary>
	public static IEnumerable<TableChunk> TableChunks(this IEnumerable<ChunkerBase> chunks)
		=> chunks.OfType<TableChunk>();

	/// <summary>
	/// Filters chunks to only MarkdownSectionChunk instances.
	/// </summary>
	public static IEnumerable<MarkdownSectionChunk> MarkdownSections(this IEnumerable<ChunkerBase> chunks)
		=> chunks.OfType<MarkdownSectionChunk>();

	/// <summary>
	/// Filters chunks by specific type name.
	/// </summary>
	/// <param name="chunks">The chunks to filter.</param>
	/// <param name="specificType">The specific type to match (e.g., "Heading1", "Paragraph").</param>
	public static IEnumerable<ChunkerBase> OfSpecificType(this IEnumerable<ChunkerBase> chunks, string specificType)
		=> chunks.Where(c => c.SpecificType.Equals(specificType, StringComparison.OrdinalIgnoreCase));

	/// <summary>
	/// Filters chunks to only those at a specific depth in the hierarchy.
	/// </summary>
	/// <param name="chunks">The chunks to filter.</param>
	/// <param name="depth">The depth level (0 = root).</param>
	public static IEnumerable<ChunkerBase> AtDepth(this IEnumerable<ChunkerBase> chunks, int depth)
		=> chunks.Where(c => c.Depth == depth);

	/// <summary>
	/// Filters chunks to those within a specific depth range (inclusive).
	/// </summary>
	/// <param name="chunks">The chunks to filter.</param>
	/// <param name="minDepth">Minimum depth (inclusive).</param>
	/// <param name="maxDepth">Maximum depth (inclusive).</param>
	public static IEnumerable<ChunkerBase> WithinDepthRange(this IEnumerable<ChunkerBase> chunks, int minDepth, int maxDepth)
		=> chunks.Where(c => c.Depth >= minDepth && c.Depth <= maxDepth);

	/// <summary>
	/// Gets root chunks (chunks with no parent).
	/// </summary>
	public static IEnumerable<ChunkerBase> RootChunks(this IEnumerable<ChunkerBase> chunks)
		=> chunks.Where(c => !c.ParentId.HasValue);

	/// <summary>
	/// Gets leaf chunks (chunks with no children).
	/// </summary>
	public static IEnumerable<ChunkerBase> LeafChunks(this IEnumerable<ChunkerBase> chunks)
	{
		var chunkList = chunks.ToList();
		var parentIds = new HashSet<Guid>(chunkList.Where(c => c.ParentId.HasValue).Select(c => c.ParentId!.Value));
		return chunkList.Where(c => !parentIds.Contains(c.Id));
	}

	/// <summary>
	/// Gets all direct children of a chunk.
	/// </summary>
	/// <param name="chunk">The parent chunk.</param>
	/// <param name="allChunks">All chunks in the collection.</param>
	public static IEnumerable<ChunkerBase> GetChildren(this ChunkerBase chunk, IEnumerable<ChunkerBase> allChunks)
		=> allChunks.Where(c => c.ParentId == chunk.Id);

	/// <summary>
	/// Gets the parent of a chunk.
	/// </summary>
	/// <param name="chunk">The child chunk.</param>
	/// <param name="allChunks">All chunks in the collection.</param>
	public static ChunkerBase? GetParent(this ChunkerBase chunk, IEnumerable<ChunkerBase> allChunks)
		=> chunk.ParentId.HasValue ? allChunks.FirstOrDefault(c => c.Id == chunk.ParentId.Value) : null;

	/// <summary>
	/// Gets all ancestors of a chunk (from immediate parent to root).
	/// </summary>
	/// <param name="chunk">The chunk.</param>
	/// <param name="allChunks">All chunks in the collection.</param>
	public static IEnumerable<ChunkerBase> GetAncestors(this ChunkerBase chunk, IEnumerable<ChunkerBase> allChunks)
	{
		var chunkList = allChunks.ToList();
		var ancestors = new List<ChunkerBase>();
		var current = chunk.GetParent(chunkList);

		while (current != null)
		{
			ancestors.Add(current);
			current = current.GetParent(chunkList);
		}

		return ancestors;
	}

	/// <summary>
	/// Gets all descendants of a chunk (all children recursively).
	/// </summary>
	/// <param name="chunk">The parent chunk.</param>
	/// <param name="allChunks">All chunks in the collection.</param>
	public static IEnumerable<ChunkerBase> GetDescendants(this ChunkerBase chunk, IEnumerable<ChunkerBase> allChunks)
	{
		var chunkList = allChunks.ToList();
		var descendants = new List<ChunkerBase>();
		var queue = new Queue<ChunkerBase>(chunk.GetChildren(chunkList));

		while (queue.Count > 0)
		{
			var current = queue.Dequeue();
			descendants.Add(current);

			foreach (var child in current.GetChildren(chunkList))
			{
				queue.Enqueue(child);
			}
		}

		return descendants;
	}

	/// <summary>
	/// Gets all siblings of a chunk (chunks with the same parent).
	/// </summary>
	/// <param name="chunk">The chunk.</param>
	/// <param name="allChunks">All chunks in the collection.</param>
	/// <param name="includeSelf">Whether to include the chunk itself in the results.</param>
	public static IEnumerable<ChunkerBase> GetSiblings(this ChunkerBase chunk, IEnumerable<ChunkerBase> allChunks, bool includeSelf = false)
	{
		var siblings = allChunks.Where(c => c.ParentId == chunk.ParentId);
		return includeSelf ? siblings : siblings.Where(c => c.Id != chunk.Id);
	}

	/// <summary>
	/// Filters chunks by tag.
	/// </summary>
	/// <param name="chunks">The chunks to filter.</param>
	/// <param name="tag">The tag to match.</param>
	public static IEnumerable<ChunkerBase> WithTag(this IEnumerable<ChunkerBase> chunks, string tag)
		=> chunks.Where(c => c.Metadata?.Tags?.Contains(tag, StringComparer.OrdinalIgnoreCase) == true);

	/// <summary>
	/// Filters chunks that have any of the specified tags.
	/// </summary>
	/// <param name="chunks">The chunks to filter.</param>
	/// <param name="tags">The tags to match.</param>
	public static IEnumerable<ChunkerBase> WithAnyTag(this IEnumerable<ChunkerBase> chunks, params string[] tags)
		=> chunks.Where(c => c.Metadata?.Tags?.Any(t => tags.Contains(t, StringComparer.OrdinalIgnoreCase)) == true);

	/// <summary>
	/// Filters chunks that have all of the specified tags.
	/// </summary>
	/// <param name="chunks">The chunks to filter.</param>
	/// <param name="tags">The tags that must all be present.</param>
	public static IEnumerable<ChunkerBase> WithAllTags(this IEnumerable<ChunkerBase> chunks, params string[] tags)
		=> chunks.Where(c => tags.All(tag => c.Metadata?.Tags?.Contains(tag, StringComparer.OrdinalIgnoreCase) == true));

	/// <summary>
	/// Filters chunks on a specific page number.
	/// </summary>
	/// <param name="chunks">The chunks to filter.</param>
	/// <param name="pageNumber">The page number.</param>
	public static IEnumerable<ChunkerBase> OnPage(this IEnumerable<ChunkerBase> chunks, int pageNumber)
		=> chunks.Where(c => c.Metadata?.PageNumber == pageNumber);

	/// <summary>
	/// Filters chunks within a page range.
	/// </summary>
	/// <param name="chunks">The chunks to filter.</param>
	/// <param name="startPage">Starting page (inclusive).</param>
	/// <param name="endPage">Ending page (inclusive).</param>
	public static IEnumerable<ChunkerBase> OnPageRange(this IEnumerable<ChunkerBase> chunks, int startPage, int endPage)
		=> chunks.Where(c => c.Metadata?.PageNumber >= startPage && c.Metadata?.PageNumber <= endPage);

	/// <summary>
	/// Filters chunks whose hierarchy path starts with the specified prefix.
	/// </summary>
	/// <param name="chunks">The chunks to filter.</param>
	/// <param name="hierarchyPrefix">The hierarchy prefix to match.</param>
	public static IEnumerable<ChunkerBase> InHierarchy(this IEnumerable<ChunkerBase> chunks, string hierarchyPrefix)
		=> chunks.Where(c => c.Metadata?.Hierarchy?.StartsWith(hierarchyPrefix, StringComparison.OrdinalIgnoreCase) == true);

	/// <summary>
	/// Filters chunks whose external hierarchy starts with the specified prefix.
	/// </summary>
	/// <param name="chunks">The chunks to filter.</param>
	/// <param name="externalHierarchyPrefix">The external hierarchy prefix to match.</param>
	public static IEnumerable<ChunkerBase> InExternalHierarchy(
		this IEnumerable<ChunkerBase> chunks,
		string externalHierarchyPrefix)
		=> chunks.Where(c => c.Metadata?.ExternalHierarchy?.StartsWith(externalHierarchyPrefix, StringComparison.OrdinalIgnoreCase) == true);

	/// <summary>
	/// Filters chunks by source ID.
	/// </summary>
	/// <param name="chunks">The chunks to filter.</param>
	/// <param name="sourceId">The source ID.</param>
	public static IEnumerable<ChunkerBase> FromSource(this IEnumerable<ChunkerBase> chunks, string sourceId)
		=> chunks.Where(c => c.Metadata?.SourceId?.Equals(sourceId, StringComparison.OrdinalIgnoreCase) == true);

	/// <summary>
	/// Filters chunks by document type.
	/// </summary>
	/// <param name="chunks">The chunks to filter.</param>
	/// <param name="documentType">The document type to match.</param>
	public static IEnumerable<ChunkerBase> OfDocumentType(this IEnumerable<ChunkerBase> chunks, string documentType)
		=> chunks.Where(c => c.Metadata?.DocumentType?.Equals(documentType, StringComparison.OrdinalIgnoreCase) == true);

	/// <summary>
	/// Filters chunks with a specific sheet name (XLSX documents).
	/// </summary>
	/// <param name="chunks">The chunks to filter.</param>
	/// <param name="sheetName">The sheet name.</param>
	public static IEnumerable<ChunkerBase> OnSheet(this IEnumerable<ChunkerBase> chunks, string sheetName)
		=> chunks.Where(c => c.Metadata?.SheetName?.Equals(sheetName, StringComparison.OrdinalIgnoreCase) == true);

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
		return chunks.Where(c =>
		{
			if (c is ContentChunk contentChunk)
			{
				return contentChunk.Content?.Contains(searchText, comparison) == true;
			}
			if (c is StructuralChunk structuralChunk)
			{
				return structuralChunk.Summary?.Contains(searchText, comparison) == true;
			}
			if (c is VisualChunk visualChunk)
			{
				return (visualChunk.Caption?.Contains(searchText, comparison) == true) ||
					   (visualChunk.GeneratedDescription?.Contains(searchText, comparison) == true);
			}
			return false;
		});
	}

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

	/// <summary>
	/// Gets the root ancestor of a chunk.
	/// </summary>
	/// <param name="chunk">The chunk.</param>
	/// <param name="allChunks">All available chunks.</param>
	public static ChunkerBase? GetRoot(this ChunkerBase chunk, IEnumerable<ChunkerBase> allChunks)
	{
		if (!chunk.ParentId.HasValue)
		{
			return chunk;
		}

		var ancestors = chunk.GetAncestors(allChunks).ToList();
		return ancestors.Count > 0 ? ancestors[^1] : null;
	}

	/// <summary>
	/// Gets the next chunk in document order.
	/// </summary>
	/// <param name="chunk">The current chunk.</param>
	/// <param name="allChunks">All available chunks.</param>
	public static ChunkerBase? GetNext(this ChunkerBase chunk, IEnumerable<ChunkerBase> allChunks)
		=> allChunks
			.Where(c => c.SequenceNumber > chunk.SequenceNumber)
			.OrderBy(c => c.SequenceNumber)
			.FirstOrDefault();

	/// <summary>
	/// Gets the previous chunk in document order.
	/// </summary>
	/// <param name="chunk">The current chunk.</param>
	/// <param name="allChunks">All available chunks.</param>
	public static ChunkerBase? GetPrevious(this ChunkerBase chunk, IEnumerable<ChunkerBase> allChunks)
		=> allChunks
			.Where(c => c.SequenceNumber < chunk.SequenceNumber)
			.OrderByDescending(c => c.SequenceNumber)
			.FirstOrDefault();

	/// <summary>
	/// Gets a range of chunks around the current chunk.
	/// </summary>
	/// <param name="chunk">The center chunk.</param>
	/// <param name="allChunks">All available chunks.</param>
	/// <param name="before">Number of chunks before.</param>
	/// <param name="after">Number of chunks after.</param>
	/// <param name="includeSelf">Whether to include the chunk itself.</param>
	public static IEnumerable<ChunkerBase> GetContext(
		this ChunkerBase chunk,
		IEnumerable<ChunkerBase> allChunks,
		int before = 1,
		int after = 1,
		bool includeSelf = true)
	{
		var chunkList = allChunks.OrderBy(c => c.SequenceNumber).ToList();
		var currentIndex = chunkList.FindIndex(c => c.Id == chunk.Id);

		if (currentIndex < 0)
		{
			return [];
		}

		var startIndex = Math.Max(0, currentIndex - before);
		var endIndex = Math.Min(chunkList.Count - 1, currentIndex + after);

		var result = chunkList.Skip(startIndex).Take(endIndex - startIndex + 1).ToList();

		if (!includeSelf)
		{
			_ = result.RemoveAll(c => c.Id == chunk.Id);
		}

		return result;
	}

	/// <summary>
	/// Orders chunks by their sequence number.
	/// </summary>
	public static IOrderedEnumerable<ChunkerBase> OrderBySequence(this IEnumerable<ChunkerBase> chunks)
		=> chunks.OrderBy(c => c.SequenceNumber);

	/// <summary>
	/// Orders chunks by their depth in the hierarchy.
	/// </summary>
	public static IOrderedEnumerable<ChunkerBase> OrderByDepth(this IEnumerable<ChunkerBase> chunks)
		=> chunks.OrderBy(c => c.Depth);

	/// <summary>
	/// Orders chunks by token count (ascending).
	/// </summary>
	public static IOrderedEnumerable<ChunkerBase> OrderByTokenCount(this IEnumerable<ChunkerBase> chunks)
		=> chunks.OrderBy(c => c.QualityMetrics?.TokenCount ?? 0);

	/// <summary>
	/// Orders chunks by token count (descending).
	/// </summary>
	public static IOrderedEnumerable<ChunkerBase> OrderByTokenCountDescending(this IEnumerable<ChunkerBase> chunks)
		=> chunks.OrderByDescending(c => c.QualityMetrics?.TokenCount ?? 0);

	/// <summary>
	/// Groups chunks by their specific type.
	/// </summary>
	public static IEnumerable<IGrouping<string, ChunkerBase>> GroupBySpecificType(this IEnumerable<ChunkerBase> chunks)
		=> chunks.GroupBy(c => c.SpecificType);

	/// <summary>
	/// Groups chunks by their depth level.
	/// </summary>
	public static IEnumerable<IGrouping<int, ChunkerBase>> GroupByDepth(this IEnumerable<ChunkerBase> chunks)
		=> chunks.GroupBy(c => c.Depth);

	/// <summary>
	/// Groups chunks by their parent ID.
	/// </summary>
	public static IEnumerable<IGrouping<Guid?, ChunkerBase>> GroupByParent(this IEnumerable<ChunkerBase> chunks)
		=> chunks.GroupBy(c => c.ParentId);

	/// <summary>
	/// Groups chunks by page number.
	/// </summary>
	/// <param name="chunks">The chunks to group.</param>
	public static IEnumerable<IGrouping<int?, ChunkerBase>> GroupByPage(this IEnumerable<ChunkerBase> chunks)
		=> chunks.GroupBy(c => c.Metadata?.PageNumber);

	/// <summary>
	/// Groups chunks by their specific type (alias for GroupBySpecificType for consistency).
	/// </summary>
	public static IEnumerable<IGrouping<string, ChunkerBase>> GroupByType(this IEnumerable<ChunkerBase> chunks)
		=> chunks.GroupBySpecificType();

	/// <summary>
	/// Gets aggregate statistics for a chunk collection.
	/// </summary>
	public static ChunkCollectionStats GetStatistics(this IEnumerable<ChunkerBase> chunks)
	{
		var chunkList = chunks.ToList();

		return new ChunkCollectionStats
		{
			TotalChunks = chunkList.Count,
			StructuralChunks = chunkList.OfType<StructuralChunk>().Count(),
			ContentChunks = chunkList.OfType<ContentChunk>().Count(),
			VisualChunks = chunkList.OfType<VisualChunk>().Count(),
			TableChunks = chunkList.OfType<TableChunk>().Count(),
			MaxDepth = chunkList.Count != 0 ? chunkList.Max(c => c.Depth) : 0,
			AverageTokens = chunkList.Count != 0 ? chunkList.Average(c => c.QualityMetrics?.TokenCount ?? 0) : 0,
			TotalTokens = chunkList.Sum(c => c.QualityMetrics?.TokenCount ?? 0),
			AverageSemanticCompleteness = chunkList.Count != 0
				? chunkList.Average(c => c.QualityMetrics?.SemanticCompleteness ?? 0)
				: 0
		};
	}
}

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
