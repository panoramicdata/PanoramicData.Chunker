using AwesomeAssertions;
using PanoramicData.Chunker.Chunkers.Markdown;
using PanoramicData.Chunker.Configuration;
using PanoramicData.Chunker.Infrastructure;
using System.Text;

namespace PanoramicData.Chunker.Tests.Unit.Chunkers;

/// <summary>
/// Tests for MarkdownDocumentChunker.
/// </summary>
public class MarkdownDocumentChunkerTests
{
	private readonly MarkdownDocumentChunker _chunker;
	private readonly ChunkingOptions _defaultOptions;

	public MarkdownDocumentChunkerTests()
	{
		var tokenCounter = new CharacterBasedTokenCounter();
		_chunker = new MarkdownDocumentChunker(tokenCounter);
		_defaultOptions = new ChunkingOptions
		{
			MaxTokens = 1000,
			ValidateChunks = true
		};
	}

	[Fact]
	public void Constructor_WithNullTokenCounter_ShouldThrow()
	{
		// Act
		var act = () => new MarkdownDocumentChunker(null!);

		// Assert
		_ = act.Should().Throw<ArgumentNullException>();
	}

	[Fact]
	public void SupportedType_ShouldReturnMarkdown()
	{
		// Act
		var result = _chunker.SupportedType;

		// Assert
		_ = result.Should().Be(DocumentType.Markdown);
	}

	[Fact]
	public async Task CanHandleAsync_WithMarkdownContent_ShouldReturnTrue()
	{
		// Arrange
		var markdown = "# Header\n\nSome content";
		using var stream = new MemoryStream(Encoding.UTF8.GetBytes(markdown));

		// Act
		var result = await _chunker.CanHandleAsync(stream);

		// Assert
		_ = result.Should().BeTrue();
	}

	[Fact]
	public async Task CanHandleAsync_WithEmptyStream_ShouldReturnFalse()
	{
		// Arrange
		using var stream = new MemoryStream();

		// Act
		var result = await _chunker.CanHandleAsync(stream);

		// Assert
		_ = result.Should().BeFalse();
	}

	[Fact]
	public async Task CanHandleAsync_WithNullStream_ShouldReturnFalse()
	{
		// Act
		var result = await _chunker.CanHandleAsync(null!);

		// Assert
		_ = result.Should().BeFalse();
	}

	[Fact]
	public async Task ChunkAsync_WithSimpleHeader_ShouldCreateSectionChunk()
	{
		// Arrange
		var markdown = "# Test Header";
		using var stream = new MemoryStream(Encoding.UTF8.GetBytes(markdown));

		// Act
		var result = await _chunker.ChunkAsync(stream, _defaultOptions);

		// Assert
		_ = result.Success.Should().BeTrue();
		_ = result.Chunks.Should().ContainSingle();
		_ = result.Chunks[0].Should().BeOfType<MarkdownSectionChunk>();

		var section = result.Chunks[0] as MarkdownSectionChunk;
		_ = section!.HeadingLevel.Should().Be(1);
		_ = section.HeadingText.Should().Be("Test Header");
		_ = section.SpecificType.Should().Be("Heading1");
	}

	[Fact]
	public async Task ChunkAsync_WithHeadersAndParagraphs_ShouldCreateHierarchy()
	{
		// Arrange
		var markdown = @"# Main Header

First paragraph.

## Subheader

Second paragraph.";
		using var stream = new MemoryStream(Encoding.UTF8.GetBytes(markdown));

		// Act
		var result = await _chunker.ChunkAsync(stream, _defaultOptions);

		// Assert
		_ = result.Success.Should().BeTrue();
		_ = result.Chunks.Should().HaveCount(4); // H1, para1, H2, para2

		// Check hierarchy
		var h1 = result.Chunks[0] as MarkdownSectionChunk;
		_ = h1.Should().NotBeNull();
		_ = h1!.HeadingLevel.Should().Be(1);
		_ = h1.ParentId.Should().BeNull();

		var para1 = result.Chunks[1] as MarkdownParagraphChunk;
		_ = para1.Should().NotBeNull();
		_ = para1!.ParentId.Should().Be(h1.Id);

		var h2 = result.Chunks[2] as MarkdownSectionChunk;
		_ = h2.Should().NotBeNull();
		_ = h2!.HeadingLevel.Should().Be(2);
		_ = h2.ParentId.Should().Be(h1.Id);

		var para2 = result.Chunks[3] as MarkdownParagraphChunk;
		_ = para2.Should().NotBeNull();
		_ = para2!.ParentId.Should().Be(h2.Id);
	}

