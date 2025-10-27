using PanoramicData.Chunker.Models;
using PanoramicData.Chunker.Utilities;

namespace PanoramicData.Chunker.Extensions;

/// <summary>
/// Extension methods for building and manipulating chunk trees.
/// </summary>
public static class ChunkTreeExtensions
{
	/// <summary>
	/// Builds a hierarchical tree structure by populating Children collections.
	/// </summary>
	/// <param name="chunks">The chunks to organize into a tree.</param>
	/// <returns>Root chunks with populated Children.</returns>
	public static IEnumerable<ChunkerBase> BuildTree(this IEnumerable<ChunkerBase> chunks)
	{
		var chunkList = chunks.ToList();
		HierarchyBuilder.PopulateChildren(chunkList);
		return chunkList.Where(c => !c.ParentId.HasValue);
	}

	/// <summary>
	/// Flattens a hierarchical tree back to a flat list.
	/// </summary>
	/// <param name="rootChunks">Root chunks with Children populated.</param>
	/// <returns>Flattened list of all chunks.</returns>
	public static IEnumerable<ChunkerBase> FlattenTree(this IEnumerable<ChunkerBase> rootChunks)
	{
		var result = new List<ChunkerBase>();

		foreach (var root in rootChunks)
		{
			FlattenChunkRecursive(root, result);
		}

		return result;
	}

	private static void FlattenChunkRecursive(ChunkerBase chunk, List<ChunkerBase> result)
	{
		result.Add(chunk);

		if (chunk is StructuralChunk structural)
		{
			foreach (var child in structural.Children.OrderBy(c => c.SequenceNumber))
			{
				FlattenChunkRecursive(child, result);
			}
		}
	}

	/// <summary>
	/// Traverses the tree in depth-first order.
	/// </summary>
	/// <param name="chunk">The root chunk.</param>
	/// <param name="action">Action to perform on each chunk.</param>
	public static void TraverseDepthFirst(this ChunkerBase chunk, Action<ChunkerBase> action)
	{
		action(chunk);

		if (chunk is StructuralChunk structural)
		{
			foreach (var child in structural.Children.OrderBy(c => c.SequenceNumber))
			{
				child.TraverseDepthFirst(action);
			}
		}
	}

	/// <summary>
	/// Traverses the tree in breadth-first order.
	/// </summary>
	/// <param name="chunk">The root chunk.</param>
	/// <param name="action">Action to perform on each chunk.</param>
	public static void TraverseBreadthFirst(this ChunkerBase chunk, Action<ChunkerBase> action)
	{
		var queue = new Queue<ChunkerBase>();
		queue.Enqueue(chunk);

		while (queue.Count > 0)
		{
			var current = queue.Dequeue();
			action(current);

			if (current is StructuralChunk structural)
			{
				foreach (var child in structural.Children.OrderBy(c => c.SequenceNumber))
				{
					queue.Enqueue(child);
				}
			}
		}
	}

	/// <summary>
	/// Gets the path from root to this chunk as a list of chunks.
	/// </summary>
	/// <param name="chunk">The target chunk.</param>
	/// <param name="allChunks">All chunks in the collection.</param>
	/// <returns>Path from root to chunk (inclusive).</returns>
	public static List<ChunkerBase> GetPathFromRoot(this ChunkerBase chunk, IEnumerable<ChunkerBase> allChunks)
	{
		var path = new List<ChunkerBase> { chunk };
		var chunkDict = allChunks.ToDictionary(c => c.Id);
		var currentId = chunk.ParentId;

		while (currentId.HasValue && chunkDict.TryGetValue(currentId.Value, out var parent))
		{
			path.Insert(0, parent);
			currentId = parent.ParentId;
		}

		return path;
	}

	/// <summary>
	/// Gets the hierarchical path as a string (e.g., "Section > Subsection > Paragraph").
	/// </summary>
	/// <param name="chunk">The chunk.</param>
	/// <param name="allChunks">All chunks in the collection.</param>
	/// <param name="separator">Separator between levels (default: " > ").</param>
	public static string GetHierarchyPath(this ChunkerBase chunk, IEnumerable<ChunkerBase> allChunks, string separator = " > ")
	{
		var path = chunk.GetPathFromRoot(allChunks);
		return string.Join(separator, path.Select(c => c.SpecificType));
	}

	/// <summary>
	/// Filters tree to only include branches containing chunks matching a predicate.
	/// </summary>
	/// <param name="chunks">The chunks to filter.</param>
	/// <param name="predicate">Predicate to match.</param>
	/// <returns>Filtered chunks including ancestors of matches.</returns>
	public static IEnumerable<ChunkerBase> FilterTreeByPredicate(this IEnumerable<ChunkerBase> chunks, Func<ChunkerBase, bool> predicate)
	{
		var chunkList = chunks.ToList();
		var chunkDict = chunkList.ToDictionary(c => c.Id);
		var includedIds = new HashSet<Guid>();

		// Find all chunks matching predicate
		foreach (var chunk in chunkList.Where(predicate))
		{
			// Include this chunk and all ancestors
			var current = chunk;
			while (current != null)
			{
				_ = includedIds.Add(current.Id);
				current = current.ParentId.HasValue && chunkDict.TryGetValue(current.ParentId.Value, out var parent)
					? parent
					: null;
			}
		}

		return chunkList.Where(c => includedIds.Contains(c.Id));
	}

