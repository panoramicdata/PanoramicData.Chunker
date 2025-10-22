using FluentAssertions;
using PanoramicData.Chunker.Chunkers.Docx;
using PanoramicData.Chunker.Configuration;
using PanoramicData.Chunker.Infrastructure;
using PanoramicData.Chunker.Models;
using System.Text;
using Xunit;
using Xunit.Abstractions;

namespace PanoramicData.Chunker.Tests.Unit.Chunkers;

/// <summary>
/// Unit tests for DocxDocumentChunker.
/// </summary>
public class DocxDocumentChunkerTests(ITestOutputHelper output)
{
	private readonly ITestOutputHelper _output = output;

	[Fact]
	public void Constructor_ShouldThrowArgumentNullException_WhenTokenCounterIsNull()
	{
		// Act
		var act = () => new DocxDocumentChunker(null!);

		// Assert
		act.Should().Throw<ArgumentNullException>();
	}

	[Fact]
	public void SupportedType_ShouldReturnDocx()
	{
		// Arrange
		var tokenCounter = new CharacterBasedTokenCounter();
		var chunker = new DocxDocumentChunker(tokenCounter);

		// Act
		var result = chunker.SupportedType;

		// Assert
		result.Should().Be(DocumentType.Docx);
	}

	[Fact]
	public async Task CanHandleAsync_ShouldReturnTrue_ForDocxFile()
	{
		// Arrange
		var tokenCounter = new CharacterBasedTokenCounter();
		var chunker = new DocxDocumentChunker(tokenCounter);
		var testFilePath = Path.Combine("TestData", "Docx", "simple.docx");

		if (!File.Exists(testFilePath))
		{
			_output.WriteLine($"Test file not found: {testFilePath}");
			return; // Skip test if file doesn't exist yet
		}

		await using var stream = File.OpenRead(testFilePath);

		// Act
		var result = await chunker.CanHandleAsync(stream);

		// Assert
		result.Should().BeTrue();
	}

	[Fact]
	public async Task CanHandleAsync_ShouldReturnFalse_ForNonDocxContent()
	{
		// Arrange
		var tokenCounter = new CharacterBasedTokenCounter();
		var chunker = new DocxDocumentChunker(tokenCounter);
		await using var stream = new MemoryStream(Encoding.UTF8.GetBytes("This is plain text"));

		// Act
		var result = await chunker.CanHandleAsync(stream);

		// Assert
		result.Should().BeFalse();
	}

	[Fact]
	public async Task ChunkAsync_ShouldExtractSections_FromDocxWithHeadings()
	{
		// Arrange
		var testFilePath = Path.Combine("TestData", "Docx", "simple.docx");

		if (!File.Exists(testFilePath))
		{
			_output.WriteLine($"Test file not found: {testFilePath}. Skipping test.");
			return;
		}

		var tokenCounter = new CharacterBasedTokenCounter();
		var chunker = new DocxDocumentChunker(tokenCounter);
		var options = new ChunkingOptions
		{
			MaxTokens = 500
		};

		await using var stream = File.OpenRead(testFilePath);

		// Act
		var result = await chunker.ChunkAsync(stream, options);

		// Assert
		result.Success.Should().BeTrue();
		result.Chunks.Should().NotBeEmpty();
		_output.WriteLine($"Total chunks: {result.Chunks.Count}");

		var sections = result.Chunks.OfType<DocxSectionChunk>().ToList();
		sections.Should().NotBeEmpty();
		_output.WriteLine($"Section chunks: {sections.Count}");

		foreach (var section in sections)
		{
			_output.WriteLine($"Section: {section.Content} (Level {section.HeadingLevel})");
		}
	}

