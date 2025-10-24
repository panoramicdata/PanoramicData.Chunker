using FluentAssertions;
using PanoramicData.Chunker.Chunkers.Csv;
using PanoramicData.Chunker.Configuration;
using PanoramicData.Chunker.Models;

namespace PanoramicData.Chunker.Tests.Integration;

/// <summary>
/// Integration tests for CSV document chunking.
/// </summary>
public class CsvIntegrationTests
{
	private readonly string _testDataPath;

	public CsvIntegrationTests()
	{
		_testDataPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TestData", "Csv");
	}

	[Fact]
	public async Task ChunkAsync_SimpleCsv_ShouldExtractRows()
	{
		// Arrange
		var filePath = Path.Combine(_testDataPath, "simple.csv");
		await using var stream = File.OpenRead(filePath);
		var options = ChunkingPresets.ForOpenAIEmbeddings();

		// Act
		var result = await DocumentChunker.ChunkAsync(stream, DocumentType.Csv, options);

		// Assert
		result.Success.Should().BeTrue();
		result.Chunks.Should().NotBeEmpty();

		// Should have document chunk
		var documents = result.Chunks.OfType<CsvDocumentChunk>().ToList();
		documents.Should().HaveCount(1);
		
		var doc = documents[0];
		doc.Delimiter.Should().Be(',');
		doc.HasHeaderRow.Should().BeTrue();
		doc.Headers.Should().Contain(new[] { "Name", "Age", "City" });
		doc.ColumnCount.Should().Be(3);

		// Should have row chunks
		var rows = result.Chunks.OfType<CsvRowChunk>().ToList();
		rows.Should().HaveCount(3); // 3 data rows
		
		rows[0].Fields.Should().Contain("Alice");
		rows[1].Fields.Should().Contain("Bob");
		rows[2].Fields.Should().Contain("Charlie");
	}

	[Fact]
	public async Task ChunkAsync_EmptyCsv_ShouldReturnEmpty()
	{
		// Arrange
		var filePath = Path.Combine(_testDataPath, "empty.csv");
		await using var stream = File.OpenRead(filePath);
		var options = ChunkingPresets.ForOpenAIEmbeddings();

		// Act
		var result = await DocumentChunker.ChunkAsync(stream, DocumentType.Csv, options);

		// Assert
		result.Success.Should().BeTrue();
		result.Chunks.Should().BeEmpty();
	}

	[Fact]
	public async Task ChunkAsync_WithQuotes_ShouldParseCorrectly()
	{
		// Arrange
		var filePath = Path.Combine(_testDataPath, "with-quotes.csv");
		await using var stream = File.OpenRead(filePath);
		var options = ChunkingPresets.ForOpenAIEmbeddings();

		// Act
		var result = await DocumentChunker.ChunkAsync(stream, DocumentType.Csv, options);

		// Assert
		result.Success.Should().BeTrue();
		
		var rows = result.Chunks.OfType<CsvRowChunk>().ToList();
		rows.Should().HaveCount(3);
		
		// Check quoted fields are parsed correctly
		rows[0].Fields[0].Should().Be("Premium Widget");
		rows[0].Fields[1].Should().Contain("High-quality");
		rows[0].HasQuotedFields.Should().BeTrue();
	}

	[Fact]
	public async Task ChunkAsync_TabDelimited_ShouldDetectDelimiter()
	{
		// Arrange
		var filePath = Path.Combine(_testDataPath, "tab-delimited.csv");
		await using var stream = File.OpenRead(filePath);
		var options = ChunkingPresets.ForOpenAIEmbeddings();

		// Act
		var result = await DocumentChunker.ChunkAsync(stream, DocumentType.Csv, options);

		// Assert
		result.Success.Should().BeTrue();
		
		var doc = result.Chunks.OfType<CsvDocumentChunk>().First();
		doc.Delimiter.Should().Be('\t');
		
		var rows = result.Chunks.OfType<CsvRowChunk>().ToList();
		rows.Should().HaveCount(2);
	}

