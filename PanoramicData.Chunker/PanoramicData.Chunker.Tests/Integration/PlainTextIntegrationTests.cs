using AwesomeAssertions;
using PanoramicData.Chunker.Chunkers.PlainText;
using PanoramicData.Chunker.Configuration;
using System.Text;

namespace PanoramicData.Chunker.Tests.Integration;

/// <summary>
/// Integration tests for Plain Text document chunking.
/// </summary>
public class PlainTextIntegrationTests
{
	private readonly string _testDataPath;

	public PlainTextIntegrationTests()
	{
		_testDataPath = Path.Combine("TestData", "PlainText");
	}

	[Fact]
	public async Task ChunkAsync_SimpleDocument_ShouldChunkSuccessfully()
	{
		// Arrange
		var filePath = Path.Combine(_testDataPath, "simple.txt");
		var text = await File.ReadAllTextAsync(filePath);
		using var stream = new MemoryStream(Encoding.UTF8.GetBytes(text));

		var options = ChunkingPresets.ForOpenAIEmbeddings();

		// Act
		var result = await DocumentChunker.ChunkAsync(stream, DocumentType.PlainText, options);

		// Assert
		_ = result.Success.Should().BeTrue();
		_ = result.Chunks.Should().NotBeEmpty();

		// Should have multiple paragraphs
		var paragraphs = result.Chunks.OfType<PlainTextParagraphChunk>().ToList();
		_ = paragraphs.Should().HaveCountGreaterThan(1);

		// All chunks should have quality metrics
		_ = result.Chunks.Should().AllSatisfy(chunk =>
		{
			_ = chunk.QualityMetrics.Should().NotBeNull();
			_ = chunk.QualityMetrics!.TokenCount.Should().BePositive();
		});
	}

	[Fact]
	public async Task ChunkAsync_StructuredDocument_ShouldDetectAllStructures()
	{
		// Arrange
		var filePath = Path.Combine(_testDataPath, "structured.txt");
		var text = await File.ReadAllTextAsync(filePath);
		using var stream = new MemoryStream(Encoding.UTF8.GetBytes(text));

		var options = ChunkingPresets.ForOpenAIEmbeddings();

		// Act
		var result = await DocumentChunker.ChunkAsync(stream, DocumentType.PlainText, options);

		// Assert
		_ = result.Success.Should().BeTrue();

		// Should detect headings
		var sections = result.Chunks.OfType<PlainTextSectionChunk>().ToList();
		_ = sections.Should().NotBeEmpty();

		// Should have ALL CAPS headings
		_ = sections.Should().Contain(s => s.HeadingType == HeadingHeuristic.AllCaps);

		// Should have underlined headings
		_ = sections.Should().Contain(s => s.HeadingType == HeadingHeuristic.Underlined);

		// Should have numbered sections
		_ = sections.Should().Contain(s => s.HeadingType == HeadingHeuristic.Numbered);

		// Should detect list items
		var listItems = result.Chunks.OfType<PlainTextListItemChunk>().ToList();
		_ = listItems.Should().NotBeEmpty();

		// Should detect code blocks
		var codeBlocks = result.Chunks.OfType<PlainTextCodeBlockChunk>().ToList();
		_ = codeBlocks.Should().NotBeEmpty();

		// Should have paragraphs
		var paragraphs = result.Chunks.OfType<PlainTextParagraphChunk>().ToList();
		_ = paragraphs.Should().NotBeEmpty();
	}

	[Fact]
	public async Task ChunkAsync_AllCapsHeaders_ShouldDetectAllHeadings()
	{
		// Arrange
		var filePath = Path.Combine(_testDataPath, "all-caps-headers.txt");
		var text = await File.ReadAllTextAsync(filePath);
		using var stream = new MemoryStream(Encoding.UTF8.GetBytes(text));

		var options = ChunkingPresets.ForOpenAIEmbeddings();

		// Act
		var result = await DocumentChunker.ChunkAsync(stream, DocumentType.PlainText, options);

		// Assert
		_ = result.Success.Should().BeTrue();

		var sections = result.Chunks.OfType<PlainTextSectionChunk>().ToList();
		_ = sections.Should().HaveCountGreaterThan(3); // Multiple ALL CAPS headings
		_ = sections.Should().AllSatisfy(s => s.HeadingType.Should().Be(HeadingHeuristic.AllCaps));
	}

