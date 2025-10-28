using AwesomeAssertions;
using PanoramicData.Chunker.Chunkers.Csv;
using PanoramicData.Chunker.Configuration;
using PanoramicData.Chunker.Models;

namespace PanoramicData.Chunker.Tests.Integration;

/// <summary>
/// Integration tests for CSV document chunking.
/// </summary>
public class CsvIntegrationTests(ITestOutputHelper output) : BaseTest(output)
{
	private readonly string _testDataPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TestData", "Csv");

	[Fact]
	public async Task ChunkAsync_SimpleCsv_ShouldExtractRows()
	{
		// Arrange
		var filePath = Path.Combine(_testDataPath, "simple.csv");
		await using var stream = File.OpenRead(filePath);
		var options = ChunkingPresets.ForOpenAIEmbeddings();

		// Act
		var result = await DocumentChunker.ChunkAsync(stream, DocumentType.Csv, options, CancellationToken);

		// Assert
		_ = result.Success.Should().BeTrue();
		_ = result.Chunks.Should().NotBeEmpty();

		// Should have document chunk
		var documents = result.Chunks.OfType<CsvDocumentChunk>().ToList();
		_ = documents.Should().ContainSingle();

		var doc = documents[0];
		_ = doc.Delimiter.Should().Be(',');
		_ = doc.HasHeaderRow.Should().BeTrue();
		_ = doc.Headers.Should().Contain(["Name", "Age", "City"]);
		_ = doc.ColumnCount.Should().Be(3);

		// Should have row chunks
		var rows = result.Chunks.OfType<CsvRowChunk>().ToList();
		_ = rows.Should().HaveCount(3); // 3 data rows

		_ = rows[0].Fields.Should().Contain("Alice");
		_ = rows[1].Fields.Should().Contain("Bob");
		_ = rows[2].Fields.Should().Contain("Charlie");
	}

	[Fact]
	public async Task ChunkAsync_EmptyCsv_ShouldReturnEmpty()
	{
		// Arrange
		var filePath = Path.Combine(_testDataPath, "empty.csv");
		await using var stream = File.OpenRead(filePath);
		var options = ChunkingPresets.ForOpenAIEmbeddings();

		// Act
		var result = await DocumentChunker.ChunkAsync(stream, DocumentType.Csv, options, CancellationToken);

		// Assert
		_ = result.Success.Should().BeTrue();
		_ = result.Chunks.Should().BeEmpty();
	}

	[Fact]
	public async Task ChunkAsync_WithQuotes_ShouldParseCorrectly()
	{
		// Arrange
		var filePath = Path.Combine(_testDataPath, "with-quotes.csv");
		await using var stream = File.OpenRead(filePath);
		var options = ChunkingPresets.ForOpenAIEmbeddings();

		// Act
		var result = await DocumentChunker.ChunkAsync(stream, DocumentType.Csv, options, CancellationToken);

		// Assert
		_ = result.Success.Should().BeTrue();

		var rows = result.Chunks.OfType<CsvRowChunk>().ToList();
		_ = rows.Should().HaveCount(3);

		// Check quoted fields are parsed correctly
		_ = rows[0].Fields[0].Should().Be("Premium Widget");
		_ = rows[0].Fields[1].Should().Contain("High-quality");
		_ = rows[0].HasQuotedFields.Should().BeTrue();
	}

	[Fact]
	public async Task ChunkAsync_TabDelimited_ShouldDetectDelimiter()
	{
		// Arrange
		var filePath = Path.Combine(_testDataPath, "tab-delimited.csv");
		await using var stream = File.OpenRead(filePath);
		var options = ChunkingPresets.ForOpenAIEmbeddings();

		// Act
		var result = await DocumentChunker.ChunkAsync(stream, DocumentType.Csv, options, CancellationToken);

		// Assert
		_ = result.Success.Should().BeTrue();

		var doc = result.Chunks.OfType<CsvDocumentChunk>().First();
		_ = doc.Delimiter.Should().Be('\t');

		var rows = result.Chunks.OfType<CsvRowChunk>().ToList();
		_ = rows.Should().HaveCount(2);
	}

