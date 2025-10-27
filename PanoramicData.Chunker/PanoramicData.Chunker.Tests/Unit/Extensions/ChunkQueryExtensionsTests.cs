using AwesomeAssertions;
using PanoramicData.Chunker.Chunkers.Markdown;
using PanoramicData.Chunker.Extensions;
using PanoramicData.Chunker.Models;

namespace PanoramicData.Chunker.Tests.Unit.Extensions;

/// <summary>
/// Tests for ChunkQueryExtensions.
/// </summary>
public class ChunkQueryExtensionsTests
{
	private readonly List<ChunkerBase> _testChunks;

	public ChunkQueryExtensionsTests()
	{
		_testChunks = CreateTestChunks();
	}

	[Fact]
	public void ContentChunks_ShouldFilterToContentChunksOnly()
	{
		// Act
		var result = _testChunks.ContentChunks().ToList();

		// Assert
		_ = result.Should().HaveCount(3);
		_ = result.Should().AllBeAssignableTo<ContentChunk>();
	}

	[Fact]
	public void StructuralChunks_ShouldFilterToStructuralChunksOnly()
	{
		// Act
		var result = _testChunks.StructuralChunks().ToList();

		// Assert
		_ = result.Should().ContainSingle();
		_ = result.Should().AllBeAssignableTo<StructuralChunk>();
	}

	[Fact]
	public void OfSpecificType_ShouldFilterByType()
	{
		// Act
		var result = _testChunks.OfSpecificType("Paragraph").ToList();

		// Assert
		_ = result.Should().HaveCount(2);
		_ = result.Should().AllSatisfy(c => c.SpecificType.Should().Be("Paragraph"));
	}

	[Fact]
	public void AtDepth_ShouldFilterByExactDepth()
	{
		// Act
		var result = _testChunks.AtDepth(1).ToList();

		// Assert
		_ = result.Should().HaveCount(3);
		_ = result.Should().AllSatisfy(c => c.Depth.Should().Be(1));
	}

	[Fact]
	public void WithinDepthRange_ShouldFilterByRange()
	{
		// Act
		var result = _testChunks.WithinDepthRange(0, 1).ToList();

		// Assert
		_ = result.Should().HaveCountGreaterThan(0);
		_ = result.Should().AllSatisfy(c => c.Depth.Should().BeInRange(0, 1));
	}

	[Fact]
	public void RootChunks_ShouldReturnOnlyRoots()
	{
		// Act
		var result = _testChunks.RootChunks().ToList();

		// Assert
		_ = result.Should().ContainSingle();
		_ = result.Should().AllSatisfy(c => c.ParentId.Should().BeNull());
	}

	[Fact]
	public void LeafChunks_ShouldReturnOnlyLeaves()
	{
		// Act
		var result = _testChunks.LeafChunks().ToList();

		// Assert
		_ = result.Should().NotBeEmpty();
		_ = result.Should().AllSatisfy(c =>
		{
			var hasChildren = _testChunks.Any(x => x.ParentId == c.Id);
			_ = hasChildren.Should().BeFalse();
		});
	}

	[Fact]
	public void GetChildren_ShouldReturnDirectChildren()
	{
		// Arrange
		var parent = _testChunks.RootChunks().First();

		// Act
		var children = parent.GetChildren(_testChunks).ToList();

		// Assert
		_ = children.Should().HaveCount(3);
		_ = children.Should().AllSatisfy(c => c.ParentId.Should().Be(parent.Id));
	}

	[Fact]
	public void GetParent_ShouldReturnParent()
	{
		// Arrange
		var child = _testChunks.First(c => c.ParentId.HasValue);
		var expectedParent = _testChunks.First(c => c.Id == child.ParentId);

		// Act
		var parent = child.GetParent(_testChunks);

		// Assert
		_ = parent.Should().NotBeNull();
		_ = parent!.Id.Should().Be(expectedParent.Id);
	}

	[Fact]
	public void GetAncestors_ShouldReturnAllAncestors()
	{
		// Arrange
		var leaf = _testChunks.LeafChunks().First();

		// Act
		var ancestors = leaf.GetAncestors(_testChunks).ToList();

		// Assert
		_ = ancestors.Should().NotBeEmpty();
		_ = ancestors.Should().BeInDescendingOrder(a => a.Depth);
	}

	[Fact]
	public void GetDescendants_ShouldReturnAllDescendants()
	{
		// Arrange
		var root = _testChunks.RootChunks().First();

		// Act
		var descendants = root.GetDescendants(_testChunks).ToList();

		// Assert
		_ = descendants.Should().HaveCount(3);
	}

	[Fact]
	public void GetSiblings_ShouldReturnSiblings()
	{
		// Arrange
		var chunk = _testChunks.First(c => c.ParentId.HasValue);

		// Act
		var siblings = chunk.GetSiblings(_testChunks, includeSelf: false).ToList();

		// Assert
		_ = siblings.Should().NotContain(chunk);
		_ = siblings.Should().AllSatisfy(s => s.ParentId.Should().Be(chunk.ParentId));
	}

