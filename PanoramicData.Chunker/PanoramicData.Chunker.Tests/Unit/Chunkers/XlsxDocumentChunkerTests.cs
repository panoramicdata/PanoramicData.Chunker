using AwesomeAssertions;
using PanoramicData.Chunker.Chunkers.Xlsx;
using PanoramicData.Chunker.Configuration;
using PanoramicData.Chunker.Infrastructure;
using PanoramicData.Chunker.Models;
using System.Text;

namespace PanoramicData.Chunker.Tests.Unit.Chunkers;

/// <summary>
/// Unit tests for XlsxDocumentChunker.
/// </summary>
public class XlsxDocumentChunkerTests : DocumentChunkerContractTests
{
	private readonly CharacterBasedTokenCounter _tokenCounter;
	private readonly XlsxDocumentChunker _chunker;

	public XlsxDocumentChunkerTests(ITestOutputHelper output) : base(output, tokenCounter => new XlsxDocumentChunker(tokenCounter), DocumentType.Xlsx)
	{
		_tokenCounter = new CharacterBasedTokenCounter();
		_chunker = new XlsxDocumentChunker(_tokenCounter);
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
		_ = File.Exists(Path.Combine(testDataPath, "simple.xlsx")).Should().BeTrue("test data file must exist in " + testDataPath);
		await using var stream = File.OpenRead(Path.Combine(testDataPath, "simple.xlsx"));

		// Act
		var result = await _chunker.CanHandleAsync(stream, CancellationToken);

		// Assert
		_ = result.Should().BeTrue();
		// Note: OpenXML may consume the stream during detection, so we don't strictly check position restoration
	}

	[Fact]
	public async Task CanHandleAsync_WithNonXlsxStream_ShouldReturnFalse()
	{
		// Arrange - Plain text is not XLSX
		var content = "This is plain text, not XLSX";
		using var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));

		// Act
		var result = await _chunker.CanHandleAsync(stream, CancellationToken);

		// Assert
		_ = result.Should().BeFalse();
	}

	[Fact]
	public async Task CanHandleAsync_WithEmptyStream_ShouldReturnFalse()
	{
		// Arrange
		using var stream = new MemoryStream();

		// Act
		var result = await _chunker.CanHandleAsync(stream, CancellationToken);

		// Assert
		_ = result.Should().BeFalse();
	}

	[Fact]
	public async Task ChunkAsync_WithValidXlsx_ShouldReturnChunks()
	{
		var result = await ChunkTestFileAsync("simple.xlsx");

		_ = result.Should().NotBeNull();
		_ = result.Success.Should().BeTrue();
		_ = result.Chunks.Should().NotBeEmpty();

		// Should have at least document chunk
		_ = result.Chunks.OfType<XlsxWorksheetChunk>().Should().NotBeEmpty();
	}

	[Fact]
	public async Task ChunkAsync_WithInvalidXlsx_ShouldReturnError()
	{
		var result = await ChunkTextAsync("This is not a valid XLSX file");

		_ = result.Should().NotBeNull();
		_ = result.Success.Should().BeFalse();
		_ = result.Warnings.Should().NotBeEmpty();
	}

	[Fact]
	public async Task ChunkAsync_WithEmptyXlsx_ShouldHandleGracefully()
	{
		var result = await ChunkTestFileAsync("empty.xlsx");

		_ = result.Should().NotBeNull();
		_ = result.Success.Should().BeTrue();
		// Empty XLSX may still have structural chunks (document, worksheet)
		// so we don't require it to be empty
	}

	[Fact]
	public async Task ChunkAsync_ShouldGenerateStatistics()
	{
		var result = await ChunkTestFileAsync("simple.xlsx");

		_ = result.Statistics.Should().NotBeNull();
		_ = result.Statistics.TotalChunks.Should().BePositive();
		_ = result.Statistics.ProcessingTime.Should().BeGreaterThan(TimeSpan.Zero);
	}

	[Fact]
	public async Task ChunkAsync_WithValidation_ShouldValidateChunks()
	{
		// Arrange
		var testDataPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TestData", "Xlsx");
		_ = File.Exists(Path.Combine(testDataPath, "simple.xlsx")).Should().BeTrue("test data file must exist in " + testDataPath);
		await using var stream = File.OpenRead(Path.Combine(testDataPath, "simple.xlsx"));
		var options = new ChunkingOptions { ValidateChunks = true };

		// Act
		var result = await _chunker.ChunkAsync(stream, options, CancellationToken);

		// Assert
		_ = result.ValidationResult.Should().NotBeNull();
		_ = result.ValidationResult!.IsValid.Should().BeTrue();
	}

	[Fact]
	public async Task ChunkAsync_ShouldCalculateQualityMetrics()
	{
		var result = await ChunkTestFileAsync("simple.xlsx");

		_ = result.Chunks.Should().AllSatisfy(chunk =>
		{
			_ = chunk.QualityMetrics.Should().NotBeNull();
		});
	}

	private async Task<ChunkingResult> ChunkTestFileAsync(string fileName)
	{
		var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TestData", "Xlsx", fileName);
		_ = File.Exists(path).Should().BeTrue("test data file must exist: " + path);
		await using var stream = File.OpenRead(path);
		return await _chunker.ChunkAsync(stream, new ChunkingOptions(), CancellationToken);
	}

	private async Task<ChunkingResult> ChunkTextAsync(string content)
	{
		using var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));
		return await _chunker.ChunkAsync(stream, new ChunkingOptions(), CancellationToken);
	}
}
