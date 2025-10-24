using FluentAssertions;
using PanoramicData.Chunker.Chunkers.Xlsx;
using PanoramicData.Chunker.Configuration;
using PanoramicData.Chunker.Models;

namespace PanoramicData.Chunker.Tests.Integration;

/// <summary>
/// Integration tests for XLSX document chunking.
/// </summary>
public class XlsxIntegrationTests
{
	private readonly string _testDataPath;

	public XlsxIntegrationTests()
	{
		_testDataPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TestData", "Xlsx");
	}

	[Fact]
	public async Task ChunkAsync_SimpleSpreadsheet_ShouldExtractTable()
	{
		// Arrange
		var filePath = Path.Combine(_testDataPath, "simple.xlsx");
		await using var stream = File.OpenRead(filePath);
		var options = ChunkingPresets.ForOpenAIEmbeddings();

		// Act
		var result = await DocumentChunker.ChunkAsync(stream, DocumentType.Xlsx, options);

		// Assert
		result.Success.Should().BeTrue();
		result.Chunks.Should().NotBeEmpty();

		// Should have worksheet chunk
		var worksheets = result.Chunks.OfType<XlsxWorksheetChunk>().ToList();
		worksheets.Should().HaveCount(1);
		worksheets[0].SheetName.Should().Be("Sheet1");

		// Should have table chunk with headers
		var tables = result.Chunks.OfType<XlsxTableChunk>().ToList();
		tables.Should().HaveCount(1);
		
		var table = tables[0];
		table.TableInfo.Should().NotBeNull();
		table.TableInfo!.Headers.Should().Contain(["Name", "Age", "City"]);
		table.TableInfo.RowCount.Should().Be(3);  // 3 data rows
		table.TableInfo.ColumnCount.Should().Be(3);
	}

	[Fact]
	public async Task ChunkAsync_EmptySpreadsheet_ShouldReturnEmptyChunks()
	{
		// Arrange
		var filePath = Path.Combine(_testDataPath, "empty.xlsx");
		await using var stream = File.OpenRead(filePath);
		var options = ChunkingPresets.ForOpenAIEmbeddings();

		// Act
		var result = await DocumentChunker.ChunkAsync(stream, DocumentType.Xlsx, options);

		// Assert
		result.Success.Should().BeTrue();
		result.Chunks.Should().BeEmpty();
	}

	[Fact]
	public async Task ChunkAsync_MultipleWorksheets_ShouldExtractAllSheets()
	{
		// Arrange
		var filePath = Path.Combine(_testDataPath, "multiple-sheets.xlsx");
		await using var stream = File.OpenRead(filePath);
		var options = ChunkingPresets.ForOpenAIEmbeddings();

		// Act
		var result = await DocumentChunker.ChunkAsync(stream, DocumentType.Xlsx, options);

		// Assert
		result.Success.Should().BeTrue();

		// Should have 3 worksheet chunks
		var worksheets = result.Chunks.OfType<XlsxWorksheetChunk>().ToList();
		worksheets.Should().HaveCount(3);
		
		worksheets.Should().Contain(w => w.SheetName == "Sales");
		worksheets.Should().Contain(w => w.SheetName == "Expenses");
		worksheets.Should().Contain(w => w.SheetName == "Summary");

		// Each sheet should have table chunks
		var tables = result.Chunks.OfType<XlsxTableChunk>().ToList();
		tables.Should().HaveCountGreaterThan(0);
	}

	[Fact]
	public async Task ChunkAsync_WithFormulas_ShouldExtractFormulas()
	{
		// Arrange
		var filePath = Path.Combine(_testDataPath, "with-formulas.xlsx");
		await using var stream = File.OpenRead(filePath);
		var options = ChunkingPresets.ForOpenAIEmbeddings();

		// Act
		var result = await DocumentChunker.ChunkAsync(stream, DocumentType.Xlsx, options);

		// Assert
		result.Success.Should().BeTrue();

		// Should have formula chunks
		var formulas = result.Chunks.OfType<XlsxFormulaChunk>().ToList();
		formulas.Should().HaveCountGreaterThan(0);

		// Check for SUM formula
		var sumFormula = formulas.FirstOrDefault(f => f.FormulaType == "SUM");
		sumFormula.Should().NotBeNull();
		sumFormula!.Formula.Should().Contain("SUM");
	}

