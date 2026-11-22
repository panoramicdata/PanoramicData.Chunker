using AwesomeAssertions;
using PanoramicData.Chunker.Chunkers.PlainText;
using PanoramicData.Chunker.Configuration;
using PanoramicData.Chunker.Infrastructure;
using PanoramicData.Chunker.Models;
using System.Text;

namespace PanoramicData.Chunker.Tests.Unit.Chunkers;

public partial class PlainTextDocumentChunkerTests : BaseTest
{
	private readonly PlainTextDocumentChunker _chunker;

	public PlainTextDocumentChunkerTests(ITestOutputHelper output) : base(output)
	{
		_chunker = new PlainTextDocumentChunker(new CharacterBasedTokenCounter());
	}

	[Fact]
	public async Task CanHandleAsync_PlainText_ShouldReturnTrue()
	{
		_ = (await CanHandleTextAsync("This is plain text.\n\nWith multiple paragraphs.")).Should().BeTrue();
	}

	[Fact]
	public async Task CanHandleAsync_HtmlContent_ShouldReturnFalse()
	{
		_ = (await CanHandleTextAsync("<html><body><p>HTML content</p></body></html>")).Should().BeFalse();
	}

	[Fact]
	public async Task CanHandleAsync_MarkdownContent_ShouldReturnFalse()
	{
		_ = (await CanHandleTextAsync("# Header\n\n**Bold** text with ![image](url)")).Should().BeFalse();
	}

	[Fact]
	public async Task CanHandleAsync_EmptyStream_ShouldReturnFalse()
	{
		_ = (await CanHandleTextAsync(string.Empty)).Should().BeFalse();
	}

	[Fact]
	public async Task ChunkAsync_WithParagraphs_ShouldCreateParagraphChunks()
	{
		var paragraphs = await ChunksOfAsync<PlainTextParagraphChunk>("First paragraph.\n\nSecond paragraph.\n\nThird paragraph.");

		_ = paragraphs.Should().HaveCount(3);
		_ = paragraphs[0].Content.Should().Contain("First");
		_ = paragraphs[1].Content.Should().Contain("Second");
		_ = paragraphs[2].Content.Should().Contain("Third");
	}

	[Fact]
	public async Task ChunkAsync_WithMultiLineParagraph_ShouldCombineLines()
	{
		var paragraphs = await ChunksOfAsync<PlainTextParagraphChunk>("This is a paragraph\nthat spans multiple\nlines without blank lines.\n\nNext paragraph.");

		_ = paragraphs.Should().HaveCount(2);
		_ = paragraphs[0].Content.Should().Contain("multiple lines");
	}

	[Fact]
	public async Task ChunkAsync_WithSingleParagraph_ShouldCreateOneChunk()
	{
		var paragraphs = await ChunksOfAsync<PlainTextParagraphChunk>("This is a single paragraph with no line breaks or additional content.");

		_ = paragraphs.Should().ContainSingle();
	}

	[Fact]
	public async Task ChunkAsync_ShouldPopulateQualityMetrics()
	{
		var result = await ChunkTextAsync("This is a paragraph with some content.");

		var chunk = result.Chunks[0];
		_ = chunk.QualityMetrics.Should().NotBeNull();
		_ = chunk.QualityMetrics!.CharacterCount.Should().BePositive();
		_ = chunk.QualityMetrics.WordCount.Should().BePositive();
		_ = chunk.QualityMetrics.TokenCount.Should().BePositive();
	}

	[Fact]
	public async Task ChunkAsync_ShouldPopulateMetadata()
	{
		var result = await ChunkTextAsync("Test content.");

		_ = result.Chunks.Should().AllSatisfy(chunk =>
		{
			_ = chunk.Metadata.Should().NotBeNull();
			_ = chunk.Metadata.DocumentType.Should().Be("PlainText");
			_ = chunk.SequenceNumber.Should().BeGreaterThanOrEqualTo(0);
		});
	}

	[Fact]
	public async Task ChunkAsync_ShouldCalculateStatistics()
	{
		var result = await ChunkTextAsync("Heading\n=======\n\nParagraph 1.\n\nParagraph 2.");

		_ = result.Statistics.Should().NotBeNull();
		_ = result.Statistics.TotalChunks.Should().BePositive();
		_ = result.Statistics.StructuralChunks.Should().BePositive();
		_ = result.Statistics.ContentChunks.Should().BePositive();
		_ = result.Statistics.TotalTokens.Should().BePositive();
	}

	[Fact]
	public async Task ChunkAsync_WithEmptyDocument_ShouldReturnEmptyResult()
	{
		var result = await ChunkTextAsync(string.Empty);

		_ = result.Success.Should().BeTrue();
		_ = result.Chunks.Should().BeEmpty();
	}

	[Fact]
	public async Task ChunkAsync_WithOnlyWhitespace_ShouldReturnEmptyResult()
	{
		var result = await ChunkTextAsync("   \n\n   \n");

		_ = result.Success.Should().BeTrue();
		_ = result.Chunks.Should().BeEmpty();
	}

	private async Task<ChunkingResult> ChunkTextAsync(string text)
	{
		using var stream = new MemoryStream(Encoding.UTF8.GetBytes(text));
		return await _chunker.ChunkAsync(stream, new ChunkingOptions(), CancellationToken);
	}

	private async Task<List<T>> ChunksOfAsync<T>(string text) where T : ChunkerBase
		=> (await ChunkTextAsync(text)).Chunks.OfType<T>().ToList();

	private async Task<bool> CanHandleTextAsync(string text)
	{
		using var stream = new MemoryStream(Encoding.UTF8.GetBytes(text));
		return await _chunker.CanHandleAsync(stream, CancellationToken);
	}
}
