using AwesomeAssertions;
using PanoramicData.Chunker.Configuration;
using PanoramicData.Chunker.Infrastructure;
using PanoramicData.Chunker.Interfaces;
using PanoramicData.Chunker.Models;
using System.Text;

namespace PanoramicData.Chunker.Tests.Unit.Chunkers;

/// <summary>
/// Contract tests shared by the Office (DOCX and PPTX) chunkers.
/// </summary>
public abstract class OfficeChunkerContractTests(
	ITestOutputHelper output,
	Func<ITokenCounter, IDocumentChunker> createChunker,
	string testDataFolder,
	string simpleFileName) : BaseTest(output)
{
	protected DocumentType SupportedType => createChunker(new CharacterBasedTokenCounter()).SupportedType;

	[Fact]
	public void Constructor_ShouldThrowArgumentNullException_WhenTokenCounterIsNull()
	{
		var act = () => createChunker(null!);

		_ = act.Should().Throw<ArgumentNullException>().WithParameterName("tokenCounter");
	}

	[Fact]
	public async Task ChunkAsync_ShouldCalculateTokenCounts()
	{
		var result = await ChunkSimpleAsync(new ChunkingOptions());

		_ = result.Success.Should().BeTrue();
		_ = result.Chunks.Should().NotBeEmpty();
		_ = MetricChunks(result).Should().AllSatisfy(chunk =>
		{
			_ = chunk.QualityMetrics.Should().NotBeNull();
			_ = chunk.QualityMetrics!.TokenCount.Should().BePositive();
			_ = chunk.QualityMetrics.CharacterCount.Should().BePositive();
		});
	}

	[Fact]
	public async Task ChunkAsync_ShouldBuildHierarchy()
	{
		var result = await ChunkSimpleAsync(new ChunkingOptions());

		_ = result.Success.Should().BeTrue();
		_ = result.Statistics.Should().NotBeNull();
		_ = result.Statistics.MaxDepth.Should().BeGreaterThanOrEqualTo(0);
		_ = result.Chunks.Where(c => c.ParentId.HasValue).Should()
			.AllSatisfy(child => _ = result.Chunks.Should().Contain(c => c.Id == child.ParentId, $"child chunk {child.Id} should have valid parent"));
	}

	[Fact]
	public async Task ChunkAsync_ShouldGenerateStatistics()
	{
		var result = await ChunkSimpleAsync(new ChunkingOptions());

		_ = result.Statistics.Should().NotBeNull();
		_ = result.Statistics.TotalChunks.Should().Be(result.Chunks.Count);
		_ = result.Statistics.StructuralChunks.Should().BeGreaterThanOrEqualTo(0);
		_ = result.Statistics.ContentChunks.Should().BeGreaterThanOrEqualTo(0);
		_ = result.Statistics.ProcessingTime.Should().BeGreaterThan(TimeSpan.Zero);
		_ = result.Statistics.TotalTokens.Should().BePositive();
	}

	[Fact]
	public async Task ChunkAsync_WithValidation_ShouldValidateChunks()
	{
		var result = await ChunkSimpleAsync(new ChunkingOptions { ValidateChunks = true });

		_ = result.Success.Should().BeTrue();
		_ = result.ValidationResult.Should().NotBeNull();
		_ = result.ValidationResult!.Issues.Should().NotContain(i => i.Severity == ValidationSeverity.Error, "there should be no error-level validation issues");
	}

	protected virtual IEnumerable<ChunkerBase> MetricChunks(ChunkingResult result) => result.Chunks;

	protected Task<ChunkingResult> ChunkSimpleAsync(ChunkingOptions options) => ChunkFixtureAsync(simpleFileName, options);

	protected Task<ChunkingResult> ChunkFixtureAsync(string fileName, ChunkingOptions options)
		=> ChunkWithAsync(createChunker(new CharacterBasedTokenCounter()), fileName, options);

	protected async Task<ChunkingResult> ChunkWithAsync(IDocumentChunker chunker, string fileName, ChunkingOptions options)
	{
		await using var stream = OpenFixture(fileName);
		return await chunker.ChunkAsync(stream, options, CancellationToken);
	}

	protected async Task<bool> CanHandleFixtureAsync(string fileName)
	{
		await using var stream = OpenFixture(fileName);
		return await createChunker(new CharacterBasedTokenCounter()).CanHandleAsync(stream, CancellationToken);
	}

	protected async Task<bool> CanHandleTextAsync(string text)
	{
		await using var stream = new MemoryStream(Encoding.UTF8.GetBytes(text));
		return await createChunker(new CharacterBasedTokenCounter()).CanHandleAsync(stream, CancellationToken);
	}

	protected FileStream OpenFixture(string fileName)
	{
		var path = Path.Combine("TestData", testDataFolder, fileName);
		_ = File.Exists(path).Should().BeTrue($"test data file '{path}' must exist");
		return File.OpenRead(path);
	}
}
