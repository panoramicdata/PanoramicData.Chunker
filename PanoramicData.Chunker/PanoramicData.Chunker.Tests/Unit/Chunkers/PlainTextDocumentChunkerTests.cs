using AwesomeAssertions;
using PanoramicData.Chunker.Chunkers.PlainText;
using PanoramicData.Chunker.Configuration;
using PanoramicData.Chunker.Infrastructure;
using System.Text;

namespace PanoramicData.Chunker.Tests.Unit.Chunkers;

public class PlainTextDocumentChunkerTests
{
	private readonly PlainTextDocumentChunker _chunker;

	public PlainTextDocumentChunkerTests()
	{
		var tokenCounter = new CharacterBasedTokenCounter();
		_chunker = new PlainTextDocumentChunker(tokenCounter);
	}

	#region CanHandleAsync Tests

	[Fact]
	public async Task CanHandleAsync_PlainText_ShouldReturnTrue()
	{
		// Arrange
		var text = "This is plain text.\n\nWith multiple paragraphs.";
		var stream = new MemoryStream(Encoding.UTF8.GetBytes(text));

		// Act
		var result = await _chunker.CanHandleAsync(stream);

		// Assert
		_ = result.Should().BeTrue();
	}

	[Fact]
	public async Task CanHandleAsync_HtmlContent_ShouldReturnFalse()
	{
		// Arrange
		var html = "<html><body><p>HTML content</p></body></html>";
		var stream = new MemoryStream(Encoding.UTF8.GetBytes(html));

		// Act
		var result = await _chunker.CanHandleAsync(stream);

		// Assert
		_ = result.Should().BeFalse();
	}

	[Fact]
	public async Task CanHandleAsync_MarkdownContent_ShouldReturnFalse()
	{
		// Arrange
		var markdown = "# Header\n\n**Bold** text with ![image](url)";
		var stream = new MemoryStream(Encoding.UTF8.GetBytes(markdown));

		// Act
		var result = await _chunker.CanHandleAsync(stream);

		// Assert
		_ = result.Should().BeFalse();
	}

	[Fact]
	public async Task CanHandleAsync_EmptyStream_ShouldReturnFalse()
	{
		// Arrange
		var stream = new MemoryStream();

		// Act
		var result = await _chunker.CanHandleAsync(stream);

		// Assert
		_ = result.Should().BeFalse();
	}

	#endregion

	#region Heading Detection Tests

	[Fact]
	public async Task ChunkAsync_WithUnderlinedHeading_ShouldDetectHeading()
	{
		// Arrange
		var text = "Main Heading\n============\n\nContent here.";
		var stream = new MemoryStream(Encoding.UTF8.GetBytes(text));
		var options = new ChunkingOptions();

		// Act
		var result = await _chunker.ChunkAsync(stream, options);

		// Assert
		_ = result.Success.Should().BeTrue();
		var sections = result.Chunks.OfType<PlainTextSectionChunk>().ToList();
		_ = sections.Should().ContainSingle();
		_ = sections[0].HeadingText.Should().Be("Main Heading");
		_ = sections[0].HeadingLevel.Should().Be(1);
		_ = sections[0].HeadingType.Should().Be(HeadingHeuristic.Underlined);
		_ = sections[0].Confidence.Should().BeGreaterThan(0.9);
	}

	[Fact]
	public async Task ChunkAsync_WithDashUnderlinedHeading_ShouldDetectLevel2()
	{
		// Arrange
		var text = "Sub Heading\n-----------\n\nContent here.";
		var stream = new MemoryStream(Encoding.UTF8.GetBytes(text));
		var options = new ChunkingOptions();

		// Act
		var result = await _chunker.ChunkAsync(stream, options);

		// Assert
		var sections = result.Chunks.OfType<PlainTextSectionChunk>().ToList();
		_ = sections.Should().ContainSingle();
		_ = sections[0].HeadingLevel.Should().Be(2);
		_ = sections[0].HeadingType.Should().Be(HeadingHeuristic.Underlined);
	}