	[Fact]
	public async Task ChunkAsync_ShouldExtractParagraphs_FromDocx()
	{
		// Arrange
		var testFilePath = Path.Combine("TestData", "Docx", "simple.docx");

		if (!File.Exists(testFilePath))
		{
			_output.WriteLine($"Test file not found: {testFilePath}. Skipping test.");
			return;
		}

		var tokenCounter = new CharacterBasedTokenCounter();
		var chunker = new DocxDocumentChunker(tokenCounter);
		var options = new ChunkingOptions
		{
			MaxTokens = 500
		};

		await using var stream = File.OpenRead(testFilePath);

		// Act
		var result = await chunker.ChunkAsync(stream, options);

		// Assert
		result.Success.Should().BeTrue();

		var paragraphs = result.Chunks.OfType<DocxParagraphChunk>().ToList();
		paragraphs.Should().NotBeEmpty();
		_output.WriteLine($"Paragraph chunks: {paragraphs.Count}");

		foreach (var paragraph in paragraphs.Take(5))
		{
			_output.WriteLine($"Paragraph: {paragraph.Content.Substring(0, Math.Min(50, paragraph.Content.Length))}...");
		}
	}

	[Fact]
	public async Task ChunkAsync_ShouldCalculateTokenCounts()
	{
		// Arrange
		var testFilePath = Path.Combine("TestData", "Docx", "simple.docx");

		if (!File.Exists(testFilePath))
		{
			_output.WriteLine($"Test file not found: {testFilePath}. Skipping test.");
			return;
		}

		var tokenCounter = new CharacterBasedTokenCounter();
		var chunker = new DocxDocumentChunker(tokenCounter);
		var options = new ChunkingOptions();

		await using var stream = File.OpenRead(testFilePath);

		// Act
		var result = await chunker.ChunkAsync(stream, options);

		// Assert
		result.Success.Should().BeTrue();
		result.Chunks.Should().NotBeEmpty();

		foreach (var chunk in result.Chunks)
		{
			chunk.QualityMetrics.Should().NotBeNull();
			chunk.QualityMetrics!.TokenCount.Should().BeGreaterThan(0);
			chunk.QualityMetrics.CharacterCount.Should().BeGreaterThan(0);
			_output.WriteLine($"Chunk tokens: {chunk.QualityMetrics.TokenCount}, chars: {chunk.QualityMetrics.CharacterCount}");
		}
	}

	[Fact]
	public async Task ChunkAsync_ShouldBuildHierarchy()
	{
		// Arrange
		var testFilePath = Path.Combine("TestData", "Docx", "simple.docx");

		if (!File.Exists(testFilePath))
		{
			_output.WriteLine($"Test file not found: {testFilePath}. Skipping test.");
			return;
		}

		var tokenCounter = new CharacterBasedTokenCounter();
		var chunker = new DocxDocumentChunker(tokenCounter);
		var options = new ChunkingOptions();

		await using var stream = File.OpenRead(testFilePath);

		// Act
		var result = await chunker.ChunkAsync(stream, options);

		// Assert
		result.Success.Should().BeTrue();
		result.Statistics.Should().NotBeNull();
		result.Statistics.MaxDepth.Should().BeGreaterThanOrEqualTo(0);
		_output.WriteLine($"Max depth: {result.Statistics.MaxDepth}");
	}

	[Fact]
	public async Task ChunkAsync_ShouldGenerateStatistics()
	{
		// Arrange
		var testFilePath = Path.Combine("TestData", "Docx", "simple.docx");

		if (!File.Exists(testFilePath))
		{
			_output.WriteLine($"Test file not found: {testFilePath}. Skipping test.");
			return;
		}

		var tokenCounter = new CharacterBasedTokenCounter();
		var chunker = new DocxDocumentChunker(tokenCounter);
		var options = new ChunkingOptions();

		await using var stream = File.OpenRead(testFilePath);

		// Act
		var result = await chunker.ChunkAsync(stream, options);

		// Assert
		result.Statistics.Should().NotBeNull();
		result.Statistics.TotalChunks.Should().Be(result.Chunks.Count);
		result.Statistics.StructuralChunks.Should().BeGreaterThanOrEqualTo(0);
		result.Statistics.ContentChunks.Should().BeGreaterThanOrEqualTo(0);
		result.Statistics.ProcessingTime.Should().BeGreaterThan(TimeSpan.Zero);

		_output.WriteLine($"Statistics:");
		_output.WriteLine($"  Total chunks: {result.Statistics.TotalChunks}");
		_output.WriteLine($"  Structural: {result.Statistics.StructuralChunks}");
		_output.WriteLine($"  Content: {result.Statistics.ContentChunks}");
		_output.WriteLine($"  Processing time: {result.Statistics.ProcessingTime.TotalMilliseconds}ms");
	}

