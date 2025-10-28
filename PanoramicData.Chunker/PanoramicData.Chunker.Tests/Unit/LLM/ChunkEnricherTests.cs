using AwesomeAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using PanoramicData.Chunker.Configuration;
using PanoramicData.Chunker.Interfaces;
using PanoramicData.Chunker.LLM;
using PanoramicData.Chunker.LLM.Caching;
using PanoramicData.Chunker.Models;
using PanoramicData.Chunker.Models.Llm;

namespace PanoramicData.Chunker.Tests.Unit.LLM;

public class ChunkEnricherTests(ITestOutputHelper output) : BaseTest(output)
{
	private static ChunkEnricher CreateEnricher(
		ILlmProvider? llmProvider = null,
		IPromptTemplate? promptManager = null,
		IEnrichmentCache? cache = null,
		LLMEnrichmentOptions? options = null)
	{
		llmProvider ??= CreateMockLlmProvider();
		promptManager ??= new PromptTemplateManager();
		cache ??= new InMemoryEnrichmentCache();
		options ??= new LLMEnrichmentOptions();

		var logger = new Mock<ILogger<ChunkEnricher>>().Object;

		return new ChunkEnricher(llmProvider, promptManager, cache, options, logger);
	}

	private static ILlmProvider CreateMockLlmProvider()
	{
		var mock = new Mock<ILlmProvider>();
		_ = mock.Setup(p => p.GenerateAsync(It.IsAny<LlmRequest>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync((LlmRequest req, CancellationToken _) =>
			{
				// Simulate different responses based on prompt content
				var text = req.Prompt.Contains("Summarize", StringComparison.OrdinalIgnoreCase) ? "This is a summary" :
						 req.Prompt.Contains("Extract", StringComparison.OrdinalIgnoreCase) && req.Prompt.Contains("keywords", StringComparison.OrdinalIgnoreCase) ? "keyword1, keyword2, keyword3" :
						   req.Prompt.Contains("entities", StringComparison.OrdinalIgnoreCase) ? "[{\"text\":\"John Doe\",\"type\":\"Person\"}]" :
						   "Response";

				return new LlmResponse
				{
					Text = text,
					Model = req.Model,
					TokensUsed = 25,
					Duration = TimeSpan.FromMilliseconds(100),
					Success = true
				};
			});

		return mock.Object;
	}

	[Fact]
	public async Task EnrichAsync_WithContentChunk_ShouldGenerateSummary()
	{
		// Arrange
		var enricher = CreateEnricher();
		var chunk = new TestContentChunk
		{
			Content = "This is test content for summarization.",
			Id = Guid.NewGuid()
		};

		// Act
		var result = await enricher.EnrichAsync(chunk, CancellationToken);

		// Assert
		_ = result.Should().NotBeNull();
		_ = result.ChunkId.Should().Be(chunk.Id);
		_ = result.OriginalContent.Should().Be(chunk.Content);
		_ = result.Summary.Should().NotBeNullOrEmpty();
		_ = result.EnrichmentDuration.Should().BeGreaterThan(TimeSpan.Zero);

		_output.WriteLine($"Summary: {result.Summary}");
		_output.WriteLine($"Duration: {result.EnrichmentDuration.TotalMilliseconds}ms");
	}

	[Fact]
	public async Task EnrichAsync_WithKeywordExtraction_ShouldExtractKeywords()
	{
		// Arrange
		var options = new LLMEnrichmentOptions
		{
			EnableSummarization = false,
			EnableKeywordExtraction = true
		};
		var enricher = CreateEnricher(options: options);
		var chunk = new TestContentChunk
		{
			Content = "Artificial intelligence and machine learning are transforming technology.",
			Id = Guid.NewGuid()
		};

		// Act
		var result = await enricher.EnrichAsync(chunk, CancellationToken);

		// Assert
		_ = result.Should().NotBeNull();
		_ = result.Keywords.Should().NotBeEmpty();

		_output.WriteLine($"Keywords: {string.Join(", ", result.Keywords)}");
	}

	[Fact]
	public async Task EnrichAsync_WithCacheEnabled_ShouldUseCacheOnSecondCall()
	{
		// Arrange
		var cache = new InMemoryEnrichmentCache();
		var options = new LLMEnrichmentOptions { EnableCaching = true };
		var enricher = CreateEnricher(cache: cache, options: options);

		var chunk1 = new TestContentChunk { Content = "Same content", Id = Guid.NewGuid() };
		var chunk2 = new TestContentChunk { Content = "Same content", Id = Guid.NewGuid() };

		// Act
		var result1 = await enricher.EnrichAsync(chunk1, CancellationToken);
		var result2 = await enricher.EnrichAsync(chunk2, CancellationToken);

		var stats = cache.GetStatistics();

		// Assert
		_ = result1.Summary.Should().Be(result2.Summary);
		_ = stats.Hits.Should().Be(1);
		_ = stats.Misses.Should().Be(1);

		_output.WriteLine($"Cache hit rate: {stats.HitRate:P}");
	}

	[Fact]
	public async Task EnrichAsync_WithEmptyContent_ShouldReturnEmptyEnrichedChunk()
	{
		// Arrange
		var enricher = CreateEnricher();
		var chunk = new TestContentChunk
		{
			Content = string.Empty,
			Id = Guid.NewGuid()
		};

		// Act
		var result = await enricher.EnrichAsync(chunk, CancellationToken);

		// Assert
		_ = result.Should().NotBeNull();
		_ = result.OriginalContent.Should().BeEmpty();
		_ = result.Summary.Should().BeNullOrEmpty();

		_output.WriteLine("Empty content handled correctly");
	}

	[Fact]
	public async Task EnrichBatchAsync_ShouldEnrichAllChunks()
	{
		// Arrange
		var enricher = CreateEnricher();
		var chunks = new[]
		{
			new TestContentChunk { Content = "Content 1", Id = Guid.NewGuid() },
			new TestContentChunk { Content = "Content 2", Id = Guid.NewGuid() },
			new TestContentChunk { Content = "Content 3", Id = Guid.NewGuid() }
		};

		// Act
		var results = await enricher.EnrichBatchAsync(chunks, CancellationToken);

		// Assert
		_ = results.Should().HaveCount(3);
		_ = results.Should().OnlyContain(r => !string.IsNullOrEmpty(r.Summary));

		_output.WriteLine($"Enriched {results.Count()} chunks");
	}

	[Fact]
	public async Task EnrichAsync_WithPreliminaryNER_ShouldExtractEntities()
	{
		// Arrange
		var options = new LLMEnrichmentOptions
		{
			EnableSummarization = false,
			EnableKeywordExtraction = false,
			EnablePreliminaryNER = true
		};
		var enricher = CreateEnricher(options: options);
		var chunk = new TestContentChunk
		{
			Content = "John Doe works at Microsoft in Seattle.",
			Id = Guid.NewGuid()
		};

		// Act
		var result = await enricher.EnrichAsync(chunk, CancellationToken);

		// Assert
		_ = result.Should().NotBeNull();
		_ = result.PreliminaryEntities.Should().NotBeEmpty();

		_output.WriteLine($"Entities: {result.PreliminaryEntities.Count}");
		foreach (var entity in result.PreliminaryEntities)
		{
			_output.WriteLine($"  - {entity.Text} ({entity.Type})");
		}
	}

	[Fact]
	public async Task EnrichAsync_WithFailedLLMCall_ShouldReturnPartialResults()
	{
		// Arrange
		var mockProvider = new Mock<ILlmProvider>();
		_ = mockProvider.Setup(p => p.GenerateAsync(It.IsAny<LlmRequest>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync(new LlmResponse
			{
				Text = string.Empty,
				Model = "test",
				TokensUsed = 0,
				Duration = TimeSpan.Zero,
				Success = false,
				ErrorMessage = "Test error"
			});

		var enricher = CreateEnricher(llmProvider: mockProvider.Object);
		var chunk = new TestContentChunk
		{
			Content = "Test content",
			Id = Guid.NewGuid()
		};

		// Act
		var result = await enricher.EnrichAsync(chunk, CancellationToken);

		// Assert
		_ = result.Should().NotBeNull();
		_ = result.Summary.Should().BeEmpty();
		_ = result.TokensUsed.Should().Be(0);

		_output.WriteLine("Handled failed Llm call gracefully");
	}

	[Fact]
	public void Constructor_WithNullParameters_ShouldThrow()
	{
		// Arrange
		var provider = CreateMockLlmProvider();
		var promptManager = new PromptTemplateManager();
		var cache = new InMemoryEnrichmentCache();
		var options = new LLMEnrichmentOptions();
		var logger = new Mock<ILogger<ChunkEnricher>>().Object;

		// Act & Assert
		var act1 = () => new ChunkEnricher(null!, promptManager, cache, options, logger);
		_ = act1.Should().Throw<ArgumentNullException>();

		var act2 = () => new ChunkEnricher(provider, null!, cache, options, logger);
		_ = act2.Should().Throw<ArgumentNullException>();

		var act3 = () => new ChunkEnricher(provider, promptManager, null!, options, logger);
		_ = act3.Should().Throw<ArgumentNullException>();

		var act4 = () => new ChunkEnricher(provider, promptManager, cache, null!, logger);
		_ = act4.Should().Throw<ArgumentNullException>();
	}

	// Helper test chunk class
	private class TestContentChunk : ContentChunk
	{
	}
}