	[Fact]
	public async Task ChunkAsync_SemicolonDelimited_ShouldDetectDelimiter()
	{
		// Arrange
		var filePath = Path.Combine(_testDataPath, "semicolon-delimited.csv");
		await using var stream = File.OpenRead(filePath);
		var options = ChunkingPresets.ForOpenAIEmbeddings();

		// Act
		var result = await DocumentChunker.ChunkAsync(stream, DocumentType.Csv, options, CancellationToken);

		// Assert
		_ = result.Success.Should().BeTrue();

		var doc = result.Chunks.OfType<CsvDocumentChunk>().First();
		_ = doc.Delimiter.Should().Be(';');
	}

	[Fact]
	public async Task ChunkAsync_PipeDelimited_ShouldDetectDelimiter()
	{
		// Arrange
		var filePath = Path.Combine(_testDataPath, "pipe-delimited.csv");
		await using var stream = File.OpenRead(filePath);
		var options = ChunkingPresets.ForOpenAIEmbeddings();

		// Act
		var result = await DocumentChunker.ChunkAsync(stream, DocumentType.Csv, options, CancellationToken);

		// Assert
		_ = result.Success.Should().BeTrue();

		var doc = result.Chunks.OfType<CsvDocumentChunk>().First();
		_ = doc.Delimiter.Should().Be('|');
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
		var result = await DocumentChunker.ChunkAsync(stream, DocumentType.Csv, options, CancellationToken);
		var processingTime = DateTime.UtcNow - startTime;

		// Assert
		_ = result.Success.Should().BeTrue();

		var rows = result.Chunks.OfType<CsvRowChunk>().ToList();
		_ = rows.Should().HaveCount(1000);

		// Should process 1000 rows reasonably fast (< 2 seconds)
		_ = processingTime.TotalSeconds.Should().BeLessThan(2);
	}

	[Fact]
	public async Task ChunkAsync_NoHeader_ShouldDetectNoHeader()
	{
		// Arrange
		var filePath = Path.Combine(_testDataPath, "no-header.csv");
		await using var stream = File.OpenRead(filePath);
		var options = ChunkingPresets.ForOpenAIEmbeddings();

		// Act
		var result = await DocumentChunker.ChunkAsync(stream, DocumentType.Csv, options, CancellationToken);

		// Assert
		_ = result.Success.Should().BeTrue();

		var doc = result.Chunks.OfType<CsvDocumentChunk>().First();
		_ = doc.HasHeaderRow.Should().BeFalse();
		_ = doc.Headers.Should().BeEmpty();

		var rows = result.Chunks.OfType<CsvRowChunk>().ToList();
		_ = rows.Should().HaveCount(3);
	}

	[Fact]
	public async Task ChunkAsync_MixedData_ShouldPreserveValues()
	{
		// Arrange
		var filePath = Path.Combine(_testDataPath, "mixed-data.csv");
		await using var stream = File.OpenRead(filePath);
		var options = ChunkingPresets.ForOpenAIEmbeddings();

		// Act
		var result = await DocumentChunker.ChunkAsync(stream, DocumentType.Csv, options, CancellationToken);

		// Assert
		_ = result.Success.Should().BeTrue();

		var doc = result.Chunks.OfType<CsvDocumentChunk>().First();
		_ = doc.Headers.Should().Contain(["Text", "Number", "Date", "Boolean", "Currency"]);

		var rows = result.Chunks.OfType<CsvRowChunk>().ToList();
		_ = rows.Should().HaveCount(3);
		_ = rows[0].Fields.Should().Contain("Sample");
		_ = rows[0].Fields.Should().Contain("123.45");
	}