	[Fact]
	public async Task ChunkAsync_WithLists_ShouldCreateListItemChunks()
	{
		// Arrange
		var markdown = @"# Header

- Item 1
- Item 2
- Item 3";
		using var stream = new MemoryStream(Encoding.UTF8.GetBytes(markdown));

		// Act
		var result = await _chunker.ChunkAsync(stream, _defaultOptions);

		// Assert
		_ = result.Success.Should().BeTrue();
		_ = result.Chunks.OfType<MarkdownListItemChunk>().Should().HaveCount(3);

		var items = result.Chunks.OfType<MarkdownListItemChunk>().ToList();
		_ = items[0].IsOrdered.Should().BeFalse();
		_ = items[0].Content.Should().Contain("Item 1");
	}

	[Fact]
	public async Task ChunkAsync_WithOrderedList_ShouldSetItemNumbers()
	{
		// Arrange
		var markdown = @"1. First
2. Second
3. Third";
		using var stream = new MemoryStream(Encoding.UTF8.GetBytes(markdown));

		// Act
		var result = await _chunker.ChunkAsync(stream, _defaultOptions);

		// Assert
		var items = result.Chunks.OfType<MarkdownListItemChunk>().ToList();
		_ = items.Should().HaveCount(3);
		_ = items[0].IsOrdered.Should().BeTrue();
		_ = items[0].ItemNumber.Should().Be(1);
		_ = items[1].ItemNumber.Should().Be(2);
	}

	[Fact]
	public async Task ChunkAsync_WithCodeBlock_ShouldCreateCodeChunk()
	{
		// Arrange
		var markdown = @"# Header

```csharp
public class Test { }
```";
		using var stream = new MemoryStream(Encoding.UTF8.GetBytes(markdown));

		// Act
		var result = await _chunker.ChunkAsync(stream, _defaultOptions);

		// Assert
		var codeChunk = result.Chunks.OfType<MarkdownCodeBlockChunk>().FirstOrDefault();
		_ = codeChunk.Should().NotBeNull();
		_ = codeChunk!.Language.Should().Be("csharp");
		_ = codeChunk.IsFenced.Should().BeTrue();
		_ = codeChunk.Content.Should().Contain("public class Test");
	}

	[Fact]
	public async Task ChunkAsync_WithBlockquote_ShouldCreateQuoteChunk()
	{
		// Arrange
		var markdown = @"# Header

> This is a quote
> with multiple lines";
		using var stream = new MemoryStream(Encoding.UTF8.GetBytes(markdown));

		// Act
		var result = await _chunker.ChunkAsync(stream, _defaultOptions);

		// Assert
		var quoteChunk = result.Chunks.OfType<MarkdownQuoteChunk>().FirstOrDefault();
		_ = quoteChunk.Should().NotBeNull();
		_ = quoteChunk!.Content.Should().Contain("This is a quote");
	}

	[Fact]
	public async Task ChunkAsync_WithTable_ShouldCreateTableChunk()
	{
		// Arrange
		var markdown = @"# Header

| Col1 | Col2 |
|------|------|
| A    | B    |
| C    | D    |";
		using var stream = new MemoryStream(Encoding.UTF8.GetBytes(markdown));

		// Act
		var result = await _chunker.ChunkAsync(stream, _defaultOptions);

		// Assert
		var tableChunk = result.Chunks.OfType<MarkdownTableChunk>().FirstOrDefault();
		_ = tableChunk.Should().NotBeNull();
		_ = tableChunk!.TableInfo.ColumnCount.Should().Be(2);
		_ = tableChunk.TableInfo.RowCount.Should().Be(2);
		_ = tableChunk.TableInfo.Headers.Should().Contain("Col1");
		_ = tableChunk.TableInfo.Headers.Should().Contain("Col2");
	}