	[Fact]
	public async Task ChunkAsync_WithTable_ShouldRecognizeTableStructure()
	{
		// Arrange
		var filePath = Path.Combine(_testDataPath, "with-table.xlsx");
		await using var stream = File.OpenRead(filePath);
		var options = ChunkingPresets.ForOpenAIEmbeddings();

		// Act
		var result = await DocumentChunker.ChunkAsync(stream, DocumentType.Xlsx, options);

		// Assert
		result.Success.Should().BeTrue();

		var tables = result.Chunks.OfType<XlsxTableChunk>().ToList();
		tables.Should().HaveCount(1);

		var table = tables[0];
		table.TableInfo.Should().NotBeNull();
		table.TableInfo!.Headers.Should().Contain("Employee");
		table.TableInfo.Headers.Should().Contain("Department");
		table.TableInfo.Headers.Should().Contain("Salary");
		table.TableInfo.RowCount.Should().Be(5);  // 5 employee records
	}

	[Fact]
	public async Task ChunkAsync_LargeSpreadsheet_ShouldHandleEfficiently()
	{
		// Arrange
		var filePath = Path.Combine(_testDataPath, "large.xlsx");
		await using var stream = File.OpenRead(filePath);
		var options = ChunkingPresets.ForOpenAIEmbeddings();

		// Act
		var startTime = DateTime.UtcNow;
		var result = await DocumentChunker.ChunkAsync(stream, DocumentType.Xlsx, options);
		var processingTime = DateTime.UtcNow - startTime;

		// Assert
		result.Success.Should().BeTrue();
		result.Chunks.Should().NotBeEmpty();

		// Should process 1000 rows reasonably fast (< 5 seconds)
		processingTime.TotalSeconds.Should().BeLessThan(5);

		// Should have table chunk with all data
		var tables = result.Chunks.OfType<XlsxTableChunk>().ToList();
		tables.Should().HaveCount(1);
		
		var table = tables[0];
		table.TableInfo.Should().NotBeNull();
		table.TableInfo!.RowCount.Should().Be(1000);
	}

	[Fact]
	public async Task ChunkAsync_WithMergedCells_ShouldHandleCorrectly()
	{
		// Arrange
		var filePath = Path.Combine(_testDataPath, "with-merged-cells.xlsx");
		await using var stream = File.OpenRead(filePath);
		var options = ChunkingPresets.ForOpenAIEmbeddings();

		// Act
		var result = await DocumentChunker.ChunkAsync(stream, DocumentType.Xlsx, options);

		// Assert
		result.Success.Should().BeTrue();
		result.Chunks.Should().NotBeEmpty();

		// Should extract table with data
		var tables = result.Chunks.OfType<XlsxTableChunk>().ToList();
		tables.Should().HaveCount(1);
	}

	[Fact]
	public async Task ChunkAsync_WithDataTypes_ShouldPreserveValues()
	{
		// Arrange
		var filePath = Path.Combine(_testDataPath, "with-data-types.xlsx");
		await using var stream = File.OpenRead(filePath);
		var options = ChunkingPresets.ForOpenAIEmbeddings();

		// Act
		var result = await DocumentChunker.ChunkAsync(stream, DocumentType.Xlsx, options);

		// Assert
		result.Success.Should().BeTrue();

		var tables = result.Chunks.OfType<XlsxTableChunk>().ToList();
		tables.Should().HaveCount(1);

		var table = tables[0];
		table.TableInfo.Should().NotBeNull();
		table.TableInfo!.Headers.Should().Contain("Text");
		table.TableInfo.Headers.Should().Contain("Number");
		table.TableInfo.Headers.Should().Contain("Date");
	}

	[Fact]
	public async Task ChunkAsync_ShouldCalculateTokenCounts()
	{
		// Arrange
		var filePath = Path.Combine(_testDataPath, "simple.xlsx");
		await using var stream = File.OpenRead(filePath);
		var options = ChunkingPresets.ForOpenAIEmbeddings();

		// Act
		var result = await DocumentChunker.ChunkAsync(stream, DocumentType.Xlsx, options);

		// Assert
		result.Success.Should().BeTrue();

		// All chunks should have quality metrics
		result.Chunks.Should().AllSatisfy(chunk =>
		{
			chunk.QualityMetrics.Should().NotBeNull();
			if (chunk is not XlsxWorksheetChunk) // Worksheet chunks don't have direct content
			{
				chunk.QualityMetrics!.TokenCount.Should().BeGreaterThan(0);
			}
		});
	}

