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

	public static void TablesAreMarkdown(IEnumerable<TableChunk> tables)
	{
		_ = tables.Should().AllSatisfy(table =>
		{
			_ = table.SerializedTable.Should().NotBeNullOrWhiteSpace();
			_ = table.SerializationFormat.Should().Be(TableSerializationFormat.Markdown);
			_ = table.TableInfo.Should().NotBeNull();
			_ = table.TableInfo.RowCount.Should().BePositive();
			_ = table.TableInfo.ColumnCount.Should().BePositive();
		});
	}

	public static void ComparableTokenCounts(ChunkingResult charResult, ChunkingResult openAIResult)
	{
		_ = charResult.Success.Should().BeTrue();
		_ = openAIResult.Success.Should().BeTrue();
		_ = charResult.Chunks.Count.Should().Be(openAIResult.Chunks.Count);
		_ = charResult.Statistics.TotalTokens.Should().BePositive("character-based counter should work");
		_ = openAIResult.Statistics.TotalTokens.Should().BePositive("OpenAI counter should work");

		var ratio = (double)Math.Max(charResult.Statistics.TotalTokens, openAIResult.Statistics.TotalTokens) /
					Math.Min(charResult.Statistics.TotalTokens, openAIResult.Statistics.TotalTokens);
		_ = ratio.Should().BeLessThan(2.0, "token counts should be within 2x of each other");
	}

	public static void AllHaveTokenCounts(IEnumerable<ChunkerBase> chunks)
		=> _ = chunks.Select(chunk => chunk.QualityMetrics).Should().OnlyContain(metrics => metrics != null && metrics.TokenCount > 0);

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