	[Fact]
	public void WithTag_ShouldFilterByTag()
	{
		// Arrange
		_testChunks[0].Metadata = new ChunkMetadata { Tags = ["test", "markdown"] };

		// Act
		var result = _testChunks.WithTag("test").ToList();

		// Assert
		_ = result.Should().Contain(_testChunks[0]);
	}

	[Fact]
	public void WithMinTokens_ShouldFilterByTokenCount()
	{
		// Act
		var result = _testChunks.WithMinTokens(10).ToList();

		// Assert
		_ = result.Should().AllSatisfy(c => c.QualityMetrics!.TokenCount.Should().BeGreaterThanOrEqualTo(10));
	}

	[Fact]
	public void WithMinSemanticCompleteness_ShouldFilterByScore()
	{
		// Act
		var result = _testChunks.WithMinSemanticCompleteness(0.9).ToList();

		// Assert
		_ = result.Should().AllSatisfy(c => c.QualityMetrics!.SemanticCompleteness.Should().BeGreaterThanOrEqualTo(0.9));
	}

	[Fact]
	public void OrderBySequence_ShouldOrderCorrectly()
	{
		// Act
		var result = _testChunks.OrderBySequence().ToList();

		// Assert
		_ = result.Should().BeInAscendingOrder(c => c.SequenceNumber);
	}

	[Fact]
	public void GroupBySpecificType_ShouldGroupCorrectly()
	{
		// Act
		var groups = _testChunks.GroupBySpecificType().ToList();

		// Assert
		_ = groups.Should().NotBeEmpty();
		_ = groups.Should().AllSatisfy(g => g.Should().NotBeEmpty());
	}

	[Fact]
	public void GetStatistics_ShouldCalculateCorrectly()
	{
		// Act
		var stats = _testChunks.GetStatistics();

		// Assert
		_ = stats.TotalChunks.Should().Be(_testChunks.Count);
		_ = stats.StructuralChunks.Should().Be(1);
		_ = stats.ContentChunks.Should().Be(3);
		_ = stats.MaxDepth.Should().BePositive();
	}

	[Fact]
	public void MarkdownSections_ShouldFilterToMarkdownSections()
	{
		// Act
		var result = _testChunks.MarkdownSections().ToList();

		// Assert
		_ = result.Should().ContainSingle();
		_ = result.Should().AllBeOfType<MarkdownSectionChunk>();
	}

	[Fact]
	public void OnSheet_ShouldFilterBySheetName()
	{
		// Arrange
		_testChunks[0].Metadata = new ChunkMetadata { SheetName = "Sheet1" };
		_testChunks[1].Metadata = new ChunkMetadata { SheetName = "Sheet2" };

		// Act
		var result = _testChunks.OnSheet("Sheet1").ToList();

		// Assert
		_ = result.Should().Contain(_testChunks[0]);
		_ = result.Should().NotContain(_testChunks[1]);
	}

	[Fact]
	public void InExternalHierarchy_ShouldFilterByExternalHierarchyPrefix()
	{
		// Arrange
		_testChunks[0].Metadata = new ChunkMetadata { ExternalHierarchy = "Docs/API/V1" };
		_testChunks[1].Metadata = new ChunkMetadata { ExternalHierarchy = "Docs/API/V2" };
		_testChunks[2].Metadata = new ChunkMetadata { ExternalHierarchy = "Guides/Tutorial" };

		// Act
		var result = _testChunks.InExternalHierarchy("Docs/API").ToList();

		// Assert
		_ = result.Should().HaveCount(2);
		_ = result.Should().Contain(_testChunks[0]);
		_ = result.Should().Contain(_testChunks[1]);
	}

	[Fact]
	public void FromSource_ShouldFilterBySourceId()
	{
		// Arrange
		_testChunks[0].Metadata = new ChunkMetadata { SourceId = "doc1" };
		_testChunks[1].Metadata = new ChunkMetadata { SourceId = "doc2" };

		// Act
		var result = _testChunks.FromSource("doc1").ToList();

		// Assert
		_ = result.Should().Contain(_testChunks[0]);
		_ = result.Should().NotContain(_testChunks[1]);
	}

	[Fact]
	public void InLanguage_ShouldFilterByLanguageCode()
	{
		// Arrange
		_testChunks[0].Metadata = new ChunkMetadata { Language = "en" };
		_testChunks[1].Metadata = new ChunkMetadata { Language = "fr" };

		// Act
		var result = _testChunks.InLanguage("en").ToList();

		// Assert
		_ = result.Should().Contain(_testChunks[0]);
		_ = result.Should().NotContain(_testChunks[1]);
	}

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

