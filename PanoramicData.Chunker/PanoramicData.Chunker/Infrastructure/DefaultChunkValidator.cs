using PanoramicData.Chunker.Interfaces;
using PanoramicData.Chunker.Models;

namespace PanoramicData.Chunker.Infrastructure;

/// <summary>
/// Default implementation of chunk validator.
/// </summary>
public class DefaultChunkValidator : IChunkValidator
{
	/// <summary>
	/// Validate a collection of chunks.
	/// </summary>
	public Task<ValidationResult> ValidateAsync(
		IEnumerable<ChunkerBase> chunks,
		CancellationToken cancellationToken)
	{
		var result = new ValidationResult();
		var chunkList = chunks.ToList();
		var chunkDictionary = chunkList.ToDictionary(c => c.Id);

		// Check for orphaned chunks (ParentId references non-existent chunk)
		var orphanedChunks = chunkList
			.Where(c => c.ParentId.HasValue && !chunkDictionary.ContainsKey(c.ParentId.Value))
			.ToList();

		if (orphanedChunks.Count != 0)
		{
			result.HasOrphanedChunks = true;
			result.IsValid = false;
			foreach (var chunk in orphanedChunks)
			{
				result.Issues.Add(new ValidationIssue
				{
					Severity = ValidationSeverity.Error,
					Message = $"Chunk references non-existent parent: {chunk.ParentId}",
					ChunkId = chunk.Id,
					Code = "ORPHANED_CHUNK"
				});
			}
		}

		// Check for circular references
		foreach (var chunk in chunkList)
		{
			if (HasCircularReference(chunk, chunkDictionary, []))
			{
				result.HasCircularReferences = true;
				result.IsValid = false;
				result.Issues.Add(new ValidationIssue
				{
					Severity = ValidationSeverity.Critical,
					Message = "Circular reference detected in chunk hierarchy",
					ChunkId = chunk.Id,
					Code = "CIRCULAR_REFERENCE"
				});
			}
		}

		// Check hierarchy integrity (Depth matches actual depth)
		foreach (var chunk in chunkList)
		{
			var actualDepth = CalculateActualDepth(chunk, chunkDictionary);
			if (actualDepth != chunk.Depth)
			{
				result.HasInvalidHierarchy = true;
				result.IsValid = false;
				result.Issues.Add(new ValidationIssue
				{
					Severity = ValidationSeverity.Warning,
					Message = $"Chunk depth mismatch. Expected: {actualDepth}, Actual: {chunk.Depth}",
					ChunkId = chunk.Id,
					Code = "DEPTH_MISMATCH"
				});
			}
		}

		return Task.FromResult(result);
	}

	private static bool HasCircularReference(
		ChunkerBase chunk,
		Dictionary<Guid, ChunkerBase> allChunks,
		HashSet<Guid> visited)
	{
		if (visited.Contains(chunk.Id))
		{
			return true;
		}

		_ = visited.Add(chunk.Id);

		if (chunk.ParentId.HasValue && allChunks.TryGetValue(chunk.ParentId.Value, out var parent))
		{
			return HasCircularReference(parent, allChunks, visited);
		}

		return false;
	}

	private static int CalculateActualDepth(ChunkerBase chunk, Dictionary<Guid, ChunkerBase> allChunks)
	{
		var depth = 0;
		var current = chunk;

		while (current.ParentId.HasValue && allChunks.TryGetValue(current.ParentId.Value, out var parent))
		{
			depth++;
			current = parent;

			// Safety check for circular references
			if (depth > 1000)
			{
				return -1; // Invalid depth
			}
		}

		return depth;
	}
}
