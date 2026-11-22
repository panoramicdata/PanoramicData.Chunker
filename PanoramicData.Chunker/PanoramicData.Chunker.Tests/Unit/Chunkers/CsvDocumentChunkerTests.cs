using AwesomeAssertions;
using PanoramicData.Chunker.Chunkers.Csv;
using PanoramicData.Chunker.Configuration;
using PanoramicData.Chunker.Infrastructure;
using PanoramicData.Chunker.Models;
using PanoramicData.Chunker.Tests.Utilities;
using System.Text;

namespace PanoramicData.Chunker.Tests.Unit.Chunkers;

/// <summary>
/// Unit tests for CsvDocumentChunker.
/// </summary>
public class CsvDocumentChunkerTests : DocumentChunkerContractTests
{
	private readonly CsvDocumentChunker _chunker;

	public CsvDocumentChunkerTests(ITestOutputHelper output) : base(output, tokenCounter => new CsvDocumentChunker(tokenCounter), DocumentType.Csv)
	{
		_chunker = new CsvDocumentChunker(new CharacterBasedTokenCounter());
	}

	[Fact]
	public void SupportedType_ShouldReturnCsv()
	{
		_ = _chunker.SupportedType.Should().Be(DocumentType.Csv);
	}

	[Theory]
	[InlineData("Name,Age,City\nAlice,30,New York\nBob,25,London", true)]
	[InlineData("Name\tAge\tCity\nAlice\t30\tNew York", true)]
	[InlineData("This is plain text without any delimiters or structure", false)]
	[InlineData("", false)]
	public async Task CanHandleAsync_ShouldDetectDelimitedContent(string content, bool expected)
	{
		using var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));

		var result = await _chunker.CanHandleAsync(stream, CancellationToken);

		_ = result.Should().Be(expected);
		_ = stream.Position.Should().Be(0);
	}

	[Fact]
	public async Task ChunkAsync_WithValidCsv_ShouldReturnChunks()
	{
		var result = await ChunkAsync("Name,Age,City\nAlice,30,New York\nBob,25,London");

		ChunkAssert.Succeeded(result);
		_ = result.Chunks.Should().NotBeEmpty();
		_ = result.Chunks.OfType<CsvDocumentChunk>().Should().ContainSingle();
		_ = result.Chunks.OfType<CsvRowChunk>().Should().HaveCount(2);
	}

	[Fact]
	public async Task ChunkAsync_WithEmptyCsv_ShouldReturnEmpty()
	{
		var result = await ChunkAsync(string.Empty);

		ChunkAssert.Succeeded(result);
		_ = result.Chunks.Should().BeEmpty();
	}

	[Theory]
	[InlineData("A,B,C\n1,2,3", ',')]
	[InlineData("A\tB\tC\n1\t2\t3", '\t')]
	public async Task ChunkAsync_ShouldDetectDelimiter(string content, char delimiter)
	{
		var result = await ChunkAsync(content);

		var doc = result.Chunks.OfType<CsvDocumentChunk>().FirstOrDefault();
		_ = doc.Should().NotBeNull();
		_ = doc!.Delimiter.Should().Be(delimiter);
	}

	[Fact]
	public async Task ChunkAsync_ShouldDetectHeaderRow()
	{
		var result = await ChunkAsync("Name,Age,City\nAlice,30,New York");

		var doc = result.Chunks.OfType<CsvDocumentChunk>().FirstOrDefault();
		_ = doc.Should().NotBeNull();
		_ = doc!.HasHeaderRow.Should().BeTrue();
		_ = doc.Headers.Should().Contain(["Name", "Age", "City"]);
	}

	[Fact]
	public async Task ChunkAsync_ShouldHandleQuotedFields()
	{
		var result = await ChunkAsync("Name,Description\n\"John Doe\",\"Hello, World\"");

		var row = result.Chunks.OfType<CsvRowChunk>().FirstOrDefault();
		_ = row.Should().NotBeNull();
		_ = row!.Fields.Should().Contain("John Doe");
		_ = row.Fields.Should().Contain("Hello, World");
		_ = row.HasQuotedFields.Should().BeTrue();
	}

	[Fact]
	public async Task ChunkAsync_ShouldGenerateStatistics()
	{
		var result = await ChunkAsync("A,B\n1,2\n3,4");

		ChunkAssert.HasStatistics(result);
	}

	[Fact]
	public async Task ChunkAsync_WithValidation_ShouldValidateChunks()
	{
		var result = await ChunkAsync("Name,Age\nAlice,30", new ChunkingOptions { ValidateChunks = true });

		ChunkAssert.IsValidated(result);
	}

	[Fact]
	public async Task ChunkAsync_ShouldCalculateQualityMetrics()
	{
		var result = await ChunkAsync("Name,Age\nAlice,30");

		var rows = result.Chunks.OfType<CsvRowChunk>();
		_ = rows.Select(chunk => chunk.QualityMetrics).Should().OnlyContain(metrics => metrics != null && metrics.TokenCount > 0);
	}

	[Fact]
	public async Task ChunkAsync_RowContent_ShouldIncludeHeaderContext()
	{
		var result = await ChunkAsync("Name,Age\nAlice,30");

		var row = result.Chunks.OfType<CsvRowChunk>().FirstOrDefault();
		_ = row.Should().NotBeNull();
		_ = row!.Content.Should().Contain("Name:");
		_ = row.Content.Should().Contain("Age:");
		_ = row.Content.Should().Contain("Alice");
	}

	private async Task<ChunkingResult> ChunkAsync(string content, ChunkingOptions? options = null)
	{
		using var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));
		return await _chunker.ChunkAsync(stream, options ?? new ChunkingOptions(), CancellationToken);
	}
}
