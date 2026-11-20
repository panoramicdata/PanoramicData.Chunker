using Microsoft.Extensions.Logging;
using PanoramicData.Chunker.Configuration;
using PanoramicData.Chunker.Models;

namespace PanoramicData.Chunker.Utilities;

/// <summary>
/// Shared ChunkAsync skeleton: argument checks, extraction, hierarchy building and result construction.
/// </summary>
internal static class ChunkRunner
{
	public static async Task<ChunkingResult> RunAsync(
		string formatName,
		Stream documentStream,
		ChunkingOptions options,
		List<ChunkerBase> chunks,
		ILogger? logger,
		Func<Task> extractAsync)
	{
		ArgumentNullException.ThrowIfNull(documentStream);
		ArgumentNullException.ThrowIfNull(options);

		var startTime = DateTime.UtcNow;
		chunks.Clear();

		try
		{
			await extractAsync();
			HierarchyBuilder.BuildHierarchy(chunks);
			logger?.LogInformation("Extracted {ChunkCount} chunks from {Format} document", chunks.Count, formatName);

			return ChunkResultHelper.CreateSuccess(chunks, startTime, options);
		}
		catch (Exception ex)
		{
			logger?.LogError(ex, "Error chunking {Format} document", formatName);
			return ChunkResultHelper.CreateFailure(formatName, ex);
		}
	}
}