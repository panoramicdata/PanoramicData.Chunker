using AwesomeAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using PanoramicData.Chunker.Configuration;
using PanoramicData.Chunker.LLM;
using PanoramicData.Chunker.LLM.Caching;
using PanoramicData.Chunker.LLM.Providers;
using PanoramicData.Chunker.Models;
using PanoramicData.Chunker.Models.Llm;

namespace PanoramicData.Chunker.Tests.Integration;

public class OllamaIntegrationTests(ITestOutputHelper output) : BaseTest(output)
{
	private static bool IsOllamaAvailable()
	{
		try
		{
			var client = new HttpClient();
			var response = client.GetAsync("http://localhost:11434/api/tags").Result;
			return response.IsSuccessStatusCode;
		}
		catch
		{
			return false;
		}
	}

	[Fact]
	public async Task RealOllama_GenerateAsync_ShouldProduceResponse()
	{
		// Skip if Ollama not available
		if (!IsOllamaAvailable())
		{
			_output.WriteLine("Skipping test: Ollama not available");
			return;
		}

		// Arrange
		var options = new OllamaOptions
		{
			BaseUrl = "http://localhost:11434",
			DefaultModel = "llama3"
		};
		var logger = new Mock<ILogger<OllamaLlmProvider>>().Object;
		var provider = new OllamaLlmProvider(options, logger);

		var request = new LlmRequest
		{
			Prompt = "What is 2+2? Answer in one word.",
			Model = "llama3",
			Temperature = 0.1,
			MaxTokens = 10
		};

		// Act
		var response = await provider.GenerateAsync(request, CancellationToken);

		// Assert
		_ = response.Should().NotBeNull();
		_ = response.Success.Should().BeTrue();
		_ = response.Text.Should().NotBeNullOrEmpty();
		_ = response.TokensUsed.Should().BePositive();

		_output.WriteLine($"Response: {response.Text}");
		_output.WriteLine($"Tokens: {response.TokensUsed}");
		_output.WriteLine($"Duration: {response.Duration.TotalMilliseconds}ms");
	}

	[Fact]
	public async Task RealOllama_EnrichChunk_ShouldGenerateSummary()
	{
		// Skip if Ollama not available
		if (!IsOllamaAvailable())
		{
			_output.WriteLine("Skipping test: Ollama not available");
			return;
		}

		// Arrange
		var ollamaOptions = new OllamaOptions { DefaultModel = "llama3" };
		var enrichmentOptions = new LLMEnrichmentOptions
		{
			EnableSummarization = true,
			EnableKeywordExtraction = false,
			EnablePreliminaryNER = false,
			SummaryMaxWords = 20
		};

		var llmProvider = new OllamaLlmProvider(
			ollamaOptions,
			new Mock<ILogger<OllamaLlmProvider>>().Object);

		var enricher = new ChunkEnricher(
			llmProvider,
			new PromptTemplateManager(),
			new InMemoryEnrichmentCache(),
			enrichmentOptions,
			new Mock<ILogger<ChunkEnricher>>().Object);

		var chunk = new TestContentChunk
		{
			Content = "Artificial intelligence is revolutionizing many industries. " +
					  "Machine learning algorithms can now process vast amounts of data " +
					  "and make predictions with remarkable accuracy.",
			Id = Guid.NewGuid()
		};

		// Act
		var enriched = await enricher.EnrichAsync(chunk, CancellationToken);

		// Assert
		_ = enriched.Should().NotBeNull();
		_ = enriched.Summary.Should().NotBeNullOrEmpty();
		_ = enriched.TokensUsed.Should().BePositive();

		_output.WriteLine($"Original: {chunk.Content}");
		_output.WriteLine($"Summary: {enriched.Summary}");
		_output.WriteLine($"Tokens: {enriched.TokensUsed}");
	}

