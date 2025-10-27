using AwesomeAssertions;
using PanoramicData.Chunker.Chunkers.Pdf;
using PanoramicData.Chunker.Configuration;
using PanoramicData.Chunker.Infrastructure;
using System.Text;

namespace PanoramicData.Chunker.Tests.Unit.Chunkers;

/// <summary>
/// Unit tests for PdfDocumentChunker.
/// </summary>
public class PdfDocumentChunkerTests
{
	private readonly CharacterBasedTokenCounter _tokenCounter;
	private readonly PdfDocumentChunker _chunker;

	public PdfDocumentChunkerTests()
	{
		_tokenCounter = new CharacterBasedTokenCounter();
		_chunker = new PdfDocumentChunker(_tokenCounter);
	}

	[Fact]
	public void Constructor_WithValidTokenCounter_ShouldInitialize()
	{
		// Arrange & Act
		var chunker = new PdfDocumentChunker(_tokenCounter);

		// Assert
		_ = chunker.Should().NotBeNull();
		_ = chunker.SupportedType.Should().Be(DocumentType.Pdf);
	}

	[Fact]
	public void Constructor_WithNullTokenCounter_ShouldThrow()
	{
		// Arrange, Act & Assert
		var act = () => new PdfDocumentChunker(null!);
		_ = act.Should().Throw<ArgumentNullException>();
	}

	[Fact]
	public void SupportedType_ShouldReturnPdf()
	{
		// Arrange & Act
		var type = _chunker.SupportedType;

		// Assert
		_ = type.Should().Be(DocumentType.Pdf);
	}

	[Fact]
	public async Task CanHandleAsync_WithPdfStream_ShouldReturnTrue()
	{
		// Arrange - PDF signature %PDF-
		var pdfSignature = "%PDF-1.7"u8.ToArray();
		using var stream = new MemoryStream(pdfSignature);

		// Act
		var result = await _chunker.CanHandleAsync(stream);

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
		var result = await _chunker.CanHandleAsync(stream);

		// Assert
		_ = result.Should().BeFalse();
	}

	[Fact]
	public async Task CanHandleAsync_WithEmptyStream_ShouldReturnFalse()
	{
		// Arrange
		using var stream = new MemoryStream();

		// Act
		var result = await _chunker.CanHandleAsync(stream);

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
		var result = await _chunker.CanHandleAsync(stream);

		// Assert
		_ = result.Should().BeFalse();
	}

	[Fact]
	public async Task ChunkAsync_WithNullStream_ShouldThrow()
	{
		// Arrange
		var options = new ChunkingOptions();

		// Act
		var act = async () => await _chunker.ChunkAsync(null!, options);

		// Assert
		_ = await act.Should().ThrowAsync<ArgumentNullException>();
	}

	[Fact]
	public async Task ChunkAsync_WithNullOptions_ShouldThrow()
	{
		// Arrange
		using var stream = new MemoryStream();

		// Act
		var act = async () => await _chunker.ChunkAsync(stream, null!);

		// Assert
		_ = await act.Should().ThrowAsync<ArgumentNullException>();
	}

	[Fact]
	public async Task ChunkAsync_WithValidPdf_ShouldReturnChunks()
	{
		// Arrange
		var testDataPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TestData", "Pdf");
		if (Directory.Exists(testDataPath) && File.Exists(Path.Combine(testDataPath, "simple.pdf")))
		{
			await using var stream = File.OpenRead(Path.Combine(testDataPath, "simple.pdf"));
			var options = new ChunkingOptions();

			// Act
			var result = await _chunker.ChunkAsync(stream, options);

			// Assert
			_ = result.Should().NotBeNull();
			_ = result.Success.Should().BeTrue();
			_ = result.Chunks.Should().NotBeEmpty();

			// Should have document and page chunks
			_ = result.Chunks.OfType<PdfDocumentChunk>().Should().ContainSingle();
			_ = result.Chunks.OfType<PdfPageChunk>().Should().NotBeEmpty();
		}
	}

	[Fact]
	public async Task ChunkAsync_WithInvalidPdf_ShouldReturnError()
	{
		// Arrange - Create invalid content with PDF signature but invalid structure
		var content = Encoding.UTF8.GetBytes("%PDF-1.7\nThis is not a valid PDF structure");
		using var stream = new MemoryStream(content);
		var options = new ChunkingOptions();

		// Act
		var result = await _chunker.ChunkAsync(stream, options);

		// Assert
		_ = result.Should().NotBeNull();
		_ = result.Success.Should().BeFalse();
		_ = result.Warnings.Should().NotBeEmpty();
	}

