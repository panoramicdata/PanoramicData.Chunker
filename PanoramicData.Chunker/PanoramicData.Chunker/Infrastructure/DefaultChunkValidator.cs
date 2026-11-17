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

		CheckOrphanedChunks(result, chunkList, chunkDictionary);
		CheckCircularReferences(result, chunkList, chunkDictionary);
		CheckDepths(result, chunkList, chunkDictionary);

		return Task.FromResult(result);
	}

	private static void CheckOrphanedChunks(ValidationResult result, List<ChunkerBase> chunkList, Dictionary<Guid, ChunkerBase> chunkDictionary)
	{
		var orphanedChunks = chunkList
			.Where(c => c.ParentId.HasValue && !chunkDictionary.ContainsKey(c.ParentId.Value))
			.ToList();

		if (orphanedChunks.Count == 0)
		{
			return;
		}

		result.HasOrphanedChunks = true;
		result.IsValid = false;
		foreach (var chunk in orphanedChunks)
		{
			AddIssue(result, ValidationSeverity.Error, $"Chunk references non-existent parent: {chunk.ParentId}", chunk, "ORPHANED_CHUNK");
		}
	}

	private static void CheckCircularReferences(ValidationResult result, List<ChunkerBase> chunkList, Dictionary<Guid, ChunkerBase> chunkDictionary)
	{
		foreach (var chunk in chunkList.Where(c => HasCircularReference(c, chunkDictionary, [])))
		{
			result.HasCircularReferences = true;
			result.IsValid = false;
			AddIssue(result, ValidationSeverity.Critical, "Circular reference detected in chunk hierarchy", chunk, "CIRCULAR_REFERENCE");
		}
	}

	private static void CheckDepths(ValidationResult result, List<ChunkerBase> chunkList, Dictionary<Guid, ChunkerBase> chunkDictionary)
	{
		foreach (var chunk in chunkList)
		{
			var actualDepth = CalculateActualDepth(chunk, chunkDictionary);
			if (actualDepth == chunk.Depth)
			{
				continue;
			}

			result.HasInvalidHierarchy = true;
			result.IsValid = false;
			AddIssue(result, ValidationSeverity.Warning, $"Chunk depth mismatch. Expected: {actualDepth}, Actual: {chunk.Depth}", chunk, "DEPTH_MISMATCH");
		}
	}

	private static void AddIssue(ValidationResult result, ValidationSeverity severity, string message, ChunkerBase chunk, string code)
		=> result.Issues.Add(new ValidationIssue
		{
			Severity = severity,
			Message = message,
			ChunkId = chunk.Id,
			Code = code
		});
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
