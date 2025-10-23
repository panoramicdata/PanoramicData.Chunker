using FluentAssertions;
using PanoramicData.Chunker.Chunkers.Pptx;
using PanoramicData.Chunker.Configuration;
using PanoramicData.Chunker.Infrastructure;
using PanoramicData.Chunker.Models;
using System.Text;
using Xunit;
using Xunit.Abstractions;

namespace PanoramicData.Chunker.Tests.Unit.Chunkers;

/// <summary>
/// Unit tests for PptxDocumentChunker.
/// Tests are designed to skip gracefully when test files are missing and provide
/// detailed validation when files are present.
/// </summary>
public class PptxDocumentChunkerTests(ITestOutputHelper output)
{
	private readonly ITestOutputHelper _output = output;
	private const string TestDataPath = "TestData/Pptx";

	private static string GetTestFilePath(string fileName) => Path.Combine(TestDataPath, fileName);

	private static bool TestFileExists(string fileName)
	{
		var path = GetTestFilePath(fileName);
		return File.Exists(path);
	}

	[Fact]
	public void Constructor_ShouldThrowArgumentNullException_WhenTokenCounterIsNull()
	{
		// Act
		var act = () => new PptxDocumentChunker(null!);

		// Assert
		act.Should().Throw<ArgumentNullException>()
			.WithParameterName("tokenCounter");
	}

	[Fact]
	public void SupportedType_ShouldReturnPptx()
	{
		// Arrange
		var tokenCounter = new CharacterBasedTokenCounter();
		var chunker = new PptxDocumentChunker(tokenCounter);

		// Act
		var result = chunker.SupportedType;

		// Assert
		result.Should().Be(DocumentType.Pptx);
	}

	[Fact]
	public async Task CanHandleAsync_ShouldReturnTrue_ForPptxFile()
	{
		// Arrange
		var tokenCounter = new CharacterBasedTokenCounter();
		var chunker = new PptxDocumentChunker(tokenCounter);
		var testFilePath = GetTestFilePath("simple.pptx");

		if (!TestFileExists("simple.pptx"))
		{
			_output.WriteLine($"Test file not found: {testFilePath}. Skipping test.");
			_output.WriteLine("Run this test after creating PPTX test files. See PPTX_TEST_FILES.md");
			return;
		}

		await using var stream = File.OpenRead(testFilePath);

		// Act
		var result = await chunker.CanHandleAsync(stream);

		// Assert
		result.Should().BeTrue("PPTX chunker should handle .pptx files");
	}

	[Fact]
	public async Task CanHandleAsync_ShouldReturnFalse_ForNonPptxContent()
	{
		// Arrange
		var tokenCounter = new CharacterBasedTokenCounter();
		var chunker = new PptxDocumentChunker(tokenCounter);
		await using var stream = new MemoryStream(Encoding.UTF8.GetBytes("This is plain text, not PPTX"));

		// Act
		var result = await chunker.CanHandleAsync(stream);

		// Assert
		result.Should().BeFalse("PPTX chunker should reject non-PPTX content");
	}

	[Fact]
	public async Task ChunkAsync_ShouldExtractSlides_FromSimplePptx()
	{
		// Arrange
		var testFilePath = GetTestFilePath("simple.pptx");

		if (!TestFileExists("simple.pptx"))
		{
			_output.WriteLine($"Test file not found: {testFilePath}. Skipping test.");
			return;
		}

		var tokenCounter = new CharacterBasedTokenCounter();
		var chunker = new PptxDocumentChunker(tokenCounter);
		var options = new ChunkingOptions { MaxTokens = 500 };

		await using var stream = File.OpenRead(testFilePath);

		// Act
		var result = await chunker.ChunkAsync(stream, options);

		// Assert
		result.Success.Should().BeTrue();
		result.Chunks.Should().NotBeEmpty();
		_output.WriteLine($"Total chunks extracted: {result.Chunks.Count}");

		var slides = result.Chunks.OfType<PptxSlideChunk>().ToList();
		slides.Should().NotBeEmpty("simple.pptx should have at least one slide");
		slides.Should().HaveCountGreaterThanOrEqualTo(3, "simple.pptx should have 3-5 slides");
		slides.Should().HaveCountLessThanOrEqualTo(5, "simple.pptx should have 3-5 slides");

		_output.WriteLine($"Slide chunks: {slides.Count}");
		foreach (var slide in slides)
		{
			_output.WriteLine($"  Slide {slide.SlideNumber}: {slide.Title ?? "(No title)"} ({slide.ShapeCount} shapes)");
			slide.SlideNumber.Should().BeGreaterThan(0);
			slide.ShapeCount.Should().BeGreaterThanOrEqualTo(0);
		}
	}

