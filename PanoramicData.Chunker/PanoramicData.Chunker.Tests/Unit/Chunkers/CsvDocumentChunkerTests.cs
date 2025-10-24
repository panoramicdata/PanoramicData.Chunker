using FluentAssertions;
using PanoramicData.Chunker.Chunkers.Csv;
using PanoramicData.Chunker.Configuration;
using PanoramicData.Chunker.Infrastructure;
using System.Text;
using Xunit;

namespace PanoramicData.Chunker.Tests.Unit.Chunkers;

/// <summary>
/// Unit tests for CsvDocumentChunker.
/// </summary>
public class CsvDocumentChunkerTests
{
	private readonly CharacterBasedTokenCounter _tokenCounter;
	private readonly CsvDocumentChunker _chunker;

	public CsvDocumentChunkerTests()
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
		chunker.Should().NotBeNull();
		chunker.SupportedType.Should().Be(DocumentType.Csv);
	}

	[Fact]
	public void Constructor_WithNullTokenCounter_ShouldThrow()
	{
		// Arrange, Act & Assert
		var act = () => new CsvDocumentChunker(null!);
		act.Should().Throw<ArgumentNullException>();
	}

	[Fact]
	public void SupportedType_ShouldReturnCsv()
	{
		// Arrange & Act
		var type = _chunker.SupportedType;

		// Assert
		type.Should().Be(DocumentType.Csv);
	}

	[Fact]
	public async Task CanHandleAsync_WithCsvStream_ShouldReturnTrue()
	{
		// Arrange - Simple CSV with comma delimiter
		var content = "Name,Age,City\nAlice,30,New York\nBob,25,London";
		using var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));

		// Act
		var result = await _chunker.CanHandleAsync(stream);

		// Assert
		result.Should().BeTrue();
		stream.Position.Should().Be(0); // Should restore position
	}

	[Fact]
	public async Task CanHandleAsync_WithTabDelimited_ShouldReturnTrue()
	{
		// Arrange - Tab-delimited
		var content = "Name\tAge\tCity\nAlice\t30\tNew York";
		using var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));

		// Act
		var result = await _chunker.CanHandleAsync(stream);

		// Assert
		result.Should().BeTrue();
	}

	[Fact]
	public async Task CanHandleAsync_WithNonCsvStream_ShouldReturnFalse()
	{
		// Arrange - Plain text without delimiters
		var content = "This is plain text without any delimiters or structure";
		using var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));

		// Act
		var result = await _chunker.CanHandleAsync(stream);

		// Assert
		result.Should().BeFalse();
	}

	[Fact]
	public async Task CanHandleAsync_WithEmptyStream_ShouldReturnFalse()
	{
		// Arrange
		using var stream = new MemoryStream();

		// Act
		var result = await _chunker.CanHandleAsync(stream);

		// Assert
		result.Should().BeFalse();
	}

	[Fact]
	public async Task ChunkAsync_WithNullStream_ShouldThrow()
	{
		// Arrange
		var options = new ChunkingOptions();

		// Act
		var act = async () => await _chunker.ChunkAsync(null!, options);

		// Assert
		await act.Should().ThrowAsync<ArgumentNullException>();
	}

	[Fact]
	public async Task ChunkAsync_WithNullOptions_ShouldThrow()
	{
		// Arrange
		using var stream = new MemoryStream();

		// Act
		var act = async () => await _chunker.ChunkAsync(stream, null!);

		// Assert
		await act.Should().ThrowAsync<ArgumentNullException>();
	}

	[Fact]
	public async Task ChunkAsync_WithValidCsv_ShouldReturnChunks()
	{
		// Arrange
		var content = "Name,Age,City\nAlice,30,New York\nBob,25,London";
		using var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));
		var options = new ChunkingOptions();

		// Act
		var result = await _chunker.ChunkAsync(stream, options);

		// Assert
		result.Should().NotBeNull();
		result.Success.Should().BeTrue();
		result.Chunks.Should().NotBeEmpty();
		
		// Should have document chunk and row chunks
		result.Chunks.OfType<CsvDocumentChunk>().Should().HaveCount(1);
		result.Chunks.OfType<CsvRowChunk>().Should().HaveCount(2); // 2 data rows
	}

	[Fact]
	public async Task ChunkAsync_WithEmptyCsv_ShouldReturnEmpty()
	{
		// Arrange
		var content = "";
		using var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));
		var options = new ChunkingOptions();

		// Act
		var result = await _chunker.ChunkAsync(stream, options);

		// Assert
		result.Should().NotBeNull();
		result.Success.Should().BeTrue();
		result.Chunks.Should().BeEmpty();
	}

	[Fact]
	public async Task ChunkAsync_ShouldDetectCommaDelimiter()
	{
		// Arrange
		var content = "A,B,C\n1,2,3";
		using var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));
		var options = new ChunkingOptions();

		// Act
		var result = await _chunker.ChunkAsync(stream, options);

		// Assert
		var doc = result.Chunks.OfType<CsvDocumentChunk>().FirstOrDefault();
		doc.Should().NotBeNull();
		doc!.Delimiter.Should().Be(',');
	}

	[Fact]
	public async Task ChunkAsync_ShouldDetectTabDelimiter()
	{
		// Arrange
		var content = "A\tB\tC\n1\t2\t3";
		using var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));
		var options = new ChunkingOptions();

		// Act
		var result = await _chunker.ChunkAsync(stream, options);

		// Assert
		var doc = result.Chunks.OfType<CsvDocumentChunk>().FirstOrDefault();
		doc.Should().NotBeNull();
		doc!.Delimiter.Should().Be('\t');
	}

	[Fact]
	public async Task ChunkAsync_ShouldDetectHeaderRow()
	{
		// Arrange - Non-numeric header
		var content = "Name,Age,City\nAlice,30,New York";
		using var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));
		var options = new ChunkingOptions();

		// Act
		var result = await _chunker.ChunkAsync(stream, options);

		// Assert
		var doc = result.Chunks.OfType<CsvDocumentChunk>().FirstOrDefault();
		doc.Should().NotBeNull();
		doc!.HasHeaderRow.Should().BeTrue();
		doc.Headers.Should().Contain(new[] { "Name", "Age", "City" });
	}

	[Fact]
	public async Task ChunkAsync_ShouldHandleQuotedFields()
	{
		// Arrange
		var content = "Name,Description\n\"John Doe\",\"Hello, World\"";
		using var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));
		var options = new ChunkingOptions();

		// Act
		var result = await _chunker.ChunkAsync(stream, options);

		// Assert
		var row = result.Chunks.OfType<CsvRowChunk>().FirstOrDefault();
		row.Should().NotBeNull();
		row!.Fields.Should().Contain("John Doe");
		row.Fields.Should().Contain("Hello, World");
		row.HasQuotedFields.Should().BeTrue();
	}

	[Fact]
	public async Task ChunkAsync_ShouldGenerateStatistics()
	{
		// Arrange
		var content = "A,B\n1,2\n3,4";
		using var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));
		var options = new ChunkingOptions();

		// Act
		var result = await _chunker.ChunkAsync(stream, options);

		// Assert
		result.Statistics.Should().NotBeNull();
		result.Statistics.TotalChunks.Should().BeGreaterThan(0);
		result.Statistics.ProcessingTime.Should().BeGreaterThan(TimeSpan.Zero);
	}

	[Fact]
	public async Task ChunkAsync_WithValidation_ShouldValidateChunks()
	{
		// Arrange
		var content = "Name,Age\nAlice,30";
		using var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));
		var options = new ChunkingOptions { ValidateChunks = true };

		// Act
		var result = await _chunker.ChunkAsync(stream, options);

		// Assert
		result.ValidationResult.Should().NotBeNull();
		result.ValidationResult!.IsValid.Should().BeTrue();
	}

	[Fact]
	public async Task ChunkAsync_ShouldCalculateQualityMetrics()
	{
		// Arrange
		var content = "Name,Age\nAlice,30";
		using var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));
		var options = new ChunkingOptions();

		// Act
		var result = await _chunker.ChunkAsync(stream, options);

		// Assert
		var rows = result.Chunks.OfType<CsvRowChunk>();
		rows.Should().AllSatisfy(chunk =>
		{
			chunk.QualityMetrics.Should().NotBeNull();
			chunk.QualityMetrics!.TokenCount.Should().BeGreaterThan(0);
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
		var result = await _chunker.ChunkAsync(stream, options);

		// Assert
		var row = result.Chunks.OfType<CsvRowChunk>().FirstOrDefault();
		row.Should().NotBeNull();
		row!.Content.Should().Contain("Name:");
		row.Content.Should().Contain("Age:");
		row.Content.Should().Contain("Alice");
	}
}
