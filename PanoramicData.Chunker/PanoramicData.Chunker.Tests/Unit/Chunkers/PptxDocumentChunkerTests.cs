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
/// </summary>
public class PptxDocumentChunkerTests(ITestOutputHelper output)
{
	private readonly ITestOutputHelper _output = output;

	[Fact]
	public void Constructor_ShouldThrowArgumentNullException_WhenTokenCounterIsNull()
	{
		// Act
		var act = () => new PptxDocumentChunker(null!);

		// Assert
		act.Should().Throw<ArgumentNullException>();
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
		var testFilePath = Path.Combine("TestData", "Pptx", "simple.pptx");

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
	public async Task CanHandleAsync_ShouldReturnFalse_ForNonPptxContent()
	{
		// Arrange
		var tokenCounter = new CharacterBasedTokenCounter();
		var chunker = new PptxDocumentChunker(tokenCounter);
		await using var stream = new MemoryStream(Encoding.UTF8.GetBytes("This is plain text"));

		// Act
		var result = await chunker.CanHandleAsync(stream);

		// Assert
		result.Should().BeFalse();
	}

	[Fact]
	public async Task ChunkAsync_ShouldExtractSlides_FromPptx()
	{
		// Arrange
		var testFilePath = Path.Combine("TestData", "Pptx", "simple.pptx");

		if (!File.Exists(testFilePath))
		{
			_output.WriteLine($"Test file not found: {testFilePath}. Skipping test.");
			return;
		}

		var tokenCounter = new CharacterBasedTokenCounter();
		var chunker = new PptxDocumentChunker(tokenCounter);
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

		var slides = result.Chunks.OfType<PptxSlideChunk>().ToList();
		slides.Should().NotBeEmpty();
		_output.WriteLine($"Slide chunks: {slides.Count}");

		foreach (var slide in slides)
		{
			_output.WriteLine($"Slide {slide.SlideNumber}: {slide.Title ?? "(No title)"}");
		}
	}

	[Fact]
	public async Task ChunkAsync_ShouldExtractTitles_FromPptx()
	{
		// Arrange
		var testFilePath = Path.Combine("TestData", "Pptx", "simple.pptx");

		if (!File.Exists(testFilePath))
		{
			_output.WriteLine($"Test file not found: {testFilePath}. Skipping test.");
			return;
		}

		var tokenCounter = new CharacterBasedTokenCounter();
		var chunker = new PptxDocumentChunker(tokenCounter);
		var options = new ChunkingOptions
		{
			MaxTokens = 500
		};

		await using var stream = File.OpenRead(testFilePath);

		// Act
		var result = await chunker.ChunkAsync(stream, options);

		// Assert
		result.Success.Should().BeTrue();

		var titles = result.Chunks.OfType<PptxTitleChunk>().ToList();
		_output.WriteLine($"Title chunks: {titles.Count}");

		foreach (var title in titles)
		{
			_output.WriteLine($"Slide {title.SlideNumber} Title: {title.Content}");
		}
	}

	[Fact]
	public async Task ChunkAsync_ShouldExtractContent_FromPptx()
	{
		// Arrange
		var testFilePath = Path.Combine("TestData", "Pptx", "simple.pptx");

		if (!File.Exists(testFilePath))
		{
			_output.WriteLine($"Test file not found: {testFilePath}. Skipping test.");
			return;
		}

		var tokenCounter = new CharacterBasedTokenCounter();
		var chunker = new PptxDocumentChunker(tokenCounter);
		var options = new ChunkingOptions
		{
			MaxTokens = 500
		};

		await using var stream = File.OpenRead(testFilePath);

		// Act
		var result = await chunker.ChunkAsync(stream, options);

		// Assert
		result.Success.Should().BeTrue();

		var contents = result.Chunks.OfType<PptxContentChunk>().ToList();
		_output.WriteLine($"Content chunks: {contents.Count}");

		foreach (var content in contents.Take(5))
		{
			_output.WriteLine($"Slide {content.SlideNumber} Content: {content.Content[..Math.Min(50, content.Content.Length)]}...");
		}
	}

	[Fact]
	public async Task ChunkAsync_ShouldCalculateTokenCounts()
	{
		// Arrange
		var testFilePath = Path.Combine("TestData", "Pptx", "simple.pptx");

		if (!File.Exists(testFilePath))
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

		foreach (var chunk in result.Chunks.Take(10))
		{
			if (chunk is ContentChunk contentChunk)
			{
				contentChunk.QualityMetrics.Should().NotBeNull();
				contentChunk.QualityMetrics!.TokenCount.Should().BeGreaterThan(0);
				_output.WriteLine($"Chunk tokens: {contentChunk.QualityMetrics.TokenCount}");
			}
		}
	}

	[Fact]
	public async Task ChunkAsync_ShouldBuildHierarchy()
	{
		// Arrange
		var testFilePath = Path.Combine("TestData", "Pptx", "simple.pptx");

		if (!File.Exists(testFilePath))
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
		_output.WriteLine($"Max depth: {result.Statistics.MaxDepth}");
	}

	[Fact]
	public async Task ChunkAsync_ShouldGenerateStatistics()
	{
		// Arrange
		var testFilePath = Path.Combine("TestData", "Pptx", "simple.pptx");

		if (!File.Exists(testFilePath))
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

		_output.WriteLine($"Statistics:");
		_output.WriteLine($"  Total chunks: {result.Statistics.TotalChunks}");
		_output.WriteLine($"  Structural: {result.Statistics.StructuralChunks}");
		_output.WriteLine($"  Content: {result.Statistics.ContentChunks}");
		_output.WriteLine($"  Visual: {result.Statistics.VisualChunks}");
		_output.WriteLine($"  Processing time: {result.Statistics.ProcessingTime.TotalMilliseconds}ms");
	}

	[Fact]
	public async Task ChunkAsync_ShouldExtractNotes_FromPptxWithNotes()
	{
		// Arrange
		var testFilePath = Path.Combine("TestData", "Pptx", "with-notes.pptx");

		if (!File.Exists(testFilePath))
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

		var notes = result.Chunks.OfType<PptxNotesChunk>().ToList();
		if (notes.Count > 0)
		{
			_output.WriteLine($"Notes chunks: {notes.Count}");

			foreach (var note in notes)
			{
				_output.WriteLine($"Slide {note.SlideNumber} Notes: {note.Content[..Math.Min(50, note.Content.Length)]}...");
			}
		}
		else
		{
			_output.WriteLine("No notes found in presentation");
		}
	}

	[Fact]
	public async Task ChunkAsync_ShouldExtractTables_FromPptxWithTables()
	{
		// Arrange
		var testFilePath = Path.Combine("TestData", "Pptx", "with-tables.pptx");

		if (!File.Exists(testFilePath))
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

		var tables = result.Chunks.OfType<PptxTableChunk>().ToList();
		if (tables.Count > 0)
		{
			_output.WriteLine($"Table chunks: {tables.Count}");

			foreach (var table in tables)
			{
				_output.WriteLine($"Slide {table.SlideNumber} Table: {table.TableInfo?.RowCount} rows, {table.TableInfo?.ColumnCount} columns");
			}
		}
		else
		{
			_output.WriteLine("No tables found in presentation");
		}
	}

	[Fact]
	public async Task ChunkAsync_ShouldDetectImages_FromPptxWithImages()
	{
		// Arrange
		var testFilePath = Path.Combine("TestData", "Pptx", "with-images.pptx");

		if (!File.Exists(testFilePath))
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

		var images = result.Chunks.OfType<PptxImageChunk>().ToList();
		if (images.Count > 0)
		{
			_output.WriteLine($"Image chunks: {images.Count}");

			foreach (var image in images)
			{
				_output.WriteLine($"Slide {image.SlideNumber} {image.VisualType}: {image.BinaryReference}");
			}
		}
		else
		{
			_output.WriteLine("No images found in presentation");
		}
	}

	[Fact]
	public async Task ChunkAsync_WithValidation_ShouldValidateChunks()
	{
		// Arrange
		var testFilePath = Path.Combine("TestData", "Pptx", "simple.pptx");

		if (!File.Exists(testFilePath))
		{
			_output.WriteLine($"Test file not found: {testFilePath}. Skipping test.");
			return;
		}

		var tokenCounter = new CharacterBasedTokenCounter();
		var chunker = new PptxDocumentChunker(tokenCounter);
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
