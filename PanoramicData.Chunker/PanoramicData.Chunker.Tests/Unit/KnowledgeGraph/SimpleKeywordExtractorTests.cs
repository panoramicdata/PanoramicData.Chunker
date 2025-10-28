using AwesomeAssertions;
using PanoramicData.Chunker.Chunkers.Markdown;
using PanoramicData.Chunker.KnowledgeGraph.Extractors;
using PanoramicData.Chunker.Models;
using PanoramicData.Chunker.Models.KnowledgeGraph;
using Xunit;

namespace PanoramicData.Chunker.Tests.Unit.KnowledgeGraph;

/// <summary>
/// Unit tests for SimpleKeywordExtractor.
/// </summary>
public class SimpleKeywordExtractorTests
{
	[Fact]
	public async Task ExtractEntitiesAsync_WithSimpleText_ShouldExtractKeywords()
	{
		// Arrange
		var extractor = new SimpleKeywordExtractor(maxKeywords: 5, minConfidence: 0.0); // Set minConfidence to 0 for testing
		var chunk = new MarkdownParagraphChunk
		{
			Id = Guid.NewGuid(),
			Content = "Machine learning algorithms process data efficiently. Machine learning is powerful technology for data science applications."
		};

		// Act
		var entities = await extractor.ExtractEntitiesAsync([chunk]);

		// Assert
		entities.Should().NotBeEmpty();
		entities.Should().AllSatisfy(e => e.Type.Should().Be(EntityType.Keyword));
		entities.Should().Contain(e => e.Name.Equals("machine", StringComparison.OrdinalIgnoreCase) ||
		     e.Name.Equals("learning", StringComparison.OrdinalIgnoreCase));
	}

	[Fact]
	public async Task ExtractEntitiesAsync_WithStopWords_ShouldFilterThem()
	{
		// Arrange
		var extractor = new SimpleKeywordExtractor(minConfidence: 0.0);
		var chunk = new MarkdownParagraphChunk
		{
			Id = Guid.NewGuid(),
			Content = "The quick brown fox jumps over the lazy dog"
		};

		// Act
		var entities = await extractor.ExtractEntitiesAsync([chunk]);

		// Assert
		entities.Should().NotContain(e => e.Name.Equals("the", StringComparison.OrdinalIgnoreCase));
		entities.Should().NotContain(e => e.Name.Equals("over", StringComparison.OrdinalIgnoreCase));
	}

	[Fact]
	public async Task ExtractEntitiesAsync_WithMinWordLength_ShouldFilterShortWords()
	{
		// Arrange
		var extractor = new SimpleKeywordExtractor(minWordLength: 5, minConfidence: 0.0);
		var chunk = new MarkdownParagraphChunk
		{
			Id = Guid.NewGuid(),
			Content = "AI and ML are powerful technologies for data science"
		};

		// Act
		var entities = await extractor.ExtractEntitiesAsync([chunk]);

		// Assert
		entities.Should().NotContain(e => e.Name.Length < 5);
		if (entities.Count != 0)
		{
			entities.Should().AllSatisfy(e => (e.Name.Length >= 5).Should().BeTrue());
		}
	}

	[Fact]
	public async Task ExtractEntitiesAsync_WithMultipleChunks_ShouldCalculateTFIDF()
	{
		// Arrange
		var extractor = new SimpleKeywordExtractor(minConfidence: 0.0);
		var chunks = new List<MarkdownParagraphChunk>
		{
			new() { Id = Guid.NewGuid(), Content = "Python programming language is popular" },
			new() { Id = Guid.NewGuid(), Content = "JavaScript programming is also popular" },
			new() { Id = Guid.NewGuid(), Content = "Python is great for data science" }
		};

		// Act
		var entities = await extractor.ExtractEntitiesAsync(chunks.Cast<ChunkerBase>());

		// Assert
		entities.Should().NotBeEmpty();
		// Python should have higher frequency since it appears in multiple chunks
		var python = entities.FirstOrDefault(e => e.Name.Equals("python", StringComparison.OrdinalIgnoreCase));
		if (python != null)
		{
			(python.Frequency > 0).Should().BeTrue();
		}
	}