	[Fact]
	public async Task ChunkAsync_WithNumberedSection_ShouldDetectHeading()
	{
		// Arrange
		var text = "1. Introduction\n\nContent here.\n\n1.1 Background\n\nMore content.";
		var stream = new MemoryStream(Encoding.UTF8.GetBytes(text));
		var options = new ChunkingOptions();

		// Act
		var result = await _chunker.ChunkAsync(stream, options);

		// Assert
		var sections = result.Chunks.OfType<PlainTextSectionChunk>().ToList();
		_ = sections.Should().HaveCount(2);
		_ = sections[0].HeadingText.Should().Be("Introduction");
		_ = sections[0].HeadingLevel.Should().Be(1);
		_ = sections[0].HeadingType.Should().Be(HeadingHeuristic.Numbered);
		_ = sections[1].HeadingText.Should().Be("Background");
		_ = sections[1].HeadingLevel.Should().Be(2);
	}

	[Fact]
	public async Task ChunkAsync_WithDeepNumberedSection_ShouldDetectCorrectLevel()
	{
		// Arrange
		var text = "1.1.1 Deep Section\n\nContent here.";
		var stream = new MemoryStream(Encoding.UTF8.GetBytes(text));
		var options = new ChunkingOptions();

		// Act
		var result = await _chunker.ChunkAsync(stream, options);

		// Assert
		var sections = result.Chunks.OfType<PlainTextSectionChunk>().ToList();
		_ = sections.Should().ContainSingle();
		_ = sections[0].HeadingLevel.Should().Be(3); // 3 dots = level 3
	}

	[Fact]
	public async Task ChunkAsync_WithAllCapsHeading_ShouldDetectHeading()
	{
		// Arrange
		var text = "INTRODUCTION\n\nThis is the introduction content.";
		var stream = new MemoryStream(Encoding.UTF8.GetBytes(text));
		var options = new ChunkingOptions();

		// Act
		var result = await _chunker.ChunkAsync(stream, options);

		// Assert
		var sections = result.Chunks.OfType<PlainTextSectionChunk>().ToList();
		_ = sections.Should().ContainSingle();
		_ = sections[0].HeadingText.Should().Be("INTRODUCTION");
		_ = sections[0].HeadingLevel.Should().Be(1);
		_ = sections[0].HeadingType.Should().Be(HeadingHeuristic.AllCaps);
	}

	[Fact]
	public async Task ChunkAsync_WithPrefixedHeading_ShouldDetectHeading()
	{
		// Arrange
		var text = "# Main Heading\n\nContent.\n\n## Sub Heading\n\nMore content.";
		var stream = new MemoryStream(Encoding.UTF8.GetBytes(text));
		var options = new ChunkingOptions();

		// Act
		var result = await _chunker.ChunkAsync(stream, options);

		// Assert
		var sections = result.Chunks.OfType<PlainTextSectionChunk>().ToList();
		_ = sections.Should().HaveCount(2);
		_ = sections[0].HeadingLevel.Should().Be(1);
		_ = sections[0].HeadingType.Should().Be(HeadingHeuristic.Prefixed);
		_ = sections[1].HeadingLevel.Should().Be(2);
	}

	[Fact]
	public async Task ChunkAsync_WithShortAllCaps_ShouldNotDetectAsHeading()
	{
		// Arrange - "USA" is too short to be a heading
		var text = "This mentions USA in the text.\n\nMore content.";
		var stream = new MemoryStream(Encoding.UTF8.GetBytes(text));
		var options = new ChunkingOptions();

		// Act
		var result = await _chunker.ChunkAsync(stream, options);

		// Assert
		var sections = result.Chunks.OfType<PlainTextSectionChunk>().ToList();
		_ = sections.Should().BeEmpty(); // Should not detect "USA" as heading
	}

	[Fact]
	public async Task ChunkAsync_WithLongAllCaps_ShouldNotDetectAsHeading()
	{
		// Arrange - Line longer than 100 chars
		var text = "THIS IS A VERY LONG LINE THAT EXCEEDS ONE HUNDRED CHARACTERS AND SHOULD NOT BE DETECTED AS A HEADING BECAUSE IT IS TOO LONG\n\nContent.";
		var stream = new MemoryStream(Encoding.UTF8.GetBytes(text));
		var options = new ChunkingOptions();

		// Act
		var result = await _chunker.ChunkAsync(stream, options);

		// Assert
		var sections = result.Chunks.OfType<PlainTextSectionChunk>().ToList();
		_ = sections.Should().BeEmpty();
	}

	#endregion

	#region Paragraph Detection Tests

