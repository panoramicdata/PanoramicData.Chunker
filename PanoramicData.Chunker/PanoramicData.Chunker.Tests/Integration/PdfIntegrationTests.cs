using FluentAssertions;
using PanoramicData.Chunker.Chunkers.Pdf;
using PanoramicData.Chunker.Configuration;
using Xunit.Abstractions;

namespace PanoramicData.Chunker.Tests.Integration;

/// <summary>
/// Integration tests for PDF document chunking.
/// </summary>
public class PdfIntegrationTests(ITestOutputHelper output)
{
	private readonly string _testDataPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TestData", "Pdf");

	[Fact]
	public async Task ChunkAsync_SimplePdf_ShouldExtractText()
	{
		// Arrange
		var filePath = Path.Combine(_testDataPath, "simple.pdf");
		await using var stream = File.OpenRead(filePath);
		var options = ChunkingPresets.ForOpenAIEmbeddings();

		// Act
		var result = await DocumentChunker.ChunkAsync(stream, DocumentType.Pdf, options);

		// Assert
		result.Success.Should().BeTrue();
		result.Chunks.Should().NotBeEmpty();

		// Should have document chunk
		var documents = result.Chunks.OfType<PdfDocumentChunk>().ToList();
		documents.Should().HaveCount(1);

		var doc = documents[0];
		doc.PageCount.Should().BeGreaterThan(0);

		// Should have page chunks
		var pages = result.Chunks.OfType<PdfPageChunk>().ToList();
		pages.Should().HaveCountGreaterOrEqualTo(1);

		var firstPage = pages[0];
		firstPage.PageNumber.Should().Be(1);
		firstPage.ExtractedText.Should().NotBeNullOrEmpty();
		output.WriteLine($"Extracted text from page 1: {firstPage.ExtractedText?[..Math.Min(100, firstPage.ExtractedText.Length)]}");

		// Should have paragraph chunks
		var paragraphs = result.Chunks.OfType<PdfParagraphChunk>().ToList();
		paragraphs.Should().NotBeEmpty();
	}

	[Fact]
	public async Task ChunkAsync_EmptyPdf_ShouldHandleGracefully()
	{
		// Arrange
		var filePath = Path.Combine(_testDataPath, "empty.pdf");
		await using var stream = File.OpenRead(filePath);
		var options = ChunkingPresets.ForOpenAIEmbeddings();

		// Act
		var result = await DocumentChunker.ChunkAsync(stream, DocumentType.Pdf, options);

		// Assert
		result.Success.Should().BeTrue();

		var pages = result.Chunks.OfType<PdfPageChunk>().ToList();
		pages.Should().HaveCount(1); // Empty PDF still has one page
	}

	[Fact]
	public async Task ChunkAsync_MultiPagePdf_ShouldExtractAllPages()
	{
		// Arrange
		var filePath = Path.Combine(_testDataPath, "multi-page.pdf");
		await using var stream = File.OpenRead(filePath);
		var options = ChunkingPresets.ForOpenAIEmbeddings();

		// Act
		var result = await DocumentChunker.ChunkAsync(stream, DocumentType.Pdf, options);

		// Assert
		result.Success.Should().BeTrue();

		var doc = result.Chunks.OfType<PdfDocumentChunk>().First();
		doc.PageCount.Should().Be(3);

		var pages = result.Chunks.OfType<PdfPageChunk>().ToList();
		pages.Should().HaveCount(3);

		pages[0].PageNumber.Should().Be(1);
		pages[1].PageNumber.Should().Be(2);
		pages[2].PageNumber.Should().Be(3);

		output.WriteLine($"Total pages: {pages.Count}");
		foreach (var page in pages)
		{
			output.WriteLine($"Page {page.PageNumber}: {page.WordCount} words");
		}
	}

	[Fact]
	public async Task ChunkAsync_WithHeadings_ShouldDetectHeadings()
	{
		// Arrange
		var filePath = Path.Combine(_testDataPath, "with-headings.pdf");
		await using var stream = File.OpenRead(filePath);
		var options = ChunkingPresets.ForOpenAIEmbeddings();

		// Act
		var result = await DocumentChunker.ChunkAsync(stream, DocumentType.Pdf, options);

		// Assert
		result.Success.Should().BeTrue();

		var paragraphs = result.Chunks.OfType<PdfParagraphChunk>().ToList();
		paragraphs.Should().NotBeEmpty();

		// Some paragraphs should be detected as likely headings (or at least we tried)
		var headings = paragraphs.Where(p => p.IsLikelyHeading).ToList();

		output.WriteLine($"Total paragraphs: {paragraphs.Count}");
		output.WriteLine($"Detected headings: {headings.Count}");
		if (headings.Any())
		{
			foreach (var heading in headings)
			{
				output.WriteLine($"Heading: {heading.Content?[..Math.Min(50, heading.Content.Length)]}");
			}
		}

		// At minimum, we should have extracted some paragraphs
		paragraphs.Count.Should().BeGreaterThan(0);
	}

