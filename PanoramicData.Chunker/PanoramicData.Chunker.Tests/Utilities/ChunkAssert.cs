using AwesomeAssertions;
using PanoramicData.Chunker.Models;

namespace PanoramicData.Chunker.Tests.Utilities;

internal static class ChunkAssert
{
	public static void AllHaveMetrics(IEnumerable<ChunkerBase> chunks)
	{
		_ = chunks.Should().AllSatisfy(chunk =>
		{
			_ = chunk.QualityMetrics.Should().NotBeNull();
			_ = chunk.QualityMetrics!.TokenCount.Should().BePositive();
			_ = chunk.QualityMetrics.CharacterCount.Should().BePositive();
		});
	}

	public static void Succeeded(ChunkingResult result)
	{
		_ = result.Should().NotBeNull();
		_ = result.Success.Should().BeTrue();
	}

	public static void FailedWithWarnings(ChunkingResult result)
	{
		_ = result.Should().NotBeNull();
		_ = result.Success.Should().BeFalse();
		_ = result.Warnings.Should().NotBeEmpty();
	}

	public static void HasStatistics(ChunkingResult result)
	{
		_ = result.Statistics.Should().NotBeNull();
		_ = result.Statistics.TotalChunks.Should().BePositive();
		_ = result.Statistics.ProcessingTime.Should().BeGreaterThan(TimeSpan.Zero);
	}

	public static void IsValidated(ChunkingResult result)
	{
		_ = result.ValidationResult.Should().NotBeNull();
		_ = result.ValidationResult!.IsValid.Should().BeTrue();
	}

	public static void SucceededWithChunks(ChunkingResult result)
	{
		_ = result.Success.Should().BeTrue();
		_ = result.Chunks.Should().NotBeEmpty();
	}

	public static void StatisticsMatch(ChunkingResult result)
	{
		_ = result.Statistics.Should().NotBeNull();
		_ = result.Statistics.TotalChunks.Should().Be(result.Chunks.Count);
		_ = result.Statistics.StructuralChunks.Should().BePositive();
		_ = result.Statistics.ProcessingTime.Should().BeGreaterThan(TimeSpan.Zero);
	}
}