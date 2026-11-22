using AwesomeAssertions;
using PanoramicData.Chunker.Chunkers.PlainText;
using PanoramicData.Chunker.Configuration;
using PanoramicData.Chunker.Models;
using PanoramicData.Chunker.Tests.Utilities;
using System.Text;

namespace PanoramicData.Chunker.Tests.Integration;

/// <summary>
/// Integration tests for Plain Text document chunking.
/// </summary>
public class PlainTextIntegrationTests(ITestOutputHelper output) : BaseTest(output)
{
	private readonly string _testDataPath = Path.Combine("TestData", "PlainText");

	[Fact]
	public async Task ChunkAsync_SimpleDocument_ShouldChunkSuccessfully()
	{
		var result = await ChunkTestFileAsync("simple.txt");

		_ = result.Success.Should().BeTrue();
		_ = result.Chunks.Should().NotBeEmpty();
		_ = result.Chunks.OfType<PlainTextParagraphChunk>().Should().HaveCountGreaterThan(1);
		ChunkAssert.AllHaveTokenCounts(result.Chunks);
	}

	[Fact]
	public async Task ChunkAsync_StructuredDocument_ShouldDetectAllStructures()
	{
		var result = await ChunkTestFileAsync("structured.txt");

		_ = result.Success.Should().BeTrue();

		var sections = result.Chunks.OfType<PlainTextSectionChunk>().ToList();
		_ = sections.Should().NotBeEmpty();
		_ = sections.Select(s => s.HeadingType).Should().Contain([HeadingHeuristic.AllCaps, HeadingHeuristic.Underlined, HeadingHeuristic.Numbered]);
		_ = result.Chunks.OfType<PlainTextListItemChunk>().Should().NotBeEmpty();
		_ = result.Chunks.OfType<PlainTextCodeBlockChunk>().Should().NotBeEmpty();
		_ = result.Chunks.OfType<PlainTextParagraphChunk>().Should().NotBeEmpty();
	}

	[Fact]
	public async Task ChunkAsync_AllCapsHeaders_ShouldDetectAllHeadings()
	{
		var sections = await SuccessfulChunksAsync<PlainTextSectionChunk>(ChunkTestFileAsync("all-caps-headers.txt"));

		_ = sections.Should().HaveCountGreaterThan(3);
		_ = sections.Should().AllSatisfy(s => s.HeadingType.Should().Be(HeadingHeuristic.AllCaps));
	}

	[Fact]
	public async Task ChunkAsync_UnderlinedHeaders_ShouldDetectLevels()
	{
		var sections = await SectionsWithLevelsAsync("underlined-headers.txt", 1, 2);

		_ = sections.Should().AllSatisfy(s => s.HeadingType.Should().Be(HeadingHeuristic.Underlined));
	}

	[Fact]
	public async Task ChunkAsync_NumberedSections_ShouldDetectHierarchy()
	{
		var sections = await SectionsWithLevelsAsync("numbered-sections.txt", 1, 2, 3);

		_ = sections.Where(s => s.HeadingLevel == 1).Should().AllSatisfy(s => s.ParentId.Should().BeNull());
		_ = sections.Where(s => s.HeadingLevel == 2).Should().AllSatisfy(s => s.ParentId.Should().NotBeNull());
	}

	[Fact]
	public async Task ChunkAsync_Lists_ShouldDetectAllListTypes()
	{
		var listItems = await SuccessfulChunksAsync<PlainTextListItemChunk>(ChunkTestFileAsync("lists.txt"));

		_ = listItems.Should().HaveCountGreaterThan(10);
		_ = listItems.Select(li => li.ListType).Should().Contain(["bullet", "numbered"]);
		_ = listItems.Should().Contain(li => li.NestingLevel == 0);
		_ = listItems.Should().Contain(li => li.NestingLevel > 0);
	}

	[Fact]
	public async Task ChunkAsync_CodeHeavy_ShouldDetectAllCodeBlocks()
	{
		var codeBlocks = await SuccessfulChunksAsync<PlainTextCodeBlockChunk>(ChunkTestFileAsync("code-heavy.txt"));

		_ = codeBlocks.Should().HaveCountGreaterThan(3);
		_ = codeBlocks.Should().Contain(cb => cb.IsFenced);
		_ = codeBlocks.Should().Contain(cb => !cb.IsFenced);
		_ = codeBlocks.Should().Contain(cb => cb.Language != null);
	}

	[Fact]
	public async Task ChunkAsync_MixedContent_ShouldHandleAllTypes()
	{
		var result = await ChunkTestFileAsync("mixed.txt");

		_ = result.Success.Should().BeTrue();
		_ = result.Chunks.OfType<PlainTextSectionChunk>().Should().NotBeEmpty();
		_ = result.Chunks.OfType<PlainTextParagraphChunk>().Should().NotBeEmpty();
		_ = result.Chunks.OfType<PlainTextListItemChunk>().Should().NotBeEmpty();
		_ = result.Chunks.OfType<PlainTextCodeBlockChunk>().Should().NotBeEmpty();
		_ = result.Statistics.TotalChunks.Should().BeGreaterThan(10);
		_ = result.Statistics.TotalTokens.Should().BePositive();
	}

	[Fact]
	public async Task ChunkAsync_LargeDocument_ShouldHandleEfficiently()
	{
		var options = ChunkingPresets.ForOpenAIEmbeddings();

		var startTime = DateTime.UtcNow;
		var result = await ChunkTextWithAsync("large.txt", options);
		var processingTime = DateTime.UtcNow - startTime;

		_ = result.Success.Should().BeTrue();
		_ = result.Chunks.Should().NotBeEmpty();
		_ = processingTime.TotalSeconds.Should().BeLessThan(5);
		_ = result.Statistics.TotalChunks.Should().BeGreaterThan(20);
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
		var result = await ChunkAutoDetectFileAsync(Path.Combine(_testDataPath, "simple.txt"), "test.txt");

		_ = result.Success.Should().BeTrue();
		_ = result.Chunks.Should().NotBeEmpty();
	}

	[Fact]
	public async Task ChunkAsync_WithDifferentTokenCounters_ShouldProduceConsistentChunks()
	{
		var charResult = await ChunkTextWithAsync("structured.txt", ChunkingPresets.ForFastProcessing());
		var openAIResult = await ChunkTextWithAsync("structured.txt", ChunkingPresets.ForOpenAIEmbeddings());

		ChunkAssert.ComparableTokenCounts(charResult, openAIResult);
	}

	private Task<ChunkingResult> ChunkTestFileAsync(string fileName)
		=> ChunkFileAsync(Path.Combine(_testDataPath, fileName), DocumentType.PlainText);

	private async Task<List<PlainTextSectionChunk>> SectionsWithLevelsAsync(string fileName, params int[] levels)
	{
		var sections = await SuccessfulChunksAsync<PlainTextSectionChunk>(ChunkTestFileAsync(fileName));

		_ = sections.Should().NotBeEmpty();
		_ = sections.Select(s => s.HeadingLevel).Should().Contain(levels);
		return sections;
	}

	private async Task<ChunkingResult> ChunkTextWithAsync(string fileName, ChunkingOptions options)
	{
		var text = await File.ReadAllTextAsync(Path.Combine(_testDataPath, fileName), CancellationToken);
		using var stream = new MemoryStream(Encoding.UTF8.GetBytes(text));
		return await DocumentChunker.ChunkAsync(stream, DocumentType.PlainText, options, CancellationToken);
	}
}