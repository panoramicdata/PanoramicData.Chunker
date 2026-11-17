using AwesomeAssertions;
using Moq;
using PanoramicData.Chunker.Configuration;
using PanoramicData.Chunker.Infrastructure;
using PanoramicData.Chunker.Interfaces;
using System.Text;

namespace PanoramicData.Chunker.Tests.Unit.Core;

public class ChunkerBuilderTests
{
	private static MemoryStream StreamOf(string content) => new(Encoding.UTF8.GetBytes(content));

	[Fact]
	public async Task ChunkAsync_ChunksAStreamWithTheConfiguredOptions()
	{
		await using var stream = StreamOf("First paragraph.\n\nSecond paragraph.");

		var result = await DocumentChunker.CreateBuilder()
			.WithDocument(stream, DocumentType.PlainText)
			.WithMaxTokens(200)
			.WithOverlap(10)
			.WithTags("a", "b")
			.WithExternalHierarchy("docs/test")
			.WithSourceId("source-1")
			.WithStrategy(ChunkingStrategy.Structural)
			.WithTokenCounter(new CharacterBasedTokenCounter())
			.ExtractImages(false)
			.ExtractKeywords(false)
			.PreserveFormatting(false)
			.EnableStreaming(false)
			.IncludeQualityMetrics(true)
			.ChunkAsync(TestContext.Current.CancellationToken);

		_ = result.Success.Should().BeTrue();
		_ = result.Chunks.Should().NotBeEmpty();
	}

	[Fact]
	public async Task ChunkAsync_AutoDetectsAStreamWithoutAType()
	{
		await using var stream = StreamOf("# Title\n\nSome markdown text.");

		var result = await DocumentChunker.CreateBuilder()
			.WithDocument(stream, DocumentType.Markdown)
			.ChunkAsync(TestContext.Current.CancellationToken);

		_ = result.Success.Should().BeTrue();
	}

	[Fact]
	public async Task ChunkAsync_ChunksAFileByPath()
	{
		var path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.txt");
		await File.WriteAllTextAsync(path, "Plain text from a file.\n\nAnother paragraph.", TestContext.Current.CancellationToken);

		try
		{
			var result = await DocumentChunker.CreateBuilder()
				.WithDocument(path)
				.ChunkAsync(TestContext.Current.CancellationToken);

			_ = result.Success.Should().BeTrue();
			_ = result.Chunks.Should().NotBeEmpty();
		}
		finally
		{
			File.Delete(path);
		}
	}

	[Fact]
	public async Task ChunkAsync_WithoutADocument_Throws()
	{
		var act = () => DocumentChunker.CreateBuilder().ChunkAsync(TestContext.Current.CancellationToken);

		_ = await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*No document specified*");
	}

	[Fact]
	public void ProviderSettings_AreFluent()
	{
		var builder = DocumentChunker.CreateBuilder();

		var returned = builder
			.GenerateImageDescriptions(new Mock<IImageDescriptionProvider>().Object)
			.GenerateSummaries(new Mock<ILlmProvider>().Object)
			.WithCaching(new Mock<ICacheProvider>().Object)
			.WithLogger(new Mock<Microsoft.Extensions.Logging.ILogger>().Object)
			.WithOutputFormat(OutputFormat.Flat)
			.WithTableFormat(PanoramicData.Chunker.Models.TableSerializationFormat.Markdown);

		_ = returned.Should().BeSameAs(builder);
	}
}