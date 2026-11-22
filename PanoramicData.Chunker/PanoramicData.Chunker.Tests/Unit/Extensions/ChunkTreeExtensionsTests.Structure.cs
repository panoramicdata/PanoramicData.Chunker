using AwesomeAssertions;
using PanoramicData.Chunker.Chunkers.Markdown;
using PanoramicData.Chunker.Extensions;
using PanoramicData.Chunker.Models;
using PanoramicData.Chunker.Tests.Utilities;
using Xunit;

namespace PanoramicData.Chunker.Tests.Unit.Extensions;

public partial class ChunkTreeExtensionsTests
{
	#region GetNestingLevel Tests

	[Fact]
	public void GetNestingLevel_ShouldReturnDepth()
	{
		// Arrange
		var code = _testChunks.OfType<MarkdownCodeBlockChunk>().First();

		// Act
		var level = code.GetNestingLevel();

		// Assert
		_ = level.Should().Be(2);
	}

	[Fact]
	public void GetNestingLevel_ForRootChunk_ShouldReturnZero()
	{
		// Arrange
		var section1 = _testChunks.First();

		// Act
		var level = section1.GetNestingLevel();

		// Assert
		_ = level.Should().Be(0);
	}

	#endregion

	#region GetLeafNodes Tests

	[Fact]
	public void GetLeafNodes_ShouldReturnOnlyLeafNodes()
	{
		// Arrange
		var section2 = _testChunks.First(c => c.SequenceNumber == 3);

		// Act
		var leaves = section2.GetLeafNodes(_testChunks).ToList();

		// Assert
		_ = leaves.Should().HaveCount(2); // code, para3
		_ = leaves.Should().NotContain(c => c.SpecificType == "Section");
		_ = leaves.Should().NotContain(c => c.SpecificType == "Subsection");
	}

	[Fact]
	public void GetLeafNodes_ForLeafNode_ShouldReturnSelf()
	{
		// Arrange
		var code = _testChunks.OfType<MarkdownCodeBlockChunk>().First();

		// Act
		var leaves = code.GetLeafNodes(_testChunks).ToList();

		// Assert
		_ = leaves.Should().ContainSingle();
		_ = leaves.Should().HaveElementAt(0, code);
	}

	#endregion

	#region PruneAtDepth Tests

	[Fact]
	public void PruneAtDepth_ShouldRemoveChunksAboveMaxDepth()
	{
		// Act
		var pruned = _testChunks.PruneAtDepth(1).ToList();

		// Assert
		_ = pruned.Should().HaveCount(5); // sections + direct children
		_ = pruned.Should().NotContain(c => c.Depth > 1);
	}

	[Fact]
	public void PruneAtDepth_AtZero_ShouldReturnOnlyRoots()
	{
		// Act
		var pruned = _testChunks.PruneAtDepth(0).ToList();

		// Assert
		_ = pruned.Should().HaveCount(2);
		_ = pruned.Should().OnlyContain(c => c.Depth == 0);
	}

	[Fact]
	public void PruneAtDepth_AtHighDepth_ShouldReturnAllChunks()
	{
		// Act
		var pruned = _testChunks.PruneAtDepth(10).ToList();

		// Assert
		_ = pruned.Should().HaveCount(_testChunks.Count);
	}

	#endregion

	#region GroupBySubtree Tests

	[Fact]
	public void GroupBySubtree_ShouldGroupByRootChunks()
	{
		// Act
		var groups = _testChunks.GroupBySubtree();

		// Assert
		_ = groups.Should().HaveCount(2);
		_ = groups.Keys.Should().OnlyContain(c => !c.ParentId.HasValue);
	}

	[Fact]
	public void GroupBySubtree_ShouldIncludeAllDescendantsInGroups()
	{
		// Act
		var groups = _testChunks.GroupBySubtree();

		// Assert
		var section1Group = groups.First().Value;
		_ = section1Group.Should().HaveCount(3); // section1 + 2 paragraphs

		var section2Group = groups.Last().Value;
		_ = section2Group.Should().HaveCount(4); // section2 + subsection + code + para3
	}

