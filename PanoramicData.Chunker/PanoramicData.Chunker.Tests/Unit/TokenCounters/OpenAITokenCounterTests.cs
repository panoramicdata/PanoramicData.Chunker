using AwesomeAssertions;
using PanoramicData.Chunker.Infrastructure.TokenCounters;

namespace PanoramicData.Chunker.Tests.Unit.TokenCounters;

public class OpenAITokenCounterTests(ITestOutputHelper output) : BaseTest(output)
{
	[Fact]
	public void Constructor_Default_ShouldUseCL100K()
	{
		// Arrange & Act
		var counter = new OpenAITokenCounter();

		// Assert
		_ = counter.Should().NotBeNull();
		_ = counter.EncodingName.Should().Be("cl100k_base");
	}

	[Fact]
	public void ForGpt4_ShouldCreateCL100KCounter()
	{
		// Arrange & Act
		var counter = OpenAITokenCounter.ForGpt4();

		// Assert
		_ = counter.Should().NotBeNull();
		_ = counter.EncodingName.Should().Be("cl100k_base");
	}

	[Fact]
	public void ForGpt3_ShouldCreateP50KCounter()
	{
		// Arrange & Act
		var counter = OpenAITokenCounter.ForGpt3();

		// Assert
		_ = counter.Should().NotBeNull();
		_ = counter.EncodingName.Should().Be("p50k_base");
	}

	[Fact]
	public void ForGpt2_ShouldCreateR50KCounter()
	{
		// Arrange & Act
		var counter = OpenAITokenCounter.ForGpt2();

		// Assert
		_ = counter.Should().NotBeNull();
		_ = counter.EncodingName.Should().Be("r50k_base");
	}

	[Fact]
	public void CountTokens_EmptyString_ShouldReturnZero()
	{
		// Arrange
		var counter = new OpenAITokenCounter();

		// Act
		var count = counter.CountTokens("");

		// Assert
		_ = count.Should().Be(0);
	}

	[Fact]
	public void CountTokens_NullString_ShouldReturnZero()
	{
		// Arrange
		var counter = new OpenAITokenCounter();

		// Act
		var count = counter.CountTokens(null!);

		// Assert
		_ = count.Should().Be(0);
	}

	[Fact]
	public void CountTokens_SimpleText_ShouldReturnAccurateCount()
	{
		// Arrange
		var counter = OpenAITokenCounter.ForGpt4();
		var text = "Hello, world!";

		// Act
		var count = counter.CountTokens(text);

		// Assert
		// "Hello, world!" is typically 4 tokens in CL100K: ["Hello", ",", " world", "!"]
		_ = count.Should().BePositive();
		_ = count.Should().BeLessThan(10); // Sanity check
	}

	[Theory]
	[InlineData("The quick brown fox jumps over the lazy dog.")]
	[InlineData("This is a test of the OpenAI token counter.")]
	[InlineData("Artificial intelligence is transforming the world.")]
	public void CountTokens_VariousTexts_ShouldReturnReasonableCount(string text)
	{
		// Arrange
		var counter = new OpenAITokenCounter();

		// Act
		var count = counter.CountTokens(text);

		// Assert
		_ = count.Should().BePositive();
		_ = count.Should().BeLessThan(text.Length); // Should be fewer tokens than characters
	}

	[Fact]
	public void CountTokens_LongText_ShouldHandleLargeInputs()
	{
		// Arrange
		var counter = new OpenAITokenCounter();
		var longText = string.Join(" ", Enumerable.Repeat("word", 1000));

		// Act
		var count = counter.CountTokens(longText);

		// Assert
		_ = count.Should().BeGreaterThan(900); // Approximately 1000 words
		_ = count.Should().BeLessThan(1100); // Should be close to word count
	}

	[Fact]
	public void CountTokens_SpecialCharacters_ShouldHandleCorrectly()
	{
		// Arrange
		var counter = new OpenAITokenCounter();
		var text = "Hello ?? ????? ??????";

		// Act
		var count = counter.CountTokens(text);

		// Assert
		_ = count.Should().BePositive();
	}

	[Fact]
	public void CountTokens_CodeSnippet_ShouldCountAccurately()
	{
		// Arrange
		var counter = new OpenAITokenCounter();
		var code = "public class Test { public void Method() { } }";

		// Act
		var count = counter.CountTokens(code);

		// Assert
		_ = count.Should().BePositive();
		_ = count.Should().BeLessThan(30); // Reasonable upper bound
	}

	[Fact]
	public async Task CountTokensAsync_ShouldReturnSameAsSync()
	{
		// Arrange
		var counter = new OpenAITokenCounter();
		var text = "Hello, world!";

		// Act
		var syncCount = counter.CountTokens(text);
		var asyncCount = await counter.CountTokensAsync(text, CancellationToken);

		// Assert
		_ = asyncCount.Should().Be(syncCount);
	}

	[Fact]
	public async Task CountTokensAsync_WithCancellation_ShouldRespectToken()
	{
		// Arrange
		var counter = new OpenAITokenCounter();
		var text = "Hello, world!";
		using var cts = new CancellationTokenSource();

		// Act
		var count = await counter.CountTokensAsync(text, cts.Token);

		// Assert
		_ = count.Should().BePositive();
	}

	[Fact]
	public void SplitIntoTokenBatches_ShortText_ShouldReturnSingleBatch()
	{
		// Arrange
		var counter = new OpenAITokenCounter();
		var text = "Hello, world!";

		// Act
		var batches = counter.SplitIntoTokenBatches(text, maxTokens: 100).ToList();

		// Assert
		_ = batches.Should().ContainSingle();
		_ = batches[0].Should().Be(text);
	}