	[Fact]
	public void GetNext_ShouldReturnNextChunkInSequence()
	{
		// Arrange
		var first = _testChunks.OrderBy(c => c.SequenceNumber).First();

		// Act
		var next = first.GetNext(_testChunks);

		// Assert
		_ = next.Should().NotBeNull();
		_ = next!.SequenceNumber.Should().Be(first.SequenceNumber + 1);
	}

	[Fact]
	public void GetNext_ForLastChunk_ShouldReturnNull()
	{
		// Arrange
		var last = _testChunks.OrderByDescending(c => c.SequenceNumber).First();

		// Act
		var next = last.GetNext(_testChunks);

		// Assert
		_ = next.Should().BeNull();
	}

	[Fact]
	public void GetPrevious_ShouldReturnPreviousChunkInSequence()
	{
		// Arrange
		var second = _testChunks.OrderBy(c => c.SequenceNumber).Skip(1).First();

		// Act
		var previous = second.GetPrevious(_testChunks);

		// Assert
		_ = previous.Should().NotBeNull();
		_ = previous!.SequenceNumber.Should().Be(second.SequenceNumber - 1);
	}

	[Fact]
	public void GetPrevious_ForFirstChunk_ShouldReturnNull()
	{
		// Arrange
		var first = _testChunks.OrderBy(c => c.SequenceNumber).First();

		// Act
		var previous = first.GetPrevious(_testChunks);

		// Assert
		_ = previous.Should().BeNull();
	}

	[Fact]
	public void GetContext_ShouldReturnSurroundingChunks()
	{
		// Arrange
		var middle = _testChunks.OrderBy(c => c.SequenceNumber).Skip(1).First();

		// Act
		var context = middle.GetContext(_testChunks, before: 1, after: 1, includeSelf: true).ToList();

		// Assert
		_ = context.Should().HaveCount(3);
		_ = context.Should().Contain(middle);
	}

	[Fact]
	public void GetContext_WithoutSelf_ShouldExcludeCurrentChunk()
	{
		// Arrange
		var middle = _testChunks.OrderBy(c => c.SequenceNumber).Skip(1).First();

		// Act
		var context = middle.GetContext(_testChunks, before: 1, after: 1, includeSelf: false).ToList();

		// Assert
		_ = context.Should().HaveCount(2);
		_ = context.Should().NotContain(middle);
	}

	[Fact]
	public void GroupByPage_ShouldGroupCorrectly()
	{
		// Arrange
		_testChunks[0].Metadata = new ChunkMetadata { PageNumber = 1 };
		_testChunks[1].Metadata = new ChunkMetadata { PageNumber = 1 };
		_testChunks[2].Metadata = new ChunkMetadata { PageNumber = 2 };

		// Act
		var groups = _testChunks.GroupByPage().ToList();

		// Assert
		_ = groups.Should().HaveCountGreaterThan(0);
		var page1Group = groups.FirstOrDefault(g => g.Key == 1);
		_ = page1Group.Should().NotBeNull();
		_ = page1Group!.Should().HaveCount(2);
	}

	[Fact]
	public void GroupByType_ShouldGroupCorrectly()
	{
		// Act
		var groups = _testChunks.GroupByType().ToList();

		// Assert
		_ = groups.Should().NotBeEmpty();
		_ = groups.Should().AllSatisfy(g => g.Should().NotBeEmpty());
	}

	private static List<ChunkerBase> CreateTestChunks()
	{
		var section = new MarkdownSectionChunk
		{
			HeadingLevel = 1,
			HeadingText = "Test Section",
			SequenceNumber = 0,
			Depth = 0,
			SpecificType = "Heading1",
			QualityMetrics = new ChunkQualityMetrics
			{
				TokenCount = 2,
				SemanticCompleteness = 1.0
			}
		};

		var para1 = new MarkdownParagraphChunk
		{
			Content = "First paragraph",
			ParentId = section.Id,
			SequenceNumber = 1,
			Depth = 1,
			SpecificType = "Paragraph",
			QualityMetrics = new ChunkQualityMetrics
			{
				TokenCount = 15,
				SemanticCompleteness = 1.0
			}
		};

		var para2 = new MarkdownParagraphChunk
		{
			Content = "Second paragraph",
			ParentId = section.Id,
			SequenceNumber = 2,
			Depth = 1,
			SpecificType = "Paragraph",
			QualityMetrics = new ChunkQualityMetrics
			{
				TokenCount = 16,
				SemanticCompleteness = 1.0
			}
		};

		var code = new MarkdownCodeBlockChunk
		{
			Content = "var x = 1;",
			Language = "csharp",
			ParentId = section.Id,
			SequenceNumber = 3,
			Depth = 1,
			SpecificType = "CodeBlock",
			QualityMetrics = new ChunkQualityMetrics
			{
				TokenCount = 10,
				SemanticCompleteness = 1.0
			}
		};

		section.AncestorIds = [];
		para1.AncestorIds = [section.Id];
		para2.AncestorIds = [section.Id];
		code.AncestorIds = [section.Id];

		return [section, para1, para2, code];
	}
}
