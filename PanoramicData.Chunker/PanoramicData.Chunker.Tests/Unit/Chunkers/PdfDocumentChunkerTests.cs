using AwesomeAssertions;
using PanoramicData.Chunker.Chunkers.Pdf;
using PanoramicData.Chunker.Configuration;
using PanoramicData.Chunker.Infrastructure;
using PanoramicData.Chunker.Models;
using System.Text;

namespace PanoramicData.Chunker.Tests.Unit.Chunkers;

/// <summary>
/// Unit tests for PdfDocumentChunker.
/// </summary>
public class PdfDocumentChunkerTests : FileBasedChunkerContractTests
{
	private readonly PdfDocumentChunker _chunker;

	public PdfDocumentChunkerTests(ITestOutputHelper output) : base(output, tokenCounter => new PdfDocumentChunker(tokenCounter), DocumentType.Pdf, "Pdf", "simple.pdf")
	{
		_chunker = new PdfDocumentChunker(new CharacterBasedTokenCounter());
	}

	[Fact]
	public void SupportedType_ShouldReturnPdf()
	{
		// Arrange & Act
		var type = _chunker.SupportedType;

		// Assert
		_ = type.Should().Be(DocumentType.Pdf, "Pdf", "simple.pdf");
	}

	[Fact]
	public async Task CanHandleAsync_WithPdfStream_ShouldReturnTrue()
	{
		// Arrange - PDF signature %PDF-
		var pdfSignature = "%PDF-1.7"u8.ToArray();
		using var stream = new MemoryStream(pdfSignature);

		// Act
		var result = await _chunker.CanHandleAsync(stream, CancellationToken);

		// Assert
		_ = result.Should().BeTrue();
		_ = stream.Position.Should().Be(0); // Should restore position
	}

	[Fact]
	public async Task CanHandleAsync_WithNonPdfStream_ShouldReturnFalse()
	{
		// Arrange - Plain text is not PDF
		var content = "This is plain text, not PDF";
		using var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));

		// Act
		var result = await _chunker.CanHandleAsync(stream, CancellationToken);

		// Assert
		_ = result.Should().BeFalse();
	}

	[Fact]
	public async Task CanHandleAsync_WithShortStream_ShouldReturnFalse()
	{
		// Arrange - Less than 5 bytes
		var content = "%PD"u8.ToArray();
		using var stream = new MemoryStream(content);

		// Act
		var result = await _chunker.CanHandleAsync(stream, CancellationToken);

		// Assert
		_ = result.Should().BeFalse();
	}

	[Fact]
	public async Task ChunkAsync_WithValidPdf_ShouldReturnChunks()
	{
		var result = await ChunkTestFileAsync("simple.pdf");

		_ = result.Should().NotBeNull();
		_ = result.Success.Should().BeTrue();
		_ = result.Chunks.Should().NotBeEmpty();

		// Should have document and page chunks
		_ = result.Chunks.OfType<PdfDocumentChunk>().Should().ContainSingle();
		_ = result.Chunks.OfType<PdfPageChunk>().Should().NotBeEmpty();
	}

	[Fact]
	public async Task ChunkAsync_WithInvalidPdf_ShouldReturnError()
	{
		// Arrange - Create invalid content with PDF signature but invalid structure
		var content = Encoding.UTF8.GetBytes("%PDF-1.7\nThis is not a valid PDF structure");
		using var stream = new MemoryStream(content);
		var options = new ChunkingOptions();

		// Act
		var result = await _chunker.ChunkAsync(stream, options, CancellationToken);

		// Assert
		_ = result.Should().NotBeNull();
		_ = result.Success.Should().BeFalse();
		_ = result.Warnings.Should().NotBeEmpty();
	}

	[Fact]
	public async Task ChunkAsync_WithEmptyPdf_ShouldHandleGracefully()
	{
		var result = await ChunkTestFileAsync("empty.pdf");

		_ = result.Should().NotBeNull();
		_ = result.Success.Should().BeTrue();
		// Empty PDF should still have document and page chunks
		_ = result.Chunks.Should().NotBeEmpty();
	}

	[Fact]
	public async Task ChunkAsync_ShouldExtractPageMetadata()
	{
		var result = await ChunkTestFileAsync("simple.pdf");

		var page = result.Chunks.OfType<PdfPageChunk>().FirstOrDefault();
		_ = page.Should().NotBeNull();
		_ = page!.PageNumber.Should().BePositive();
		_ = page.Width.Should().BePositive();
		_ = page.Height.Should().BePositive();
	}

	[Fact]
	public async Task ChunkAsync_ShouldExtractDocumentMetadata()
	{
		var result = await ChunkTestFileAsync("simple.pdf");

		var doc = result.Chunks.OfType<PdfDocumentChunk>().FirstOrDefault();
		_ = doc.Should().NotBeNull();
		_ = doc!.PdfVersion.Should().NotBeNullOrEmpty();
		_ = doc.PageCount.Should().BePositive();
	}

	[Fact]
	public async Task ChunkAsync_ShouldBuildHierarchy()
	{
		var result = await ChunkTestFileAsync("simple.pdf");

		var doc = result.Chunks.OfType<PdfDocumentChunk>().First();
		var pages = result.Chunks.OfType<PdfPageChunk>();

		// All pages should be children of document
		_ = pages.Should().AllSatisfy(page =>
		{
			_ = page.ParentId.Should().Be(doc.Id);
		});
	}
}
