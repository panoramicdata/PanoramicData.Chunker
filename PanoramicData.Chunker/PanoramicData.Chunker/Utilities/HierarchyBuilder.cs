using PanoramicData.Chunker.Models;

namespace PanoramicData.Chunker.Utilities;

/// <summary>
/// Utilities for building and managing chunk hierarchies.
/// </summary>
public static class HierarchyBuilder
{
	/// <summary>
	/// Build hierarchy information (Depth, AncestorIds) for all chunks.
	/// </summary>
	/// <param name="chunks">The chunks to process.</param>
	public static void BuildHierarchy(IEnumerable<ChunkerBase> chunks)
	{
		var chunkList = chunks.ToList();
		var chunkDictionary = chunkList.ToDictionary(c => c.Id);

		foreach (var chunk in chunkList)
		{
			// Calculate depth and ancestor IDs
			var (depth, ancestorIds) = CalculateHierarchyInfo(chunk, chunkDictionary);
			chunk.Depth = depth;
			chunk.AncestorIds = ancestorIds;
		}
	}

	/// <summary>
	/// Calculate depth and ancestor IDs for a chunk.
	/// </summary>
	private static (int depth, Guid[] ancestorIds) CalculateHierarchyInfo(
		ChunkerBase chunk,
		Dictionary<Guid, ChunkerBase> allChunks)
	{
		if (!chunk.ParentId.HasValue)
		{
			return (0, []);
		}

		var ancestors = new List<Guid>();
		var currentDepth = 0;
		var currentId = chunk.ParentId;

		// Traverse up the hierarchy
		while (currentId.HasValue)
		{
			if (!allChunks.TryGetValue(currentId.Value, out var parent))
			{
				// Parent not found - orphaned chunk
				break;
			}

			ancestors.Insert(0, currentId.Value); // Insert at beginning to maintain order
			currentDepth++;
			currentId = parent.ParentId;

			// Safety check for circular references
			if (currentDepth > 1000)
			{
				throw new InvalidOperationException($"Circular reference detected in chunk hierarchy for chunk {chunk.Id}");
			}
		}

		return (currentDepth, [.. ancestors]);
	}

	/// <summary>
	/// Populate Children collections for hierarchical output.
	/// </summary>
	/// <param name="chunks">The chunks to process.</param>
	public static void PopulateChildren(IEnumerable<ChunkerBase> chunks)
	{
		var chunkList = chunks.ToList();
		var chunkDictionary = chunkList.ToDictionary(c => c.Id);

		// Clear existing children
		foreach (var chunk in chunkList.OfType<StructuralChunk>())
		{
			chunk.Children.Clear();
		}

		// Populate children
		foreach (var chunk in chunkList)
		{
			if (chunk.ParentId.HasValue && chunkDictionary.TryGetValue(chunk.ParentId.Value, out var parent))
			{
				if (parent is StructuralChunk structuralParent)
				{
					structuralParent.Children.Add(chunk);
				}
			}
		}
	}

	/// <summary>
	/// Get root chunks (chunks with no parent).
	/// </summary>
	public static IEnumerable<ChunkerBase> GetRootChunks(IEnumerable<ChunkerBase> chunks)
	{
		return chunks.Where(c => !c.ParentId.HasValue);
	}

	/// <summary>
	/// Get leaf chunks (chunks with no children).
	/// </summary>
	public static IEnumerable<ChunkerBase> GetLeafChunks(IEnumerable<ChunkerBase> chunks)
	{
		var chunkList = chunks.ToList();
		var parentIds = new HashSet<Guid>(chunkList.Where(c => c.ParentId.HasValue).Select(c => c.ParentId!.Value));
		
		return chunkList.Where(c => !parentIds.Contains(c.Id));
	}

	/// <summary>
	/// Validate hierarchy integrity.
	/// </summary>
	public static bool ValidateHierarchy(IEnumerable<ChunkerBase> chunks, out List<string> errors)
	{
		errors = [];
		var chunkList = chunks.ToList();
		var chunkDictionary = chunkList.ToDictionary(c => c.Id);

		foreach (var chunk in chunkList)
		{
			// Check for orphaned chunks
			if (chunk.ParentId.HasValue && !chunkDictionary.ContainsKey(chunk.ParentId.Value))
			{
				errors.Add($"Chunk {chunk.Id} references non-existent parent {chunk.ParentId.Value}");
			}

			// Verify depth matches calculated depth
			var (calculatedDepth, _) = CalculateHierarchyInfo(chunk, chunkDictionary);
			if (chunk.Depth != calculatedDepth)
			{
				errors.Add($"Chunk {chunk.Id} has incorrect depth. Expected: {calculatedDepth}, Actual: {chunk.Depth}");
			}
		}

		return errors.Count == 0;
	}
}
