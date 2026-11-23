using AwesomeAssertions;
using PanoramicData.Chunker.Chunkers.Xlsx;
using PanoramicData.Chunker.Configuration;
using PanoramicData.Chunker.Models;
using PanoramicData.Chunker.Tests.Utilities;

namespace PanoramicData.Chunker.Tests.Integration;

/// <summary>
/// Integration tests for XLSX document chunking.
/// </summary>
public class XlsxIntegrationTests(ITestOutputHelper output) : BaseTest(output)
{
	private readonly string _testDataPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TestData", "Xlsx");

	[Fact]
	public async Task ChunkAsync_SimpleSpreadsheet_ShouldExtractTable()
	{
		var result = await ChunkTestFileAsync("simple.xlsx");

		ChunkAssert.SucceededWithChunks(result);
		_ = SingleOf<XlsxWorksheetChunk>(result).SheetName.Should().Be("Sheet1");

		var table = SingleOf<XlsxTableChunk>(result);
		_ = table.TableInfo.Should().NotBeNull();
		_ = table.TableInfo!.Headers.Should().Contain(["Name", "Age", "City"]);
		_ = table.TableInfo.RowCount.Should().Be(3);  // 3 data rows
		_ = table.TableInfo.ColumnCount.Should().Be(3);
	}

	[Fact]
	public async Task ChunkAsync_EmptySpreadsheet_ShouldReturnEmptyChunks()
	{
		var result = await ChunkTestFileAsync("empty.xlsx");

		_ = result.Success.Should().BeTrue();
		_ = result.Chunks.Should().BeEmpty();
	}

	[Fact]
	public async Task ChunkAsync_MultipleWorksheets_ShouldExtractAllSheets()
	{
		var result = await ChunkTestFileAsync("multiple-sheets.xlsx");

		_ = result.Success.Should().BeTrue();

		var worksheets = result.Chunks.OfType<XlsxWorksheetChunk>().ToList();
		_ = worksheets.Should().HaveCount(3);
		_ = worksheets.Select(w => w.SheetName).Should().Contain(["Sales", "Expenses", "Summary"]);
		_ = result.Chunks.OfType<XlsxTableChunk>().Should().HaveCountGreaterThan(0);
	}

	[Fact]
	public async Task ChunkAsync_WithFormulas_ShouldExtractFormulas()
	{
		var formulas = await SuccessfulChunksAsync<XlsxFormulaChunk>(ChunkTestFileAsync("with-formulas.xlsx"));

		_ = formulas.Should().HaveCountGreaterThan(0);

		var sumFormula = formulas.FirstOrDefault(f => f.FormulaType == "SUM");
		_ = sumFormula.Should().NotBeNull();
		_ = sumFormula!.Formula.Should().Contain("SUM");
	}

	[Fact]
	public async Task ChunkAsync_WithTable_ShouldRecognizeTableStructure()
	{
		var table = await SingleTableAsync("with-table.xlsx");

		_ = table.TableInfo!.Headers.Should().Contain(["Employee", "Department", "Salary"]);
		_ = table.TableInfo.RowCount.Should().Be(5);  // 5 employee records
	}

	[Fact]
	public async Task ChunkAsync_LargeSpreadsheet_ShouldHandleEfficiently()
	{
		var (result, processingTime) = await ChunkTimedAsync(Path.Combine(_testDataPath, "large.xlsx"), DocumentType.Xlsx);

		ChunkAssert.SucceededWithChunks(result);
		_ = processingTime.TotalSeconds.Should().BeLessThan(5);

		var table = SingleOf<XlsxTableChunk>(result);
		_ = table.TableInfo.Should().NotBeNull();
		_ = table.TableInfo!.RowCount.Should().Be(1000);
	}

	[Fact]
	public async Task ChunkAsync_WithMergedCells_ShouldHandleCorrectly()
	{
		_ = await SingleTableAsync("with-merged-cells.xlsx");
	}

