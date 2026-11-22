using AwesomeAssertions;
using PanoramicData.Chunker.Chunkers.Markdown;
using PanoramicData.Chunker.Extensions;
using PanoramicData.Chunker.Models;

namespace PanoramicData.Chunker.Tests.Unit.Extensions;

public partial class ChunkQueryExtensionsTests
{
	[Fact]
	public void WithMinWords_ShouldFilterByMinimumWordCount()
	{
		// Arrange
		_testChunks[1].QualityMetrics = new ChunkQualityMetrics { WordCount = 20 };
		_testChunks[2].QualityMetrics = new ChunkQualityMetrics { WordCount = 5 };

		// Act
		var result = _testChunks.WithMinWords(10).ToList();

		// Assert
		_ = result.Should().Contain(_testChunks[1]);
		_ = result.Should().NotContain(_testChunks[2]);
	}

	[Fact]
	public void WithMinCharacters_ShouldFilterByMinimumCharacterCount()
	{
		// Arrange
		_testChunks[1].QualityMetrics = new ChunkQualityMetrics { CharacterCount = 100 };
		_testChunks[2].QualityMetrics = new ChunkQualityMetrics { CharacterCount = 20 };

		// Act
		var result = _testChunks.WithMinCharacters(50).ToList();

		// Assert
		_ = result.Should().Contain(_testChunks[1]);
		_ = result.Should().NotContain(_testChunks[2]);
	}

	[Fact]
	public void WithCompleteSentences_ShouldExcludeTruncatedSentences()
	{
		// Arrange
		_testChunks[1].QualityMetrics = new ChunkQualityMetrics { HasTruncatedSentence = false };
		_testChunks[2].QualityMetrics = new ChunkQualityMetrics { HasTruncatedSentence = true };

		// Act
		var result = _testChunks.WithCompleteSentences().ToList();

		// Assert
		_ = result.Should().Contain(_testChunks[1]);
		_ = result.Should().NotContain(_testChunks[2]);
	}

	[Fact]
	public void WithCompleteTables_ShouldExcludeIncompleteTables()
	{
		// Arrange
		_testChunks[1].QualityMetrics = new ChunkQualityMetrics { HasIncompleteTable = false };
		_testChunks[2].QualityMetrics = new ChunkQualityMetrics { HasIncompleteTable = true };

		// Act
		var result = _testChunks.WithCompleteTables().ToList();

		// Assert
		_ = result.Should().Contain(_testChunks[1]);
		_ = result.Should().NotContain(_testChunks[2]);
	}

	[Fact]
	public void ContainingText_WithCaseInsensitive_ShouldFindMatches()
	{
		// Arrange
		var para = (MarkdownParagraphChunk)_testChunks[1];
		para.Content = "This contains IMPORTANT information";

		// Act
		var result = _testChunks.ContainingText("important", ignoreCase: true).ToList();

		// Assert
		_ = result.Should().Contain(para);
	}

	[Fact]
	public void ContainingText_WithCaseSensitive_ShouldRespectCase()
	{
		// Arrange
		var para = (MarkdownParagraphChunk)_testChunks[1];
		para.Content = "This contains IMPORTANT information";

		// Act
		var result = _testChunks.ContainingText("important", ignoreCase: false).ToList();

		// Assert
		_ = result.Should().BeEmpty();
	}

	[Fact]
	public void WithKeywords_ShouldFilterByKeywords()
	{
		// Arrange
		var para = (MarkdownParagraphChunk)_testChunks[1];
		para.Keywords = ["api", "documentation", "tutorial"];

		// Act
		var result = _testChunks.WithKeywords("api", "guide").ToList();

		// Assert
		_ = result.Should().Contain(para);
	}

	[Fact]
	public void WithAnnotations_ShouldFilterChunksWithAnnotations()
	{
		// Arrange
		var para = (MarkdownParagraphChunk)_testChunks[1];
		para.Annotations =
		[
			new ContentAnnotation { Type = AnnotationType.Bold, StartIndex = 0, Length = 4 }
		];

		// Act
		var result = _testChunks.WithAnnotations().ToList();

		// Assert
		_ = result.Should().Contain(para);
		_ = result.Should().ContainSingle();
	}

	[Fact]
	public void WithAnnotationType_ShouldFilterByAnnotationType()
	{
		// Arrange
		var para1 = (MarkdownParagraphChunk)_testChunks[1];
		para1.Annotations =
		[
			new ContentAnnotation { Type = AnnotationType.Bold, StartIndex = 0, Length = 4 }
		];
		
		var para2 = (MarkdownParagraphChunk)_testChunks[2];
		para2.Annotations =
		[
			new ContentAnnotation { Type = AnnotationType.Link, StartIndex = 0, Length = 4 }
		];

		// Act
		var result = _testChunks.WithAnnotationType(AnnotationType.Bold).ToList();

		// Assert
		_ = result.Should().Contain(para1);
		_ = result.Should().NotContain(para2);
	}

	[Fact]
	public void GetRoot_ShouldReturnRootAncestor()
	{
		// Arrange
		var leaf = _testChunks.LeafChunks().First();
		var expectedRoot = _testChunks.RootChunks().First();

		// Act
		var root = leaf.GetRoot(_testChunks);

		// Assert
		_ = root.Should().Be(expectedRoot);
	}

	[Fact]
	public void GetRoot_ForRootChunk_ShouldReturnSelf()
	{
		// Arrange
		var root = _testChunks.RootChunks().First();

		// Act
		var result = root.GetRoot(_testChunks);

		// Assert
		_ = result.Should().Be(root);
	}
}