	[Fact]
	public async Task ChunkAsync_SemicolonDelimited_ShouldDetectDelimiter()
	{
		// Arrange
		var filePath = Path.Combine(_testDataPath, "semicolon-delimited.csv");
		await using var stream = File.OpenRead(filePath);
		var options = ChunkingPresets.ForOpenAIEmbeddings();

		// Act
		var result = await DocumentChunker.ChunkAsync(stream, DocumentType.Csv, options);

		// Assert
		result.Success.Should().BeTrue();
		
		var doc = result.Chunks.OfType<CsvDocumentChunk>().First();
		doc.Delimiter.Should().Be(';');
	}

	[Fact]
	public async Task ChunkAsync_PipeDelimited_ShouldDetectDelimiter()
	{
		// Arrange
		var filePath = Path.Combine(_testDataPath, "pipe-delimited.csv");
		await using var stream = File.OpenRead(filePath);
		var options = ChunkingPresets.ForOpenAIEmbeddings();

		// Act
		var result = await DocumentChunker.ChunkAsync(stream, DocumentType.Csv, options);

		// Assert
		result.Success.Should().BeTrue();
		
		var doc = result.Chunks.OfType<CsvDocumentChunk>().First();
		doc.Delimiter.Should().Be('|');
	}

	[Fact]
	public async Task ChunkAsync_LargeCsv_ShouldHandleEfficiently()
	{
		// Arrange
		var filePath = Path.Combine(_testDataPath, "large.csv");
		await using var stream = File.OpenRead(filePath);
		var options = ChunkingPresets.ForOpenAIEmbeddings();

		// Act
		var startTime = DateTime.UtcNow;
		var result = await DocumentChunker.ChunkAsync(stream, DocumentType.Csv, options);
		var processingTime = DateTime.UtcNow - startTime;

		// Assert
		result.Success.Should().BeTrue();
		
		var rows = result.Chunks.OfType<CsvRowChunk>().ToList();
		rows.Should().HaveCount(1000);
		
		// Should process 1000 rows reasonably fast (< 2 seconds)
		processingTime.TotalSeconds.Should().BeLessThan(2);
	}

	[Fact]
	public async Task ChunkAsync_NoHeader_ShouldDetectNoHeader()
	{
		// Arrange
		var filePath = Path.Combine(_testDataPath, "no-header.csv");
		await using var stream = File.OpenRead(filePath);
		var options = ChunkingPresets.ForOpenAIEmbeddings();

		// Act
		var result = await DocumentChunker.ChunkAsync(stream, DocumentType.Csv, options);

		// Assert
		result.Success.Should().BeTrue();
		
		var doc = result.Chunks.OfType<CsvDocumentChunk>().First();
		doc.HasHeaderRow.Should().BeFalse();
		doc.Headers.Should().BeEmpty();
		
		var rows = result.Chunks.OfType<CsvRowChunk>().ToList();
		rows.Should().HaveCount(3);
	}

	[Fact]
	public async Task ChunkAsync_MixedData_ShouldPreserveValues()
	{
		// Arrange
		var filePath = Path.Combine(_testDataPath, "mixed-data.csv");
		await using var stream = File.OpenRead(filePath);
		var options = ChunkingPresets.ForOpenAIEmbeddings();

		// Act
		var result = await DocumentChunker.ChunkAsync(stream, DocumentType.Csv, options);

		// Assert
		result.Success.Should().BeTrue();
		
		var doc = result.Chunks.OfType<CsvDocumentChunk>().First();
		doc.Headers.Should().Contain(new[] { "Text", "Number", "Date", "Boolean", "Currency" });
		
		var rows = result.Chunks.OfType<CsvRowChunk>().ToList();
		rows.Should().HaveCount(3);
		rows[0].Fields.Should().Contain("Sample");
		rows[0].Fields.Should().Contain("123.45");
	}

	[Fact]
	public async Task ChunkAsync_ShouldCalculateTokenCounts()
	{
		// Arrange
		var filePath = Path.Combine(_testDataPath, "simple.csv");
		await using var stream = File.OpenRead(filePath);
		var options = ChunkingPresets.ForOpenAIEmbeddings();

		// Act
		var result = await DocumentChunker.ChunkAsync(stream, DocumentType.Csv, options);

		// Assert
		result.Success.Should().BeTrue();
		
		// Row chunks should have quality metrics
		var rows = result.Chunks.OfType<CsvRowChunk>().ToList();
		rows.Should().AllSatisfy(row =>
		{
			row.QualityMetrics.Should().NotBeNull();
			row.QualityMetrics!.TokenCount.Should().BeGreaterThan(0);
		});
	}