	[Fact]
	public async Task RealOllama_ExtractKeywords_ShouldReturnKeywords()
	{
		// Skip if Ollama not available
		if (!IsOllamaAvailable())
		{
			_output.WriteLine("Skipping test: Ollama not available");
			return;
		}

		// Arrange
		var ollamaOptions = new OllamaOptions { DefaultModel = "llama3" };
		var enrichmentOptions = new LLMEnrichmentOptions
		{
			EnableSummarization = false,
			EnableKeywordExtraction = true,
			MaxKeywords = 5
		};

		var llmProvider = new OllamaLlmProvider(
			ollamaOptions,
			new Mock<ILogger<OllamaLlmProvider>>().Object);

		var enricher = new ChunkEnricher(
			llmProvider,
			new PromptTemplateManager(),
			new InMemoryEnrichmentCache(),
			enrichmentOptions,
			new Mock<ILogger<ChunkEnricher>>().Object);

		var chunk = new TestContentChunk
		{
			Content = "Cloud computing enables scalable and flexible infrastructure for modern applications.",
			Id = Guid.NewGuid()
		};

		// Act
		var enriched = await enricher.EnrichAsync(chunk, CancellationToken);

		// Assert
		_ = enriched.Should().NotBeNull();
		_ = enriched.Keywords.Should().NotBeEmpty();
		enriched.Keywords.Count.Should().BeLessThanOrEqualTo(5);

		_output.WriteLine($"Content: {chunk.Content}");
		_output.WriteLine($"Keywords: {string.Join(", ", enriched.Keywords)}");
	}

	[Fact]
	public async Task RealOllama_BatchEnrichment_ShouldProcessMultipleChunks()
	{
		// Skip if Ollama not available
		if (!IsOllamaAvailable())
		{
			_output.WriteLine("Skipping test: Ollama not available");
			return;
		}

		// Arrange
		var ollamaOptions = new OllamaOptions { DefaultModel = "llama3" };
		var enrichmentOptions = new LLMEnrichmentOptions
		{
			EnableSummarization = true,
			MaxConcurrency = 2
		};

		var llmProvider = new OllamaLlmProvider(
			ollamaOptions,
			new Mock<ILogger<OllamaLlmProvider>>().Object);

		var enricher = new ChunkEnricher(
			llmProvider,
			new PromptTemplateManager(),
			new InMemoryEnrichmentCache(),
			enrichmentOptions,
			new Mock<ILogger<ChunkEnricher>>().Object);

		var chunks = new[]
		{
			new TestContentChunk { Content = "Python is a popular programming language.", Id = Guid.NewGuid() },
			new TestContentChunk { Content = "Docker containers enable portable deployments.", Id = Guid.NewGuid() },
			new TestContentChunk { Content = "React is a JavaScript library for building user interfaces.", Id = Guid.NewGuid() }
		};

		// Act
		var results = await enricher.EnrichBatchAsync(chunks, CancellationToken);

		// Assert
		_ = results.Should().HaveCount(3);
		_ = results.Should().OnlyContain(r => !string.IsNullOrEmpty(r.Summary));

		foreach (var result in results)
		{
			_output.WriteLine($"Summary: {result.Summary}");
		}
	}

	[Fact]
	public async Task RealOllama_CacheEfficiency_ShouldReuseResults()
	{
		// Skip if Ollama not available
		if (!IsOllamaAvailable())
		{
			_output.WriteLine("Skipping test: Ollama not available");
			return;
		}

		// Arrange
		var cache = new InMemoryEnrichmentCache();
		var ollamaOptions = new OllamaOptions { DefaultModel = "llama3" };
		var enrichmentOptions = new LLMEnrichmentOptions
		{
			EnableCaching = true,
			EnableSummarization = true
		};

		var llmProvider = new OllamaLlmProvider(
			ollamaOptions,
			new Mock<ILogger<OllamaLlmProvider>>().Object);

		var enricher = new ChunkEnricher(
			llmProvider,
			new PromptTemplateManager(),
			cache,
			enrichmentOptions,
			new Mock<ILogger<ChunkEnricher>>().Object);

		var chunk1 = new TestContentChunk
		{
			Content = "Machine learning models require training data.",
			Id = Guid.NewGuid()
		};
		var chunk2 = new TestContentChunk
		{
			Content = "Machine learning models require training data.",
			Id = Guid.NewGuid()
		};

		// Act
		var result1 = await enricher.EnrichAsync(chunk1, CancellationToken);
		var result2 = await enricher.EnrichAsync(chunk2, CancellationToken);
		var stats = cache.GetStatistics();

		// Assert
		_ = stats.Hits.Should().Be(1);
		_ = stats.HitRate.Should().Be(0.5);
		_ = result1.Summary.Should().Be(result2.Summary);

		_output.WriteLine($"Cache hits: {stats.Hits}, misses: {stats.Misses}");
		_output.WriteLine($"Hit rate: {stats.HitRate:P}");
	}

	// Helper test chunk class
	private class TestContentChunk : ContentChunk
	{
	}
}
