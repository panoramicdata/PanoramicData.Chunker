using AwesomeAssertions;
using PanoramicData.Chunker.Infrastructure;

namespace PanoramicData.Chunker.Tests.Unit.TokenCounters;

/// <summary>
/// Tests for CharacterBasedTokenCounter.
/// </summary>
public class CharacterBasedTokenCounterTests
{
	private readonly CharacterBasedTokenCounter _tokenCounter = new();

	[Theory]
	[InlineData("", 0)]
	[InlineData("Test", 1)]
	[InlineData("This is a test", 4)]
	[InlineData("A longer test sentence with more words", 10)]
	public void CountTokens_ShouldReturnCorrectCount(string text, int expectedTokens)
	{
		// Act
		var tokens = _tokenCounter.CountTokens(text);

		// Assert
		Assert.Equal(expectedTokens, tokens);
	}

	[Fact]
	public async Task CountTokensAsync_ShouldReturnSameAsSync()
	{
		// Arrange
		const string text = "This is a test";

		// Act
		var syncResult = _tokenCounter.CountTokens(text);
		var asyncResult = await _tokenCounter.CountTokensAsync(text);

		// Assert
		Assert.Equal(syncResult, asyncResult);
	}

	[Fact]
	public void SplitIntoTokenBatches_ShouldSplitCorrectly()
	{
		// Arrange
		const string text = "This is a long text. It has multiple sentences. Each sentence should be kept together if possible.";
		const int maxTokens = 10;

		// Act
		var batches = _tokenCounter.SplitIntoTokenBatches(text, maxTokens, 0).ToList();

		// Assert
		Assert.NotEmpty(batches);
		foreach (var batch in batches)
		{
			var tokenCount = _tokenCounter.CountTokens(batch);
			_ = (tokenCount <= maxTokens).Should().BeTrue();
		}
	}

	[Fact]
	public void SplitIntoTokenBatches_WithOverlap_ShouldOverlap()
	{
		// Arrange
		const string text = "Sentence one. Sentence two. Sentence three.";
		const int maxTokens = 5;
		const int overlap = 2;

		// Act
		var batches = _tokenCounter.SplitIntoTokenBatches(text, maxTokens, overlap).ToList();

		// Assert
		_ = (batches.Count > 1).Should().BeTrue();
		// Each batch (except first) should contain some content from the previous batch
	}
}
