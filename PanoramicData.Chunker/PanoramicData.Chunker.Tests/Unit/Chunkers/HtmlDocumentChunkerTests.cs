using FluentAssertions;
using PanoramicData.Chunker.Chunkers.Html;
using PanoramicData.Chunker.Configuration;
using PanoramicData.Chunker.Infrastructure;
using PanoramicData.Chunker.Models;
using System.Text;

namespace PanoramicData.Chunker.Tests.Unit.Chunkers;

public class HtmlDocumentChunkerTests
{
	private readonly HtmlDocumentChunker _chunker;

	public HtmlDocumentChunkerTests()
	{
		var tokenCounter = new CharacterBasedTokenCounter();
		_chunker = new HtmlDocumentChunker(tokenCounter);
	}

	[Fact]
	public async Task ChunkAsync_SimpleHtml_ShouldReturnChunks()
	{
		// Arrange
		var html = @"
			<!DOCTYPE html>
			<html>
			<head><title>Test</title></head>
			<body>
				<h1>Main Title</h1>
				<p>This is a paragraph.</p>
			</body>
			</html>";

		var stream = new MemoryStream(Encoding.UTF8.GetBytes(html));
		var options = new ChunkingOptions();

		// Act
		var result = await _chunker.ChunkAsync(stream, options);

		// Assert
		result.Should().NotBeNull();
		result.Success.Should().BeTrue();
		result.Chunks.Should().NotBeEmpty();
		result.Statistics.Should().NotBeNull();
	}

	[Fact]
	public async Task CanHandleAsync_HtmlContent_ShouldReturnTrue()
	{
		// Arrange
		var html = "<!DOCTYPE html><html><body><p>Test</p></body></html>";
		var stream = new MemoryStream(Encoding.UTF8.GetBytes(html));

		// Act
		var result = await _chunker.CanHandleAsync(stream);

		// Assert
		result.Should().BeTrue();
	}

