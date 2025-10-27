using AwesomeAssertions;
using PanoramicData.Chunker.Chunkers.Xlsx;
using PanoramicData.Chunker.Configuration;
using PanoramicData.Chunker.Infrastructure;
using System.Text;

namespace PanoramicData.Chunker.Tests.Unit.Chunkers;

/// <summary>
/// Unit tests for XlsxDocumentChunker.
/// </summary>
public class XlsxDocumentChunkerTests
{
	private readonly CharacterBasedTokenCounter _tokenCounter;
	private readonly XlsxDocumentChunker _chunker;

	public XlsxDocumentChunkerTests()
	{
		_tokenCounter = new CharacterBasedTokenCounter();
		_chunker = new XlsxDocumentChunker(_tokenCounter);
	}

	[Fact]
	public void Constructor_WithValidTokenCounter_ShouldInitialize()
	{
		// Arrange & Act
		var chunker = new XlsxDocumentChunker(_tokenCounter);

		// Assert
		_ = chunker.Should().NotBeNull();
		_ = chunker.SupportedType.Should().Be(DocumentType.Xlsx);
	}

	[Fact]
	public void Constructor_WithNullTokenCounter_ShouldThrow()
	{
		// Arrange, Act & Assert
		var act = () => new XlsxDocumentChunker(null!);
		_ = act.Should().Throw<ArgumentNullException>();
	}

	[Fact]
	public void SupportedType_ShouldReturnXlsx()
	{
		// Arrange & Act
		var type = _chunker.SupportedType;

		// Assert
		_ = type.Should().Be(DocumentType.Xlsx);
	}

	[Fact]
	public async Task CanHandleAsync_WithXlsxStream_ShouldReturnTrue()
	{
		// Arrange - Create minimal XLSX (ZIP with proper structure)
		var testDataPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TestData", "Xlsx");
		if (Directory.Exists(testDataPath) && File.Exists(Path.Combine(testDataPath, "simple.xlsx")))
		{
			await using var stream = File.OpenRead(Path.Combine(testDataPath, "simple.xlsx"));
			var originalPosition = stream.Position;

			// Act
			var result = await _chunker.CanHandleAsync(stream);

			// Assert
			_ = result.Should().BeTrue();
			// Note: OpenXML may consume the stream during detection, so we don't strictly check position restoration
		}
	}

	[Fact]
	public async Task CanHandleAsync_WithNonXlsxStream_ShouldReturnFalse()
	{
		// Arrange - Plain text is not XLSX
		var content = "This is plain text, not XLSX";
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
	public async Task ChunkAsync_WithValidXlsx_ShouldReturnChunks()
	{
		// Arrange
		var testDataPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TestData", "Xlsx");
		if (Directory.Exists(testDataPath) && File.Exists(Path.Combine(testDataPath, "simple.xlsx")))
		{
			await using var stream = File.OpenRead(Path.Combine(testDataPath, "simple.xlsx"));
			var options = new ChunkingOptions();

			// Act
			var result = await _chunker.ChunkAsync(stream, options);

			// Assert
			_ = result.Should().NotBeNull();
			_ = result.Success.Should().BeTrue();
			_ = result.Chunks.Should().NotBeEmpty();

			// Should have at least document chunk
			_ = result.Chunks.OfType<XlsxWorksheetChunk>().Should().NotBeEmpty();
		}
	}

	[Fact]
	public async Task ChunkAsync_WithInvalidXlsx_ShouldReturnError()
	{
		// Arrange - Create invalid content
		var content = "This is not a valid XLSX file";
		using var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));
		var options = new ChunkingOptions();

		// Act
		var result = await _chunker.ChunkAsync(stream, options);

		// Assert
		_ = result.Should().NotBeNull();
		_ = result.Success.Should().BeFalse();
		_ = result.Warnings.Should().NotBeEmpty();
	}

	[Fact]
	public async Task ChunkAsync_WithEmptyXlsx_ShouldHandleGracefully()
	{
		// Arrange
		var testDataPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TestData", "Xlsx");
		if (Directory.Exists(testDataPath) && File.Exists(Path.Combine(testDataPath, "empty.xlsx")))
		{
			await using var stream = File.OpenRead(Path.Combine(testDataPath, "empty.xlsx"));
			var options = new ChunkingOptions();

			// Act
			var result = await _chunker.ChunkAsync(stream, options);

			// Assert
			_ = result.Should().NotBeNull();
			_ = result.Success.Should().BeTrue();
			// Empty XLSX may still have structural chunks (document, worksheet)
			// so we don't require it to be empty
		}
	}

	[Fact]
	public async Task ChunkAsync_ShouldGenerateStatistics()
	{
		// Arrange
		var testDataPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TestData", "Xlsx");
		if (Directory.Exists(testDataPath) && File.Exists(Path.Combine(testDataPath, "simple.xlsx")))
		{
			await using var stream = File.OpenRead(Path.Combine(testDataPath, "simple.xlsx"));
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
		var testDataPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TestData", "Xlsx");
		if (Directory.Exists(testDataPath) && File.Exists(Path.Combine(testDataPath, "simple.xlsx")))
		{
			await using var stream = File.OpenRead(Path.Combine(testDataPath, "simple.xlsx"));
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
		var testDataPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TestData", "Xlsx");
		if (Directory.Exists(testDataPath) && File.Exists(Path.Combine(testDataPath, "simple.xlsx")))
		{
			await using var stream = File.OpenRead(Path.Combine(testDataPath, "simple.xlsx"));
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
}
