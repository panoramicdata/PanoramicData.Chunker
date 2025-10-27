using AwesomeAssertions;
using PanoramicData.Chunker.Chunkers.Docx;
using PanoramicData.Chunker.Configuration;
using PanoramicData.Chunker.Infrastructure;
using Xunit;
using Xunit.Abstractions;

namespace PanoramicData.Chunker.Tests.Integration;

/// <summary>
/// Integration tests for DOCX document chunking.
/// Tests end-to-end chunking of real DOCX files.
/// </summary>
public class DocxIntegrationTests(ITestOutputHelper output)
{
	private readonly ITestOutputHelper _output = output;

	[Fact]
	public async Task ChunkAsync_SimpleDocument_ShouldExtractAllElements()
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
		var options = ChunkingPresets.ForRAG();

		await using var stream = File.OpenRead(testFilePath);

		// Act
		var result = await chunker.ChunkAsync(stream, options);

		// Assert
		_ = result.Success.Should().BeTrue();
		_ = result.Chunks.Should().NotBeEmpty();
		_ = result.Statistics.Should().NotBeNull();

		_output.WriteLine($"Simple document results:");
		_output.WriteLine($"  Total chunks: {result.Statistics.TotalChunks}");
		_output.WriteLine($"  Structural: {result.Statistics.StructuralChunks}");
		_output.WriteLine($"  Content: {result.Statistics.ContentChunks}");
		_output.WriteLine($"  Max depth: {result.Statistics.MaxDepth}");
		_output.WriteLine($"  Processing time: {result.Statistics.ProcessingTime.TotalMilliseconds}ms");
	}

	[Fact]
	public async Task ChunkAsync_ComplexDocument_ShouldHandleMixedContent()
	{
		// Arrange
		var testFilePath = Path.Combine("TestData", "Docx", "complex.docx");

		if (!File.Exists(testFilePath))
		{
			_output.WriteLine($"Test file not found: {testFilePath}. Skipping test.");
			return;
		}

		var tokenCounter = new CharacterBasedTokenCounter();
		var chunker = new DocxDocumentChunker(tokenCounter);
		var options = ChunkingPresets.ForRAG();

		await using var stream = File.OpenRead(testFilePath);

		// Act
		var result = await chunker.ChunkAsync(stream, options);

		// Assert
		_ = result.Success.Should().BeTrue();
		_ = result.Chunks.Should().NotBeEmpty();

		var sections = result.Chunks.OfType<DocxSectionChunk>().ToList();
		var paragraphs = result.Chunks.OfType<DocxParagraphChunk>().ToList();
		var listItems = result.Chunks.OfType<DocxListItemChunk>().ToList();
		var tables = result.Chunks.OfType<DocxTableChunk>().ToList();

		_output.WriteLine($"Complex document results:");
		_output.WriteLine($"  Sections: {sections.Count}");
		_output.WriteLine($"  Paragraphs: {paragraphs.Count}");
		_output.WriteLine($"  List items: {listItems.Count}");
		_output.WriteLine($"  Tables: {tables.Count}");

		// Verify hierarchy
		_ = result.Statistics.MaxDepth.Should().BePositive();
	}

	[Fact]
	public async Task ChunkAsync_WithTables_ShouldExtractTableStructure()
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
		var options = ChunkingPresets.ForRAG();

		await using var stream = File.OpenRead(testFilePath);

		// Act
		var result = await chunker.ChunkAsync(stream, options);

		// Assert
		_ = result.Success.Should().BeTrue();

		var tables = result.Chunks.OfType<DocxTableChunk>().ToList();
		_ = tables.Should().NotBeEmpty();

		foreach (var table in tables)
		{
			_output.WriteLine($"Table: {table.TableInfo?.RowCount} rows × {table.TableInfo?.ColumnCount} columns");
			_output.WriteLine($"  Has header: {table.TableInfo?.HasHeaderRow}");
			_output.WriteLine($"  Serialization format: {table.SerializationFormat}");

			_ = table.SerializedTable.Should().NotBeEmpty();
			_ = table.TableInfo.Should().NotBeNull();
			_ = table.TableInfo!.RowCount.Should().BePositive();
			_ = table.TableInfo.ColumnCount.Should().BePositive();
		}
	}

	[Fact]
	public async Task ChunkAsync_WithLists_ShouldPreserveListStructure()
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
		var options = ChunkingPresets.ForRAG();

		await using var stream = File.OpenRead(testFilePath);

		// Act
		var result = await chunker.ChunkAsync(stream, options);

		// Assert
		_ = result.Success.Should().BeTrue();

		var listItems = result.Chunks.OfType<DocxListItemChunk>().ToList();
		_ = listItems.Should().NotBeEmpty();

		_output.WriteLine($"List items: {listItems.Count}");

		// Group by list level
		var byLevel = listItems.GroupBy(li => li.ListLevel).OrderBy(g => g.Key);
		foreach (var group in byLevel)
		{
			_output.WriteLine($"  Level {group.Key}: {group.Count()} items");
		}

		// Verify list levels are reasonable
		_ = listItems.Max(li => li.ListLevel).Should().BeLessThan(10);
	}

	[Fact]
	public async Task ChunkAsync_WithOpenAITokenCounter_ShouldCalculateAccurateTokens()
	{
		// Arrange
		var testFilePath = Path.Combine("TestData", "Docx", "simple.docx");

		if (!File.Exists(testFilePath))
		{
			_output.WriteLine($"Test file not found: {testFilePath}. Skipping test.");
			return;
		}

		var options = ChunkingPresets.ForOpenAIEmbeddings();
		var tokenCounter = TokenCounterFactory.GetOrCreate(options);
		var chunker = new DocxDocumentChunker(tokenCounter);

		await using var stream = File.OpenRead(testFilePath);

		// Act
		var result = await chunker.ChunkAsync(stream, options);

		// Assert
		_ = result.Success.Should().BeTrue();
		_ = result.Chunks.Should().NotBeEmpty();

		foreach (var chunk in result.Chunks)
		{
			_ = chunk.QualityMetrics.Should().NotBeNull();
			_ = chunk.QualityMetrics!.TokenCount.Should().BePositive();

			_output.WriteLine($"Chunk: {chunk.QualityMetrics.TokenCount} tokens");
		}

		// Verify no chunks exceed the limit
		var maxTokens = options.MaxTokens;
		_ = result.Chunks.Should().OnlyContain(c => c.QualityMetrics!.TokenCount <= maxTokens);
	}

	[Fact]
	public async Task ChunkAsync_LargeDocument_ShouldHandleEfficiently()
	{
		// Arrange
		var testFilePath = Path.Combine("TestData", "Docx", "large.docx");

		if (!File.Exists(testFilePath))
		{
			_output.WriteLine($"Test file not found: {testFilePath}. Skipping test.");
			return;
		}

		var tokenCounter = new CharacterBasedTokenCounter();
		var chunker = new DocxDocumentChunker(tokenCounter);
		var options = ChunkingPresets.ForRAG();

		await using var stream = File.OpenRead(testFilePath);

		// Act
		var startTime = DateTime.UtcNow;
		var result = await chunker.ChunkAsync(stream, options);
		var duration = DateTime.UtcNow - startTime;

		// Assert
		_ = result.Success.Should().BeTrue();
		_ = result.Chunks.Should().NotBeEmpty();

		_output.WriteLine($"Large document results:");
		_output.WriteLine($"  Total chunks: {result.Statistics.TotalChunks}");
		_output.WriteLine($"  Processing time: {duration.TotalMilliseconds}ms");
		_output.WriteLine($"  Chunks per second: {result.Statistics.TotalChunks / duration.TotalSeconds:F2}");

		// Performance assertion: should process reasonably fast
		_ = duration.Should().BeLessThan(TimeSpan.FromSeconds(30));
	}

	[Fact]
	public async Task ChunkAsync_WithFormattedText_ShouldExtractAnnotations()
	{
		// Arrange
		var testFilePath = Path.Combine("TestData", "Docx", "formatted.docx");

		if (!File.Exists(testFilePath))
		{
			_output.WriteLine($"Test file not found: {testFilePath}. Skipping test.");
			return;
		}

		var tokenCounter = new CharacterBasedTokenCounter();
		var chunker = new DocxDocumentChunker(tokenCounter);
		var options = ChunkingPresets.ForRAG();

		await using var stream = File.OpenRead(testFilePath);

		// Act
		var result = await chunker.ChunkAsync(stream, options);

		// Assert
		_ = result.Success.Should().BeTrue();

		var paragraphs = result.Chunks.OfType<DocxParagraphChunk>()
			.Where(p => p.Annotations.Count > 0)
			.ToList();

		_output.WriteLine($"Paragraphs with annotations: {paragraphs.Count}");

		foreach (var paragraph in paragraphs.Take(5))
		{
			_output.WriteLine($"  Paragraph: {paragraph.Content[..Math.Min(50, paragraph.Content.Length)]}...");
			_output.WriteLine($"  Annotations: {paragraph.Annotations.Count}");

			foreach (var annotation in paragraph.Annotations.Take(3))
			{
				_output.WriteLine($"    - {annotation.Type}");
			}
		}
	}
}
