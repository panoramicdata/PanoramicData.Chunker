using AwesomeAssertions;
using PanoramicData.Chunker.Chunkers.Markdown;
using PanoramicData.Chunker.Configuration;
using PanoramicData.Chunker.Infrastructure;
using PanoramicData.Chunker.Models;
using System.Text;

namespace PanoramicData.Chunker.Tests.Unit.Chunkers;

/// <summary>
/// Tests for MarkdownDocumentChunker.
/// </summary>
public class MarkdownDocumentChunkerTests : BaseTest
{
	private readonly MarkdownDocumentChunker _chunker;
	private readonly ChunkingOptions _defaultOptions;

	public MarkdownDocumentChunkerTests(ITestOutputHelper output) : base(output)
	{
		_chunker = new MarkdownDocumentChunker(new CharacterBasedTokenCounter());
		_defaultOptions = new ChunkingOptions
		{
			MaxTokens = 1000,
			ValidateChunks = true
		};
	}

	[Fact]
	public void Constructor_WithNullTokenCounter_ShouldThrow()
	{
		var act = () => new MarkdownDocumentChunker(null!);

		_ = act.Should().Throw<ArgumentNullException>();
	}

	[Fact]
	public void SupportedType_ShouldReturnMarkdown()
	{
		_ = _chunker.SupportedType.Should().Be(DocumentType.Markdown);
	}

	[Fact]
	public async Task CanHandleAsync_WithMarkdownContent_ShouldReturnTrue()
	{
		using var stream = new MemoryStream(Encoding.UTF8.GetBytes("# Header\n\nSome content"));

		_ = (await _chunker.CanHandleAsync(stream, CancellationToken)).Should().BeTrue();
	}

	[Fact]
	public async Task CanHandleAsync_WithEmptyStream_ShouldReturnFalse()
	{
		using var stream = new MemoryStream();

		_ = (await _chunker.CanHandleAsync(stream, CancellationToken)).Should().BeFalse();
	}

	[Fact]
	public async Task CanHandleAsync_WithNullStream_ShouldReturnFalse()
	{
		_ = (await _chunker.CanHandleAsync(null!, CancellationToken)).Should().BeFalse();
	}

	[Fact]
	public async Task ChunkAsync_WithSimpleHeader_ShouldCreateSectionChunk()
	{
		var result = await ChunkMarkdownAsync("# Test Header");

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
		var result = await ChunkMarkdownAsync("# Main Header\n\nFirst paragraph.\n\n## Subheader\n\nSecond paragraph.");

		_ = result.Success.Should().BeTrue();
		_ = result.Chunks.Should().HaveCount(4);

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
		var result = await ChunkMarkdownAsync("# Header\n\n- Item 1\n- Item 2\n- Item 3");

		_ = result.Success.Should().BeTrue();

		var items = result.Chunks.OfType<MarkdownListItemChunk>().ToList();
		_ = items.Should().HaveCount(3);
		_ = items[0].IsOrdered.Should().BeFalse();
		_ = items[0].Content.Should().Contain("Item 1");
	}

	[Fact]
	public async Task ChunkAsync_WithOrderedList_ShouldSetItemNumbers()
	{
		var result = await ChunkMarkdownAsync("1. First\n2. Second\n3. Third");

		var items = result.Chunks.OfType<MarkdownListItemChunk>().ToList();
		_ = items.Should().HaveCount(3);
		_ = items[0].IsOrdered.Should().BeTrue();
		_ = items[0].ItemNumber.Should().Be(1);
		_ = items[1].ItemNumber.Should().Be(2);
	}

	[Fact]
	public async Task ChunkAsync_WithCodeBlock_ShouldCreateCodeChunk()
	{
		var result = await ChunkMarkdownAsync("# Header\n\n```csharp\npublic class Test { }\n```");

		var codeChunk = result.Chunks.OfType<MarkdownCodeBlockChunk>().FirstOrDefault();
		_ = codeChunk.Should().NotBeNull();
		_ = codeChunk!.Language.Should().Be("csharp");
		_ = codeChunk.IsFenced.Should().BeTrue();
		_ = codeChunk.Content.Should().Contain("public class Test");
	}

	[Fact]
	public async Task ChunkAsync_WithBlockquote_ShouldCreateQuoteChunk()
	{
		var result = await ChunkMarkdownAsync("# Header\n\n> This is a quote\n> with multiple lines");

		var quoteChunk = result.Chunks.OfType<MarkdownQuoteChunk>().FirstOrDefault();
		_ = quoteChunk.Should().NotBeNull();
		_ = quoteChunk!.Content.Should().Contain("This is a quote");
	}

	[Fact]
	public async Task ChunkAsync_WithTable_ShouldCreateTableChunk()
	{
		var result = await ChunkMarkdownAsync("# Header\n\n| Col1 | Col2 |\n|------|------|\n| A    | B    |\n| C    | D    |");

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
		var result = await ChunkMarkdownAsync("# Header\n\nTest paragraph with some content.");

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
		var result = await ChunkMarkdownAsync("# Header\n\nParagraph.");

		_ = result.Statistics.Should().NotBeNull();
		_ = result.Statistics.TotalChunks.Should().Be(2);
		_ = result.Statistics.StructuralChunks.Should().Be(1);
		_ = result.Statistics.ContentChunks.Should().Be(1);
		_ = result.Statistics.ProcessingTime.Should().BeGreaterThan(TimeSpan.Zero);
	}

	[Fact]
	public async Task ChunkAsync_WithValidation_ShouldValidateChunks()
	{
		var result = await ChunkMarkdownAsync("# Header\n\nContent");

		_ = result.ValidationResult.Should().NotBeNull();
		_ = result.ValidationResult!.IsValid.Should().BeTrue();
	}

	[Fact]
	public async Task ChunkAsync_WithOversizedChunk_ShouldSplit()
	{
		var longText = string.Join(". ", Enumerable.Range(1, 100).Select(i => $"Sentence {i}"));
		var options = new ChunkingOptions
		{
			MaxTokens = 50,
			ValidateChunks = false
		};

		var result = await ChunkMarkdownAsync($"# Header\n\n{longText}", options);

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
		var result = await ChunkMarkdownAsync("# H1\n\nP1\n\n## H2\n\nP2");

		_ = result.Chunks.Select(c => c.SequenceNumber).Should().BeInAscendingOrder();
		_ = result.Chunks[0].SequenceNumber.Should().Be(0);
	}

	[Fact]
	public async Task ChunkAsync_ShouldBuildHierarchy()
	{
		var result = await ChunkMarkdownAsync("# H1\n\n## H2\n\n### H3\n\nContent");

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

	private async Task<ChunkingResult> ChunkMarkdownAsync(string markdown, ChunkingOptions? options = null)
	{
		using var stream = new MemoryStream(Encoding.UTF8.GetBytes(markdown));
		return await _chunker.ChunkAsync(stream, options ?? _defaultOptions, CancellationToken);
	}
}