	[Fact]
	public async Task ChunkAsync_ShouldPopulateQualityMetrics()
	{
		// Arrange
		var markdown = "# Header\n\nTest paragraph with some content.";
		using var stream = new MemoryStream(Encoding.UTF8.GetBytes(markdown));

		// Act
		var result = await _chunker.ChunkAsync(stream, _defaultOptions);

		// Assert
		_ = result.Chunks.Should().AllSatisfy(chunk =>
		{
			_ = chunk.QualityMetrics.Should().NotBeNull();
			_ = chunk.QualityMetrics!.TokenCount.Should().BePositive();
			_ = chunk.QualityMetrics.CharacterCount.Should().BePositive();
		});
	}

	[Fact]
	public async Task ChunkAsync_ShouldPopulateStatistics()
	{
		// Arrange
		var markdown = "# Header\n\nParagraph.";
		using var stream = new MemoryStream(Encoding.UTF8.GetBytes(markdown));

		// Act
		var result = await _chunker.ChunkAsync(stream, _defaultOptions);

		// Assert
		_ = result.Statistics.Should().NotBeNull();
		_ = result.Statistics.TotalChunks.Should().Be(2);
		_ = result.Statistics.StructuralChunks.Should().Be(1);
		_ = result.Statistics.ContentChunks.Should().Be(1);
		_ = result.Statistics.ProcessingTime.Should().BeGreaterThan(TimeSpan.Zero);
	}

	[Fact]
	public async Task ChunkAsync_WithValidation_ShouldValidateChunks()
	{
		// Arrange
		var markdown = "# Header\n\nContent";
		using var stream = new MemoryStream(Encoding.UTF8.GetBytes(markdown));

		// Act
		var result = await _chunker.ChunkAsync(stream, _defaultOptions);

		// Assert
		_ = result.ValidationResult.Should().NotBeNull();
		_ = result.ValidationResult!.IsValid.Should().BeTrue();
	}

	[Fact]
	public async Task ChunkAsync_WithOversizedChunk_ShouldSplit()
	{
		// Arrange
		var longText = string.Join(". ", Enumerable.Range(1, 100).Select(i => $"Sentence {i}"));
		var markdown = $"# Header\n\n{longText}";
		using var stream = new MemoryStream(Encoding.UTF8.GetBytes(markdown));

		var options = new ChunkingOptions
		{
			MaxTokens = 50,
			ValidateChunks = false
		};

		// Act
		var result = await _chunker.ChunkAsync(stream, options);

		// Assert
		var paragraphs = result.Chunks.OfType<MarkdownParagraphChunk>().ToList();
		_ = paragraphs.Should().HaveCountGreaterThan(1);
		paragraphs.Should().AllSatisfy(p =>
		{
			p.QualityMetrics!.TokenCount.Should().BeLessThanOrEqualTo(options.MaxTokens);
		});
	}

	[Fact]
	public async Task ChunkAsync_ShouldSetSequenceNumbers()
	{
		// Arrange
		var markdown = "# H1\n\nP1\n\n## H2\n\nP2";
		using var stream = new MemoryStream(Encoding.UTF8.GetBytes(markdown));

		// Act
		var result = await _chunker.ChunkAsync(stream, _defaultOptions);

		// Assert
		_ = result.Chunks.Select(c => c.SequenceNumber).Should().BeInAscendingOrder();
		_ = result.Chunks[0].SequenceNumber.Should().Be(0);
	}

	[Fact]
	public async Task ChunkAsync_ShouldBuildHierarchy()
	{
		// Arrange
		var markdown = "# H1\n\n## H2\n\n### H3\n\nContent";
		using var stream = new MemoryStream(Encoding.UTF8.GetBytes(markdown));

		// Act
		var result = await _chunker.ChunkAsync(stream, _defaultOptions);

		// Assert
		var h1 = result.Chunks[0];
		var h2 = result.Chunks[1];
		var h3 = result.Chunks[2];

		_ = h1.Depth.Should().Be(0);
		_ = h2.Depth.Should().Be(1);
		_ = h3.Depth.Should().Be(2);

		_ = h2.AncestorIds.Should().Contain(h1.Id);
		_ = h3.AncestorIds.Should().Contain(h1.Id);
		_ = h3.AncestorIds.Should().Contain(h2.Id);
	}
}