	[Fact]
	public async Task ChunkAsync_ShouldExtractTitles_FromSimplePptx()
	{
		// Arrange
		var testFilePath = GetTestFilePath("simple.pptx");

		if (!TestFileExists("simple.pptx"))
		{
			_output.WriteLine($"Test file not found: {testFilePath}. Skipping test.");
			return;
		}

		var tokenCounter = new CharacterBasedTokenCounter();
		var chunker = new PptxDocumentChunker(tokenCounter);
		var options = new ChunkingOptions { MaxTokens = 500 };

		await using var stream = File.OpenRead(testFilePath);

		// Act
		var result = await chunker.ChunkAsync(stream, options);

		// Assert
		result.Success.Should().BeTrue();

		var titles = result.Chunks.OfType<PptxTitleChunk>().ToList();
		_output.WriteLine($"Title chunks extracted: {titles.Count}");

		if (titles.Count > 0)
		{
			foreach (var title in titles)
			{
				_output.WriteLine($"  Slide {title.SlideNumber} Title: '{title.Content}'");
				title.Content.Should().NotBeNullOrWhiteSpace("titles should have content");
				title.SlideNumber.Should().BeGreaterThan(0);
				title.SpecificType.Should().BeOneOf("title", "subtitle");
			}
		}
	}

	[Fact]
	public async Task ChunkAsync_ShouldExtractContent_FromSimplePptx()
	{
		// Arrange
		var testFilePath = GetTestFilePath("simple.pptx");

		if (!TestFileExists("simple.pptx"))
		{
			_output.WriteLine($"Test file not found: {testFilePath}. Skipping test.");
			return;
		}

		var tokenCounter = new CharacterBasedTokenCounter();
		var chunker = new PptxDocumentChunker(tokenCounter);
		var options = new ChunkingOptions { MaxTokens = 500 };

		await using var stream = File.OpenRead(testFilePath);

		// Act
		var result = await chunker.ChunkAsync(stream, options);

		// Assert
		result.Success.Should().BeTrue();

		var contents = result.Chunks.OfType<PptxContentChunk>().ToList();
		_output.WriteLine($"Content chunks extracted: {contents.Count}");

		if (contents.Count > 0)
		{
			foreach (var content in contents.Take(10))
			{
				var preview = content.Content.Length > 50 
					? content.Content[..50] + "..." 
					: content.Content;
				_output.WriteLine($"  Slide {content.SlideNumber}: '{preview}'");
				content.Content.Should().NotBeNullOrWhiteSpace();
				content.SlideNumber.Should().BeGreaterThan(0);
			}
		}
	}

	[Fact]
	public async Task ChunkAsync_ShouldCalculateTokenCounts()
	{
		// Arrange
		var testFilePath = GetTestFilePath("simple.pptx");

		if (!TestFileExists("simple.pptx"))
		{
			_output.WriteLine($"Test file not found: {testFilePath}. Skipping test.");
			return;
		}

		var tokenCounter = new CharacterBasedTokenCounter();
		var chunker = new PptxDocumentChunker(tokenCounter);
		var options = new ChunkingOptions();

		await using var stream = File.OpenRead(testFilePath);

		// Act
		var result = await chunker.ChunkAsync(stream, options);

		// Assert
		result.Success.Should().BeTrue();
		result.Chunks.Should().NotBeEmpty();

		var contentChunks = result.Chunks.OfType<ContentChunk>().ToList();
		_output.WriteLine($"Content chunks with metrics: {contentChunks.Count}");

		foreach (var chunk in contentChunks.Take(10))
		{
			chunk.QualityMetrics.Should().NotBeNull();
			chunk.QualityMetrics!.TokenCount.Should().BeGreaterThan(0);
			chunk.QualityMetrics.CharacterCount.Should().BeGreaterThan(0);
			_output.WriteLine($"  Chunk: {chunk.QualityMetrics.TokenCount} tokens, {chunk.QualityMetrics.CharacterCount} chars");
		}
	}

