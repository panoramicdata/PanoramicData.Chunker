using AwesomeAssertions;
using PanoramicData.Chunker.Chunkers.Pdf;
using PanoramicData.Chunker.Configuration;
using PanoramicData.Chunker.Infrastructure;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace PanoramicData.Chunker.Tests.Unit.Chunkers;

/// <summary>
/// Separately positioned text blocks must not be fused into one token.
/// </summary>
public class PdfDocumentChunkerTextJoiningTests
{
	private static readonly CancellationToken _cancellationToken = TestContext.Current.CancellationToken;

	private static byte[] BuildHandbookPdf()
	{
		QuestPDF.Settings.License = LicenseType.Community;

		return Document.Create(container => container.Page(page =>
		{
			page.Size(PageSizes.A4);
			page.Margin(40);
			page.Header().Text("Contoso Staff Handbook Page 1 of 4");
			page.Content().Column(column =>
			{
				column.Spacing(10);
				column.Item().Text("Section 1").SemiBold();
				column.Item().Width(150).Text("Expenses must be submitted within thirty days and over ten pounds need approval.");
			});
			page.Footer().Text("Document reference HB-1");
		})).GeneratePdf();
	}

	private static async Task<string> ChunkTextAsync()
	{
		using var stream = new MemoryStream(BuildHandbookPdf());
		var chunker = new PdfDocumentChunker(new CharacterBasedTokenCounter());

		var result = await chunker.ChunkAsync(stream, new ChunkingOptions(), _cancellationToken);

		_ = result.Success.Should().BeTrue();
		return string.Join("\n", result.Chunks.OfType<PdfParagraphChunk>().Select(chunk => chunk.Content));
	}

	[Fact]
	public async Task ChunkAsync_ShouldNotFuseWordsAcrossSeparateBlocks()
	{
		var text = await ChunkTextAsync();

		_ = text.Should().Contain("4").And.NotContain("4Section").And.NotContain("1Expenses");
		_ = text.Should().NotContain("pounds.Document");
	}

	[Fact]
	public async Task ChunkAsync_ShouldSeparateWrappedLinesWithWhitespace()
	{
		var text = await ChunkTextAsync();

		_ = text.Split([' ', '\n', '\r'], StringSplitOptions.RemoveEmptyEntries)
			.Should().Contain(["Section", "1", "Expenses", "approval.", "Document"]);
		_ = text.Should().NotContain("overten").And.NotContain("daysand");
	}
}
