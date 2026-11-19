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
		var result = await ChunkCsvAsync("simple.csv");

		_ = result.Success.Should().BeTrue();
		_ = result.Chunks.Should().NotBeEmpty();

		var documents = result.Chunks.OfType<CsvDocumentChunk>().ToList();
		_ = documents.Should().ContainSingle();

		var doc = documents[0];
		_ = doc.Delimiter.Should().Be(',');
		_ = doc.HasHeaderRow.Should().BeTrue();
		_ = doc.Headers.Should().Contain(["Name", "Age", "City"]);
		_ = doc.ColumnCount.Should().Be(3);

		var rows = Rows(result);
		_ = rows.Should().HaveCount(3);

		_ = rows[0].Fields.Should().Contain("Alice");
		_ = rows[1].Fields.Should().Contain("Bob");
		_ = rows[2].Fields.Should().Contain("Charlie");
	}

	[Fact]
	public async Task ChunkAsync_EmptyCsv_ShouldReturnEmpty()
	{
		var result = await ChunkCsvAsync("empty.csv");

		_ = result.Success.Should().BeTrue();
		_ = result.Chunks.Should().BeEmpty();
	}

	[Fact]
	public async Task ChunkAsync_WithQuotes_ShouldParseCorrectly()
	{
		var result = await ChunkCsvAsync("with-quotes.csv");

		_ = result.Success.Should().BeTrue();

		var rows = Rows(result);
		_ = rows.Should().HaveCount(3);
		_ = rows[0].Fields[0].Should().Be("Premium Widget");
		_ = rows[0].Fields[1].Should().Contain("High-quality");
		_ = rows[0].HasQuotedFields.Should().BeTrue();
	}

	[Fact]
	public async Task ChunkAsync_TabDelimited_ShouldDetectDelimiter()
	{
		var result = await ChunkCsvAsync("tab-delimited.csv");

		_ = result.Success.Should().BeTrue();
		_ = Document(result).Delimiter.Should().Be('\t');
		_ = Rows(result).Should().HaveCount(2);
	}

	[Theory]
	[InlineData("semicolon-delimited.csv", ';')]
	[InlineData("pipe-delimited.csv", '|')]
	public async Task ChunkAsync_OtherDelimiters_ShouldDetectDelimiter(string fileName, char delimiter)
	{
		var result = await ChunkCsvAsync(fileName);

		_ = result.Success.Should().BeTrue();
		_ = Document(result).Delimiter.Should().Be(delimiter);
	}

	[Fact]
	public async Task ChunkAsync_LargeCsv_ShouldHandleEfficiently()
	{
		var startTime = DateTime.UtcNow;
		var result = await ChunkCsvAsync("large.csv");
		var processingTime = DateTime.UtcNow - startTime;

		_ = result.Success.Should().BeTrue();
		_ = Rows(result).Should().HaveCount(1000);
		_ = processingTime.TotalSeconds.Should().BeLessThan(2);
	}

	[Fact]
	public async Task ChunkAsync_NoHeader_ShouldDetectNoHeader()
	{
		var result = await ChunkCsvAsync("no-header.csv");

		_ = result.Success.Should().BeTrue();

		var doc = Document(result);
		_ = doc.HasHeaderRow.Should().BeFalse();
		_ = doc.Headers.Should().BeEmpty();
		_ = Rows(result).Should().HaveCount(3);
	}

	[Fact]
	public async Task ChunkAsync_MixedData_ShouldPreserveValues()
	{
		var result = await ChunkCsvAsync("mixed-data.csv");

		_ = result.Success.Should().BeTrue();
		_ = Document(result).Headers.Should().Contain(["Text", "Number", "Date", "Boolean", "Currency"]);

		var rows = Rows(result);
		_ = rows.Should().HaveCount(3);
		_ = rows[0].Fields.Should().Contain("Sample");
		_ = rows[0].Fields.Should().Contain("123.45");
	}

	[Fact]
	public async Task ChunkAsync_ShouldCalculateTokenCounts()
	{
		var result = await ChunkCsvAsync("simple.csv");

		_ = result.Success.Should().BeTrue();
		_ = Rows(result).Should().AllSatisfy(row =>
		{
			_ = row.QualityMetrics.Should().NotBeNull();
			_ = row.QualityMetrics!.TokenCount.Should().BePositive();
		});
	}

	[Fact]
	public async Task ChunkAsync_ShouldBuildHierarchy()
	{
		var result = await ChunkCsvAsync("simple.csv");

		_ = result.Success.Should().BeTrue();

		var doc = Document(result);
		_ = Rows(result).Should().AllSatisfy(row =>
		{
			_ = row.ParentId.Should().Be(doc.Id);
			_ = row.Depth.Should().BeGreaterThan(doc.Depth);
		});
	}

	[Fact]
	public async Task ChunkAsync_ShouldGenerateStatistics()
	{
		var result = await ChunkCsvAsync("simple.csv");

		_ = result.Statistics.Should().NotBeNull();
		_ = result.Statistics.TotalChunks.Should().Be(result.Chunks.Count);
		_ = result.Statistics.StructuralChunks.Should().Be(1);
		_ = result.Statistics.TableChunks.Should().Be(3);
		_ = result.Statistics.ProcessingTime.Should().BeGreaterThan(TimeSpan.Zero);
	}

	[Fact]
	public async Task ChunkAsync_WithAutoDetect_ShouldDetectCsv()
	{
		await using var stream = File.OpenRead(Path.Combine(_testDataPath, "simple.csv"));

		var result = await DocumentChunker.ChunkAutoDetectAsync(stream, "test.csv", ChunkingPresets.ForOpenAIEmbeddings(), CancellationToken);

		_ = result.Success.Should().BeTrue();
		_ = result.Chunks.Should().NotBeEmpty();
	}

	[Fact]
	public async Task ChunkAsync_SerializedRow_ShouldBeMarkdownFormat()
	{
		var result = await ChunkCsvAsync("simple.csv");

		var row = Rows(result)[0];
		_ = row.SerializationFormat.Should().Be(TableSerializationFormat.Markdown);
		_ = row.SerializedTable.Should().Contain("|");
		_ = row.SerializedTable.Should().Contain("---");
	}

	[Fact]
	public async Task ChunkAsync_WithValidation_ShouldValidateChunks()
	{
		var result = await ChunkFileAsync(
			Path.Combine(_testDataPath, "simple.csv"),
			DocumentType.Csv,
			new ChunkingOptions { ValidateChunks = true });

		_ = result.ValidationResult.Should().NotBeNull();
		_ = result.ValidationResult!.IsValid.Should().BeTrue();
	}

	[Fact]
	public async Task ChunkAsync_RowContent_ShouldIncludeHeaderContext()
	{
		var result = await ChunkCsvAsync("simple.csv");

		var row = Rows(result)[0];
		_ = row.Content.Should().Contain("Name:");
		_ = row.Content.Should().Contain("Age:");
		_ = row.Content.Should().Contain("City:");
		_ = row.Content.Should().Contain("Alice");
	}

	private Task<ChunkingResult> ChunkCsvAsync(string fileName)
		=> ChunkFileAsync(Path.Combine(_testDataPath, fileName), DocumentType.Csv);

	private static CsvDocumentChunk Document(ChunkingResult result) => result.Chunks.OfType<CsvDocumentChunk>().First();

	private static List<CsvRowChunk> Rows(ChunkingResult result) => [.. result.Chunks.OfType<CsvRowChunk>()];
}