	[Fact]
	public async Task ChunkAsync_WithParagraphs_ShouldCreateParagraphChunks()
	{
		// Arrange
		var text = "First paragraph.\n\nSecond paragraph.\n\nThird paragraph.";
		var stream = new MemoryStream(Encoding.UTF8.GetBytes(text));
		var options = new ChunkingOptions();

		// Act
		var result = await _chunker.ChunkAsync(stream, options);

		// Assert
		var paragraphs = result.Chunks.OfType<PlainTextParagraphChunk>().ToList();
		_ = paragraphs.Should().HaveCount(3);
		_ = paragraphs[0].Content.Should().Contain("First");
		_ = paragraphs[1].Content.Should().Contain("Second");
		_ = paragraphs[2].Content.Should().Contain("Third");
	}

	[Fact]
	public async Task ChunkAsync_WithMultiLineParagraph_ShouldCombineLines()
	{
		// Arrange
		var text = "This is a paragraph\nthat spans multiple\nlines without blank lines.\n\nNext paragraph.";
		var stream = new MemoryStream(Encoding.UTF8.GetBytes(text));
		var options = new ChunkingOptions();

		// Act
		var result = await _chunker.ChunkAsync(stream, options);

		// Assert
		var paragraphs = result.Chunks.OfType<PlainTextParagraphChunk>().ToList();
		_ = paragraphs.Should().HaveCount(2);
		_ = paragraphs[0].Content.Should().Contain("multiple lines");
	}

	[Fact]
	public async Task ChunkAsync_WithSingleParagraph_ShouldCreateOneChunk()
	{
		// Arrange
		var text = "This is a single paragraph with no line breaks or additional content.";
		var stream = new MemoryStream(Encoding.UTF8.GetBytes(text));
		var options = new ChunkingOptions();

		// Act
		var result = await _chunker.ChunkAsync(stream, options);

		// Assert
		var paragraphs = result.Chunks.OfType<PlainTextParagraphChunk>().ToList();
		_ = paragraphs.Should().ContainSingle();
	}

	#endregion

	#region List Detection Tests

	[Fact]
	public async Task ChunkAsync_WithBulletList_ShouldDetectListItems()
	{
		// Arrange
		var text = "Items:\n- First item\n- Second item\n- Third item";
		var stream = new MemoryStream(Encoding.UTF8.GetBytes(text));
		var options = new ChunkingOptions();

		// Act
		var result = await _chunker.ChunkAsync(stream, options);

		// Assert
		var listItems = result.Chunks.OfType<PlainTextListItemChunk>().ToList();
		_ = listItems.Should().HaveCount(3);
		_ = listItems.Should().AllSatisfy(item =>
		{
			_ = item.ListType.Should().Be("bullet");
			_ = item.Marker.Should().Be("-");
			_ = item.IsOrdered.Should().BeFalse();
		});
	}

	[Fact]
	public async Task ChunkAsync_WithNumberedList_ShouldDetectListItems()
	{
		// Arrange
		var text = "Steps:\n1. First step\n2. Second step\n3. Third step";
		var stream = new MemoryStream(Encoding.UTF8.GetBytes(text));
		var options = new ChunkingOptions();

		// Act
		var result = await _chunker.ChunkAsync(stream, options);

		// Assert
		var listItems = result.Chunks.OfType<PlainTextListItemChunk>().ToList();
		_ = listItems.Should().HaveCount(3);
		_ = listItems.Should().AllSatisfy(item =>
		{
			_ = item.ListType.Should().Be("numbered");
			_ = item.IsOrdered.Should().BeTrue();
		});
	}

	[Fact]
	public async Task ChunkAsync_WithAsteriskBullets_ShouldDetectListItems()
	{
		// Arrange
		var text = "* First\n* Second\n* Third";
		var stream = new MemoryStream(Encoding.UTF8.GetBytes(text));
		var options = new ChunkingOptions();

		// Act
		var result = await _chunker.ChunkAsync(stream, options);

		// Assert
		var listItems = result.Chunks.OfType<PlainTextListItemChunk>().ToList();
		_ = listItems.Should().HaveCount(3);
		_ = listItems.Should().AllSatisfy(item => item.Marker.Should().Be("*"));
	}

	[Fact]
	public async Task ChunkAsync_WithUnicodeBullets_ShouldDetectListItems()
	{
		// Arrange
		var text = "• First\n• Second\n• Third";
		var stream = new MemoryStream(Encoding.UTF8.GetBytes(text));
		var options = new ChunkingOptions();

		// Act
		var result = await _chunker.ChunkAsync(stream, options);

		// Assert
		var listItems = result.Chunks.OfType<PlainTextListItemChunk>().ToList();
		_ = listItems.Should().HaveCount(3);
		_ = listItems.Should().AllSatisfy(item => item.Marker.Should().Be("•"));
	}

