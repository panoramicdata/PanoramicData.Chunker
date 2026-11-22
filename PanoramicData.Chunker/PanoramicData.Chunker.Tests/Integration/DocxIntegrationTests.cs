using AwesomeAssertions;
using PanoramicData.Chunker.Chunkers.Docx;
using PanoramicData.Chunker.Configuration;
using PanoramicData.Chunker.Infrastructure;

namespace PanoramicData.Chunker.Tests.Integration;

/// <summary>
/// Integration tests for DOCX document chunking.
/// Tests end-to-end chunking of real DOCX files.
/// </summary>
public class DocxIntegrationTests(ITestOutputHelper output) : BaseTest(output)
{
	[Fact]
	public async Task ChunkAsync_SimpleDocument_ShouldExtractAllElements()
	{
		var result = await ChunkDocxAsync("simple.docx", ChunkingPresets.ForRAG());

		_ = result.Success.Should().BeTrue();
		_ = result.Chunks.Should().NotBeEmpty();
		_ = result.Statistics.Should().NotBeNull();
		_output.WriteLine($"Simple document: {result.Statistics.TotalChunks} chunks, {result.Statistics.StructuralChunks} structural, {result.Statistics.ContentChunks} content, max depth {result.Statistics.MaxDepth}");
	}

	[Fact]
	public async Task ChunkAsync_ComplexDocument_ShouldHandleMixedContent()
	{
		var result = await ChunkDocxAsync("complex.docx", ChunkingPresets.ForRAG());

		_ = result.Success.Should().BeTrue();
		_ = result.Chunks.Should().NotBeEmpty();
		_output.WriteLine($"Complex document: {result.Chunks.OfType<DocxSectionChunk>().Count()} sections, {result.Chunks.OfType<DocxParagraphChunk>().Count()} paragraphs, {result.Chunks.OfType<DocxListItemChunk>().Count()} list items, {result.Chunks.OfType<DocxTableChunk>().Count()} tables");
		_ = result.Statistics.MaxDepth.Should().BePositive();
	}

	[Fact]
	public async Task ChunkAsync_WithTables_ShouldExtractTableStructure()
	{
		var result = await ChunkDocxAsync("with-tables.docx", ChunkingPresets.ForRAG());

		_ = result.Success.Should().BeTrue();

		var tables = result.Chunks.OfType<DocxTableChunk>().ToList();
		_ = tables.Should().NotBeEmpty();
		_ = tables.Should().AllSatisfy(table =>
		{
			_ = table.SerializedTable.Should().NotBeEmpty();
			_ = table.TableInfo.Should().NotBeNull();
			_ = table.TableInfo!.RowCount.Should().BePositive();
			_ = table.TableInfo.ColumnCount.Should().BePositive();
		});
	}

	[Fact]
	public async Task ChunkAsync_WithLists_ShouldPreserveListStructure()
	{
		var result = await ChunkDocxAsync("with-lists.docx", ChunkingPresets.ForRAG());

		_ = result.Success.Should().BeTrue();

		var listItems = result.Chunks.OfType<DocxListItemChunk>().ToList();
		_ = listItems.Should().NotBeEmpty();
		_ = listItems.Max(li => li.ListLevel).Should().BeLessThan(10);
	}

	[Fact]
	public async Task ChunkAsync_WithOpenAITokenCounter_ShouldCalculateAccurateTokens()
	{
		var options = ChunkingPresets.ForOpenAIEmbeddings();

		var result = await ChunkDocxAsync("simple.docx", options, TokenCounterFactory.GetOrCreate(options));

		_ = result.Success.Should().BeTrue();
		_ = result.Chunks.Should().NotBeEmpty();
		_ = result.Chunks.Select(chunk => chunk.QualityMetrics).Should().OnlyContain(metrics => metrics != null && metrics.TokenCount > 0);

		var maxTokens = options.MaxTokens;
		_ = result.Chunks.Should().OnlyContain(c => c.QualityMetrics!.TokenCount <= maxTokens);
	}

	[Fact]
	public async Task ChunkAsync_LargeDocument_ShouldHandleEfficiently()
	{
		var startTime = DateTime.UtcNow;
		var result = await ChunkDocxAsync("large.docx", ChunkingPresets.ForRAG());
		var duration = DateTime.UtcNow - startTime;

		_ = result.Success.Should().BeTrue();
		_ = result.Chunks.Should().NotBeEmpty();
		_output.WriteLine($"Large document: {result.Statistics.TotalChunks} chunks in {duration.TotalMilliseconds}ms");
		_ = duration.Should().BeLessThan(TimeSpan.FromSeconds(30));
	}

	[Fact]
	public async Task ChunkAsync_WithFormattedText_ShouldExtractAnnotations()
	{
		var result = await ChunkDocxAsync("formatted.docx", ChunkingPresets.ForRAG());

		_ = result.Success.Should().BeTrue();

		var paragraphs = result.Chunks.OfType<DocxParagraphChunk>()
			.Where(p => p.Annotations?.Count > 0)
			.ToList();

		_output.WriteLine($"Paragraphs with annotations: {paragraphs.Count}");
	}
}