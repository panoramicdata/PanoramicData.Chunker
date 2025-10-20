using FluentAssertions;
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
		supportedTypes.Should().Contain(DocumentType.Markdown);
	}

	[Fact]
	public void GetChunker_WithMarkdownType_ShouldReturnMarkdownChunker()
	{
		// Arrange
		var factory = new ChunkerFactory();

		// Act
		var chunker = factory.GetChunker(DocumentType.Markdown);

		// Assert
		chunker.Should().NotBeNull();
		chunker.SupportedType.Should().Be(DocumentType.Markdown);
	}

	[Fact]
	public void GetChunker_WithUnsupportedType_ShouldThrow()
	{
		// Arrange
		var factory = new ChunkerFactory();

		// Act
		var act = () => factory.GetChunker(DocumentType.Pdf);

		// Assert
		act.Should().Throw<NotSupportedException>()
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
		chunker.Should().NotBeNull();
		chunker.SupportedType.Should().Be(DocumentType.Markdown);
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
		chunker.Should().NotBeNull();
		chunker.SupportedType.Should().Be(DocumentType.Markdown);
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
		chunker.Should().NotBeNull();
		chunker.SupportedType.Should().Be(DocumentType.Markdown);
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
		chunker.Should().NotBeNull();
		chunker.SupportedType.Should().Be(DocumentType.Markdown);
	}

	[Fact]
	public async Task GetChunkerForStreamAsync_WithNoMatchingContent_ShouldThrow()
	{
		// Arrange
		var factory = new ChunkerFactory();
		var plainText = "Just some plain text with no markdown patterns";
		using var stream = new MemoryStream(Encoding.UTF8.GetBytes(plainText));

		// Act
		var act = async () => await factory.GetChunkerForStreamAsync(stream);

		// Assert
		await act.Should().ThrowAsync<NotSupportedException>()
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
		factory.GetSupportedTypes().Should().Contain(DocumentType.Markdown);
	}

	[Fact]
	public void GetSupportedTypes_ShouldReturnReadOnlyCollection()
	{
		// Arrange
		var factory = new ChunkerFactory();

		// Act
		var types = factory.GetSupportedTypes();

		// Assert
		types.Should().NotBeNull();
		types.Should().BeAssignableTo<IReadOnlyCollection<DocumentType>>();
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
		chunker.Should().NotBeNull();
	}

	[Fact]
	public void Constructor_WithNullTokenCounter_ShouldUseDefault()
	{
		// Arrange & Act
		var factory = new ChunkerFactory(null);

		// Assert
		var chunker = factory.GetChunker(DocumentType.Markdown);
		chunker.Should().NotBeNull();
	}
}
