using AwesomeAssertions;
using PanoramicData.Chunker.Chunkers.Markdown;
using PanoramicData.Chunker.Extensions;
using PanoramicData.Chunker.Models;

namespace PanoramicData.Chunker.Tests.Unit.Extensions;

public class ChunkConversionExtensionsTests
{
	private static MarkdownParagraphChunk Paragraph(string content, int sequence = 0) => new()
	{
		Content = content,
		SequenceNumber = sequence,
		SpecificType = "paragraph"
	};

	private static MarkdownSectionChunk Section(string text, int level, int sequence = 0) => new()
	{
		HeadingText = text,
		HeadingLevel = level,
		MarkdownSyntax = $"{new string('#', level)} {text}",
		SequenceNumber = sequence,
		SpecificType = $"Heading{level}"
	};

	[Fact]
	public void ToPlainText_UsesContentOrHeadingText()
	{
		_ = Paragraph("Hello").ToPlainText().Should().Be("Hello");
		_ = Section("Title", 1).ToPlainText().Should().Be("Title");
	}

	[Fact]
	public void ToPlainText_JoinsNonEmptyChunksWithTheSeparator()
	{
		var chunks = new ChunkerBase[] { Paragraph("one"), Paragraph("  "), Paragraph("two") };

		_ = chunks.ToPlainText().Should().Be("one\n\ntwo");
		_ = chunks.ToPlainText(" | ").Should().Be("one | two");
	}

	[Fact]
	public void ToMarkdown_PrefersMarkdownContentAndFallsBackToContent()
	{
		var withMarkdown = Paragraph("plain");
		withMarkdown.MarkdownContent = "**rich**";

		_ = withMarkdown.ToMarkdown().Should().Be("**rich**");
		_ = Paragraph("plain").ToMarkdown().Should().Be("plain");
		_ = Section("Title", 2).ToMarkdown().Should().Be("## Title");
	}

	[Fact]
	public void ToMarkdown_OrdersBySequenceAndIndentsByDepthWhenAsked()
	{
		var child = Paragraph("child", 1);
		child.Depth = 2;
		var chunks = new ChunkerBase[] { child, Section("Title", 1) };

		_ = chunks.ToMarkdown().Should().Be("# Title\n\nchild".Replace("\n", Environment.NewLine));
		_ = chunks.ToMarkdown(includeHierarchy: true).Should().Contain("    child");
	}

	[Fact]
	public void ToHtml_EncodesTextAndUsesHeadingTags()
	{
		_ = Paragraph("Fish & <chips>").ToHtml().Should().Be("<p>Fish &amp; &lt;chips&gt;</p>");
		_ = Section("Title", 3).ToHtml().Should().Be("<h3>Title</h3>");
	}

	[Fact]
	public void ToHtml_CanWrapChunksInADocument()
	{
		var html = new ChunkerBase[] { Paragraph("one") }.ToHtml(wrapInDocument: true);

		_ = html.Should().StartWith("<!DOCTYPE html>").And.Contain("<p>one</p>").And.Contain("</html>");
	}

	[Theory]
	[InlineData("short", 100, "short")]
	[InlineData("a long sentence that will not fit", 10, "a long ...")]
	public void ToSummary_TruncatesLongText(string text, int maxLength, string expected)
	{
		_ = Paragraph(text).ToSummary(maxLength).Should().Be(expected);
	}

	[Fact]
	public void ToSummary_NamesTheTypeWhenThereIsNoText()
	{
		_ = Paragraph(string.Empty).ToSummary().Should().Be("[paragraph]");
	}

	[Fact]
	public void ToTreeString_ListsChildrenUnderTheirParents()
	{
		var section = Section("Title", 1);
		var child = Paragraph("body", 1);
		child.ParentId = section.Id;

		var tree = new ChunkerBase[] { section, child }.ToTreeString();

		_ = tree.Should().Contain("[Heading1] Title").And.Contain("  [paragraph] body");
	}

	[Fact]
	public void ExtractAllText_IncludesDescendants()
	{
		var section = Section("Title", 1);
		var child = Paragraph("body", 1);
		child.ParentId = section.Id;

		_ = section.ExtractAllText([section, child]).Should().Contain("Title").And.Contain("body");
	}
}