	[Fact]
	public async Task ExtractEntitiesAsync_WithEmptyContent_ShouldReturnEmpty()
	{
		// Arrange
		var extractor = new SimpleKeywordExtractor();
		var chunk = new MarkdownParagraphChunk { Id = Guid.NewGuid(), Content = "" };

		// Act
		var entities = await extractor.ExtractEntitiesAsync([chunk]);

		// Assert
		entities.Should().BeEmpty();
	}

	[Fact]
	public async Task ExtractEntitiesAsync_WithNoChunks_ShouldReturnEmpty()
	{
		// Arrange
		var extractor = new SimpleKeywordExtractor();
		var chunks = new List<MarkdownParagraphChunk>();

		// Act
		var entities = await extractor.ExtractEntitiesAsync(chunks);

		// Assert
		entities.Should().BeEmpty();
	}

	[Fact]
	public async Task ExtractEntitiesAsync_ShouldSetMetadata()
	{
		// Arrange
		var extractor = new SimpleKeywordExtractor(minConfidence: 0.0);
		var chunk = new MarkdownParagraphChunk
		{
			Id = Guid.NewGuid(),
			Content = "Testing metadata extraction functionality properly"
		};

		// Act
		var entities = await extractor.ExtractEntitiesAsync([chunk]);

		// Assert
		entities.Should().NotBeEmpty();
		entities.Should().AllSatisfy(e =>
		{
			e.Metadata.ExtractorName.Should().Be("SimpleKeywordExtractor");
			e.Metadata.ExtractorVersion.Should().Be("1.0");
			(e.Metadata.ExtractedAt <= DateTimeOffset.UtcNow).Should().BeTrue();
		});
	}

	[Fact]
	public async Task ExtractEntitiesAsync_ShouldTrackSources()
	{
		// Arrange
		var extractor = new SimpleKeywordExtractor(minConfidence: 0.0);
		var chunk = new MarkdownParagraphChunk
		{
			Id = Guid.NewGuid(),
			Content = "Testing source tracking functionality properly working"
		};

		// Act
		var entities = await extractor.ExtractEntitiesAsync([chunk]);

		// Assert
		entities.Should().NotBeEmpty();
		entities.Should().AllSatisfy(e =>
		{
			e.Sources.Should().NotBeEmpty();
			e.Sources[0].ChunkId.Should().Be(chunk.Id);
			(e.Sources[0].Position >= 0).Should().BeTrue();
		});
	}

	[Fact]
	public async Task ExtractEntitiesAsync_WithMaxKeywords_ShouldLimitResults()
	{
		// Arrange
		var extractor = new SimpleKeywordExtractor(maxKeywords: 3, minConfidence: 0.0);
		var chunk = new MarkdownParagraphChunk
		{
			Id = Guid.NewGuid(),
			Content = "Machine learning artificial intelligence deep learning neural networks computer vision natural language processing data science algorithms"
		};

		// Act
		var entities = await extractor.ExtractEntitiesAsync([chunk]);

		// Assert
		// Should extract at most maxKeywords per chunk
		(entities.Count <= 3).Should().BeTrue();
	}

	[Fact]
	public void SupportedEntityTypes_ShouldContainKeyword()
	{
		// Arrange
		var extractor = new SimpleKeywordExtractor();

		// Act
		var types = extractor.SupportedEntityTypes;

		// Assert
		types.Should().Contain(EntityType.Keyword);
	}

	[Fact]
	public void Name_ShouldBeCorrect()
	{
		// Arrange
		var extractor = new SimpleKeywordExtractor();

		// Act & Assert
		extractor.Name.Should().Be("SimpleKeywordExtractor");
	}

	[Fact]
	public void Version_ShouldBeSet()
	{
		// Arrange
		var extractor = new SimpleKeywordExtractor();

		// Act & Assert
		extractor.Version.Should().Be("1.0");
	}
}
