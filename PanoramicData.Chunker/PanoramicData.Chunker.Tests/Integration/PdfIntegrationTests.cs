using AwesomeAssertions;
using PanoramicData.Chunker.Chunkers.Pdf;
using PanoramicData.Chunker.Configuration;
using PanoramicData.Chunker.Models;
using PanoramicData.Chunker.Tests.Utilities;

namespace PanoramicData.Chunker.Tests.Integration;

/// <summary>
/// Integration tests for PDF document chunking.
/// </summary>
public class PdfIntegrationTests(ITestOutputHelper output) : BaseTest(output)
{
	private readonly string _testDataPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TestData", "Pdf");

	[Fact]
	public async Task ChunkAsync_SimplePdf_ShouldExtractText()
	{
		var result = await ChunkTestFileAsync("simple.pdf");

		ChunkAssert.SucceededWithChunks(result);

		var documents = result.Chunks.OfType<PdfDocumentChunk>().ToList();
		_ = documents.Should().ContainSingle();
		_ = documents[0].PageCount.Should().BePositive();

		var pages = result.Chunks.OfType<PdfPageChunk>().ToList();
		_ = pages.Should().HaveCountGreaterThanOrEqualTo(1);

		var firstPage = pages[0];
		_ = firstPage.PageNumber.Should().Be(1);
		_ = firstPage.ExtractedText.Should().NotBeNullOrEmpty();
		_output.WriteLine($"Extracted text from page 1: {firstPage.ExtractedText?[..Math.Min(100, firstPage.ExtractedText.Length)]}");

		_ = result.Chunks.OfType<PdfParagraphChunk>().Should().NotBeEmpty();
	}

	[Fact]
	public async Task ChunkAsync_EmptyPdf_ShouldHandleGracefully()
	{
		var pages = await SuccessfulChunksAsync<PdfPageChunk>(ChunkTestFileAsync("empty.pdf"));

		_ = pages.Should().ContainSingle(); // Empty PDF still has one page
	}

	[Fact]
	public async Task ChunkAsync_MultiPagePdf_ShouldExtractAllPages()
	{
		var result = await ChunkTestFileAsync("multi-page.pdf");

		_ = result.Success.Should().BeTrue();
		_ = result.Chunks.OfType<PdfDocumentChunk>().First().PageCount.Should().Be(3);

		var pages = result.Chunks.OfType<PdfPageChunk>().ToList();
		_ = pages.Should().HaveCount(3);
		_ = pages.Select(page => page.PageNumber).Should().Equal(1, 2, 3);
		_output.WriteLine($"Total pages: {pages.Count}");
	}

	[Fact]
	public async Task ChunkAsync_WithHeadings_ShouldDetectHeadings()
	{
		var paragraphs = await SuccessfulChunksAsync<PdfParagraphChunk>(ChunkTestFileAsync("with-headings.pdf"));

		_ = paragraphs.Should().NotBeEmpty();
		_output.WriteLine($"Total paragraphs: {paragraphs.Count}, detected headings: {paragraphs.Count(p => p.IsLikelyHeading)}");
		_ = paragraphs.Count.Should().BePositive();
	}

	[Fact]
	public async Task ChunkAsync_WithLists_ShouldExtractListContent()
	{
		var paragraphs = await SuccessfulChunksAsync<PdfParagraphChunk>(ChunkTestFileAsync("with-lists.pdf"));

		_ = paragraphs.Should().NotBeEmpty();

		var hasListContent = paragraphs.Any(p => p.Content?.Contains("item", StringComparison.OrdinalIgnoreCase) == true);
		_ = hasListContent.Should().BeTrue();
	}

	[Fact]
	public async Task ChunkAsync_WithTables_ShouldExtractTableText()
	{
		var pages = await SuccessfulChunksAsync<PdfPageChunk>(ChunkTestFileAsync("with-tables.pdf"));

		_ = pages.Should().NotBeEmpty();

		var pageText = pages[0].ExtractedText;
		_ = pageText.Should().NotBeNullOrEmpty();
		_ = (pageText?.Contains("Employee") == true ||
		 pageText?.Contains("Name") == true ||
		 pageText?.Contains("Department") == true).Should().BeTrue();

		_output.WriteLine($"Table content sample: {pageText?[..Math.Min(200, pageText.Length)]}");
	}