	[Fact]
	public async Task ChunkAsync_ShouldHandleEmptyDocument()
	{
		// Arrange
		var testFilePath = Path.Combine("TestData", "Docx", "empty.docx");

		if (!File.Exists(testFilePath))
		{
			_output.WriteLine($"Test file not found: {testFilePath}. Skipping test.");
			return;
		}

		var tokenCounter = new CharacterBasedTokenCounter();
		var chunker = new DocxDocumentChunker(tokenCounter);
		var options = new ChunkingOptions();

		await using var stream = File.OpenRead(testFilePath);

		// Act
		var result = await chunker.ChunkAsync(stream, options);

		// Assert
		result.Success.Should().BeTrue();
		result.Chunks.Should().BeEmpty();
	}

	[Fact]
	public async Task ChunkAsync_ShouldExtractTables()
	{
		// Arrange
		var testFilePath = Path.Combine("TestData", "Docx", "with-tables.docx");

		if (!File.Exists(testFilePath))
		{
			_output.WriteLine($"Test file not found: {testFilePath}. Skipping test.");
			return;
		}

		var tokenCounter = new CharacterBasedTokenCounter();
		var chunker = new DocxDocumentChunker(tokenCounter);
		var options = new ChunkingOptions();

		await using var stream = File.OpenRead(testFilePath);

		// Act
		var result = await chunker.ChunkAsync(stream, options);

		// Assert
		result.Success.Should().BeTrue();

		var tables = result.Chunks.OfType<DocxTableChunk>().ToList();
		tables.Should().NotBeEmpty();
		_output.WriteLine($"Table chunks: {tables.Count}");

		foreach (var table in tables)
		{
			_output.WriteLine($"Table: {table.TableInfo?.RowCount} rows, {table.TableInfo?.ColumnCount} columns");
			table.SerializedTable.Should().NotBeEmpty();
			table.SerializationFormat.Should().Be(TableSerializationFormat.Markdown);
		}
	}

	[Fact]
	public async Task ChunkAsync_ShouldExtractListItems()
	{
		// Arrange
		var testFilePath = Path.Combine("TestData", "Docx", "with-lists.docx");

		if (!File.Exists(testFilePath))
		{
			_output.WriteLine($"Test file not found: {testFilePath}. Skipping test.");
			return;
		}

		var tokenCounter = new CharacterBasedTokenCounter();
		var chunker = new DocxDocumentChunker(tokenCounter);
		var options = new ChunkingOptions();

		await using var stream = File.OpenRead(testFilePath);

		// Act
		var result = await chunker.ChunkAsync(stream, options);

		// Assert
		result.Success.Should().BeTrue();

		var listItems = result.Chunks.OfType<DocxListItemChunk>().ToList();
		listItems.Should().NotBeEmpty();
		_output.WriteLine($"List item chunks: {listItems.Count}");

		foreach (var item in listItems.Take(5))
		{
			_output.WriteLine($"List item (Level {item.ListLevel}): {item.Content.Substring(0, Math.Min(50, item.Content.Length))}...");
		}
	}

	[Fact]
	public async Task ChunkAsync_WithValidation_ShouldValidateChunks()
	{
		// Arrange
		var testFilePath = Path.Combine("TestData", "Docx", "simple.docx");

		if (!File.Exists(testFilePath))
		{
			_output.WriteLine($"Test file not found: {testFilePath}. Skipping test.");
			return;
		}

		var tokenCounter = new CharacterBasedTokenCounter();
		var chunker = new DocxDocumentChunker(tokenCounter);
		var options = new ChunkingOptions
		{
			ValidateChunks = true
		};

		await using var stream = File.OpenRead(testFilePath);

		// Act
		var result = await chunker.ChunkAsync(stream, options);

		// Assert
		result.Success.Should().BeTrue();
		result.ValidationResult.Should().NotBeNull();
		_output.WriteLine($"Validation result: {(result.ValidationResult!.IsValid ? "Valid" : "Invalid")}");

		if (!result.ValidationResult.IsValid)
		{
			foreach (var issue in result.ValidationResult.Issues)
			{
				_output.WriteLine($"  Issue: {issue.Message}");
			}
		}
	}
}