	[Fact]
	public void GroupBySubtree_WithEmptyList_ShouldReturnEmptyDictionary()
	{
		// Arrange
		var emptyList = new List<ChunkerBase>();

		// Act
		var groups = emptyList.GroupBySubtree();

		// Assert
		_ = groups.Should().BeEmpty();
	}

	#endregion

	#region Integration Tests

	[Fact]
	public void BuildTreeAndFlattenTree_ShouldRoundTrip()
	{
		// Arrange
		var originalCount = _testChunks.Count;

		// Act
		var roots = _testChunks.BuildTree().ToList();
		var flattened = roots.FlattenTree().ToList();

		// Assert
		_ = flattened.Should().HaveCount(originalCount);
	}

	[Fact]
	public void ComplexTraversal_ShouldVisitAllNodes()
	{
		// Arrange
		var roots = _testChunks.BuildTree().ToList();
		var visitedDepthFirst = new List<Guid>();
		var visitedBreadthFirst = new List<Guid>();

		// Act
		foreach (var root in roots)
		{
			root.TraverseDepthFirst(c => visitedDepthFirst.Add(c.Id));
			root.TraverseBreadthFirst(c => visitedBreadthFirst.Add(c.Id));
		}

		// Assert
		_ = visitedDepthFirst.Should().HaveCount(_testChunks.Count);
		_ = visitedBreadthFirst.Should().HaveCount(_testChunks.Count);
		_ = visitedDepthFirst.Should().OnlyHaveUniqueItems();
		_ = visitedBreadthFirst.Should().OnlyHaveUniqueItems();
	}

	[Fact]
	public void FilterAndCount_ShouldBeConsistent()
	{
		// Arrange
		var section2 = _testChunks.First(c => c.SequenceNumber == 3);

		// Act
		var filtered = _testChunks.FilterTreeByPredicate(c => c.IsAncestorOf(section2) || c.Id == section2.Id).ToList();
		var subtree = section2.CloneSubtree(_testChunks, true);
		var descendantCount = section2.CountDescendants(_testChunks);

		// Assert
		_ = filtered.Should().NotBeEmpty();
		_ = subtree.Should().HaveCount(descendantCount + 1); // +1 for the root itself
	}

	#endregion

	#region Edge Cases

	[Fact]
	public void TraverseDepthFirst_WithSingleNode_ShouldVisitOnce()
	{
		// Arrange
		var singleNode = new MarkdownParagraphChunk
		{
			Id = Guid.NewGuid(),
			SpecificType = "Paragraph",
			Content = "Single",
			Depth = 0,
			SequenceNumber = 0
		};
		var visitCount = 0;

		// Act
		singleNode.TraverseDepthFirst(_ => visitCount++);

		// Assert
		_ = visitCount.Should().Be(1);
	}

	[Fact]
	public void GetPathFromRoot_WithOrphanedChunk_ShouldHandleGracefully()
	{
		// Arrange
		var orphan = new MarkdownParagraphChunk
		{
			Id = Guid.NewGuid(),
			ParentId = Guid.NewGuid(), // Non-existent parent
			SpecificType = "Paragraph",
			Content = "Orphaned",
			Depth = 1,
			SequenceNumber = 99
		};

		// Act
		var path = orphan.GetPathFromRoot(_testChunks);

		// Assert
		_ = path.Should().ContainSingle();
		_ = path.Should().HaveElementAt(0, orphan);
	}

	[Fact]
	public void FilterTreeByPredicate_WithAlwaysTruePredicate_ShouldReturnAll()
	{
		// Act
		var filtered = _testChunks.FilterTreeByPredicate(_ => true).ToList();

		// Assert
		_ = filtered.Should().HaveCount(_testChunks.Count);
	}

	[Fact]
	public void PruneAtDepth_WithNegativeDepth_ShouldReturnEmpty()
	{
		// Act
		var pruned = _testChunks.PruneAtDepth(-1).ToList();

		// Assert
		_ = pruned.Should().BeEmpty();
	}

	#endregion
}