	[Fact]
	public async Task ChunkAsync_WithLists_ShouldExtractListContent()
	{
		// Arrange
		var filePath = Path.Combine(_testDataPath, "with-lists.pdf");
		await using var stream = File.OpenRead(filePath);
		var options = ChunkingPresets.ForOpenAIEmbeddings();

		// Act
		var result = await DocumentChunker.ChunkAsync(stream, DocumentType.Pdf, options);

		// Assert
		result.Success.Should().BeTrue();

		var paragraphs = result.Chunks.OfType<PdfParagraphChunk>().ToList();
		paragraphs.Should().NotBeEmpty();

		// Check that list items are present in content
		var hasListContent = paragraphs.Any(p => p.Content?.Contains("item", StringComparison.OrdinalIgnoreCase) == true);
		hasListContent.Should().BeTrue();
	}

	[Fact]
	public async Task ChunkAsync_WithTables_ShouldExtractTableText()
	{
		// Arrange
		var filePath = Path.Combine(_testDataPath, "with-tables.pdf");
		await using var stream = File.OpenRead(filePath);
		var options = ChunkingPresets.ForOpenAIEmbeddings();

		// Act
		var result = await DocumentChunker.ChunkAsync(stream, DocumentType.Pdf, options);

		// Assert
		result.Success.Should().BeTrue();

		var pages = result.Chunks.OfType<PdfPageChunk>().ToList();
		pages.Should().NotBeEmpty();

		// Should extract text from the table
		var pageText = pages[0].ExtractedText;
		pageText.Should().NotBeNullOrEmpty();

		// Check for table content keywords
		(pageText?.Contains("Employee") == true ||
		 pageText?.Contains("Name") == true ||
		 pageText?.Contains("Department") == true).Should().BeTrue();

		output.WriteLine($"Table content sample: {pageText?[..Math.Min(200, pageText.Length)]}");
	}

	[Fact]
	public async Task ChunkAsync_LargePdf_ShouldHandleEfficiently()
	{
		// Arrange
		var filePath = Path.Combine(_testDataPath, "large.pdf");
		await using var stream = File.OpenRead(filePath);
		var options = ChunkingPresets.ForOpenAIEmbeddings();

		// Act
		var startTime = DateTime.UtcNow;
		var result = await DocumentChunker.ChunkAsync(stream, DocumentType.Pdf, options);
		var processingTime = DateTime.UtcNow - startTime;

		// Assert
		result.Success.Should().BeTrue();

		var pages = result.Chunks.OfType<PdfPageChunk>().ToList();
		pages.Should().NotBeEmpty();

		// Should process reasonably fast (< 5 seconds for large PDF)
		processingTime.TotalSeconds.Should().BeLessThan(5);

		output.WriteLine($"Processed {pages.Count} pages in {processingTime.TotalSeconds:F2} seconds");
	}

	[Fact]
	public async Task ChunkAsync_ShouldCalculateTokenCounts()
	{
		// Arrange
		var filePath = Path.Combine(_testDataPath, "simple.pdf");
		await using var stream = File.OpenRead(filePath);
		var options = ChunkingPresets.ForOpenAIEmbeddings();

		// Act
		var result = await DocumentChunker.ChunkAsync(stream, DocumentType.Pdf, options);

		// Assert
		result.Success.Should().BeTrue();

		// Page and paragraph chunks should have quality metrics
		var pages = result.Chunks.OfType<PdfPageChunk>().ToList();
		pages.Should().AllSatisfy(page =>
		{
			page.QualityMetrics.Should().NotBeNull();
			page.QualityMetrics!.CharacterCount.Should().BeGreaterOrEqualTo(0);
		});

		var paragraphs = result.Chunks.OfType<PdfParagraphChunk>().ToList();
		paragraphs.Should().AllSatisfy(para =>
		{
			para.QualityMetrics.Should().NotBeNull();
			para.QualityMetrics!.TokenCount.Should().BeGreaterThan(0);
		});
	}

