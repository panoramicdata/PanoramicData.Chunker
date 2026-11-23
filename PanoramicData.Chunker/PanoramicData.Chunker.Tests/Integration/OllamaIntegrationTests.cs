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

[Trait("Category", "Integration")]
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

	private static void AssertOllamaAvailable()
	{
		_ = IsOllamaAvailable().Should().BeTrue("Ollama must be running at http://localhost:11434 (run 'ollama serve') with the models these tests use pulled; run this class only where Ollama is available");
	}

	private static ChunkEnricher CreateEnricher(LLMEnrichmentOptions enrichmentOptions, InMemoryEnrichmentCache? cache = null)
		=> new(
			new OllamaLlmProvider(new OllamaOptions { DefaultModel = "llama3" }, new Mock<ILogger<OllamaLlmProvider>>().Object),
			new PromptTemplateManager(),
			cache ?? new InMemoryEnrichmentCache(),
			enrichmentOptions,
			new Mock<ILogger<ChunkEnricher>>().Object);

	private static TestContentChunk CreateChunk(string content) => new() { Content = content, Id = Guid.NewGuid() };

	[Fact]
	public async Task RealOllama_GenerateAsync_ShouldProduceResponse()
	{
		AssertOllamaAvailable();

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

		var response = await provider.GenerateAsync(request, CancellationToken);

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
		AssertOllamaAvailable();

		var enricher = CreateEnricher(new LLMEnrichmentOptions
		{
			EnableSummarization = true,
			EnableKeywordExtraction = false,
			EnablePreliminaryNER = false,
			SummaryMaxWords = 20
		});
		var chunk = CreateChunk(
			"Artificial intelligence is revolutionizing many industries. " +
			"Machine learning algorithms can now process vast amounts of data " +
			"and make predictions with remarkable accuracy.");

		var enriched = await enricher.EnrichAsync(chunk, CancellationToken);

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
		AssertOllamaAvailable();

		var enricher = CreateEnricher(new LLMEnrichmentOptions
		{
			EnableSummarization = false,
			EnableKeywordExtraction = true,
			MaxKeywords = 5
		});
		var chunk = CreateChunk("Cloud computing enables scalable and flexible infrastructure for modern applications.");

		var enriched = await enricher.EnrichAsync(chunk, CancellationToken);

		_ = enriched.Should().NotBeNull();
		_ = enriched.Keywords.Should().NotBeEmpty();
		enriched.Keywords.Count.Should().BeLessThanOrEqualTo(5);

		_output.WriteLine($"Content: {chunk.Content}");
		_output.WriteLine($"Keywords: {string.Join(", ", enriched.Keywords)}");
	}

	[Fact]
	public async Task RealOllama_BatchEnrichment_ShouldProcessMultipleChunks()
	{
		AssertOllamaAvailable();

		var enricher = CreateEnricher(new LLMEnrichmentOptions
		{
			EnableSummarization = true,
			MaxConcurrency = 2
		});
		var chunks = new[]
		{
			CreateChunk("Python is a popular programming language."),
			CreateChunk("Docker containers enable portable deployments."),
			CreateChunk("React is a JavaScript library for building user interfaces.")
		};

		var results = await enricher.EnrichBatchAsync(chunks, CancellationToken);

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
		AssertOllamaAvailable();

		var cache = new InMemoryEnrichmentCache();
		var enricher = CreateEnricher(new LLMEnrichmentOptions
		{
			EnableCaching = true,
			EnableSummarization = true
		}, cache);
		var chunk1 = CreateChunk("Machine learning models require training data.");
		var chunk2 = CreateChunk("Machine learning models require training data.");

		var result1 = await enricher.EnrichAsync(chunk1, CancellationToken);
		var result2 = await enricher.EnrichAsync(chunk2, CancellationToken);
		var stats = cache.GetStatistics();

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