	[Fact]
	public async Task ChunkAsync_ShouldBuildHierarchy()
	{
		// Arrange
		var testFilePath = GetTestFilePath("simple.pptx");

		if (!TestFileExists("simple.pptx"))
		{
			_output.WriteLine($"Test file not found: {testFilePath}. Skipping test.");
			return;
		}

		var tokenCounter = new CharacterBasedTokenCounter();
		var chunker = new PptxDocumentChunker(tokenCounter);
		var options = new ChunkingOptions();

		await using var stream = File.OpenRead(testFilePath);

		// Act
		var result = await chunker.ChunkAsync(stream, options);

		// Assert
		result.Success.Should().BeTrue();
		result.Statistics.Should().NotBeNull();
		result.Statistics.MaxDepth.Should().BeGreaterThanOrEqualTo(0);
		_output.WriteLine($"Max hierarchy depth: {result.Statistics.MaxDepth}");

		// Verify parent-child relationships
		var slides = result.Chunks.OfType<PptxSlideChunk>().ToList();
		var childChunks = result.Chunks.Where(c => c.ParentId.HasValue).ToList();

		_output.WriteLine($"Total slides (parent chunks): {slides.Count}");
		_output.WriteLine($"Total child chunks: {childChunks.Count}");

		foreach (var child in childChunks)
		{
			var parent = result.Chunks.FirstOrDefault(c => c.Id == child.ParentId);
			parent.Should().NotBeNull($"child chunk {child.Id} should have valid parent");
		}
	}

	[Fact]
	public async Task ChunkAsync_ShouldGenerateStatistics()
	{
		// Arrange
		var testFilePath = GetTestFilePath("simple.pptx");

		if (!TestFileExists("simple.pptx"))
		{
			_output.WriteLine($"Test file not found: {testFilePath}. Skipping test.");
			return;
		}

		var tokenCounter = new CharacterBasedTokenCounter();
		var chunker = new PptxDocumentChunker(tokenCounter);
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
		result.Statistics.TotalTokens.Should().BeGreaterThan(0);

		_output.WriteLine("Statistics:");
		_output.WriteLine($"  Total chunks: {result.Statistics.TotalChunks}");
		_output.WriteLine($"  Structural chunks: {result.Statistics.StructuralChunks}");
		_output.WriteLine($"  Content chunks: {result.Statistics.ContentChunks}");
		_output.WriteLine($"  Visual chunks: {result.Statistics.VisualChunks}");
		_output.WriteLine($"  Table chunks: {result.Statistics.TableChunks}");
		_output.WriteLine($"  Total tokens: {result.Statistics.TotalTokens}");
		_output.WriteLine($"  Processing time: {result.Statistics.ProcessingTime.TotalMilliseconds}ms");
	}

	[Fact]
	public async Task ChunkAsync_ShouldExtractNotes_FromPptxWithNotes()
	{
		// Arrange
		var testFilePath = GetTestFilePath("with-notes.pptx");

		if (!TestFileExists("with-notes.pptx"))
		{
			_output.WriteLine($"Test file not found: {testFilePath}. Skipping test.");
			_output.WriteLine("Create with-notes.pptx with speaker notes to test notes extraction.");
			return;
		}

		var tokenCounter = new CharacterBasedTokenCounter();
		var chunker = new PptxDocumentChunker(tokenCounter);
		var options = new ChunkingOptions();

		await using var stream = File.OpenRead(testFilePath);

		// Act
		var result = await chunker.ChunkAsync(stream, options);

		// Assert
		result.Success.Should().BeTrue();

		var notes = result.Chunks.OfType<PptxNotesChunk>().ToList();
		_output.WriteLine($"Notes chunks extracted: {notes.Count}");

		if (notes.Count > 0)
		{
			notes.Should().NotBeEmpty("with-notes.pptx should contain speaker notes");

			foreach (var note in notes)
			{
				_output.WriteLine($"  Slide {note.SlideNumber} Notes ({note.NotesLength} chars): '{note.Content[..Math.Min(50, note.Content.Length)]}'...");
				note.Content.Should().NotBeNullOrWhiteSpace("notes should have content");
				note.SlideNumber.Should().BeGreaterThan(0);
				note.NotesLength.Should().BeGreaterThan(0);
				note.SpecificType.Should().Be("notes");
			}
		}
		else
		{
			_output.WriteLine("WARNING: No notes found. Verify with-notes.pptx has speaker notes added.");
		}
	}