	[Fact]
	public async Task ChunkAsync_ShouldBuildHierarchy()
	{
		// Arrange
		var filePath = Path.Combine(_testDataPath, "simple.csv");
		await using var stream = File.OpenRead(filePath);
		var options = ChunkingPresets.ForOpenAIEmbeddings();

		// Act
		var result = await DocumentChunker.ChunkAsync(stream, DocumentType.Csv, options);

		// Assert
		result.Success.Should().BeTrue();
		
		var doc = result.Chunks.OfType<CsvDocumentChunk>().First();
		var rows = result.Chunks.OfType<CsvRowChunk>().ToList();
		
		// All rows should be children of document
		rows.Should().AllSatisfy(row =>
		{
			row.ParentId.Should().Be(doc.Id);
			row.Depth.Should().BeGreaterThan(doc.Depth);
		});
	}

	[Fact]
	public async Task ChunkAsync_ShouldGenerateStatistics()
	{
		// Arrange
		var filePath = Path.Combine(_testDataPath, "simple.csv");
		await using var stream = File.OpenRead(filePath);
		var options = ChunkingPresets.ForOpenAIEmbeddings();

		// Act
		var result = await DocumentChunker.ChunkAsync(stream, DocumentType.Csv, options);

		// Assert
		result.Statistics.Should().NotBeNull();
		result.Statistics.TotalChunks.Should().Be(result.Chunks.Count);
		result.Statistics.StructuralChunks.Should().Be(1); // Document chunk
		result.Statistics.TableChunks.Should().Be(3); // 3 row chunks
		result.Statistics.ProcessingTime.Should().BeGreaterThan(TimeSpan.Zero);
	}

	[Fact]
	public async Task ChunkAsync_WithAutoDetect_ShouldDetectCsv()
	{
		// Arrange
		var filePath = Path.Combine(_testDataPath, "simple.csv");
		await using var stream = File.OpenRead(filePath);
		var options = ChunkingPresets.ForOpenAIEmbeddings();

		// Act - Use auto-detect
		var result = await DocumentChunker.ChunkAutoDetectAsync(stream, "test.csv", options);

		// Assert
		result.Success.Should().BeTrue();
		result.Chunks.Should().NotBeEmpty();
	}

	[Fact]
	public async Task ChunkAsync_SerializedRow_ShouldBeMarkdownFormat()
	{
		// Arrange
		var filePath = Path.Combine(_testDataPath, "simple.csv");
		await using var stream = File.OpenRead(filePath);
		var options = ChunkingPresets.ForOpenAIEmbeddings();

		// Act
		var result = await DocumentChunker.ChunkAsync(stream, DocumentType.Csv, options);

		// Assert
		var row = result.Chunks.OfType<CsvRowChunk>().First();
		row.SerializationFormat.Should().Be(TableSerializationFormat.Markdown);
		row.SerializedTable.Should().Contain("|");
		row.SerializedTable.Should().Contain("---");
	}

	[Fact]
	public async Task ChunkAsync_WithValidation_ShouldValidateChunks()
	{
		// Arrange
		var filePath = Path.Combine(_testDataPath, "simple.csv");
		await using var stream = File.OpenRead(filePath);
		var options = new ChunkingOptions
		{
			ValidateChunks = true
		};

		// Act
		var result = await DocumentChunker.ChunkAsync(stream, DocumentType.Csv, options);

		// Assert
		result.ValidationResult.Should().NotBeNull();
		result.ValidationResult!.IsValid.Should().BeTrue();
	}

	[Fact]
	public async Task ChunkAsync_RowContent_ShouldIncludeHeaderContext()
	{
		// Arrange
		var filePath = Path.Combine(_testDataPath, "simple.csv");
		await using var stream = File.OpenRead(filePath);
		var options = ChunkingPresets.ForOpenAIEmbeddings();

		// Act
		var result = await DocumentChunker.ChunkAsync(stream, DocumentType.Csv, options);

		// Assert
		var row = result.Chunks.OfType<CsvRowChunk>().First();
		
		// Content should have header context (Name: Alice, Age: 30, City: New York)
		row.Content.Should().Contain("Name:");
		row.Content.Should().Contain("Age:");
		row.Content.Should().Contain("City:");
		row.Content.Should().Contain("Alice");
	}
}
