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
}