	[Fact]
	public async Task ChunkAsync_ShouldBuildHierarchy()
	{
		// Arrange
		var filePath = Path.Combine(_testDataPath, "simple.xlsx");
		await using var stream = File.OpenRead(filePath);
		var options = ChunkingPresets.ForOpenAIEmbeddings();

		// Act
		var result = await DocumentChunker.ChunkAsync(stream, DocumentType.Xlsx, options);

		// Assert
		result.Success.Should().BeTrue();

		// Tables should be children of worksheets
		var worksheet = result.Chunks.OfType<XlsxWorksheetChunk>().First();
		var table = result.Chunks.OfType<XlsxTableChunk>().First();
		
		table.ParentId.Should().Be(worksheet.Id);
		table.Depth.Should().BeGreaterThan(worksheet.Depth);
	}

	[Fact]
	public async Task ChunkAsync_ShouldGenerateStatistics()
	{
		// Arrange
		var filePath = Path.Combine(_testDataPath, "simple.xlsx");
		await using var stream = File.OpenRead(filePath);
		var options = ChunkingPresets.ForOpenAIEmbeddings();

		// Act
		var result = await DocumentChunker.ChunkAsync(stream, DocumentType.Xlsx, options);

		// Assert
		result.Statistics.Should().NotBeNull();
		result.Statistics.TotalChunks.Should().Be(result.Chunks.Count);
		result.Statistics.StructuralChunks.Should().BeGreaterThan(0);
		result.Statistics.ProcessingTime.Should().BeGreaterThan(TimeSpan.Zero);
		result.Statistics.TotalTokens.Should().BeGreaterThan(0);
	}

	[Fact]
	public async Task ChunkAsync_WithAutoDetect_ShouldDetectXlsx()
	{
		// Arrange
		var filePath = Path.Combine(_testDataPath, "simple.xlsx");
		await using var stream = File.OpenRead(filePath);
		var options = ChunkingPresets.ForOpenAIEmbeddings();

		// Act - Use auto-detect
		var result = await DocumentChunker.ChunkAutoDetectAsync(stream, "test.xlsx", options);

		// Assert
		result.Success.Should().BeTrue();
		result.Chunks.Should().NotBeEmpty();
	}

	[Fact]
	public async Task ChunkAsync_SerializedTable_ShouldBeMarkdownFormat()
	{
		// Arrange
		var filePath = Path.Combine(_testDataPath, "simple.xlsx");
		await using var stream = File.OpenRead(filePath);
		var options = ChunkingPresets.ForOpenAIEmbeddings();

		// Act
		var result = await DocumentChunker.ChunkAsync(stream, DocumentType.Xlsx, options);

		// Assert
		var table = result.Chunks.OfType<XlsxTableChunk>().First();
		table.SerializationFormat.Should().Be(TableSerializationFormat.Markdown);
		table.SerializedTable.Should().Contain("|");
		table.SerializedTable.Should().Contain("---");
	}

	[Fact]
	public async Task ChunkAsync_MetadataSheetName_ShouldBeSet()
	{
		// Arrange
		var filePath = Path.Combine(_testDataPath, "simple.xlsx");
		await using var stream = File.OpenRead(filePath);
		var options = ChunkingPresets.ForOpenAIEmbeddings();

		// Act
		var result = await DocumentChunker.ChunkAsync(stream, DocumentType.Xlsx, options);

		// Assert
		result.Chunks.Should().AllSatisfy(chunk =>
		{
			chunk.Metadata.Should().NotBeNull();
			chunk.Metadata!.DocumentType.Should().Be("XLSX");
			if (chunk is not XlsxWorksheetChunk worksheet || !string.IsNullOrEmpty(worksheet.SheetName))
			{
				chunk.Metadata.SheetName.Should().NotBeNullOrEmpty();
			}
		});
	}

	[Fact]
	public async Task ChunkAsync_WithValidation_ShouldValidateChunks()
	{
		// Arrange
		var filePath = Path.Combine(_testDataPath, "simple.xlsx");
		await using var stream = File.OpenRead(filePath);
		var options = new ChunkingOptions
		{
			ValidateChunks = true
		};

		// Act
		var result = await DocumentChunker.ChunkAsync(stream, DocumentType.Xlsx, options);

		// Assert
		result.ValidationResult.Should().NotBeNull();
		result.ValidationResult!.IsValid.Should().BeTrue();
	}
}