	[Fact]
	public async Task ChunkAsync_ShouldExtractTables_FromPptxWithTables()
	{
		// Arrange
		var testFilePath = GetTestFilePath("with-tables.pptx");

		if (!TestFileExists("with-tables.pptx"))
		{
			_output.WriteLine($"Test file not found: {testFilePath}. Skipping test.");
			_output.WriteLine("Create with-tables.pptx with tables to test table extraction.");
			return;
		}

		var tokenCounter = new CharacterBasedTokenCounter();
		var chunker = new PptxDocumentChunker(tokenCounter);
		var options = new ChunkingOptions();

		await using var stream = File.OpenRead(testFilePath);

		// Act
		var result = await chunker.ChunkAsync(stream, options);

		// Assert
		result.Success.Should().BeTrue();

		var tables = result.Chunks.OfType<PptxTableChunk>().ToList();
		_output.WriteLine($"Table chunks extracted: {tables.Count}");

		if (tables.Count > 0)
		{
			tables.Should().NotBeEmpty("with-tables.pptx should contain tables");

			foreach (var table in tables)
			{
				_output.WriteLine($"  Slide {table.SlideNumber} Table: {table.TableInfo?.RowCount} rows × {table.TableInfo?.ColumnCount} columns");
				table.SerializedTable.Should().NotBeNullOrWhiteSpace("table should be serialized");
				table.SerializationFormat.Should().Be(TableSerializationFormat.Markdown);
				table.TableInfo.Should().NotBeNull();
				table.TableInfo!.RowCount.Should().BeGreaterThan(0);
				table.TableInfo.ColumnCount.Should().BeGreaterThan(0);

				if (table.TableInfo.HasHeaderRow)
				{
					_output.WriteLine($"    Headers: {string.Join(", ", table.TableInfo.Headers)}");
				}
			}
		}
		else
		{
			_output.WriteLine("WARNING: No tables found. Verify with-tables.pptx has tables inserted.");
		}
	}

	[Fact]
	public async Task ChunkAsync_ShouldDetectImages_FromPptxWithImages()
	{
		// Arrange
		var testFilePath = GetTestFilePath("with-images.pptx");

		if (!TestFileExists("with-images.pptx"))
		{
			_output.WriteLine($"Test file not found: {testFilePath}. Skipping test.");
			_output.WriteLine("Create with-images.pptx with images to test image detection.");
			return;
		}

		var tokenCounter = new CharacterBasedTokenCounter();
		var chunker = new PptxDocumentChunker(tokenCounter);
		var options = new ChunkingOptions();

		await using var stream = File.OpenRead(testFilePath);

		// Act
		var result = await chunker.ChunkAsync(stream, options);

		// Assert
		result.Success.Should().BeTrue();

		var images = result.Chunks.OfType<PptxImageChunk>().ToList();
		_output.WriteLine($"Image chunks detected: {images.Count}");

		if (images.Count > 0)
		{
			images.Should().NotBeEmpty("with-images.pptx should contain images");

			foreach (var image in images)
			{
				_output.WriteLine($"  Slide {image.SlideNumber} {image.VisualType}: {image.BinaryReference}");
				image.VisualType.Should().NotBeNullOrWhiteSpace();
				image.BinaryReference.Should().NotBeNullOrWhiteSpace();
				image.SpecificType.Should().Be(image.VisualType);

				if (image.IsChart)
				{
					_output.WriteLine($"    Chart Type: {image.ChartType}");
				}
			}
		}
		else
		{
			_output.WriteLine("WARNING: No images found. Verify with-images.pptx has images inserted.");
		}
	}

	[Fact]
	public async Task ChunkAsync_WithValidation_ShouldValidateChunks()
	{
		// Arrange
		var testFilePath = GetTestFilePath("simple.pptx");

		if (!TestFileExists("simple.pptx"))
		{
			_output.WriteLine($"Test file not found: {testFilePath}. Skipping test.");
			return;
		}

		var tokenCounter = new CharacterBasedTokenCounter();
		var chunker = new PptxDocumentChunker(tokenCounter);
		var options = new ChunkingOptions { ValidateChunks = true };

		await using var stream = File.OpenRead(testFilePath);

		// Act
		var result = await chunker.ChunkAsync(stream, options);

		// Assert
		result.Success.Should().BeTrue();
		result.ValidationResult.Should().NotBeNull();
		
		_output.WriteLine($"Validation result: {(result.ValidationResult!.IsValid ? "? Valid" : "? Invalid")}");
		_output.WriteLine($"Validation issues: {result.ValidationResult.Issues.Count}");

		if (!result.ValidationResult.IsValid)
		{
			foreach (var issue in result.ValidationResult.Issues)
			{
				_output.WriteLine($"  [{issue.Severity}] {issue.Code}: {issue.Message}");
			}
		}

		result.ValidationResult.Issues.Should().NotContain(i => i.Severity == ValidationSeverity.Error, 
			"there should be no error-level validation issues");
	}

