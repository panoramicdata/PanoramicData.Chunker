using AwesomeAssertions;
using PanoramicData.Chunker.Chunkers.Markdown;
using PanoramicData.Chunker.Extensions;
using PanoramicData.Chunker.Models;

namespace PanoramicData.Chunker.Tests.Unit.Extensions;

/// <summary>
/// Tests for ChunkQueryExtensions.
/// </summary>
public partial class ChunkQueryExtensionsTests
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

	private static List<ChunkerBase> CreateTestChunks()
	{
		var section = new MarkdownSectionChunk
		{
			HeadingLevel = 1,
			HeadingText = "Test Section",
			SequenceNumber = 0,
			Depth = 0,
			SpecificType = "Heading1",
			AncestorIds = [],
			QualityMetrics = CreateMetrics(2)
		};

		return
		[
			section,
			ChildOf(section, new MarkdownParagraphChunk { Content = "First paragraph", SequenceNumber = 1, SpecificType = "Paragraph" }, 15),
			ChildOf(section, new MarkdownParagraphChunk { Content = "Second paragraph", SequenceNumber = 2, SpecificType = "Paragraph" }, 16),
			ChildOf(section, new MarkdownCodeBlockChunk { Content = "var x = 1;", Language = "csharp", SequenceNumber = 3, SpecificType = "CodeBlock" }, 10)
		];
	}

	private static T ChildOf<T>(MarkdownSectionChunk section, T chunk, int tokenCount)
		where T : ChunkerBase
	{
		chunk.ParentId = section.Id;
		chunk.Depth = 1;
		chunk.AncestorIds = [section.Id];
		chunk.QualityMetrics = CreateMetrics(tokenCount);
		return chunk;
	}

	private static ChunkQualityMetrics CreateMetrics(int tokenCount) => new()
	{
		TokenCount = tokenCount,
		SemanticCompleteness = 1.0
	};
}