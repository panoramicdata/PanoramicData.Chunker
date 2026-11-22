using AwesomeAssertions;
using PanoramicData.Chunker.Chunkers.Docx;
using PanoramicData.Chunker.Configuration;
using PanoramicData.Chunker.Infrastructure.TokenCounters;
using PanoramicData.Chunker.Models;
using PanoramicData.Chunker.Tests.Utilities;

namespace PanoramicData.Chunker.Tests.Unit.Chunkers;

/// <summary>
/// Unit tests for DocxDocumentChunker.
/// </summary>
public class DocxDocumentChunkerTests(ITestOutputHelper output)
	: OfficeChunkerContractTests(output, tokenCounter => new DocxDocumentChunker(tokenCounter), "Docx", "simple.docx")
{
	[Fact]
	public void SupportedType_ShouldReturnDocx()
	{
		_ = SupportedType.Should().Be(DocumentType.Docx);
	}

	[Fact]
	public async Task CanHandleAsync_ShouldReturnTrue_ForDocxFile()
	{
		_ = (await CanHandleFixtureAsync("simple.docx")).Should().BeTrue();
	}

	[Fact]
	public async Task CanHandleAsync_ShouldReturnFalse_ForNonDocxContent()
	{
		_ = (await CanHandleTextAsync("This is plain text")).Should().BeFalse();
	}

	[Fact]
	public async Task ChunkAsync_ShouldExtractSections_FromDocxWithHeadings()
	{
		var result = await ChunkSimpleAsync(new ChunkingOptions { MaxTokens = 500 });

		_ = result.Success.Should().BeTrue();
		_ = result.Chunks.Should().NotBeEmpty();
		_ = result.Chunks.OfType<DocxSectionChunk>().Should().NotBeEmpty();
	}

	[Fact]
	public async Task ChunkAsync_ShouldExtractParagraphs_FromDocx()
	{
		var result = await ChunkSimpleAsync(new ChunkingOptions { MaxTokens = 500 });

		_ = result.Success.Should().BeTrue();
		_ = result.Chunks.OfType<DocxParagraphChunk>().Should().NotBeEmpty();
	}

	[Fact]
	public async Task ChunkAsync_WithOpenAITokenCounter_ShouldCalculateAccurateTokenCounts()
	{
		var chunker = new DocxDocumentChunker(OpenAITokenCounter.ForGpt4());

		var result = await ChunkWithAsync(chunker, "simple.docx", new ChunkingOptions());

		_ = result.Success.Should().BeTrue();
		_ = result.Chunks.Should().NotBeEmpty();
		ChunkAssert.AllHaveMetrics(result.Chunks);

		var hasAccurateTokenization = result.Chunks.Any(chunk => chunk.QualityMetrics!.TokenCount != (int)Math.Ceiling(chunk.QualityMetrics.CharacterCount / 4.0));

		_ = hasAccurateTokenization.Should().BeTrue("OpenAI tokenization should differ from char-based approximation for at least some chunks");
	}

	[Fact]
	public async Task ChunkAsync_ShouldHandleEmptyDocument()
	{
		var result = await ChunkFixtureAsync("empty.docx", new ChunkingOptions());

		_ = result.Success.Should().BeTrue();
		_ = result.Chunks.Should().BeEmpty();
	}

	[Fact]
	public async Task ChunkAsync_ShouldExtractTables()
	{
		var result = await ChunkFixtureAsync("with-tables.docx", new ChunkingOptions());

		_ = result.Success.Should().BeTrue();
		var tables = result.Chunks.OfType<DocxTableChunk>().ToList();
		_ = tables.Should().NotBeEmpty();
		ChunkAssert.TablesAreMarkdown(tables);
	}

	[Fact]
	public async Task ChunkAsync_ShouldExtractListItems()
	{
		var result = await ChunkFixtureAsync("with-lists.docx", new ChunkingOptions());

		_ = result.Success.Should().BeTrue();
		_ = result.Chunks.OfType<DocxListItemChunk>().Should().NotBeEmpty();
	}
}