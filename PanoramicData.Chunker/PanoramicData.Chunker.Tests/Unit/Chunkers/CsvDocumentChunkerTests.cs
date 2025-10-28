using AwesomeAssertions;
using PanoramicData.Chunker.Chunkers.Csv;
using PanoramicData.Chunker.Configuration;
using PanoramicData.Chunker.Infrastructure;
using System.Text;

namespace PanoramicData.Chunker.Tests.Unit.Chunkers;

/// <summary>
/// Unit tests for CsvDocumentChunker.
/// </summary>
public class CsvDocumentChunkerTests : BaseTest
{
	private readonly CharacterBasedTokenCounter _tokenCounter;
	private readonly CsvDocumentChunker _chunker;

	public CsvDocumentChunkerTests(ITestOutputHelper output) : base(output)
	{
		_tokenCounter = new CharacterBasedTokenCounter();
		_chunker = new CsvDocumentChunker(_tokenCounter);
	}

	[Fact]
	public void Constructor_WithValidTokenCounter_ShouldInitialize()
	{
		// Arrange & Act
		var chunker = new CsvDocumentChunker(_tokenCounter);

		// Assert
		_ = chunker.Should().NotBeNull();
		_ = chunker.SupportedType.Should().Be(DocumentType.Csv);
	}

	[Fact]
	public void Constructor_WithNullTokenCounter_ShouldThrow()
	{
		// Arrange, Act & Assert
		var act = () => new CsvDocumentChunker(null!);
		_ = act.Should().Throw<ArgumentNullException>();
	}

	[Fact]
	public void SupportedType_ShouldReturnCsv()
	{
		// Arrange & Act
		var type = _chunker.SupportedType;

		// Assert
		_ = type.Should().Be(DocumentType.Csv);
	}