	/// <summary>
	/// Clones a chunk tree with optionally filtering descendants.
	/// </summary>
	/// <param name="chunk">The chunk to clone.</param>
	/// <param name="allChunks">All chunks in the collection.</param>
	/// <param name="includeDescendants">Whether to include descendants.</param>
	/// <returns>List of cloned chunks.</returns>
	public static List<ChunkerBase> CloneSubtree(this ChunkerBase chunk, IEnumerable<ChunkerBase> allChunks, bool includeDescendants = true)
	{
		var result = new List<ChunkerBase> { chunk };

		if (includeDescendants)
		{
			var descendants = GetDescendantsHelper(chunk, allChunks);
			result.AddRange(descendants);
		}

		return result;
	}

	/// <summary>
	/// Counts total descendants of a chunk.
	/// </summary>
	/// <param name="chunk">The parent chunk.</param>
	/// <param name="allChunks">All chunks in the collection.</param>
	public static int CountDescendants(this ChunkerBase chunk, IEnumerable<ChunkerBase> allChunks)
		=> GetDescendantsHelper(chunk, allChunks).Count;

	/// <summary>
	/// Checks if a chunk has any descendants.
	/// </summary>
	/// <param name="chunk">The chunk to check.</param>
	/// <param name="allChunks">All chunks in the collection.</param>
	public static bool HasDescendants(this ChunkerBase chunk, IEnumerable<ChunkerBase> allChunks)
		=> allChunks.Any(c => c.ParentId == chunk.Id);

	/// <summary>
	/// Checks if a chunk is an ancestor of another chunk.
	/// </summary>
	/// <param name="potentialAncestor">The potential ancestor.</param>
	/// <param name="descendant">The potential descendant.</param>
	public static bool IsAncestorOf(this ChunkerBase potentialAncestor, ChunkerBase descendant)
		=> descendant.AncestorIds?.Contains(potentialAncestor.Id) == true;

	/// <summary>
	/// Gets the level of nesting for this chunk (0 = root, 1 = direct child of root, etc.).
	/// </summary>
	public static int GetNestingLevel(this ChunkerBase chunk)
		=> chunk.Depth;

	/// <summary>
	/// Gets all leaf nodes in a subtree.
	/// </summary>
	/// <param name="chunk">The root of the subtree.</param>
	/// <param name="allChunks">All chunks in the collection.</param>
	public static IEnumerable<ChunkerBase> GetLeafNodes(this ChunkerBase chunk, IEnumerable<ChunkerBase> allChunks)
	{
		var subtree = new List<ChunkerBase> { chunk };
		subtree.AddRange(GetDescendantsHelper(chunk, allChunks));

		var parentIds = new HashSet<Guid>(subtree.Where(c => c.ParentId.HasValue).Select(c => c.ParentId!.Value));
		return subtree.Where(c => !parentIds.Contains(c.Id));
	}

	/// <summary>
	/// Prunes branches deeper than the specified depth.
	/// </summary>
	/// <param name="chunks">The chunks to prune.</param>
	/// <param name="maxDepth">Maximum depth to keep.</param>
	public static IEnumerable<ChunkerBase> PruneAtDepth(this IEnumerable<ChunkerBase> chunks, int maxDepth)
		=> chunks.Where(c => c.Depth <= maxDepth);

	/// <summary>
	/// Groups chunks into subtrees by root chunk.
	/// </summary>
	public static Dictionary<ChunkerBase, List<ChunkerBase>> GroupBySubtree(this IEnumerable<ChunkerBase> chunks)
	{
		var chunkList = chunks.ToList();
		var roots = chunkList.Where(c => !c.ParentId.HasValue).ToList();
		var result = new Dictionary<ChunkerBase, List<ChunkerBase>>();

		foreach (var root in roots)
		{
			var subtree = new List<ChunkerBase> { root };
			subtree.AddRange(GetDescendantsHelper(root, chunkList));
			result[root] = subtree;
		}

		return result;
	}

	/// <summary>
	/// Helper method to get descendants.
	/// </summary>
	private static List<ChunkerBase> GetDescendantsHelper(ChunkerBase chunk, IEnumerable<ChunkerBase> allChunks)
	{
		var chunkList = allChunks.ToList();
		var descendants = new List<ChunkerBase>();
		var queue = new Queue<ChunkerBase>(chunkList.Where(c => c.ParentId == chunk.Id));

		while (queue.Count > 0)
		{
			var current = queue.Dequeue();
			descendants.Add(current);

			foreach (var child in chunkList.Where(c => c.ParentId == current.Id))
			{
				queue.Enqueue(child);
			}
		}

		return descendants;
	}
}