	[Fact]
	public async Task ChunkAsync_WithDataTypes_ShouldPreserveValues()
	{
		var table = await SingleTableAsync("with-data-types.xlsx");

		_ = table.TableInfo!.Headers.Should().Contain(["Text", "Number", "Date"]);
	}

	[Fact]
	public async Task ChunkAsync_ShouldCalculateTokenCounts()
	{
		var result = await ChunkTestFileAsync("simple.xlsx");

		_ = result.Success.Should().BeTrue();
		_ = result.Chunks.Should().AllSatisfy(chunk =>
		{
			_ = chunk.QualityMetrics.Should().NotBeNull();
			if (chunk is not XlsxWorksheetChunk) // Worksheet chunks don't have direct content
			{
				_ = chunk.QualityMetrics!.TokenCount.Should().BePositive();
			}
		});
	}

	[Fact]
	public async Task ChunkAsync_ShouldBuildHierarchy()
	{
		var result = await ChunkTestFileAsync("simple.xlsx");

		_ = result.Success.Should().BeTrue();

		var worksheet = result.Chunks.OfType<XlsxWorksheetChunk>().First();
		var table = result.Chunks.OfType<XlsxTableChunk>().First();

		_ = table.ParentId.Should().Be(worksheet.Id);
		_ = table.Depth.Should().BeGreaterThan(worksheet.Depth);
	}

	[Fact]
	public async Task ChunkAsync_ShouldGenerateStatistics()
	{
		var result = await ChunkTestFileAsync("simple.xlsx");

		ChunkAssert.StatisticsMatch(result);
		_ = result.Statistics.TotalTokens.Should().BePositive();
	}

	[Fact]
	public async Task ChunkAsync_WithAutoDetect_ShouldDetectXlsx()
	{
		var result = await ChunkAutoDetectFileAsync(Path.Combine(_testDataPath, "simple.xlsx"), "test.xlsx");

		ChunkAssert.SucceededWithChunks(result);
	}

	[Fact]
	public async Task ChunkAsync_SerializedTable_ShouldBeMarkdownFormat()
	{
		var result = await ChunkTestFileAsync("simple.xlsx");

		var table = result.Chunks.OfType<XlsxTableChunk>().First();
		_ = table.SerializationFormat.Should().Be(TableSerializationFormat.Markdown);
		_ = table.SerializedTable.Should().Contain("|");
		_ = table.SerializedTable.Should().Contain("---");
	}

	[Fact]
	public async Task ChunkAsync_MetadataSheetName_ShouldBeSet()
	{
		var result = await ChunkTestFileAsync("simple.xlsx");

		_ = result.Chunks.Should().AllSatisfy(chunk =>
		{
			_ = chunk.Metadata.Should().NotBeNull();
			_ = chunk.Metadata!.DocumentType.Should().Be("XLSX");
			if (chunk is not XlsxWorksheetChunk worksheet || !string.IsNullOrEmpty(worksheet.SheetName))
			{
				_ = chunk.Metadata.SheetName.Should().NotBeNullOrEmpty();
			}
		});
	}

	[Fact]
	public async Task ChunkAsync_WithValidation_ShouldValidateChunks()
	{
		var result = await ChunkValidatedAsync(Path.Combine(_testDataPath, "simple.xlsx"), DocumentType.Xlsx);

		ChunkAssert.IsValidated(result);
	}

	private Task<ChunkingResult> ChunkTestFileAsync(string fileName)
		=> ChunkFileAsync(Path.Combine(_testDataPath, fileName), DocumentType.Xlsx);

	private async Task<XlsxTableChunk> SingleTableAsync(string fileName)
	{
		var result = await ChunkTestFileAsync(fileName);

		_ = result.Success.Should().BeTrue();

		var table = SingleOf<XlsxTableChunk>(result);
		_ = table.TableInfo.Should().NotBeNull();
		return table;
	}

	private static T SingleOf<T>(ChunkingResult result) where T : ChunkerBase
	{
		var matches = result.Chunks.OfType<T>().ToList();

		_ = matches.Should().ContainSingle();
		return matches[0];
	}
}