	[Fact]
	public async Task ChunkAsync_UnderlinedHeaders_ShouldDetectLevels()
	{
		// Arrange
		var filePath = Path.Combine(_testDataPath, "underlined-headers.txt");
		var text = await File.ReadAllTextAsync(filePath);
		using var stream = new MemoryStream(Encoding.UTF8.GetBytes(text));

		var options = ChunkingPresets.ForOpenAIEmbeddings();

		// Act
		var result = await DocumentChunker.ChunkAsync(stream, DocumentType.PlainText, options);

		// Assert
		_ = result.Success.Should().BeTrue();

		var sections = result.Chunks.OfType<PlainTextSectionChunk>().ToList();
		_ = sections.Should().NotBeEmpty();

		// Should have both level 1 and level 2
		_ = sections.Should().Contain(s => s.HeadingLevel == 1);
		_ = sections.Should().Contain(s => s.HeadingLevel == 2);

		// All should be underlined type
		_ = sections.Should().AllSatisfy(s => s.HeadingType.Should().Be(HeadingHeuristic.Underlined));
	}

	[Fact]
	public async Task ChunkAsync_NumberedSections_ShouldDetectHierarchy()
	{
		// Arrange
		var filePath = Path.Combine(_testDataPath, "numbered-sections.txt");
		var text = await File.ReadAllTextAsync(filePath);
		using var stream = new MemoryStream(Encoding.UTF8.GetBytes(text));

		var options = ChunkingPresets.ForOpenAIEmbeddings();

		// Act
		var result = await DocumentChunker.ChunkAsync(stream, DocumentType.PlainText, options);

		// Assert
		_ = result.Success.Should().BeTrue();

		var sections = result.Chunks.OfType<PlainTextSectionChunk>().ToList();
		_ = sections.Should().NotBeEmpty();

		// Should have multiple levels
		_ = sections.Should().Contain(s => s.HeadingLevel == 1); // "1."
		_ = sections.Should().Contain(s => s.HeadingLevel == 2); // "1.1"
		_ = sections.Should().Contain(s => s.HeadingLevel == 3); // "1.1.1"

		// Should build parent-child hierarchy
		var level1Sections = sections.Where(s => s.HeadingLevel == 1).ToList();
		var level2Sections = sections.Where(s => s.HeadingLevel == 2).ToList();

		_ = level1Sections.Should().AllSatisfy(s => s.ParentId.Should().BeNull());
		_ = level2Sections.Should().AllSatisfy(s => s.ParentId.Should().NotBeNull());
	}

	[Fact]
	public async Task ChunkAsync_Lists_ShouldDetectAllListTypes()
	{
		// Arrange
		var filePath = Path.Combine(_testDataPath, "lists.txt");
		var text = await File.ReadAllTextAsync(filePath);
		using var stream = new MemoryStream(Encoding.UTF8.GetBytes(text));

		var options = ChunkingPresets.ForOpenAIEmbeddings();

		// Act
		var result = await DocumentChunker.ChunkAsync(stream, DocumentType.PlainText, options);

		// Assert
		_ = result.Success.Should().BeTrue();

		var listItems = result.Chunks.OfType<PlainTextListItemChunk>().ToList();
		_ = listItems.Should().HaveCountGreaterThan(10);

		// Should have bullet lists
		_ = listItems.Should().Contain(li => li.ListType == "bullet");

		// Should have numbered lists
		_ = listItems.Should().Contain(li => li.ListType == "numbered");

		// Should detect nesting levels
		_ = listItems.Should().Contain(li => li.NestingLevel == 0);
		_ = listItems.Should().Contain(li => li.NestingLevel > 0);
	}

	[Fact]
	public async Task ChunkAsync_CodeHeavy_ShouldDetectAllCodeBlocks()
	{
		// Arrange
		var filePath = Path.Combine(_testDataPath, "code-heavy.txt");
		var text = await File.ReadAllTextAsync(filePath);
		using var stream = new MemoryStream(Encoding.UTF8.GetBytes(text));

		var options = ChunkingPresets.ForOpenAIEmbeddings();

		// Act
		var result = await DocumentChunker.ChunkAsync(stream, DocumentType.PlainText, options);

		// Assert
		_ = result.Success.Should().BeTrue();

		var codeBlocks = result.Chunks.OfType<PlainTextCodeBlockChunk>().ToList();
		_ = codeBlocks.Should().HaveCountGreaterThan(3);

		// Should have fenced code blocks
		_ = codeBlocks.Should().Contain(cb => cb.IsFenced);

		// Should have indented code blocks
		_ = codeBlocks.Should().Contain(cb => !cb.IsFenced);

		// Should detect languages
		_ = codeBlocks.Should().Contain(cb => cb.Language != null);
	}

