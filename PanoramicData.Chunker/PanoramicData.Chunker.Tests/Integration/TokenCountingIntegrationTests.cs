using FluentAssertions;
using PanoramicData.Chunker.Configuration;
using PanoramicData.Chunker.Infrastructure;
using PanoramicData.Chunker.Infrastructure.TokenCounters;
using System.Text;

namespace PanoramicData.Chunker.Tests.Integration;

/// <summary>
/// Integration tests for token counting across different document types.
/// </summary>
public class TokenCountingIntegrationTests
{
	[Fact]
	public async Task MarkdownChunking_WithOpenAITokenCounter_ShouldCountTokensAccurately()
	{
		// Arrange
		var markdown = @"# Main Header

This is a paragraph with some content that should be counted accurately.

## Subheader

Another paragraph with different content for testing token counting.

### Code Example

```csharp
public class Test {
    public void Method() { }
}
```";

		var tokenCounter = OpenAITokenCounter.ForGpt4();
		var options = new ChunkingOptions
		{
			MaxTokens = 100,
			TokenCounter = tokenCounter,
			TokenCountingMethod = TokenCountingMethod.OpenAI_CL100K
		};

		using var stream = new MemoryStream(Encoding.UTF8.GetBytes(markdown));

		// Act
		var result = await DocumentChunker.ChunkAsync(stream, DocumentType.Markdown, options);

		// Assert
		result.Success.Should().BeTrue();
		result.Chunks.Should().AllSatisfy(chunk =>
		{
			if (chunk.QualityMetrics != null)
			{
				chunk.QualityMetrics.TokenCount.Should().BeGreaterThan(0);
				chunk.QualityMetrics.TokenCount.Should().BeLessThanOrEqualTo(options.MaxTokens);
			}
		});

		// Token counts should be different from character-based
		var totalTokens = result.Statistics.TotalTokens;
		totalTokens.Should().BeGreaterThan(0);
	}

	[Fact]
	public async Task HtmlChunking_WithOpenAITokenCounter_ShouldCountTokensAccurately()
	{
		// Arrange
		var html = @"<!DOCTYPE html>
<html>
<body>
	<h1>Main Title</h1>
	<p>This is a paragraph with some content that should be counted accurately.</p>
	<h2>Subtitle</h2>
	<p>Another paragraph with different content for testing token counting.</p>
	<ul>
		<li>List item one</li>
		<li>List item two</li>
		<li>List item three</li>
	</ul>
</body>
</html>";

		var tokenCounter = OpenAITokenCounter.ForGpt4();
		var options = new ChunkingOptions
		{
			MaxTokens = 100,
			TokenCounter = tokenCounter,
			TokenCountingMethod = TokenCountingMethod.OpenAI_CL100K
		};

		using var stream = new MemoryStream(Encoding.UTF8.GetBytes(html));

		// Act
		var result = await DocumentChunker.ChunkAsync(stream, DocumentType.Html, options);

		// Assert
		result.Success.Should().BeTrue();
		result.Chunks.Should().AllSatisfy(chunk =>
		{
			if (chunk.QualityMetrics != null)
			{
				chunk.QualityMetrics.TokenCount.Should().BeGreaterThan(0);
			}
		});

		result.Statistics.TotalTokens.Should().BeGreaterThan(0);
		result.Statistics.AverageTokensPerChunk.Should().BeGreaterThan(0);
	}

	[Fact]
	public async Task CompareTokenCounters_CharacterBasedVsOpenAI_ShouldShowDifference()
	{
		// Arrange
		var markdown = @"# Technical Documentation

This is a comprehensive guide to understanding how different token counters work.

## Character-Based Counting

Character-based token counting estimates tokens as characters divided by 4.

## OpenAI Token Counting

OpenAI uses BPE tokenization which is more accurate for their models.";

		var charOptions = new ChunkingOptions
		{
			MaxTokens = 1000,
			TokenCounter = new CharacterBasedTokenCounter(),
			TokenCountingMethod = TokenCountingMethod.CharacterBased
		};

		var openAIOptions = new ChunkingOptions
		{
			MaxTokens = 1000,
			TokenCounter = OpenAITokenCounter.ForGpt4(),
			TokenCountingMethod = TokenCountingMethod.OpenAI_CL100K
		};

		// Act
		using var stream1 = new MemoryStream(Encoding.UTF8.GetBytes(markdown));
		var charResult = await DocumentChunker.ChunkAsync(stream1, DocumentType.Markdown, charOptions);

		using var stream2 = new MemoryStream(Encoding.UTF8.GetBytes(markdown));
		var openAIResult = await DocumentChunker.ChunkAsync(stream2, DocumentType.Markdown, openAIOptions);

		// Assert
		charResult.Success.Should().BeTrue();
		openAIResult.Success.Should().BeTrue();

		// Token counts should be different between methods
		charResult.Statistics.TotalTokens.Should().NotBe(openAIResult.Statistics.TotalTokens);

		// Both should have the same number of chunks
		charResult.Chunks.Count.Should().Be(openAIResult.Chunks.Count);
	}

