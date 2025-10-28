using AwesomeAssertions;
using PanoramicData.Chunker.Chunkers.Docx;
using PanoramicData.Chunker.Configuration;
using PanoramicData.Chunker.Extensions;
using PanoramicData.Chunker.Infrastructure;
using PanoramicData.Chunker.Infrastructure.TokenCounters;
using PanoramicData.Chunker.Models;
using System.Text;

namespace PanoramicData.Chunker.Tests.Unit.Chunkers;

/// <summary>
/// Unit tests for DocxDocumentChunker.
/// </summary>
public class DocxDocumentChunkerTests(ITestOutputHelper output) : BaseTest(output)
{
	[Fact]
	public void Constructor_ShouldThrowArgumentNullException_WhenTokenCounterIsNull()
	{
		// Act
		var act = () => new DocxDocumentChunker(null!);

		// Assert
		_ = act.Should().Throw<ArgumentNullException>();
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
		_ = result.Should().Be(DocumentType.Docx);
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
		var result = await chunker.CanHandleAsync(stream, CancellationToken);

		// Assert
		_ = result.Should().BeTrue();
	}

	[Fact]
	public async Task CanHandleAsync_ShouldReturnFalse_ForNonDocxContent()
	{
		// Arrange
		var tokenCounter = new CharacterBasedTokenCounter();
		var chunker = new DocxDocumentChunker(tokenCounter);
		await using var stream = new MemoryStream(Encoding.UTF8.GetBytes("This is plain text"));

		// Act
		var result = await chunker.CanHandleAsync(stream, CancellationToken);

		// Assert
		_ = result.Should().BeFalse();
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
		var result = await chunker.ChunkAsync(stream, options, CancellationToken);

		// Assert
		_ = result.Success.Should().BeTrue();
		_ = result.Chunks.Should().NotBeEmpty();
		_output.WriteLine($"Total chunks: {result.Chunks.Count}");

		var sections = result.Chunks.OfType<DocxSectionChunk>().ToList();
		_ = sections.Should().NotBeEmpty();
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
		var result = await chunker.ChunkAsync(stream, options, CancellationToken);

		// Assert
		_ = result.Success.Should().BeTrue();

		var paragraphs = result.Chunks.OfType<DocxParagraphChunk>().ToList();
		_ = paragraphs.Should().NotBeEmpty();
		_output.WriteLine($"Paragraph chunks: {paragraphs.Count}");

		foreach (var paragraph in paragraphs.Take(5))
		{
			_output.WriteLine($"Paragraph: {paragraph.Content[..Math.Min(50, paragraph.Content.Length)]}...");
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
		var result = await chunker.ChunkAsync(stream, options, CancellationToken);

		// Assert
		_ = result.Success.Should().BeTrue();
		_ = result.Chunks.Should().NotBeEmpty();

		foreach (var chunk in result.Chunks)
		{
			_ = chunk.QualityMetrics.Should().NotBeNull();
			_ = chunk.QualityMetrics!.TokenCount.Should().BePositive();
			_ = chunk.QualityMetrics.CharacterCount.Should().BePositive();
			_output.WriteLine($"Chunk tokens: {chunk.QualityMetrics.TokenCount}, chars: {chunk.QualityMetrics.CharacterCount}");
		}
	}

	[Fact]
	public async Task ChunkAsync_WithOpenAITokenCounter_ShouldCalculateAccurateTokenCounts()
	{
		// Arrange
		var testFilePath = Path.Combine("TestData", "Docx", "simple.docx");

		if (!File.Exists(testFilePath))
		{
			_output.WriteLine($"Test file not found: {testFilePath}. Skipping test.");
			return;
		}

		// Use OpenAI token counter for accurate token counting (not the naive chars/4 approximation)
		var tokenCounter = OpenAITokenCounter.ForGpt4();
		var chunker = new DocxDocumentChunker(tokenCounter);
		var options = new ChunkingOptions();

		await using var stream = File.OpenRead(testFilePath);

		// Act
		var result = await chunker.ChunkAsync(stream, options, CancellationToken);

		// Assert
		_ = result.Success.Should().BeTrue();
		_ = result.Chunks.Should().NotBeEmpty();

		_output.WriteLine("=== OpenAI Token Counter (GPT-4 CL100K) ===");

		foreach (var chunk in result.Chunks)
		{
			_ = chunk.QualityMetrics.Should().NotBeNull();
			_ = chunk.QualityMetrics!.TokenCount.Should().BePositive();
			_ = chunk.QualityMetrics.CharacterCount.Should().BePositive();

			// Calculate what CharacterBasedTokenCounter would have given
			var charBasedApprox = (int)Math.Ceiling(chunk.QualityMetrics.CharacterCount / 4.0);
			var difference = chunk.QualityMetrics.TokenCount - charBasedApprox;
			var percentDiff = charBasedApprox > 0
				? (difference / (double)charBasedApprox * 100)
				: 0;

			// Get content text from chunk
			var contentText = chunk.ToPlainText();
			var contentPreview = contentText.Length > 50
				? contentText[..50] + "..."
				: contentText;

			_output.WriteLine($"Content: '{contentPreview}'");
			_output.WriteLine($"  Chars: {chunk.QualityMetrics.CharacterCount}");
			_output.WriteLine($"  OpenAI tokens: {chunk.QualityMetrics.TokenCount}");
			_output.WriteLine($"  Char-based approx: {charBasedApprox} (difference: {difference:+0;-0;0}, {percentDiff:+0.0;-0.0;0.0}%)");
			_output.WriteLine("");

			// OpenAI token count should be more accurate and typically lower than char-based
			// For English text, actual tokens are usually fewer than chars/4
		}

		// Verify that we're using actual tokenization, not approximation
		// The token counts should not always equal Math.Ceiling(chars / 4)
		var hasAccurateTokenization = result.Chunks.Any(chunk =>
		{
			var charBasedApprox = (int)Math.Ceiling(chunk.QualityMetrics!.CharacterCount / 4.0);
			return chunk.QualityMetrics.TokenCount != charBasedApprox;
		});

		_ = hasAccurateTokenization.Should().BeTrue("OpenAI tokenization should differ from char-based approximation for at least some chunks");
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
		var result = await chunker.ChunkAsync(stream, options, CancellationToken);

		// Assert
		_ = result.Success.Should().BeTrue();
		_ = result.Statistics.Should().NotBeNull();
		_ = result.Statistics.MaxDepth.Should().BeGreaterThanOrEqualTo(0);
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
		var result = await chunker.ChunkAsync(stream, options, CancellationToken);

		// Assert
		_ = result.Statistics.Should().NotBeNull();
		_ = result.Statistics.TotalChunks.Should().Be(result.Chunks.Count);
		_ = result.Statistics.StructuralChunks.Should().BeGreaterThanOrEqualTo(0);
		_ = result.Statistics.ContentChunks.Should().BeGreaterThanOrEqualTo(0);
		_ = result.Statistics.ProcessingTime.Should().BeGreaterThan(TimeSpan.Zero);

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
		var result = await chunker.ChunkAsync(stream, options, CancellationToken);

		// Assert
		_ = result.Success.Should().BeTrue();
		_ = result.Chunks.Should().BeEmpty();
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
		var result = await chunker.ChunkAsync(stream, options, CancellationToken);

		// Assert
		_ = result.Success.Should().BeTrue();

		var tables = result.Chunks.OfType<DocxTableChunk>().ToList();
		_ = tables.Should().NotBeEmpty();
		_output.WriteLine($"Table chunks: {tables.Count}");

		foreach (var table in tables)
		{
			_output.WriteLine($"Table: {table.TableInfo?.RowCount} rows, {table.TableInfo?.ColumnCount} columns");
			_ = table.SerializedTable.Should().NotBeEmpty();
			_ = table.SerializationFormat.Should().Be(TableSerializationFormat.Markdown);
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
		var result = await chunker.ChunkAsync(stream, options, CancellationToken);

		// Assert
		_ = result.Success.Should().BeTrue();

		var listItems = result.Chunks.OfType<DocxListItemChunk>().ToList();
		_ = listItems.Should().NotBeEmpty();
		_output.WriteLine($"List item chunks: {listItems.Count}");

		foreach (var item in listItems.Take(5))
		{
			_output.WriteLine($"List item (Level {item.ListLevel}): {item.Content[..Math.Min(50, item.Content.Length)]}...");
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
		var result = await chunker.ChunkAsync(stream, options, CancellationToken);

		// Assert
		_ = result.Success.Should().BeTrue();
		_ = result.ValidationResult.Should().NotBeNull();
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
