using AwesomeAssertions;
using PanoramicData.Chunker.Configuration;
using PanoramicData.Chunker.Infrastructure;
using PanoramicData.Chunker.Infrastructure.TokenCounters;
using PanoramicData.Chunker.Models;
using PanoramicData.Chunker.Tests.Utilities;
using System.Text;

namespace PanoramicData.Chunker.Tests.Integration;

/// <summary>
/// Integration tests for token counting across different document types.
/// </summary>
public class TokenCountingIntegrationTests(ITestOutputHelper output) : BaseTest(output)
{
	[Fact]
	public async Task MarkdownChunking_WithOpenAITokenCounter_ShouldCountTokensAccurately()
	{
		const string markdown = "# Main Header\n\nThis is a paragraph with some content that should be counted accurately.\n\n## Subheader\n\nAnother paragraph with different content for testing token counting.\n\n### Code Example\n\n```csharp\npublic class Test {\n    public void Method() { }\n}\n```";
		var options = OpenAIOptions(100);

		var result = await ChunkTextAsync(markdown, DocumentType.Markdown, options);

		_ = result.Success.Should().BeTrue();
		AssertChunkTokenCounts(result, options.MaxTokens);
		_ = result.Statistics.TotalTokens.Should().BePositive();
	}

	[Fact]
	public async Task HtmlChunking_WithOpenAITokenCounter_ShouldCountTokensAccurately()
	{
		const string html = "<!DOCTYPE html>\n<html>\n<body>\n\t<h1>Main Title</h1>\n\t<p>This is a paragraph with some content that should be counted accurately.</p>\n\t<h2>Subtitle</h2>\n\t<p>Another paragraph with different content for testing token counting.</p>\n\t<ul>\n\t\t<li>List item one</li>\n\t\t<li>List item two</li>\n\t\t<li>List item three</li>\n\t</ul>\n</body>\n</html>";

		var result = await ChunkTextAsync(html, DocumentType.Html, OpenAIOptions(100));

		_ = result.Success.Should().BeTrue();
		AssertChunkTokenCounts(result);
		_ = result.Statistics.TotalTokens.Should().BePositive();
		_ = result.Statistics.AverageTokensPerChunk.Should().BePositive();
	}

	[Fact]
	public async Task CompareTokenCounters_CharacterBasedVsOpenAI_ShouldShowDifference()
	{
		const string markdown = "# Technical Documentation\n\nThis is a comprehensive guide to understanding how different token counters work.\n\n## Character-Based Counting\n\nCharacter-based token counting estimates tokens as characters divided by 4.\n\n## OpenAI Token Counting\n\nOpenAI uses BPE tokenization which is more accurate for their models.";
		var charOptions = new ChunkingOptions
		{
			MaxTokens = 1000,
			TokenCounter = new CharacterBasedTokenCounter(),
			TokenCountingMethod = TokenCountingMethod.CharacterBased
		};

		var charResult = await ChunkTextAsync(markdown, DocumentType.Markdown, charOptions);
		var openAIResult = await ChunkTextAsync(markdown, DocumentType.Markdown, OpenAIOptions(1000));

		ChunkAssert.ComparableTokenCounts(charResult, openAIResult);
	}

	[Fact]
	public async Task ChunkingPresets_ForOpenAIEmbeddings_ShouldUseOpenAITokenCounter()
	{
		var options = ChunkingPresets.ForOpenAIEmbeddings();

		var result = await ChunkTextAsync("# Test Document\n\nThis is test content for verifying preset configuration.", DocumentType.Markdown, options);

		_ = result.Success.Should().BeTrue();
		_ = options.TokenCountingMethod.Should().Be(TokenCountingMethod.OpenAI_CL100K);
		_ = options.TokenCounter.Should().BeOfType<OpenAITokenCounter>();
		AssertChunkTokenCounts(result);
	}

	[Fact]
	public async Task ChunkingPresets_ForRAG_ShouldUseOpenAITokenCounter()
	{
		var options = ChunkingPresets.ForRAG();

		var result = await ChunkTextAsync("# RAG Test\n\nContent for retrieval augmented generation testing.", DocumentType.Markdown, options);

		_ = result.Success.Should().BeTrue();
		_ = options.TokenCountingMethod.Should().Be(TokenCountingMethod.OpenAI_CL100K);
		_ = options.TokenCounter.Should().BeOfType<OpenAITokenCounter>();
	}