	[Fact]
	public async Task ChunkAsync_ShouldCalculateTokenCounts()
	{
		// Arrange
		var filePath = Path.Combine(_testDataPath, "simple.csv");
		await using var stream = File.OpenRead(filePath);
		var options = ChunkingPresets.ForOpenAIEmbeddings();

		// Act
		var result = await DocumentChunker.ChunkAsync(stream, DocumentType.Csv, options, CancellationToken);

		// Assert
		_ = result.Success.Should().BeTrue();

		// Row chunks should have quality metrics
		var rows = result.Chunks.OfType<CsvRowChunk>().ToList();
		_ = rows.Should().AllSatisfy(row =>
		{
			_ = row.QualityMetrics.Should().NotBeNull();
			_ = row.QualityMetrics!.TokenCount.Should().BePositive();
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
		var result = await DocumentChunker.ChunkAsync(stream, DocumentType.Csv, options, CancellationToken);

		// Assert
		_ = result.Success.Should().BeTrue();

		var doc = result.Chunks.OfType<CsvDocumentChunk>().First();
		var rows = result.Chunks.OfType<CsvRowChunk>().ToList();

		// All rows should be children of document
		_ = rows.Should().AllSatisfy(row =>
		{
			_ = row.ParentId.Should().Be(doc.Id);
			_ = row.Depth.Should().BeGreaterThan(doc.Depth);
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
		var result = await DocumentChunker.ChunkAsync(stream, DocumentType.Csv, options, CancellationToken);

		// Assert
		_ = result.Statistics.Should().NotBeNull();
		_ = result.Statistics.TotalChunks.Should().Be(result.Chunks.Count);
		_ = result.Statistics.StructuralChunks.Should().Be(1); // Document chunk
		_ = result.Statistics.TableChunks.Should().Be(3); // 3 row chunks
		_ = result.Statistics.ProcessingTime.Should().BeGreaterThan(TimeSpan.Zero);
	}

	[Fact]
	public async Task ChunkAsync_WithAutoDetect_ShouldDetectCsv()
	{
		// Arrange
		var filePath = Path.Combine(_testDataPath, "simple.csv");
		await using var stream = File.OpenRead(filePath);
		var options = ChunkingPresets.ForOpenAIEmbeddings();

		// Act - Use auto-detect
		var result = await DocumentChunker.ChunkAutoDetectAsync(stream, "test.csv", options, CancellationToken);

		// Assert
		_ = result.Success.Should().BeTrue();
		_ = result.Chunks.Should().NotBeEmpty();
	}

	[Fact]
	public async Task ChunkAsync_SerializedRow_ShouldBeMarkdownFormat()
	{
		// Arrange
		var filePath = Path.Combine(_testDataPath, "simple.csv");
		await using var stream = File.OpenRead(filePath);
		var options = ChunkingPresets.ForOpenAIEmbeddings();

		// Act
		var result = await DocumentChunker.ChunkAsync(stream, DocumentType.Csv, options, CancellationToken);

		// Assert
		var row = result.Chunks.OfType<CsvRowChunk>().First();
		_ = row.SerializationFormat.Should().Be(TableSerializationFormat.Markdown);
		_ = row.SerializedTable.Should().Contain("|");
		_ = row.SerializedTable.Should().Contain("---");
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
		var result = await DocumentChunker.ChunkAsync(stream, DocumentType.Csv, options, CancellationToken);

		// Assert
		_ = result.ValidationResult.Should().NotBeNull();
		_ = result.ValidationResult!.IsValid.Should().BeTrue();
	}

	[Fact]
	public async Task ChunkAsync_RowContent_ShouldIncludeHeaderContext()
	{
		// Arrange
		var filePath = Path.Combine(_testDataPath, "simple.csv");
		await using var stream = File.OpenRead(filePath);
		var options = ChunkingPresets.ForOpenAIEmbeddings();

		// Act
		var result = await DocumentChunker.ChunkAsync(stream, DocumentType.Csv, options, CancellationToken);

		// Assert
		var row = result.Chunks.OfType<CsvRowChunk>().First();

		// Content should have header context (Name: Alice, Age: 30, City: New York)
		_ = row.Content.Should().Contain("Name:");
		_ = row.Content.Should().Contain("Age:");
		_ = row.Content.Should().Contain("City:");
		_ = row.Content.Should().Contain("Alice");
	}
}
