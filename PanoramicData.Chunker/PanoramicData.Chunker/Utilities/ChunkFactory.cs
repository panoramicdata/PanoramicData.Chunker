using PanoramicData.Chunker.Configuration;
using PanoramicData.Chunker.Interfaces;
using PanoramicData.Chunker.Models;

namespace PanoramicData.Chunker.Utilities;

/// <summary>
/// Shared construction of the metadata, quality metrics and empty result used by the document chunkers.
/// </summary>
internal static class ChunkFactory
{
	public static ChunkMetadata CreateMetadata(string documentType, string hierarchy, IEnumerable<string> tags) => new()
	{
		DocumentType = documentType,
		SourceId = string.Empty,
		Hierarchy = hierarchy,
		Tags = [.. tags],
		CreatedAt = DateTimeOffset.UtcNow
	};

	public static ChunkQualityMetrics CreateQualityMetrics(ITokenCounter tokenCounter, string text, int wordCount) => new()
	{
		TokenCount = tokenCounter.CountTokens(text),
		CharacterCount = text.Length,
		WordCount = wordCount,
		SemanticCompleteness = 1.0
	};

	public static int CountSpaceSeparatedWords(string text)
		=> text.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;

	public static ChunkingResult CreateEmptyResult(DateTime startTime) => new()
	{
		Chunks = [],
		Statistics = new ChunkingStatistics
		{
			ProcessingTime = DateTime.UtcNow - startTime
		},
		Warnings = [],
		Success = true
	};

	public static async Task<byte[]> PeekAsync(Stream documentStream, int count, CancellationToken cancellationToken)
	{
		try
		{
			var originalPosition = documentStream.Position;

			var buffer = new byte[count];
			var bytesRead = await documentStream.ReadAsync(buffer, cancellationToken);

			documentStream.Position = originalPosition;

			return buffer[..bytesRead];
		}
		catch
		{
			return [];
		}
	}
}