	[Fact]
	public async Task LargeDocument_WithTokenCounting_ShouldRespectMaxTokens()
	{
		var longText = string.Join("\n\n", Enumerable.Range(1, 50)
			.Select(i => $"This is sentence number {i} with enough content to test token counting."));
		var options = OpenAIOptions(50);

		var result = await ChunkTextAsync($"# Large Document\n\n{longText}", DocumentType.Markdown, options);

		_ = result.Success.Should().BeTrue();
		_ = result.Chunks.Should().AllSatisfy(chunk =>
		{
			if (chunk.QualityMetrics != null)
			{
				// Allow some margin for structural chunks
				_ = chunk.QualityMetrics.TokenCount.Should().BeLessThanOrEqualTo(options.MaxTokens + 10);
			}
		});
	}

	[Theory]
	[InlineData(TokenCountingMethod.CharacterBased)]
	[InlineData(TokenCountingMethod.OpenAI_CL100K)]
	[InlineData(TokenCountingMethod.OpenAI_P50K)]
	[InlineData(TokenCountingMethod.OpenAI_R50K)]
	public async Task DifferentTokenCounters_ShouldAllWork(TokenCountingMethod method)
	{
		var options = new ChunkingOptions
		{
			MaxTokens = 100,
			TokenCounter = TokenCounterFactory.Create(method),
			TokenCountingMethod = method
		};

		var result = await ChunkTextAsync("# Test\n\nContent for testing different token counting methods.", DocumentType.Markdown, options);

		_ = result.Success.Should().BeTrue();
		_ = result.Statistics.TotalTokens.Should().BePositive();
		AssertChunkTokenCounts(result);
	}

	[Fact]
	public async Task TokenCounterFactory_FromOptions_ShouldCreateCorrectCounter()
	{
		var options = new ChunkingOptions
		{
			TokenCountingMethod = TokenCountingMethod.OpenAI_CL100K
		};
		var counter = TokenCounterFactory.GetOrCreate(options);
		options.TokenCounter = counter;

		var result = await ChunkTextAsync("# Factory Test\n\nTesting factory integration.", DocumentType.Markdown, options);

		_ = result.Success.Should().BeTrue();
		_ = counter.Should().BeOfType<OpenAITokenCounter>();
		_ = result.Statistics.TotalTokens.Should().BePositive();
	}

	[Fact]
	public async Task Statistics_ShouldIncludeAccurateTokenMetrics()
	{
		var result = await ChunkTextAsync(
			"# Document\n\nFirst paragraph.\n\n## Section\n\nSecond paragraph.\n\nThird paragraph.",
			DocumentType.Markdown,
			ChunkingPresets.ForOpenAIEmbeddings());

		_ = result.Statistics.TotalTokens.Should().BePositive();
		_ = result.Statistics.AverageTokensPerChunk.Should().BePositive();
		_ = result.Statistics.MaxTokensInChunk.Should().BePositive();
		_ = result.Statistics.MinTokensInChunk.Should().BeGreaterThanOrEqualTo(0);
		_ = result.Statistics.MaxTokensInChunk.Should().BeGreaterThanOrEqualTo(result.Statistics.AverageTokensPerChunk);
		_ = result.Statistics.AverageTokensPerChunk.Should().BeGreaterThanOrEqualTo(result.Statistics.MinTokensInChunk);
	}

	private static ChunkingOptions OpenAIOptions(int maxTokens) => new()
	{
		MaxTokens = maxTokens,
		TokenCounter = OpenAITokenCounter.ForGpt4(),
		TokenCountingMethod = TokenCountingMethod.OpenAI_CL100K
	};

	private static async Task<ChunkingResult> ChunkTextAsync(string content, DocumentType documentType, ChunkingOptions options)
	{
		using var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));
		return await DocumentChunker.ChunkAsync(stream, documentType, options, CancellationToken);
	}

	private static void AssertChunkTokenCounts(ChunkingResult result, int? maxTokens = null)
	{
		_ = result.Chunks.Should().AllSatisfy(chunk =>
		{
			if (chunk.QualityMetrics != null)
			{
				_ = chunk.QualityMetrics.TokenCount.Should().BePositive();
				_ = chunk.QualityMetrics.TokenCount.Should().BeLessThanOrEqualTo(maxTokens ?? int.MaxValue);
			}
		});
	}
}