using PanoramicData.Chunker.Configuration;
using PanoramicData.Chunker.Models;

namespace PanoramicData.Chunker.Utilities;

/// <summary>
/// Shared statistics, validation and result construction used by the document chunkers.
/// </summary>
internal static class ChunkResultHelper
{
	public static ChunkingStatistics CalculateStatistics(List<ChunkerBase> chunks, DateTime startTime)
	{
		var tokens = chunks.Select(c => c.QualityMetrics?.TokenCount ?? 0).ToList();
		var hasChunks = tokens.Count != 0;

		return new ChunkingStatistics
		{
			TotalChunks = chunks.Count,
			StructuralChunks = chunks.OfType<StructuralChunk>().Count(),
			ContentChunks = chunks.OfType<ContentChunk>().Count(),
			VisualChunks = chunks.OfType<VisualChunk>().Count(),
			TableChunks = chunks.OfType<TableChunk>().Count(),
			MaxDepth = hasChunks ? chunks.Max(c => c.Depth) : 0,
			ProcessingTime = DateTime.UtcNow - startTime,
			AverageTokensPerChunk = hasChunks ? (int)tokens.Average() : 0,
			TotalTokens = tokens.Sum(),
			MaxTokensInChunk = hasChunks ? tokens.Max() : 0,
			MinTokensInChunk = hasChunks ? tokens.Min() : 0
		};
	}

	public static ValidationResult Validate(List<ChunkerBase> chunks)
	{
		var chunkIds = new HashSet<Guid>(chunks.Select(c => c.Id));
		var issues = chunks
			.Where(c => c.ParentId.HasValue && !chunkIds.Contains(c.ParentId.Value))
			.Select(c => new ValidationIssue
			{
				Severity = ValidationSeverity.Warning,
				Message = $"Chunk {c.Id} references non-existent parent {c.ParentId!.Value}",
				ChunkId = c.Id,
				Code = "ORPHANED_CHUNK"
			})
			.ToList();

		return new ValidationResult
		{
			IsValid = issues.Count == 0,
			Issues = issues,
			HasOrphanedChunks = issues.Count != 0
		};
	}

	public static ChunkingResult CreateSuccess(
		List<ChunkerBase> chunks,
		DateTime startTime,
		ChunkingOptions options) => new()
		{
			Chunks = chunks,
			Statistics = CalculateStatistics(chunks, startTime),
			ValidationResult = options.ValidateChunks ? Validate(chunks) : null,
			Warnings = [],
			Success = true
		};

	public static ChunkingResult CreateFailure(string formatName, Exception exception) => new()
	{
		Chunks = [],
		Statistics = new ChunkingStatistics(),
		Warnings =
		[
			new ChunkingWarning
			{
				Level = WarningLevel.Error,
				Message = $"Failed to chunk {formatName} document: {exception.Message}"
			}
		],
		Success = false
	};
}