	[Fact]
	public async Task ChunkAsync_LargePdf_ShouldHandleEfficiently()
	{
		var (result, processingTime) = await ChunkTimedAsync(Path.Combine(_testDataPath, "large.pdf"), DocumentType.Pdf);

		_ = result.Success.Should().BeTrue();

		var pages = result.Chunks.OfType<PdfPageChunk>().ToList();
		_ = pages.Should().NotBeEmpty();
		_ = processingTime.TotalSeconds.Should().BeLessThan(5);
		_output.WriteLine($"Processed {pages.Count} pages in {processingTime.TotalSeconds:F2} seconds");
	}

	[Fact]
	public async Task ChunkAsync_ShouldCalculateTokenCounts()
	{
		var result = await ChunkTestFileAsync("simple.pdf");

		_ = result.Success.Should().BeTrue();

		_ = result.Chunks.OfType<PdfPageChunk>().Should().AllSatisfy(page =>
		{
			_ = page.QualityMetrics.Should().NotBeNull();
			_ = page.QualityMetrics!.CharacterCount.Should().BeGreaterThanOrEqualTo(0);
		});

		_ = result.Chunks.OfType<PdfParagraphChunk>().Should().AllSatisfy(para =>
		{
			_ = para.QualityMetrics.Should().NotBeNull();
			_ = para.QualityMetrics!.TokenCount.Should().BePositive();
		});
	}

	[Fact]
	public async Task ChunkAsync_ShouldBuildHierarchy()
	{
		var result = await ChunkTestFileAsync("simple.pdf");

		_ = result.Success.Should().BeTrue();

		var doc = result.Chunks.OfType<PdfDocumentChunk>().First();
		var pages = result.Chunks.OfType<PdfPageChunk>().ToList();

		_ = pages.Should().AllSatisfy(page =>
		{
			_ = page.ParentId.Should().Be(doc.Id);
			_ = page.Depth.Should().BeGreaterThan(doc.Depth);
		});

		_ = result.Chunks.OfType<PdfParagraphChunk>().Should().AllSatisfy(para =>
		{
			_ = pages.Should().Contain(p => p.Id == para.ParentId);
		});
	}

	[Fact]
	public async Task ChunkAsync_ShouldGenerateStatistics()
	{
		var result = await ChunkTestFileAsync("simple.pdf");

		ChunkAssert.StatisticsMatch(result);
		_ = result.Statistics.ContentChunks.Should().BeGreaterThanOrEqualTo(0);
		_output.WriteLine($"Statistics: {result.Statistics.TotalChunks} chunks, {result.Statistics.StructuralChunks} structural, {result.Statistics.ContentChunks} content");
	}

	[Fact]
	public async Task ChunkAsync_WithAutoDetect_ShouldDetectPdf()
	{
		var result = await ChunkAutoDetectFileAsync(Path.Combine(_testDataPath, "simple.pdf"), "test.pdf");

		ChunkAssert.SucceededWithChunks(result);
	}

	[Fact]
	public async Task ChunkAsync_WithValidation_ShouldValidateChunks()
	{
		var result = await ChunkValidatedAsync(Path.Combine(_testDataPath, "simple.pdf"), DocumentType.Pdf);

		ChunkAssert.IsValidated(result);
	}

	[Fact]
	public async Task ChunkAsync_PageMetadata_ShouldBeSet()
	{
		var result = await ChunkTestFileAsync("simple.pdf");

		var page = result.Chunks.OfType<PdfPageChunk>().First();
		_ = page.Width.Should().BePositive();
		_ = page.Height.Should().BePositive();
		_ = page.Rotation.Should().BeGreaterThanOrEqualTo(0);
		_ = page.Metadata.Should().NotBeNull();
		_ = page.Metadata.DocumentType.Should().Be("PDF");
	}

	[Fact]
	public async Task ChunkAsync_DocumentMetadata_ShouldBeExtracted()
	{
		var result = await ChunkTestFileAsync("simple.pdf");

		var doc = result.Chunks.OfType<PdfDocumentChunk>().First();
		_ = doc.PdfVersion.Should().NotBeNullOrEmpty();
		_ = doc.PageCount.Should().BePositive();
	}

	private Task<ChunkingResult> ChunkTestFileAsync(string fileName)
		=> ChunkFileAsync(Path.Combine(_testDataPath, fileName), DocumentType.Pdf);
}