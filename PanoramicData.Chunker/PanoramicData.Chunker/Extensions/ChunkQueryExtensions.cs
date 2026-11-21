using System.Runtime.InteropServices;
using PanoramicData.Chunker.Chunkers.Markdown;
using PanoramicData.Chunker.Models;
using PanoramicData.Chunker.Utilities;

namespace PanoramicData.Chunker.Extensions;

/// <summary>
/// LINQ-style extension methods for querying and filtering chunk collections.
/// </summary>
public static partial class ChunkQueryExtensions
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
		=> HierarchyBuilder.GetRootChunks(chunks);

	/// <summary>
	/// Gets leaf chunks (chunks with no children).
	/// </summary>
	public static IEnumerable<ChunkerBase> LeafChunks(this IEnumerable<ChunkerBase> chunks)
		=> HierarchyBuilder.GetLeafChunks(chunks);

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
	public static IEnumerable<ChunkerBase> GetSiblings(this ChunkerBase chunk, IEnumerable<ChunkerBase> allChunks, [Optional][DefaultParameterValue(false)] bool includeSelf)
	{
		var siblings = allChunks.Where(c => c.ParentId == chunk.ParentId);
		return includeSelf ? siblings : siblings.Where(c => c.Id != chunk.Id);
	}
}
