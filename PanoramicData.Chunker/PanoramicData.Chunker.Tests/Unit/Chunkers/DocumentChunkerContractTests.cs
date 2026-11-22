using AwesomeAssertions;
using PanoramicData.Chunker.Configuration;
using PanoramicData.Chunker.Infrastructure;
using PanoramicData.Chunker.Interfaces;
using PanoramicData.Chunker.Models;

namespace PanoramicData.Chunker.Tests.Unit.Chunkers;

/// <summary>
/// Contract tests shared by chunkers that take a token counter.
/// </summary>
public abstract class DocumentChunkerContractTests(
	ITestOutputHelper output,
	Func<ITokenCounter, IDocumentChunker> createChunker,
	DocumentType expectedType) : BaseTest(output)
{
	[Fact]
	public void Constructor_WithValidTokenCounter_ShouldInitialize()
	{
		var chunker = createChunker(new CharacterBasedTokenCounter());

		_ = chunker.Should().NotBeNull();
		_ = chunker.SupportedType.Should().Be(expectedType);
	}

	[Fact]
	public void Constructor_WithNullTokenCounter_ShouldThrow()
	{
		var act = () => createChunker(null!);
		_ = act.Should().Throw<ArgumentNullException>();
	}

	[Fact]
	public async Task ChunkAsync_WithNullStream_ShouldThrow()
	{
		var chunker = createChunker(new CharacterBasedTokenCounter());

		var act = async () => await chunker.ChunkAsync(null!, new ChunkingOptions(), CancellationToken);

		_ = await act.Should().ThrowAsync<ArgumentNullException>();
	}

	[Fact]
	public async Task ChunkAsync_WithNullOptions_ShouldThrow()
	{
		var chunker = createChunker(new CharacterBasedTokenCounter());
		using var stream = new MemoryStream();

		var act = async () => await chunker.ChunkAsync(stream, null!, CancellationToken);

		_ = await act.Should().ThrowAsync<ArgumentNullException>();
	}
}
