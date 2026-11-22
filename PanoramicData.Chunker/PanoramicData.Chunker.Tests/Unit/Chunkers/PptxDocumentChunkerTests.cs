using AwesomeAssertions;
using PanoramicData.Chunker.Chunkers.Pptx;
using PanoramicData.Chunker.Configuration;
using PanoramicData.Chunker.Models;

namespace PanoramicData.Chunker.Tests.Unit.Chunkers;

/// <summary>
/// Unit tests for PptxDocumentChunker.
/// Tests fail with a message naming the file when a test data file is missing.
/// </summary>
public class PptxDocumentChunkerTests(ITestOutputHelper output)
	: OfficeChunkerContractTests(output, tokenCounter => new PptxDocumentChunker(tokenCounter), "Pptx", "simple.pptx")
{
	protected override IEnumerable<ChunkerBase> MetricChunks(ChunkingResult result) => result.Chunks.OfType<ContentChunk>().Take(10);

	[Fact]
	public void SupportedType_ShouldReturnPptx()
	{
		_ = SupportedType.Should().Be(DocumentType.Pptx);
	}

	[Fact]
	public async Task CanHandleAsync_ShouldReturnTrue_ForPptxFile()
	{
		_ = (await CanHandleFixtureAsync("simple.pptx")).Should().BeTrue("PPTX chunker should handle .pptx files");
	}

	[Fact]
	public async Task CanHandleAsync_ShouldReturnFalse_ForNonPptxContent()
	{
		_ = (await CanHandleTextAsync("This is plain text, not PPTX")).Should().BeFalse("PPTX chunker should reject non-PPTX content");
	}

	[Fact]
	public async Task ChunkAsync_ShouldExtractSlides_FromSimplePptx()
	{
		var result = await ChunkSimpleAsync(new ChunkingOptions { MaxTokens = 500 });

		_ = result.Success.Should().BeTrue();
		_ = result.Chunks.Should().NotBeEmpty();

		var slides = result.Chunks.OfType<PptxSlideChunk>().ToList();
		_ = slides.Should().NotBeEmpty("simple.pptx should have at least one slide");
		_ = slides.Should().HaveCountGreaterThanOrEqualTo(3, "simple.pptx should have 3-5 slides");
		_ = slides.Should().HaveCountLessThanOrEqualTo(5, "simple.pptx should have 3-5 slides");
		_ = slides.Should().AllSatisfy(slide =>
		{
			_ = slide.SlideNumber.Should().BePositive();
			_ = slide.ShapeCount.Should().BeGreaterThanOrEqualTo(0);
		});
	}

	[Fact]
	public async Task ChunkAsync_ShouldExtractTitles_FromSimplePptx()
	{
		var result = await ChunkSimpleAsync(new ChunkingOptions { MaxTokens = 500 });

		_ = result.Success.Should().BeTrue();
		_ = result.Chunks.OfType<PptxTitleChunk>().Should().AllSatisfy(title =>
		{
			_ = title.Content.Should().NotBeNullOrWhiteSpace("titles should have content");
			_ = title.SlideNumber.Should().BePositive();
			_ = title.SpecificType.Should().BeOneOf("title", "subtitle");
		});
	}

	[Fact]
	public async Task ChunkAsync_ShouldExtractContent_FromSimplePptx()
	{
		var result = await ChunkSimpleAsync(new ChunkingOptions { MaxTokens = 500 });

		_ = result.Success.Should().BeTrue();
		_ = result.Chunks.OfType<PptxContentChunk>().Take(10).Should().AllSatisfy(content =>
		{
			_ = content.Content.Should().NotBeNullOrWhiteSpace();
			_ = content.SlideNumber.Should().BePositive();
		});
	}

	[Fact]
	public async Task ChunkAsync_ShouldExtractNotes_FromPptxWithNotes()
	{
		var result = await ChunkFixtureAsync("with-notes.pptx", new ChunkingOptions());

		_ = result.Success.Should().BeTrue();
		_ = result.Chunks.OfType<PptxNotesChunk>().Should().AllSatisfy(note =>
		{
			_ = note.Content.Should().NotBeNullOrWhiteSpace("notes should have content");
			_ = note.SlideNumber.Should().BePositive();
			_ = note.NotesLength.Should().BePositive();
			_ = note.SpecificType.Should().Be("notes");
		});
	}

	[Fact]
	public async Task ChunkAsync_ShouldExtractTables_FromPptxWithTables()
	{
		var result = await ChunkFixtureAsync("with-tables.pptx", new ChunkingOptions());

		_ = result.Success.Should().BeTrue();
		_ = result.Chunks.OfType<PptxTableChunk>().Should().AllSatisfy(table =>
		{
			_ = table.SerializedTable.Should().NotBeNullOrWhiteSpace("table should be serialized");
			_ = table.SerializationFormat.Should().Be(TableSerializationFormat.Markdown);
			_ = table.TableInfo.Should().NotBeNull();
			_ = table.TableInfo!.RowCount.Should().BePositive();
			_ = table.TableInfo.ColumnCount.Should().BePositive();
		});
	}

	[Fact]
	public async Task ChunkAsync_ShouldDetectImages_FromPptxWithImages()
	{
		var result = await ChunkFixtureAsync("with-images.pptx", new ChunkingOptions());

		_ = result.Success.Should().BeTrue();
		_ = result.Chunks.OfType<PptxImageChunk>().Should().AllSatisfy(image =>
		{
			_ = image.VisualType.Should().NotBeNullOrWhiteSpace();
			_ = image.BinaryReference.Should().NotBeNullOrWhiteSpace();
			_ = image.SpecificType.Should().Be(image.VisualType);
		});
	}

	[Fact]
	public async Task ChunkAsync_ShouldHandleEmptyPresentation()
	{
		var result = await ChunkFixtureAsync("empty.pptx", new ChunkingOptions());

		_ = result.Success.Should().BeTrue("empty presentation should not cause errors");
		_ = result.Chunks.Count.Should().BeLessThanOrEqualTo(1, "empty presentation should have 0 or 1 chunks");
	}

	[Fact]
	public async Task ChunkAsync_ShouldProcessComplexPresentation()
	{
		var result = await ChunkFixtureAsync("complex.pptx", new ChunkingOptions { ValidateChunks = true });

		_ = result.Success.Should().BeTrue();

		var slides = result.Chunks.OfType<PptxSlideChunk>().Count();
		var others = result.Chunks.OfType<PptxTitleChunk>().Count()
			+ result.Chunks.OfType<PptxContentChunk>().Count()
			+ result.Chunks.OfType<PptxTableChunk>().Count()
			+ result.Chunks.OfType<PptxImageChunk>().Count()
			+ result.Chunks.OfType<PptxNotesChunk>().Count();

		_ = slides.Should().BePositive("complex.pptx should have slides");
		_ = others.Should().BePositive("complex.pptx should have content");
	}

	[Fact]
	public async Task ChunkAsync_ShouldProcessLargePresentation()
	{
		var startTime = DateTime.UtcNow;
		var result = await ChunkFixtureAsync("large.pptx", new ChunkingOptions());
		var duration = DateTime.UtcNow - startTime;

		_ = result.Success.Should().BeTrue();
		_ = duration.Should().BeLessThan(TimeSpan.FromSeconds(20), "large presentation should process in under 20 seconds");
		_ = result.Chunks.Count.Should().BeGreaterThan(50, "large.pptx should have many chunks");
	}
}
