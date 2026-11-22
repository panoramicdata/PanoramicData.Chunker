using AwesomeAssertions;
using PanoramicData.Chunker.Configuration;
using PanoramicData.Chunker.Infrastructure;
using PanoramicData.Chunker.Interfaces;
using PanoramicData.Chunker.Models;
using PanoramicData.Chunker.Tests.Utilities;
using System.Text;

namespace PanoramicData.Chunker.Tests.Unit.Chunkers;

/// <summary>
/// Contract tests shared by binary-format chunkers that are exercised with generated test files.
/// </summary>
public abstract class FileBasedChunkerContractTests(
	ITestOutputHelper output,
	Func<ITokenCounter, IDocumentChunker> createChunker,
	DocumentType expectedType,
	string testDataFolder,
	string simpleFileName) : DocumentChunkerContractTests(output, createChunker, expectedType)
{
	private readonly IDocumentChunker _contractChunker = createChunker(new CharacterBasedTokenCounter());

	[Fact]
	public async Task CanHandleAsync_WithEmptyStream_ShouldReturnFalse()
	{
		using var stream = new MemoryStream();

		var result = await _contractChunker.CanHandleAsync(stream, CancellationToken);

		_ = result.Should().BeFalse();
	}

	[Fact]
	public async Task ChunkAsync_ShouldGenerateStatistics()
	{
		var result = await ChunkTestFileAsync(simpleFileName);

		ChunkAssert.HasStatistics(result);
	}

	[Fact]
	public async Task ChunkAsync_WithValidation_ShouldValidateChunks()
	{
		await using var stream = OpenTestFile(simpleFileName);

		var result = await _contractChunker.ChunkAsync(stream, new ChunkingOptions { ValidateChunks = true }, CancellationToken);

		ChunkAssert.IsValidated(result);
	}

	[Fact]
	public async Task ChunkAsync_ShouldCalculateQualityMetrics()
	{
		var result = await ChunkTestFileAsync(simpleFileName);

		_ = result.Chunks.Should().AllSatisfy(chunk =>
		{
			_ = chunk.QualityMetrics.Should().NotBeNull();
		});
	}

	protected async Task<ChunkingResult> ChunkSucceededAsync(string fileName)
	{
		var result = await ChunkTestFileAsync(fileName);
		ChunkAssert.Succeeded(result);
		return result;
	}

	protected async Task<ChunkingResult> ChunkTestFileAsync(string fileName)
	{
		await using var stream = OpenTestFile(fileName);
		return await _contractChunker.ChunkAsync(stream, new ChunkingOptions(), CancellationToken);
	}

	protected async Task<ChunkingResult> ChunkTextAsync(string content)
	{
		using var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));
		return await _contractChunker.ChunkAsync(stream, new ChunkingOptions(), CancellationToken);
	}

	private FileStream OpenTestFile(string fileName)
	{
		var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TestData", testDataFolder, fileName);
		_ = File.Exists(path).Should().BeTrue("test data file must exist: " + path);
		return File.OpenRead(path);
	}
}