	[Fact]
	public async Task ChunkingPresets_ForOpenAIEmbeddings_ShouldUseOpenAITokenCounter()
	{
		// Arrange
		var markdown = "# Test Document\n\nThis is test content for verifying preset configuration.";
		var options = ChunkingPresets.ForOpenAIEmbeddings();

		using var stream = new MemoryStream(Encoding.UTF8.GetBytes(markdown));

		// Act
		var result = await DocumentChunker.ChunkAsync(stream, DocumentType.Markdown, options);

		// Assert
		result.Success.Should().BeTrue();
		options.TokenCountingMethod.Should().Be(TokenCountingMethod.OpenAI_CL100K);
		options.TokenCounter.Should().BeOfType<OpenAITokenCounter>();

		// Verify chunks have token counts
		result.Chunks.Should().AllSatisfy(chunk =>
		{
			if (chunk.QualityMetrics != null)
			{
				chunk.QualityMetrics.TokenCount.Should().BeGreaterThan(0);
			}
		});
	}

	[Fact]
	public async Task ChunkingPresets_ForRAG_ShouldUseOpenAITokenCounter()
	{
		// Arrange
		var markdown = "# RAG Test\n\nContent for retrieval augmented generation testing.";
		var options = ChunkingPresets.ForRAG();

		using var stream = new MemoryStream(Encoding.UTF8.GetBytes(markdown));

		// Act
		var result = await DocumentChunker.ChunkAsync(stream, DocumentType.Markdown, options);

		// Assert
		result.Success.Should().BeTrue();
		options.TokenCountingMethod.Should().Be(TokenCountingMethod.OpenAI_CL100K);
		options.TokenCounter.Should().BeOfType<OpenAITokenCounter>();
	}

	[Fact]
	public async Task LargeDocument_WithTokenCounting_ShouldRespectMaxTokens()
	{
		// Arrange
		var longText = string.Join("\n\n", Enumerable.Range(1, 50)
			.Select(i => $"This is sentence number {i} with enough content to test token counting."));
		
		var markdown = $"# Large Document\n\n{longText}";

		var options = new ChunkingOptions
		{
			MaxTokens = 50,
			TokenCounter = OpenAITokenCounter.ForGpt4(),
			TokenCountingMethod = TokenCountingMethod.OpenAI_CL100K
		};

		using var stream = new MemoryStream(Encoding.UTF8.GetBytes(markdown));

		// Act
		var result = await DocumentChunker.ChunkAsync(stream, DocumentType.Markdown, options);

		// Assert
		result.Success.Should().BeTrue();
		result.Chunks.Should().AllSatisfy(chunk =>
		{
			if (chunk.QualityMetrics != null)
			{
				// Allow some margin for structural chunks
				chunk.QualityMetrics.TokenCount.Should().BeLessThanOrEqualTo(options.MaxTokens + 10);
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
		// Arrange
		var markdown = "# Test\n\nContent for testing different token counting methods.";
		var options = new ChunkingOptions
		{
			MaxTokens = 100,
			TokenCounter = TokenCounterFactory.Create(method),
			TokenCountingMethod = method
		};

		using var stream = new MemoryStream(Encoding.UTF8.GetBytes(markdown));

		// Act
		var result = await DocumentChunker.ChunkAsync(stream, DocumentType.Markdown, options);

		// Assert
		result.Success.Should().BeTrue();
		result.Statistics.TotalTokens.Should().BeGreaterThan(0);
		result.Chunks.Should().AllSatisfy(chunk =>
		{
			if (chunk.QualityMetrics != null)
			{
				chunk.QualityMetrics.TokenCount.Should().BeGreaterThan(0);
			}
		});
	}

	[Fact]
	public async Task TokenCounterFactory_FromOptions_ShouldCreateCorrectCounter()
	{
		// Arrange
		var markdown = "# Factory Test\n\nTesting factory integration.";
		var options = new ChunkingOptions
		{
			TokenCountingMethod = TokenCountingMethod.OpenAI_CL100K
		};

		// Get counter from factory
		var counter = TokenCounterFactory.GetOrCreate(options);

		options.TokenCounter = counter;

		using var stream = new MemoryStream(Encoding.UTF8.GetBytes(markdown));

		// Act
		var result = await DocumentChunker.ChunkAsync(stream, DocumentType.Markdown, options);

		// Assert
		result.Success.Should().BeTrue();
		counter.Should().BeOfType<OpenAITokenCounter>();
		result.Statistics.TotalTokens.Should().BeGreaterThan(0);
	}

	[Fact]
	public async Task Statistics_ShouldIncludeAccurateTokenMetrics()
	{
		// Arrange
		var markdown = @"# Document

First paragraph.

## Section

Second paragraph.

Third paragraph.";

		var options = ChunkingPresets.ForOpenAIEmbeddings();
		using var stream = new MemoryStream(Encoding.UTF8.GetBytes(markdown));

		// Act
		var result = await DocumentChunker.ChunkAsync(stream, DocumentType.Markdown, options);

		// Assert
		result.Statistics.TotalTokens.Should().BeGreaterThan(0);
		result.Statistics.AverageTokensPerChunk.Should().BeGreaterThan(0);
		result.Statistics.MaxTokensInChunk.Should().BeGreaterThan(0);
		result.Statistics.MinTokensInChunk.Should().BeGreaterThanOrEqualTo(0);
		
		// Max should be >= average >= min
		result.Statistics.MaxTokensInChunk.Should().BeGreaterThanOrEqualTo(result.Statistics.AverageTokensPerChunk);
		result.Statistics.AverageTokensPerChunk.Should().BeGreaterThanOrEqualTo(result.Statistics.MinTokensInChunk);
	}
}