	[Fact]
	public async Task CanHandleAsync_NonHtmlContent_ShouldReturnFalse()
	{
		// Arrange
		var content = "This is plain text without HTML tags.";
		var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));

		// Act
		var result = await _chunker.CanHandleAsync(stream);

		// Assert
		result.Should().BeFalse();
	}

	[Fact]
	public async Task ChunkAsync_WithHeaders_ShouldCreateStructuralChunks()
	{
		// Arrange
		var html = @"
			<!DOCTYPE html>
			<html>
			<body>
				<h1>Heading 1</h1>
				<p>Paragraph 1</p>
				<h2>Heading 2</h2>
				<p>Paragraph 2</p>
				<h3>Heading 3</h3>
				<p>Paragraph 3</p>
			</body>
			</html>";

		var stream = new MemoryStream(Encoding.UTF8.GetBytes(html));
		var options = new ChunkingOptions();

		// Act
		var result = await _chunker.ChunkAsync(stream, options);

		// Assert
		var sections = result.Chunks.OfType<HtmlSectionChunk>().ToList();
		sections.Should().HaveCount(3);
		sections[0].HeadingLevel.Should().Be(1);
		sections[1].HeadingLevel.Should().Be(2);
		sections[2].HeadingLevel.Should().Be(3);
	}

	[Fact]
	public async Task ChunkAsync_WithParagraphs_ShouldCreateContentChunks()
	{
		// Arrange
		var html = @"
			<!DOCTYPE html>
			<html>
			<body>
				<p>First paragraph</p>
				<p>Second paragraph</p>
				<p>Third paragraph</p>
			</body>
			</html>";

		var stream = new MemoryStream(Encoding.UTF8.GetBytes(html));
		var options = new ChunkingOptions();

		// Act
		var result = await _chunker.ChunkAsync(stream, options);

		// Assert
		var paragraphs = result.Chunks.OfType<HtmlParagraphChunk>().ToList();
		paragraphs.Should().HaveCount(3);
		paragraphs[0].Content.Should().Contain("First");
		paragraphs[1].Content.Should().Contain("Second");
		paragraphs[2].Content.Should().Contain("Third");
	}

	[Fact]
	public async Task ChunkAsync_WithLists_ShouldCreateListItemChunks()
	{
		// Arrange
		var html = @"
			<!DOCTYPE html>
			<html>
			<body>
				<ul>
					<li>Item 1</li>
					<li>Item 2</li>
					<li>Item 3</li>
				</ul>
			</body>
			</html>";

		var stream = new MemoryStream(Encoding.UTF8.GetBytes(html));
		var options = new ChunkingOptions();

		// Act
		var result = await _chunker.ChunkAsync(stream, options);

		// Assert
		var listItems = result.Chunks.OfType<HtmlListItemChunk>().ToList();
		listItems.Should().HaveCount(3);
		listItems.All(li => li.ListType == "ul").Should().BeTrue();
	}

	[Fact]
	public async Task ChunkAsync_WithNestedLists_ShouldDetectNestingLevel()
	{
		// Arrange
		var html = @"
			<!DOCTYPE html>
			<html>
			<body>
				<ul>
					<li>Item 1
						<ul>
							<li>Nested Item 1</li>
							<li>Nested Item 2</li>
						</ul>
					</li>
					<li>Item 2</li>
				</ul>
			</body>
			</html>";

		var stream = new MemoryStream(Encoding.UTF8.GetBytes(html));
		var options = new ChunkingOptions();

		// Act
		var result = await _chunker.ChunkAsync(stream, options);

		// Assert
		var listItems = result.Chunks.OfType<HtmlListItemChunk>().ToList();
		listItems.Should().Contain(li => li.NestingLevel == 0);
		listItems.Should().Contain(li => li.NestingLevel == 1);
	}

	[Fact]
	public async Task ChunkAsync_WithOrderedList_ShouldDetectListType()
	{
		// Arrange
		var html = @"
			<!DOCTYPE html>
			<html>
			<body>
				<ol>
					<li>First</li>
					<li>Second</li>
				</ol>
			</body>
			</html>";

		var stream = new MemoryStream(Encoding.UTF8.GetBytes(html));
		var options = new ChunkingOptions();

		// Act
		var result = await _chunker.ChunkAsync(stream, options);

		// Assert
		var listItems = result.Chunks.OfType<HtmlListItemChunk>().ToList();
		listItems.All(li => li.ListType == "ol").Should().BeTrue();
	}

	[Fact]
	public async Task ChunkAsync_WithCodeBlocks_ShouldCreateCodeChunks()
	{
		// Arrange
		var html = @"
			<!DOCTYPE html>
			<html>
			<body>
				<pre><code class=""language-csharp"">public class Test { }</code></pre>
			</body>
			</html>";

		var stream = new MemoryStream(Encoding.UTF8.GetBytes(html));
		var options = new ChunkingOptions();

		// Act
		var result = await _chunker.ChunkAsync(stream, options);

		// Assert
		var codeBlocks = result.Chunks.OfType<HtmlCodeBlockChunk>().ToList();
		codeBlocks.Should().HaveCount(1);
		codeBlocks[0].Language.Should().Be("csharp");
		codeBlocks[0].HasCodeElement.Should().BeTrue();
	}

	[Fact]
	public async Task ChunkAsync_WithBlockquote_ShouldCreateBlockquoteChunk()
	{
		// Arrange
		var html = @"
			<!DOCTYPE html>
			<html>
			<body>
				<blockquote cite=""https://example.com"">This is a quote.</blockquote>
			</body>
			</html>";

		var stream = new MemoryStream(Encoding.UTF8.GetBytes(html));
		var options = new ChunkingOptions();

		// Act
		var result = await _chunker.ChunkAsync(stream, options);

		// Assert
		var blockquotes = result.Chunks.OfType<HtmlBlockquoteChunk>().ToList();
		blockquotes.Should().HaveCount(1);
		blockquotes[0].Content.Should().Contain("quote");
		blockquotes[0].CiteUrl.Should().Be("https://example.com");
	}

	[Fact]
	public async Task ChunkAsync_WithTable_ShouldCreateTableChunk()
	{
		// Arrange
		var html = @"
			<!DOCTYPE html>
			<html>
			<body>
				<table>
					<thead>
						<tr><th>Name</th><th>Age</th></tr>
					</thead>
					<tbody>
						<tr><td>John</td><td>30</td></tr>
						<tr><td>Jane</td><td>25</td></tr>
					</tbody>
				</table>
			</body>
			</html>";

		var stream = new MemoryStream(Encoding.UTF8.GetBytes(html));
		var options = new ChunkingOptions();

		// Act
		var result = await _chunker.ChunkAsync(stream, options);

		// Assert
		var tables = result.Chunks.OfType<HtmlTableChunk>().ToList();
		tables.Should().HaveCount(1);
		var table = tables[0];
		table.TableInfo.RowCount.Should().Be(2);
		table.TableInfo.ColumnCount.Should().Be(2);
		table.TableInfo.Headers.Should().Contain("Name");
		table.TableInfo.Headers.Should().Contain("Age");
	}

	[Fact]
	public async Task ChunkAsync_WithImage_ShouldCreateImageChunk()
	{
		// Arrange
		var html = @"
			<!DOCTYPE html>
			<html>
			<body>
				<img src=""test.jpg"" alt=""Test image"" width=""100"" height=""50"">
			</body>
			</html>";

		var stream = new MemoryStream(Encoding.UTF8.GetBytes(html));
		var options = new ChunkingOptions();

		// Act
		var result = await _chunker.ChunkAsync(stream, options);

		// Assert
		var images = result.Chunks.OfType<HtmlImageChunk>().ToList();
		images.Should().HaveCount(1);
		var image = images[0];
		image.BinaryReference.Should().Be("test.jpg");
		image.Caption.Should().Be("Test image");
		image.Width.Should().Be(100);
		image.Height.Should().Be(50);
	}

	[Fact]
	public async Task ChunkAsync_WithLinks_ShouldExtractLinkAnnotations()
	{
		// Arrange
		var html = @"
			<!DOCTYPE html>
			<html>
			<body>
				<p>Visit <a href=""https://example.com"">our website</a> for more.</p>
			</body>
			</html>";

		var stream = new MemoryStream(Encoding.UTF8.GetBytes(html));
		var options = new ChunkingOptions();

		// Act
		var result = await _chunker.ChunkAsync(stream, options);

		// Assert
		var paragraph = result.Chunks.OfType<HtmlParagraphChunk>().FirstOrDefault();
		paragraph.Should().NotBeNull();
		paragraph!.Annotations.Should().NotBeNull();
		var linkAnnotation = paragraph.Annotations!.FirstOrDefault(a => a.Type == AnnotationType.Link);
		linkAnnotation.Should().NotBeNull();
		linkAnnotation!.Attributes.Should().ContainKey("href");
		linkAnnotation.Attributes!["href"].Should().Be("https://example.com");
	}

	[Fact]
	public async Task ChunkAsync_WithBoldText_ShouldExtractBoldAnnotations()
	{
		// Arrange
		var html = @"
			<!DOCTYPE html>
			<html>
			<body>
				<p>This is <strong>bold</strong> text.</p>
			</body>
			</html>";

		var stream = new MemoryStream(Encoding.UTF8.GetBytes(html));
		var options = new ChunkingOptions();

		// Act
		var result = await _chunker.ChunkAsync(stream, options);

		// Assert
		var paragraph = result.Chunks.OfType<HtmlParagraphChunk>().FirstOrDefault();
		paragraph.Should().NotBeNull();
		paragraph!.Annotations.Should().NotBeNull();
		var boldAnnotation = paragraph.Annotations!.FirstOrDefault(a => a.Type == AnnotationType.Bold);
		boldAnnotation.Should().NotBeNull();
	}

	[Fact]
	public async Task ChunkAsync_WithItalicText_ShouldExtractItalicAnnotations()
	{
		// Arrange
		var html = @"
			<!DOCTYPE html>
			<html>
			<body>
				<p>This is <em>italic</em> text.</p>
			</body>
			</html>";

		var stream = new MemoryStream(Encoding.UTF8.GetBytes(html));
		var options = new ChunkingOptions();

		// Act
		var result = await _chunker.ChunkAsync(stream, options);

		// Assert
		var paragraph = result.Chunks.OfType<HtmlParagraphChunk>().FirstOrDefault();
		paragraph.Should().NotBeNull();
		paragraph!.Annotations.Should().NotBeNull();
		var italicAnnotation = paragraph.Annotations!.FirstOrDefault(a => a.Type == AnnotationType.Italic);
		italicAnnotation.Should().NotBeNull();
	}

	[Fact]
	public async Task ChunkAsync_WithInlineCode_ShouldExtractCodeAnnotations()
	{
		// Arrange
		var html = @"
			<!DOCTYPE html>
			<html>
			<body>
				<p>Use <code>Console.WriteLine</code> to print.</p>
			</body>
			</html>";

		var stream = new MemoryStream(Encoding.UTF8.GetBytes(html));
		var options = new ChunkingOptions();

		// Act
		var result = await _chunker.ChunkAsync(stream, options);

		// Assert
		var paragraph = result.Chunks.OfType<HtmlParagraphChunk>().FirstOrDefault();
		paragraph.Should().NotBeNull();
		paragraph!.Annotations.Should().NotBeNull();
		var codeAnnotation = paragraph.Annotations!.FirstOrDefault(a => a.Type == AnnotationType.Code);
		codeAnnotation.Should().NotBeNull();
	}

	[Fact]
	public async Task ChunkAsync_WithSemanticElements_ShouldCreateStructuralChunks()
	{
		// Arrange
		var html = @"
			<!DOCTYPE html>
			<html>
			<body>
				<article>
					<p>Article content</p>
				</article>
				<section>
					<p>Section content</p>
				</section>
			</body>
			</html>";

		var stream = new MemoryStream(Encoding.UTF8.GetBytes(html));
		var options = new ChunkingOptions();

		// Act
		var result = await _chunker.ChunkAsync(stream, options);

		// Assert
		var sections = result.Chunks.OfType<HtmlSectionChunk>().ToList();
		sections.Should().Contain(s => s.TagName == "article");
		sections.Should().Contain(s => s.TagName == "section");
	}

	[Fact]
	public async Task ChunkAsync_ShouldFilterScriptAndStyleTags()
	{
		// Arrange
		var html = @"
			<!DOCTYPE html>
			<html>
			<head>
				<style>body { color: red; }</style>
			</head>
			<body>
				<p>Visible content</p>
				<script>console.log('hidden');</script>
			</body>
			</html>";

		var stream = new MemoryStream(Encoding.UTF8.GetBytes(html));
		var options = new ChunkingOptions();

		// Act
		var result = await _chunker.ChunkAsync(stream, options);

		// Assert
		result.Chunks.OfType<ContentChunk>().Should().NotContain(c => c.Content.Contains("console.log"));
		result.Chunks.OfType<ContentChunk>().Should().NotContain(c => c.Content.Contains("color: red"));
	}

	[Fact]
	public async Task ChunkAsync_WithHierarchy_ShouldBuildParentChildRelationships()
	{
		// Arrange
		var html = @"
			<!DOCTYPE html>
			<html>
			<body>
				<h1>Main Title</h1>
				<p>Paragraph under main</p>
				<h2>Sub Title</h2>
				<p>Paragraph under sub</p>
			</body>
			</html>";

		var stream = new MemoryStream(Encoding.UTF8.GetBytes(html));
		var options = new ChunkingOptions();

		// Act
		var result = await _chunker.ChunkAsync(stream, options);

		// Assert
		var h1 = result.Chunks.OfType<HtmlSectionChunk>().First(s => s.HeadingLevel == 1);
		var h2 = result.Chunks.OfType<HtmlSectionChunk>().First(s => s.HeadingLevel == 2);
		
		h1.ParentId.Should().BeNull();
		h2.ParentId.Should().Be(h1.Id);
	}

	[Fact]
	public async Task ChunkAsync_ShouldPopulateMetadata()
	{
		// Arrange
		var html = @"
			<!DOCTYPE html>
			<html>
			<body>
				<p>Test paragraph</p>
			</body>
			</html>";

		var stream = new MemoryStream(Encoding.UTF8.GetBytes(html));
		var options = new ChunkingOptions();

		// Act
		var result = await _chunker.ChunkAsync(stream, options);

		// Assert
		result.Chunks.Should().AllSatisfy(chunk =>
		{
			chunk.Metadata.Should().NotBeNull();
			chunk.Metadata.DocumentType.Should().Be("HTML");
			chunk.SequenceNumber.Should().BeGreaterThanOrEqualTo(0);
		});
	}

	[Fact]
	public async Task ChunkAsync_ShouldPopulateQualityMetrics()
	{
		// Arrange
		var html = @"
			<!DOCTYPE html>
			<html>
			<body>
				<p>Test paragraph with some content.</p>
			</body>
			</html>";

		var stream = new MemoryStream(Encoding.UTF8.GetBytes(html));
		var options = new ChunkingOptions();

		// Act
		var result = await _chunker.ChunkAsync(stream, options);

		// Assert
		var paragraph = result.Chunks.OfType<HtmlParagraphChunk>().First();
		paragraph.QualityMetrics.Should().NotBeNull();
		paragraph.QualityMetrics!.CharacterCount.Should().BeGreaterThan(0);
		paragraph.QualityMetrics.WordCount.Should().BeGreaterThan(0);
	}
}
