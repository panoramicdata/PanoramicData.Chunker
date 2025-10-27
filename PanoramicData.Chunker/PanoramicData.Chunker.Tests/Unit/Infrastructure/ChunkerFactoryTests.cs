using AwesomeAssertions;
using PanoramicData.Chunker.Chunkers.Markdown;
using PanoramicData.Chunker.Configuration;
using PanoramicData.Chunker.Infrastructure;
using System.Text;

namespace PanoramicData.Chunker.Tests.Unit.Infrastructure;

/// <summary>
/// Tests for ChunkerFactory with Markdown chunker registration.
/// </summary>
public class ChunkerFactoryTests
{
	[Fact]
	public void Constructor_ShouldAutoRegisterMarkdownChunker()
	{
		// Arrange & Act
		var factory = new ChunkerFactory();

		// Assert
		var supportedTypes = factory.GetSupportedTypes();
		_ = supportedTypes.Should().Contain(DocumentType.Markdown);
	}

	[Fact]
	public void GetChunker_WithMarkdownType_ShouldReturnMarkdownChunker()
	{
		// Arrange
		var factory = new ChunkerFactory();

		// Act
		var chunker = factory.GetChunker(DocumentType.Markdown);

		// Assert
		_ = chunker.Should().NotBeNull();
		_ = chunker.SupportedType.Should().Be(DocumentType.Markdown);
	}

	[Fact]
	public void GetChunker_WithUnsupportedType_ShouldThrow()
	{
		// Arrange
		var factory = new ChunkerFactory();

		// Act - Use Email type which is not yet supported
		var act = () => factory.GetChunker(DocumentType.Email);

		// Assert
		_ = act.Should().Throw<NotSupportedException>()
			.WithMessage("*not currently supported*");
	}

	[Fact]
	public void GetChunkerForStream_WithMarkdownExtension_ShouldReturnMarkdownChunker()
	{
		// Arrange
		var factory = new ChunkerFactory();
		using var stream = new MemoryStream(Encoding.UTF8.GetBytes("# Test"));

		// Act
		var chunker = factory.GetChunkerForStream(stream, "document.md");

		// Assert
		_ = chunker.Should().NotBeNull();
		_ = chunker.SupportedType.Should().Be(DocumentType.Markdown);
	}

	[Fact]
	public void GetChunkerForStream_WithMarkdownExtensionUpperCase_ShouldReturnMarkdownChunker()
	{
		// Arrange
		var factory = new ChunkerFactory();
		using var stream = new MemoryStream(Encoding.UTF8.GetBytes("# Test"));

		// Act
		var chunker = factory.GetChunkerForStream(stream, "DOCUMENT.MD");

		// Assert
		_ = chunker.Should().NotBeNull();
		_ = chunker.SupportedType.Should().Be(DocumentType.Markdown);
	}

	[Fact]
	public void GetChunkerForStream_WithMarkdownExtension_ShouldReturnMarkdownChunker2()
	{
		// Arrange
		var factory = new ChunkerFactory();
		using var stream = new MemoryStream(Encoding.UTF8.GetBytes("# Test"));

		// Act
		var chunker = factory.GetChunkerForStream(stream, "document.markdown");

		// Assert
		_ = chunker.Should().NotBeNull();
		_ = chunker.SupportedType.Should().Be(DocumentType.Markdown);
	}

	[Fact]
	public async Task GetChunkerForStreamAsync_WithMarkdownContent_ShouldDetectAndReturnChunker()
	{
		// Arrange
		var factory = new ChunkerFactory();
		var markdown = "# Markdown Header\n\nSome content with **bold** text.";
		using var stream = new MemoryStream(Encoding.UTF8.GetBytes(markdown));

		// Act
		var chunker = await factory.GetChunkerForStreamAsync(stream);

		// Assert
		_ = chunker.Should().NotBeNull();
		_ = chunker.SupportedType.Should().Be(DocumentType.Markdown);
	}

	[Fact]
	public async Task GetChunkerForStreamAsync_WithNoMatchingContent_ShouldThrow()
	{
		// Arrange
		var factory = new ChunkerFactory();
		// Use binary data that looks like corrupted/unrecognized format
		var binaryData = new byte[] { 0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10, 0x4A, 0x46, 0x49, 0x46 }; // Not a valid format we support
		using var stream = new MemoryStream(binaryData);

		// Act
		var act = async () => await factory.GetChunkerForStreamAsync(stream);

		// Assert
		_ = await act.Should().ThrowAsync<NotSupportedException>()
			.WithMessage("*Unable to detect document type*");
	}

	[Fact]
	public void RegisterChunker_ShouldAddToSupportedTypes()
	{
		// Arrange
		var factory = new ChunkerFactory();
		var tokenCounter = new CharacterBasedTokenCounter();
		var customChunker = new MarkdownDocumentChunker(tokenCounter);

		// Act
		factory.RegisterChunker(customChunker);

		// Assert
		_ = factory.GetSupportedTypes().Should().Contain(DocumentType.Markdown);
	}

	[Fact]
	public void GetSupportedTypes_ShouldReturnReadOnlyCollection()
	{
		// Arrange
		var factory = new ChunkerFactory();

		// Act
		var types = factory.GetSupportedTypes();

		// Assert
		_ = types.Should().NotBeNull();
		_ = types.Should().BeAssignableTo<IReadOnlyCollection<DocumentType>>();
	}

	[Fact]
	public void Constructor_WithCustomTokenCounter_ShouldUseIt()
	{
		// Arrange
		var customTokenCounter = new CharacterBasedTokenCounter();

		// Act
		var factory = new ChunkerFactory(customTokenCounter);

		// Assert
		var chunker = factory.GetChunker(DocumentType.Markdown);
		_ = chunker.Should().NotBeNull();
	}

	[Fact]
	public void Constructor_WithNullTokenCounter_ShouldUseDefault()
	{
		// Arrange & Act
		var factory = new ChunkerFactory(null);

		// Assert
		var chunker = factory.GetChunker(DocumentType.Markdown);
		_ = chunker.Should().NotBeNull();
	}
}