	[Fact]
	public async Task ChunkAsync_WithEmptyPdf_ShouldHandleGracefully()
	{
		// Arrange
		var testDataPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TestData", "Pdf");
		if (Directory.Exists(testDataPath) && File.Exists(Path.Combine(testDataPath, "empty.pdf")))
		{
			await using var stream = File.OpenRead(Path.Combine(testDataPath, "empty.pdf"));
			var options = new ChunkingOptions();

			// Act
			var result = await _chunker.ChunkAsync(stream, options);

			// Assert
			_ = result.Should().NotBeNull();
			_ = result.Success.Should().BeTrue();
			// Empty PDF should still have document and page chunks
			_ = result.Chunks.Should().NotBeEmpty();
		}
	}

	[Fact]
	public async Task ChunkAsync_ShouldGenerateStatistics()
	{
		// Arrange
		var testDataPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TestData", "Pdf");
		if (Directory.Exists(testDataPath) && File.Exists(Path.Combine(testDataPath, "simple.pdf")))
		{
			await using var stream = File.OpenRead(Path.Combine(testDataPath, "simple.pdf"));
			var options = new ChunkingOptions();

			// Act
			var result = await _chunker.ChunkAsync(stream, options);

			// Assert
			_ = result.Statistics.Should().NotBeNull();
			_ = result.Statistics.TotalChunks.Should().BePositive();
			_ = result.Statistics.ProcessingTime.Should().BeGreaterThan(TimeSpan.Zero);
		}
	}

	[Fact]
	public async Task ChunkAsync_WithValidation_ShouldValidateChunks()
	{
		// Arrange
		var testDataPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TestData", "Pdf");
		if (Directory.Exists(testDataPath) && File.Exists(Path.Combine(testDataPath, "simple.pdf")))
		{
			await using var stream = File.OpenRead(Path.Combine(testDataPath, "simple.pdf"));
			var options = new ChunkingOptions { ValidateChunks = true };

			// Act
			var result = await _chunker.ChunkAsync(stream, options);

			// Assert
			_ = result.ValidationResult.Should().NotBeNull();
			_ = result.ValidationResult!.IsValid.Should().BeTrue();
		}
	}

	[Fact]
	public async Task ChunkAsync_ShouldCalculateQualityMetrics()
	{
		// Arrange
		var testDataPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TestData", "Pdf");
		if (Directory.Exists(testDataPath) && File.Exists(Path.Combine(testDataPath, "simple.pdf")))
		{
			await using var stream = File.OpenRead(Path.Combine(testDataPath, "simple.pdf"));
			var options = new ChunkingOptions();

			// Act
			var result = await _chunker.ChunkAsync(stream, options);

			// Assert
			_ = result.Chunks.Should().AllSatisfy(chunk =>
			{
				_ = chunk.QualityMetrics.Should().NotBeNull();
			});
		}
	}

	[Fact]
	public async Task ChunkAsync_ShouldExtractPageMetadata()
	{
		// Arrange
		var testDataPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TestData", "Pdf");
		if (Directory.Exists(testDataPath) && File.Exists(Path.Combine(testDataPath, "simple.pdf")))
		{
			await using var stream = File.OpenRead(Path.Combine(testDataPath, "simple.pdf"));
			var options = new ChunkingOptions();

			// Act
			var result = await _chunker.ChunkAsync(stream, options);

			// Assert
			var page = result.Chunks.OfType<PdfPageChunk>().FirstOrDefault();
			_ = page.Should().NotBeNull();
			_ = page!.PageNumber.Should().BePositive();
			_ = page.Width.Should().BePositive();
			_ = page.Height.Should().BePositive();
		}
	}

	[Fact]
	public async Task ChunkAsync_ShouldExtractDocumentMetadata()
	{
		// Arrange
		var testDataPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TestData", "Pdf");
		if (Directory.Exists(testDataPath) && File.Exists(Path.Combine(testDataPath, "simple.pdf")))
		{
			await using var stream = File.OpenRead(Path.Combine(testDataPath, "simple.pdf"));
			var options = new ChunkingOptions();

			// Act
			var result = await _chunker.ChunkAsync(stream, options);

			// Assert
			var doc = result.Chunks.OfType<PdfDocumentChunk>().FirstOrDefault();
			_ = doc.Should().NotBeNull();
			_ = doc!.PdfVersion.Should().NotBeNullOrEmpty();
			_ = doc.PageCount.Should().BePositive();
		}
	}

	[Fact]
	public async Task ChunkAsync_ShouldBuildHierarchy()
	{
		// Arrange
		var testDataPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TestData", "Pdf");
		if (Directory.Exists(testDataPath) && File.Exists(Path.Combine(testDataPath, "simple.pdf")))
		{
			await using var stream = File.OpenRead(Path.Combine(testDataPath, "simple.pdf"));
			var options = new ChunkingOptions();

			// Act
			var result = await _chunker.ChunkAsync(stream, options);

			// Assert
			var doc = result.Chunks.OfType<PdfDocumentChunk>().First();
			var pages = result.Chunks.OfType<PdfPageChunk>();

			// All pages should be children of document
			_ = pages.Should().AllSatisfy(page =>
			{
				_ = page.ParentId.Should().Be(doc.Id);
			});
		}
	}
}