	[Fact]
	public async Task ChunkAsync_WithNestedList_ShouldDetectNestingLevels()
	{
		// Arrange
		var text = "- Level 0\n  - Level 1\n    - Level 2";
		var stream = new MemoryStream(Encoding.UTF8.GetBytes(text));
		var options = new ChunkingOptions();

		// Act
		var result = await _chunker.ChunkAsync(stream, options);

		// Assert
		var listItems = result.Chunks.OfType<PlainTextListItemChunk>().ToList();
		_ = listItems.Should().HaveCount(3);
		_ = listItems[0].NestingLevel.Should().Be(0);
		_ = listItems[1].NestingLevel.Should().Be(1); // 2 spaces = level 1
		_ = listItems[2].NestingLevel.Should().Be(2); // 4 spaces = level 2
	}

	#endregion

	#region Code Block Detection Tests

	[Fact]
	public async Task ChunkAsync_WithFencedCodeBlock_ShouldDetectCode()
	{
		// Arrange
		var text = "Text before\n\n```csharp\npublic class Test {}\n```\n\nText after";
		var stream = new MemoryStream(Encoding.UTF8.GetBytes(text));
		var options = new ChunkingOptions();

		// Act
		var result = await _chunker.ChunkAsync(stream, options);

		// Assert
		var codeBlocks = result.Chunks.OfType<PlainTextCodeBlockChunk>().ToList();
		_ = codeBlocks.Should().ContainSingle();
		_ = codeBlocks[0].IsFenced.Should().BeTrue();
		_ = codeBlocks[0].Language.Should().Be("csharp");
		_ = codeBlocks[0].Content.Should().Contain("public class Test");
	}

	[Fact]
	public async Task ChunkAsync_WithFencedCodeBlockNoLanguage_ShouldDetectCode()
	{
		// Arrange
		var text = "```\nsome code\n```";
		var stream = new MemoryStream(Encoding.UTF8.GetBytes(text));
		var options = new ChunkingOptions();

		// Act
		var result = await _chunker.ChunkAsync(stream, options);

		// Assert
		var codeBlocks = result.Chunks.OfType<PlainTextCodeBlockChunk>().ToList();
		_ = codeBlocks.Should().ContainSingle();
		_ = codeBlocks[0].IsFenced.Should().BeTrue();
		_ = codeBlocks[0].Language.Should().BeNull();
	}

	[Fact]
	public async Task ChunkAsync_WithIndentedCode_ShouldDetectCode()
	{
		// Arrange
		var text = "Normal text\n\n    public class Test\n    {\n      // code\n    }\n\nBack to normal";
		var stream = new MemoryStream(Encoding.UTF8.GetBytes(text));
		var options = new ChunkingOptions();

		// Act
		var result = await _chunker.ChunkAsync(stream, options);

		// Assert
		var codeBlocks = result.Chunks.OfType<PlainTextCodeBlockChunk>().ToList();
		_ = codeBlocks.Should().ContainSingle();
		_ = codeBlocks[0].IsFenced.Should().BeFalse();
		_ = codeBlocks[0].IndentationLevel.Should().Be(4);
	}

	[Fact]
	public async Task ChunkAsync_WithCodeIndicators_ShouldPopulateIndicators()
	{
		// Arrange
		var text = "    public class Test\n    {\n        public void Method() { }\n    }";
		var stream = new MemoryStream(Encoding.UTF8.GetBytes(text));
		var options = new ChunkingOptions();

		// Act
		var result = await _chunker.ChunkAsync(stream, options);

		// Assert
		var codeBlocks = result.Chunks.OfType<PlainTextCodeBlockChunk>().ToList();
		_ = codeBlocks.Should().ContainSingle();
		_ = codeBlocks[0].CodeIndicators.Should().Contain("public");
		_ = codeBlocks[0].CodeIndicators.Should().Contain("class");
	}

	#endregion

	#region Hierarchy Tests

