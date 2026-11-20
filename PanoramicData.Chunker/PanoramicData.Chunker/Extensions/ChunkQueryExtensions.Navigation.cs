using System.Runtime.InteropServices;
using PanoramicData.Chunker.Models;

namespace PanoramicData.Chunker.Extensions;

public static partial class ChunkQueryExtensions
{
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
		[Optional][DefaultParameterValue(1)] int before,
		[Optional][DefaultParameterValue(1)] int after,
		[Optional][DefaultParameterValue(true)] bool includeSelf)
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
		var tokens = chunkList.Select(c => c.QualityMetrics?.TokenCount ?? 0).ToList();

		return new ChunkCollectionStats
		{
			TotalChunks = chunkList.Count,
			StructuralChunks = chunkList.OfType<StructuralChunk>().Count(),
			ContentChunks = chunkList.OfType<ContentChunk>().Count(),
			VisualChunks = chunkList.OfType<VisualChunk>().Count(),
			TableChunks = chunkList.OfType<TableChunk>().Count(),
			MaxDepth = chunkList.Select(c => c.Depth).DefaultIfEmpty(0).Max(),
			AverageTokens = tokens.DefaultIfEmpty(0).Average(),
			TotalTokens = tokens.Sum(),
			AverageSemanticCompleteness = chunkList.Select(c => c.QualityMetrics?.SemanticCompleteness ?? 0).DefaultIfEmpty(0).Average()
		};
	}
}