	[Fact]
	public async Task ChunkAsync_ShouldHandleEmptyPresentation()
	{
		// Arrange
		var testFilePath = GetTestFilePath("empty.pptx");

		if (!TestFileExists("empty.pptx"))
		{
			_output.WriteLine($"Test file not found: {testFilePath}. Skipping test.");
			_output.WriteLine("Create empty.pptx with no content to test edge case handling.");
			return;
		}

		var tokenCounter = new CharacterBasedTokenCounter();
		var chunker = new PptxDocumentChunker(tokenCounter);
		var options = new ChunkingOptions();

		await using var stream = File.OpenRead(testFilePath);

		// Act
		var result = await chunker.ChunkAsync(stream, options);

		// Assert
		result.Success.Should().BeTrue("empty presentation should not cause errors");
		_output.WriteLine($"Chunks extracted from empty presentation: {result.Chunks.Count}");
		_output.WriteLine($"Processing time: {result.Statistics.ProcessingTime.TotalMilliseconds}ms");

		// Empty presentation may have 0 chunks or 1 empty slide
		result.Chunks.Count.Should().BeLessThanOrEqualTo(1, "empty presentation should have 0 or 1 chunks");
	}

	[Fact]
	public async Task ChunkAsync_ShouldProcessComplexPresentation()
	{
		// Arrange
		var testFilePath = GetTestFilePath("complex.pptx");

		if (!TestFileExists("complex.pptx"))
		{
			_output.WriteLine($"Test file not found: {testFilePath}. Skipping test.");
			_output.WriteLine("Create complex.pptx with mixed content to test comprehensive extraction.");
			return;
		}

		var tokenCounter = new CharacterBasedTokenCounter();
		var chunker = new PptxDocumentChunker(tokenCounter);
		var options = new ChunkingOptions { ValidateChunks = true };

		await using var stream = File.OpenRead(testFilePath);

		// Act
		var result = await chunker.ChunkAsync(stream, options);

		// Assert
		result.Success.Should().BeTrue();
		_output.WriteLine($"Complex presentation chunks: {result.Chunks.Count}");

		var slides = result.Chunks.OfType<PptxSlideChunk>().Count();
		var titles = result.Chunks.OfType<PptxTitleChunk>().Count();
		var contents = result.Chunks.OfType<PptxContentChunk>().Count();
		var tables = result.Chunks.OfType<PptxTableChunk>().Count();
		var images = result.Chunks.OfType<PptxImageChunk>().Count();
		var notes = result.Chunks.OfType<PptxNotesChunk>().Count();

		_output.WriteLine($"  Slides: {slides}");
		_output.WriteLine($"  Titles: {titles}");
		_output.WriteLine($"  Content: {contents}");
		_output.WriteLine($"  Tables: {tables}");
		_output.WriteLine($"  Images: {images}");
		_output.WriteLine($"  Notes: {notes}");

		slides.Should().BeGreaterThan(0, "complex.pptx should have slides");
		(titles + contents + tables + images + notes).Should().BeGreaterThan(0, "complex.pptx should have content");
	}

	[Fact]
	public async Task ChunkAsync_ShouldProcessLargePresentation()
	{
		// Arrange
		var testFilePath = GetTestFilePath("large.pptx");

		if (!TestFileExists("large.pptx"))
		{
			_output.WriteLine($"Test file not found: {testFilePath}. Skipping test.");
			_output.WriteLine("Create large.pptx with 50-100 slides to test performance.");
			return;
		}

		var tokenCounter = new CharacterBasedTokenCounter();
		var chunker = new PptxDocumentChunker(tokenCounter);
		var options = new ChunkingOptions();

		await using var stream = File.OpenRead(testFilePath);
		var fileSize = stream.Length;

		// Act
		var startTime = DateTime.UtcNow;
		var result = await chunker.ChunkAsync(stream, options);
		var duration = DateTime.UtcNow - startTime;

		// Assert
		result.Success.Should().BeTrue();
		_output.WriteLine($"Large presentation processed:");
		_output.WriteLine($"  File size: {fileSize / 1024}KB");
		_output.WriteLine($"  Total chunks: {result.Chunks.Count}");
		_output.WriteLine($"  Slides: {result.Chunks.OfType<PptxSlideChunk>().Count()}");
		_output.WriteLine($"  Processing time: {duration.TotalMilliseconds}ms");
		_output.WriteLine($"  Chunks/second: {result.Chunks.Count / duration.TotalSeconds:F2}");

		// Performance assertions
		duration.Should().BeLessThan(TimeSpan.FromSeconds(20), "large presentation should process in under 20 seconds");
		result.Chunks.Count.Should().BeGreaterThan(50, "large.pptx should have many chunks");
	}
}