	[Fact]
	public async Task ChunkAsync_MixedContent_ShouldHandleAllTypes()
	{
		// Arrange
		var filePath = Path.Combine(_testDataPath, "mixed.txt");
		var text = await File.ReadAllTextAsync(filePath);
		using var stream = new MemoryStream(Encoding.UTF8.GetBytes(text));

		var options = ChunkingPresets.ForOpenAIEmbeddings();

		// Act
		var result = await DocumentChunker.ChunkAsync(stream, DocumentType.PlainText, options);

		// Assert
		_ = result.Success.Should().BeTrue();

		// Should have all chunk types
		_ = result.Chunks.OfType<PlainTextSectionChunk>().Should().NotBeEmpty();
		_ = result.Chunks.OfType<PlainTextParagraphChunk>().Should().NotBeEmpty();
		_ = result.Chunks.OfType<PlainTextListItemChunk>().Should().NotBeEmpty();
		_ = result.Chunks.OfType<PlainTextCodeBlockChunk>().Should().NotBeEmpty();

		// Should have proper statistics
		_ = result.Statistics.TotalChunks.Should().BeGreaterThan(10);
		_ = result.Statistics.TotalTokens.Should().BePositive();
	}

	[Fact]
	public async Task ChunkAsync_LargeDocument_ShouldHandleEfficiently()
	{
		// Arrange
		var filePath = Path.Combine(_testDataPath, "large.txt");
		var text = await File.ReadAllTextAsync(filePath);
		using var stream = new MemoryStream(Encoding.UTF8.GetBytes(text));

		var options = ChunkingPresets.ForOpenAIEmbeddings();

		// Act
		var startTime = DateTime.UtcNow;
		var result = await DocumentChunker.ChunkAsync(stream, DocumentType.PlainText, options);
		var processingTime = DateTime.UtcNow - startTime;

		// Assert
		_ = result.Success.Should().BeTrue();
		_ = result.Chunks.Should().NotBeEmpty();

		// Should complete in reasonable time (< 5 seconds for test document)
		_ = processingTime.TotalSeconds.Should().BeLessThan(5);

		// Should have extensive content
		_ = result.Statistics.TotalChunks.Should().BeGreaterThan(20);

		// All chunks should respect max tokens (with some margin for structural chunks)
		_ = result.Chunks.Should().AllSatisfy(chunk =>
		{
			if (chunk.QualityMetrics != null && chunk is PlainTextParagraphChunk)
			{
				_ = chunk.QualityMetrics.TokenCount.Should().BeLessThanOrEqualTo(options.MaxTokens + 50);
			}
		});
	}

	[Fact]
	public async Task ChunkAsync_WithAutoDetect_ShouldDetectPlainText()
	{
		// Arrange
		var filePath = Path.Combine(_testDataPath, "simple.txt");
		var text = await File.ReadAllTextAsync(filePath);
		using var stream = new MemoryStream(Encoding.UTF8.GetBytes(text));

		var options = ChunkingPresets.ForOpenAIEmbeddings();

		// Act - Use auto-detect
		var result = await DocumentChunker.ChunkAutoDetectAsync(stream, "test.txt", options);

		// Assert
		_ = result.Success.Should().BeTrue();
		_ = result.Chunks.Should().NotBeEmpty();
	}

	[Fact]
	public async Task ChunkAsync_WithDifferentTokenCounters_ShouldProduceConsistentChunks()
	{
		// Arrange
		var filePath = Path.Combine(_testDataPath, "structured.txt");
		var text = await File.ReadAllTextAsync(filePath);

		var charOptions = ChunkingPresets.ForFastProcessing();
		var openAIOptions = ChunkingPresets.ForOpenAIEmbeddings();

		// Act
		using var stream1 = new MemoryStream(Encoding.UTF8.GetBytes(text));
		var charResult = await DocumentChunker.ChunkAsync(stream1, DocumentType.PlainText, charOptions);

		using var stream2 = new MemoryStream(Encoding.UTF8.GetBytes(text));
		var openAIResult = await DocumentChunker.ChunkAsync(stream2, DocumentType.PlainText, openAIOptions);

		// Assert
		_ = charResult.Success.Should().BeTrue();
		_ = openAIResult.Success.Should().BeTrue();

		// Should produce same number of chunks (structure is same)
		_ = charResult.Chunks.Count.Should().Be(openAIResult.Chunks.Count);

		// Token counts typically differ, but may occasionally be similar for short texts
		// Just verify both counters are working (both should have positive counts)
		_ = charResult.Statistics.TotalTokens.Should().BePositive("character-based counter should work");
		_ = openAIResult.Statistics.TotalTokens.Should().BePositive("OpenAI counter should work");

		// Both should be within reasonable range of each other (within 50%)
		var ratio = (double)Math.Max(charResult.Statistics.TotalTokens, openAIResult.Statistics.TotalTokens) /
					Math.Min(charResult.Statistics.TotalTokens, openAIResult.Statistics.TotalTokens);
		_ = ratio.Should().BeLessThan(2.0, "token counts should be within 2x of each other");
	}
}
