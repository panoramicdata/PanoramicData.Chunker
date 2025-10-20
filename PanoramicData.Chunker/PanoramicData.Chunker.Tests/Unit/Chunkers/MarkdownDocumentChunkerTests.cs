using FluentAssertions;
using PanoramicData.Chunker.Chunkers.Markdown;
using PanoramicData.Chunker.Configuration;
using PanoramicData.Chunker.Infrastructure;

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
		act.Should().Throw<ArgumentNullException>();
	}

	[Fact]
	public void SupportedType_ShouldReturnMarkdown()
	{
		// Act
		var result = _chunker.SupportedType;

		// Assert
		result.Should().Be(DocumentType.Markdown);
	}

	[Fact]
	public async Task CanHandleAsync_WithMarkdownContent_ShouldReturnTrue()
	{
		// Arrange
		var markdown = "# Header\n\nSome content";
		using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(markdown));

		// Act
		var result = await _chunker.CanHandleAsync(stream);

		// Assert
		result.Should().BeTrue();
	}

	[Fact]
	public async Task CanHandleAsync_WithEmptyStream_ShouldReturnFalse()
	{
		// Arrange
		using var stream = new MemoryStream();

		// Act
		var result = await _chunker.CanHandleAsync(stream);

		// Assert
		result.Should().BeFalse();
	}

	[Fact]
	public async Task CanHandleAsync_WithNullStream_ShouldReturnFalse()
	{
		// Act
		var result = await _chunker.CanHandleAsync(null!);

		// Assert
		result.Should().BeFalse();
	}

	[Fact]
	public async Task ChunkAsync_WithSimpleHeader_ShouldCreateSectionChunk()
	{
		// Arrange
		var markdown = "# Test Header";
		using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(markdown));

		// Act
		var result = await _chunker.ChunkAsync(stream, _defaultOptions);

		// Assert
		result.Success.Should().BeTrue();
		result.Chunks.Should().HaveCount(1);
		result.Chunks[0].Should().BeOfType<MarkdownSectionChunk>();

		var section = result.Chunks[0] as MarkdownSectionChunk;
		section!.HeadingLevel.Should().Be(1);
		section.HeadingText.Should().Be("Test Header");
		section.SpecificType.Should().Be("Heading1");
	}

	[Fact]
	public async Task ChunkAsync_WithHeadersAndParagraphs_ShouldCreateHierarchy()
	{
		// Arrange
		var markdown = @"# Main Header

First paragraph.

## Subheader

Second paragraph.";
		using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(markdown));

		// Act
		var result = await _chunker.ChunkAsync(stream, _defaultOptions);

		// Assert
		result.Success.Should().BeTrue();
		result.Chunks.Should().HaveCount(4); // H1, para1, H2, para2

		// Check hierarchy
		var h1 = result.Chunks[0] as MarkdownSectionChunk;
		h1.Should().NotBeNull();
		h1!.HeadingLevel.Should().Be(1);
		h1.ParentId.Should().BeNull();

		var para1 = result.Chunks[1] as MarkdownParagraphChunk;
		para1.Should().NotBeNull();
		para1!.ParentId.Should().Be(h1.Id);

		var h2 = result.Chunks[2] as MarkdownSectionChunk;
		h2.Should().NotBeNull();
		h2!.HeadingLevel.Should().Be(2);
		h2.ParentId.Should().Be(h1.Id);

		var para2 = result.Chunks[3] as MarkdownParagraphChunk;
		para2.Should().NotBeNull();
		para2!.ParentId.Should().Be(h2.Id);
	}

	[Fact]
	public async Task ChunkAsync_WithLists_ShouldCreateListItemChunks()
	{
		// Arrange
		var markdown = @"# Header

- Item 1
- Item 2
- Item 3";
		using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(markdown));

		// Act
		var result = await _chunker.ChunkAsync(stream, _defaultOptions);

		// Assert
		result.Success.Should().BeTrue();
		result.Chunks.OfType<MarkdownListItemChunk>().Should().HaveCount(3);

		var items = result.Chunks.OfType<MarkdownListItemChunk>().ToList();
		items[0].IsOrdered.Should().BeFalse();
		items[0].Content.Should().Contain("Item 1");
	}

	[Fact]
	public async Task ChunkAsync_WithOrderedList_ShouldSetItemNumbers()
	{
		// Arrange
		var markdown = @"1. First
2. Second
3. Third";
		using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(markdown));

		// Act
		var result = await _chunker.ChunkAsync(stream, _defaultOptions);

		// Assert
		var items = result.Chunks.OfType<MarkdownListItemChunk>().ToList();
		items.Should().HaveCount(3);
		items[0].IsOrdered.Should().BeTrue();
		items[0].ItemNumber.Should().Be(1);
		items[1].ItemNumber.Should().Be(2);
	}

	[Fact]
	public async Task ChunkAsync_WithCodeBlock_ShouldCreateCodeChunk()
	{
		// Arrange
		var markdown = @"# Header

```csharp
public class Test { }
```";
		using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(markdown));

		// Act
		var result = await _chunker.ChunkAsync(stream, _defaultOptions);

		// Assert
		var codeChunk = result.Chunks.OfType<MarkdownCodeBlockChunk>().FirstOrDefault();
		codeChunk.Should().NotBeNull();
		codeChunk!.Language.Should().Be("csharp");
		codeChunk.IsFenced.Should().BeTrue();
		codeChunk.Content.Should().Contain("public class Test");
	}

	[Fact]
	public async Task ChunkAsync_WithBlockquote_ShouldCreateQuoteChunk()
	{
		// Arrange
		var markdown = @"# Header

> This is a quote
> with multiple lines";
		using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(markdown));

		// Act
		var result = await _chunker.ChunkAsync(stream, _defaultOptions);

		// Assert
		var quoteChunk = result.Chunks.OfType<MarkdownQuoteChunk>().FirstOrDefault();
		quoteChunk.Should().NotBeNull();
		quoteChunk!.Content.Should().Contain("This is a quote");
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
		using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(markdown));

		// Act
		var result = await _chunker.ChunkAsync(stream, _defaultOptions);

		// Assert
		var tableChunk = result.Chunks.OfType<MarkdownTableChunk>().FirstOrDefault();
		tableChunk.Should().NotBeNull();
		tableChunk!.TableInfo.ColumnCount.Should().Be(2);
		tableChunk.TableInfo.RowCount.Should().Be(2);
		tableChunk.TableInfo.Headers.Should().Contain("Col1");
		tableChunk.TableInfo.Headers.Should().Contain("Col2");
	}

	[Fact]
	public async Task ChunkAsync_ShouldPopulateQualityMetrics()
	{
		// Arrange
		var markdown = "# Header\n\nTest paragraph with some content.";
		using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(markdown));

		// Act
		var result = await _chunker.ChunkAsync(stream, _defaultOptions);

		// Assert
		result.Chunks.Should().AllSatisfy(chunk =>
		{
			chunk.QualityMetrics.Should().NotBeNull();
			chunk.QualityMetrics!.TokenCount.Should().BeGreaterThan(0);
			chunk.QualityMetrics.CharacterCount.Should().BeGreaterThan(0);
		});
	}

	[Fact]
	public async Task ChunkAsync_ShouldPopulateStatistics()
	{
		// Arrange
		var markdown = "# Header\n\nParagraph.";
		using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(markdown));

		// Act
		var result = await _chunker.ChunkAsync(stream, _defaultOptions);

		// Assert
		result.Statistics.Should().NotBeNull();
		result.Statistics.TotalChunks.Should().Be(2);
		result.Statistics.StructuralChunks.Should().Be(1);
		result.Statistics.ContentChunks.Should().Be(1);
		result.Statistics.ProcessingTime.Should().BeGreaterThan(TimeSpan.Zero);
	}

	[Fact]
	public async Task ChunkAsync_WithValidation_ShouldValidateChunks()
	{
		// Arrange
		var markdown = "# Header\n\nContent";
		using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(markdown));

		// Act
		var result = await _chunker.ChunkAsync(stream, _defaultOptions);

		// Assert
		result.ValidationResult.Should().NotBeNull();
		result.ValidationResult!.IsValid.Should().BeTrue();
	}

	[Fact]
	public async Task ChunkAsync_WithOversizedChunk_ShouldSplit()
	{
		// Arrange
		var longText = string.Join(". ", Enumerable.Range(1, 100).Select(i => $"Sentence {i}"));
		var markdown = $"# Header\n\n{longText}";
		using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(markdown));

		var options = new ChunkingOptions
		{
			MaxTokens = 50,
			ValidateChunks = false
		};

		// Act
		var result = await _chunker.ChunkAsync(stream, options);

		// Assert
		var paragraphs = result.Chunks.OfType<MarkdownParagraphChunk>().ToList();
		paragraphs.Should().HaveCountGreaterThan(1);
		paragraphs.Should().AllSatisfy(p =>
		{
			p.QualityMetrics!.TokenCount.Should().BeLessOrEqualTo(options.MaxTokens);
		});
	}

	[Fact]
	public async Task ChunkAsync_ShouldSetSequenceNumbers()
	{
		// Arrange
		var markdown = "# H1\n\nP1\n\n## H2\n\nP2";
		using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(markdown));

		// Act
		var result = await _chunker.ChunkAsync(stream, _defaultOptions);

		// Assert
		result.Chunks.Select(c => c.SequenceNumber).Should().BeInAscendingOrder();
		result.Chunks[0].SequenceNumber.Should().Be(0);
	}

	[Fact]
	public async Task ChunkAsync_ShouldBuildHierarchy()
	{
		// Arrange
		var markdown = "# H1\n\n## H2\n\n### H3\n\nContent";
		using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(markdown));

		// Act
		var result = await _chunker.ChunkAsync(stream, _defaultOptions);

		// Assert
		var h1 = result.Chunks[0];
		var h2 = result.Chunks[1];
		var h3 = result.Chunks[2];

		h1.Depth.Should().Be(0);
		h2.Depth.Should().Be(1);
		h3.Depth.Should().Be(2);

		h2.AncestorIds.Should().Contain(h1.Id);
		h3.AncestorIds.Should().Contain(h1.Id);
		h3.AncestorIds.Should().Contain(h2.Id);
	}
}