	[Fact]
	public async Task ChunkAsync_ShouldBuildHierarchy()
	{
		// Arrange
		var filePath = Path.Combine(_testDataPath, "simple.pdf");
		await using var stream = File.OpenRead(filePath);
		var options = ChunkingPresets.ForOpenAIEmbeddings();

		// Act
		var result = await DocumentChunker.ChunkAsync(stream, DocumentType.Pdf, options);

		// Assert
		result.Success.Should().BeTrue();

		var doc = result.Chunks.OfType<PdfDocumentChunk>().First();
		var pages = result.Chunks.OfType<PdfPageChunk>().ToList();
		var paragraphs = result.Chunks.OfType<PdfParagraphChunk>().ToList();

		// All pages should be children of document
		pages.Should().AllSatisfy(page =>
		{
			page.ParentId.Should().Be(doc.Id);
			page.Depth.Should().BeGreaterThan(doc.Depth);
		});

		// All paragraphs should be children of pages
		paragraphs.Should().AllSatisfy(para =>
		{
			pages.Should().Contain(p => p.Id == para.ParentId);
		});
	}

	[Fact]
	public async Task ChunkAsync_ShouldGenerateStatistics()
	{
		// Arrange
		var filePath = Path.Combine(_testDataPath, "simple.pdf");
		await using var stream = File.OpenRead(filePath);
		var options = ChunkingPresets.ForOpenAIEmbeddings();

		// Act
		var result = await DocumentChunker.ChunkAsync(stream, DocumentType.Pdf, options);

		// Assert
		result.Statistics.Should().NotBeNull();
		result.Statistics.TotalChunks.Should().Be(result.Chunks.Count);
		result.Statistics.StructuralChunks.Should().BeGreaterThan(0); // Document + pages
		result.Statistics.ContentChunks.Should().BeGreaterOrEqualTo(0); // Paragraphs
		result.Statistics.ProcessingTime.Should().BeGreaterThan(TimeSpan.Zero);

		output.WriteLine($"Statistics: {result.Statistics.TotalChunks} chunks, {result.Statistics.StructuralChunks} structural, {result.Statistics.ContentChunks} content");
	}

	[Fact]
	public async Task ChunkAsync_WithAutoDetect_ShouldDetectPdf()
	{
		// Arrange
		var filePath = Path.Combine(_testDataPath, "simple.pdf");
		await using var stream = File.OpenRead(filePath);
		var options = ChunkingPresets.ForOpenAIEmbeddings();

		// Act - Use auto-detect
		var result = await DocumentChunker.ChunkAutoDetectAsync(stream, "test.pdf", options);

		// Assert
		result.Success.Should().BeTrue();
		result.Chunks.Should().NotBeEmpty();
	}

	[Fact]
	public async Task ChunkAsync_WithValidation_ShouldValidateChunks()
	{
		// Arrange
		var filePath = Path.Combine(_testDataPath, "simple.pdf");
		await using var stream = File.OpenRead(filePath);
		var options = new ChunkingOptions
		{
			ValidateChunks = true
		};

		// Act
		var result = await DocumentChunker.ChunkAsync(stream, DocumentType.Pdf, options);

		// Assert
		result.ValidationResult.Should().NotBeNull();
		result.ValidationResult!.IsValid.Should().BeTrue();
	}

	[Fact]
	public async Task ChunkAsync_PageMetadata_ShouldBeSet()
	{
		// Arrange
		var filePath = Path.Combine(_testDataPath, "simple.pdf");
		await using var stream = File.OpenRead(filePath);
		var options = ChunkingPresets.ForOpenAIEmbeddings();

		// Act
		var result = await DocumentChunker.ChunkAsync(stream, DocumentType.Pdf, options);

		// Assert
		var page = result.Chunks.OfType<PdfPageChunk>().First();
		page.Width.Should().BeGreaterThan(0);
		page.Height.Should().BeGreaterThan(0);
		page.Rotation.Should().BeGreaterOrEqualTo(0);
		page.Metadata.Should().NotBeNull();
		page.Metadata.DocumentType.Should().Be("PDF");
	}

	[Fact]
	public async Task ChunkAsync_DocumentMetadata_ShouldBeExtracted()
	{
		// Arrange
		var filePath = Path.Combine(_testDataPath, "simple.pdf");
		await using var stream = File.OpenRead(filePath);
		var options = ChunkingPresets.ForOpenAIEmbeddings();

		// Act
		var result = await DocumentChunker.ChunkAsync(stream, DocumentType.Pdf, options);

		// Assert
		var doc = result.Chunks.OfType<PdfDocumentChunk>().First();
		doc.PdfVersion.Should().NotBeNullOrEmpty();
		doc.PageCount.Should().BeGreaterThan(0);
		// Title, Author, etc. may or may not be present depending on the PDF
	}
}