	[Fact]
	public async Task ChunkAsync_WithNestedHeadings_ShouldBuildHierarchy()
	{
		// Arrange
		var text = "Main\n====\n\nContent 1\n\nSub\n---\n\nContent 2";
		var stream = new MemoryStream(Encoding.UTF8.GetBytes(text));
		var options = new ChunkingOptions();

		// Act
		var result = await _chunker.ChunkAsync(stream, options);

		// Assert
		var sections = result.Chunks.OfType<PlainTextSectionChunk>().ToList();
		_ = sections.Should().HaveCount(2);
		
		var mainSection = sections.First(s => s.HeadingLevel == 1);
		var subSection = sections.First(s => s.HeadingLevel == 2);

		_ = mainSection.ParentId.Should().BeNull();
		_ = subSection.ParentId.Should().Be(mainSection.Id);
	}

	[Fact]
	public async Task ChunkAsync_WithContentUnderHeading_ShouldAssignParent()
	{
		// Arrange
		var text = "Heading\n=======\n\nParagraph content.";
		var stream = new MemoryStream(Encoding.UTF8.GetBytes(text));
		var options = new ChunkingOptions();

		// Act
		var result = await _chunker.ChunkAsync(stream, options);

		// Assert
		var section = result.Chunks.OfType<PlainTextSectionChunk>().First();
		var paragraph = result.Chunks.OfType<PlainTextParagraphChunk>().First();

		_ = paragraph.ParentId.Should().Be(section.Id);
	}

	#endregion

	#region Quality Metrics Tests

	[Fact]
	public async Task ChunkAsync_ShouldPopulateQualityMetrics()
	{
		// Arrange
		var text = "This is a paragraph with some content.";
		var stream = new MemoryStream(Encoding.UTF8.GetBytes(text));
		var options = new ChunkingOptions();

		// Act
		var result = await _chunker.ChunkAsync(stream, options);

		// Assert
		var chunk = result.Chunks.First();
		_ = chunk.QualityMetrics.Should().NotBeNull();
		_ = chunk.QualityMetrics!.CharacterCount.Should().BePositive();
		_ = chunk.QualityMetrics.WordCount.Should().BePositive();
		_ = chunk.QualityMetrics.TokenCount.Should().BePositive();
	}

	#endregion

	#region Metadata Tests

	[Fact]
	public async Task ChunkAsync_ShouldPopulateMetadata()
	{
		// Arrange
		var text = "Test content.";
		var stream = new MemoryStream(Encoding.UTF8.GetBytes(text));
		var options = new ChunkingOptions();

		// Act
		var result = await _chunker.ChunkAsync(stream, options);

		// Assert
		_ = result.Chunks.Should().AllSatisfy(chunk =>
		{
			_ = chunk.Metadata.Should().NotBeNull();
			_ = chunk.Metadata.DocumentType.Should().Be("PlainText");
			_ = chunk.SequenceNumber.Should().BeGreaterThanOrEqualTo(0);
		});
	}

	#endregion

	#region Statistics Tests

	[Fact]
	public async Task ChunkAsync_ShouldCalculateStatistics()
	{
		// Arrange
		var text = "Heading\n=======\n\nParagraph 1.\n\nParagraph 2.";
		var stream = new MemoryStream(Encoding.UTF8.GetBytes(text));
		var options = new ChunkingOptions();

		// Act
		var result = await _chunker.ChunkAsync(stream, options);

		// Assert
		_ = result.Statistics.Should().NotBeNull();
		_ = result.Statistics.TotalChunks.Should().BePositive();
		_ = result.Statistics.StructuralChunks.Should().BePositive();
		_ = result.Statistics.ContentChunks.Should().BePositive();
		_ = result.Statistics.TotalTokens.Should().BePositive();
	}

	#endregion

	#region Edge Cases

	[Fact]
	public async Task ChunkAsync_WithEmptyDocument_ShouldReturnEmptyResult()
	{
		// Arrange
		var stream = new MemoryStream(Encoding.UTF8.GetBytes(""));
		var options = new ChunkingOptions();

		// Act
		var result = await _chunker.ChunkAsync(stream, options);

		// Assert
		_ = result.Success.Should().BeTrue();
		_ = result.Chunks.Should().BeEmpty();
	}

	[Fact]
	public async Task ChunkAsync_WithOnlyWhitespace_ShouldReturnEmptyResult()
	{
		// Arrange
		var stream = new MemoryStream(Encoding.UTF8.GetBytes("   \n\n   \n"));
		var options = new ChunkingOptions();

		// Act
		var result = await _chunker.ChunkAsync(stream, options);

		// Assert
		_ = result.Success.Should().BeTrue();
		_ = result.Chunks.Should().BeEmpty();
	}

	#endregion
}