	[Fact]
	public async Task CanHandleAsync_WithCsvStream_ShouldReturnTrue()
	{
		// Arrange - Simple CSV with comma delimiter
		var content = "Name,Age,City\nAlice,30,New York\nBob,25,London";
		using var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));

		// Act
		var result = await _chunker.CanHandleAsync(stream, CancellationToken);

		// Assert
		_ = result.Should().BeTrue();
		_ = stream.Position.Should().Be(0); // Should restore position
	}

	[Fact]
	public async Task CanHandleAsync_WithTabDelimited_ShouldReturnTrue()
	{
		// Arrange - Tab-delimited
		var content = "Name\tAge\tCity\nAlice\t30\tNew York";
		using var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));

		// Act
		var result = await _chunker.CanHandleAsync(stream, CancellationToken);

		// Assert
		_ = result.Should().BeTrue();
	}

	[Fact]
	public async Task CanHandleAsync_WithNonCsvStream_ShouldReturnFalse()
	{
		// Arrange - Plain text without delimiters
		var content = "This is plain text without any delimiters or structure";
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
	public async Task ChunkAsync_WithNullStream_ShouldThrow()
	{
		// Arrange
		var options = new ChunkingOptions();

		// Act
		var act = async () => await _chunker.ChunkAsync(null!, options, CancellationToken);

		// Assert
		_ = await act.Should().ThrowAsync<ArgumentNullException>();
	}

	[Fact]
	public async Task ChunkAsync_WithNullOptions_ShouldThrow()
	{
		// Arrange
		using var stream = new MemoryStream();

		// Act
		var act = async () => await _chunker.ChunkAsync(stream, null!, CancellationToken);

		// Assert
		_ = await act.Should().ThrowAsync<ArgumentNullException>();
	}

	[Fact]
	public async Task ChunkAsync_WithValidCsv_ShouldReturnChunks()
	{
		// Arrange
		var content = "Name,Age,City\nAlice,30,New York\nBob,25,London";
		using var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));
		var options = new ChunkingOptions();

		// Act
		var result = await _chunker.ChunkAsync(stream, options, CancellationToken);

		// Assert
		_ = result.Should().NotBeNull();
		_ = result.Success.Should().BeTrue();
		_ = result.Chunks.Should().NotBeEmpty();

		// Should have document chunk and row chunks
		_ = result.Chunks.OfType<CsvDocumentChunk>().Should().ContainSingle();
		_ = result.Chunks.OfType<CsvRowChunk>().Should().HaveCount(2); // 2 data rows
	}

	[Fact]
	public async Task ChunkAsync_WithEmptyCsv_ShouldReturnEmpty()
	{
		// Arrange
		var content = "";
		using var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));
		var options = new ChunkingOptions();

		// Act
		var result = await _chunker.ChunkAsync(stream, options, CancellationToken);

		// Assert
		_ = result.Should().NotBeNull();
		_ = result.Success.Should().BeTrue();
		_ = result.Chunks.Should().BeEmpty();
	}

	[Fact]
	public async Task ChunkAsync_ShouldDetectCommaDelimiter()
	{
		// Arrange
		var content = "A,B,C\n1,2,3";
		using var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));
		var options = new ChunkingOptions();

		// Act
		var result = await _chunker.ChunkAsync(stream, options, CancellationToken);

		// Assert
		var doc = result.Chunks.OfType<CsvDocumentChunk>().FirstOrDefault();
		_ = doc.Should().NotBeNull();
		_ = doc!.Delimiter.Should().Be(',');
	}

	[Fact]
	public async Task ChunkAsync_ShouldDetectTabDelimiter()
	{
		// Arrange
		var content = "A\tB\tC\n1\t2\t3";
		using var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));
		var options = new ChunkingOptions();

		// Act
		var result = await _chunker.ChunkAsync(stream, options, CancellationToken);

		// Assert
		var doc = result.Chunks.OfType<CsvDocumentChunk>().FirstOrDefault();
		_ = doc.Should().NotBeNull();
		_ = doc!.Delimiter.Should().Be('\t');
	}

	[Fact]
	public async Task ChunkAsync_ShouldDetectHeaderRow()
	{
		// Arrange - Non-numeric header
		var content = "Name,Age,City\nAlice,30,New York";
		using var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));
		var options = new ChunkingOptions();

		// Act
		var result = await _chunker.ChunkAsync(stream, options, CancellationToken);

		// Assert
		var doc = result.Chunks.OfType<CsvDocumentChunk>().FirstOrDefault();
		_ = doc.Should().NotBeNull();
		_ = doc!.HasHeaderRow.Should().BeTrue();
		_ = doc.Headers.Should().Contain(["Name", "Age", "City"]);
	}

	[Fact]
	public async Task ChunkAsync_ShouldHandleQuotedFields()
	{
		// Arrange
		var content = "Name,Description\n\"John Doe\",\"Hello, World\"";
		using var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));
		var options = new ChunkingOptions();

		// Act
		var result = await _chunker.ChunkAsync(stream, options, CancellationToken);

		// Assert
		var row = result.Chunks.OfType<CsvRowChunk>().FirstOrDefault();
		_ = row.Should().NotBeNull();
		_ = row!.Fields.Should().Contain("John Doe");
		_ = row.Fields.Should().Contain("Hello, World");
		_ = row.HasQuotedFields.Should().BeTrue();
	}

	[Fact]
	public async Task ChunkAsync_ShouldGenerateStatistics()
	{
		// Arrange
		var content = "A,B\n1,2\n3,4";
		using var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));
		var options = new ChunkingOptions();

		// Act
		var result = await _chunker.ChunkAsync(stream, options, CancellationToken);

		// Assert
		_ = result.Statistics.Should().NotBeNull();
		_ = result.Statistics.TotalChunks.Should().BePositive();
		_ = result.Statistics.ProcessingTime.Should().BeGreaterThan(TimeSpan.Zero);
	}

	[Fact]
	public async Task ChunkAsync_WithValidation_ShouldValidateChunks()
	{
		// Arrange
		var content = "Name,Age\nAlice,30";
		using var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));
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
		// Arrange
		var content = "Name,Age\nAlice,30";
		using var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));
		var options = new ChunkingOptions();

		// Act
		var result = await _chunker.ChunkAsync(stream, options, CancellationToken);

		// Assert
		var rows = result.Chunks.OfType<CsvRowChunk>();
		_ = rows.Should().AllSatisfy(chunk =>
		{
			_ = chunk.QualityMetrics.Should().NotBeNull();
			_ = chunk.QualityMetrics!.TokenCount.Should().BePositive();
		});
	}

	[Fact]
	public async Task ChunkAsync_RowContent_ShouldIncludeHeaderContext()
	{
		// Arrange
		var content = "Name,Age\nAlice,30";
		using var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));
		var options = new ChunkingOptions();

		// Act
		var result = await _chunker.ChunkAsync(stream, options, CancellationToken);

		// Assert
		var row = result.Chunks.OfType<CsvRowChunk>().FirstOrDefault();
		_ = row.Should().NotBeNull();
		_ = row!.Content.Should().Contain("Name:");
		_ = row.Content.Should().Contain("Age:");
		_ = row.Content.Should().Contain("Alice");
	}
}