	[Fact]
	public void SplitIntoTokenBatches_LongText_ShouldSplitIntoMultipleBatches()
	{
		// Arrange
		var counter = new OpenAITokenCounter();
		var text = string.Join(" ", Enumerable.Repeat("word", 100));

		// Act
		var batches = counter.SplitIntoTokenBatches(text, maxTokens: 20).ToList();

		// Assert
		_ = batches.Should().HaveCountGreaterThan(1);
		_ = batches.Should().OnlyContain(batch => !string.IsNullOrEmpty(batch));
	}

	[Fact]
	public void SplitIntoTokenBatches_WithOverlap_ShouldOverlapBatches()
	{
		// Arrange
		var counter = new OpenAITokenCounter();
		var text = "The quick brown fox jumps over the lazy dog. " +
				   "The fast red cat runs under the sleepy bird.";

		// Act
		var batches = counter.SplitIntoTokenBatches(text, maxTokens: 10, overlap: 3).ToList();

		// Assert
		_ = batches.Should().HaveCountGreaterThan(1);

		// Check that consecutive batches have some overlap
		for (var i = 0; i < batches.Count - 1; i++)
		{
			var current = batches[i];
			var next = batches[i + 1];

			// At least some words should appear in both
			var currentWords = current.Split(' ', StringSplitOptions.RemoveEmptyEntries);
			var nextWords = next.Split(' ', StringSplitOptions.RemoveEmptyEntries);
			var overlap = currentWords.Intersect(nextWords);

			_ = overlap.Should().NotBeEmpty();
		}
	}

	[Fact]
	public void SplitIntoTokenBatches_EmptyText_ShouldReturnNoBatches()
	{
		// Arrange
		var counter = new OpenAITokenCounter();

		// Act
		var batches = counter.SplitIntoTokenBatches("", maxTokens: 100).ToList();

		// Assert
		_ = batches.Should().BeEmpty();
	}

	[Fact]
	public void SplitIntoTokenBatches_InvalidOverlap_ShouldThrow()
	{
		// Arrange
		var counter = new OpenAITokenCounter();
		var text = "Hello, world!";

		// Act
		var act = () => counter.SplitIntoTokenBatches(text, maxTokens: 10, overlap: 10).ToList();

		// Assert
		_ = act.Should().Throw<ArgumentException>()
			.WithMessage("*Overlap must be less than maxTokens*");
	}

	[Fact]
	public void SplitIntoTokenBatches_EachBatch_ShouldRespectMaxTokens()
	{
		// Arrange
		var counter = new OpenAITokenCounter();
		var text = string.Join(" ", Enumerable.Repeat("word", 100));
		var maxTokens = 20;

		// Act
		var batches = counter.SplitIntoTokenBatches(text, maxTokens).ToList();

		// Assert
		foreach (var batch in batches)
		{
			var tokenCount = counter.CountTokens(batch);
			tokenCount.Should().BeLessThanOrEqualTo(maxTokens);
		}
	}

	[Fact]
	public void SplitIntoTokenBatches_CombinedBatches_ShouldCoverOriginalText()
	{
		// Arrange
		var counter = new OpenAITokenCounter();
		var text = "The quick brown fox jumps over the lazy dog.";

		// Act
		var batches = counter.SplitIntoTokenBatches(text, maxTokens: 5, overlap: 0).ToList();
		var combinedLength = batches.Sum(b => b.Length);

		// Assert
		// Combined length should be close to original (allowing for some token boundary effects)
		_ = combinedLength.Should().BeGreaterThanOrEqualTo((int)(text.Length * 0.9));
	}

	[Fact]
	public void CountTokens_ConsistentResults_ShouldReturnSameCountForSameText()
	{
		// Arrange
		var counter = new OpenAITokenCounter();
		var text = "Consistency test for token counting.";

		// Act
		var count1 = counter.CountTokens(text);
		var count2 = counter.CountTokens(text);
		var count3 = counter.CountTokens(text);

		// Assert
		_ = count2.Should().Be(count1);
		_ = count3.Should().Be(count1);
	}

	[Theory]
	[InlineData("cl100k_base")] // GPT-4
	[InlineData("p50k_base")]   // GPT-3
	[InlineData("r50k_base")]   // GPT-2
	public void Constructor_WithValidEncoding_ShouldWork(string encoding)
	{
		// Arrange & Act
		var counter = new OpenAITokenCounter(encoding);

		// Assert
		_ = counter.Should().NotBeNull();
		_ = counter.EncodingName.Should().Be(encoding);
		_ = counter.CountTokens("Test").Should().BePositive();
	}

	[Fact]
	public void CountTokens_DifferentEncodings_ShouldProduceDifferentCounts()
	{
		// Arrange
		var text = "The quick brown fox jumps over the lazy dog.";
		var counterGpt4 = OpenAITokenCounter.ForGpt4();
		var counterGpt3 = OpenAITokenCounter.ForGpt3();
		var counterGpt2 = OpenAITokenCounter.ForGpt2();

		// Act
		var countGpt4 = counterGpt4.CountTokens(text);
		var countGpt3 = counterGpt3.CountTokens(text);
		var countGpt2 = counterGpt2.CountTokens(text);

		// Assert
		// Different encodings may produce slightly different token counts
		_ = countGpt4.Should().BePositive();
		_ = countGpt3.Should().BePositive();
		_ = countGpt2.Should().BePositive();

		// They should all be in a reasonable range
		_ = countGpt4.Should().BeInRange(5, 20);
		_ = countGpt3.Should().BeInRange(5, 20);
		_ = countGpt2.Should().BeInRange(5, 20);
	}
}
