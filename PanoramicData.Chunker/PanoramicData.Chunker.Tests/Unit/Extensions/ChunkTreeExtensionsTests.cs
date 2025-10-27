using AwesomeAssertions;
using PanoramicData.Chunker.Chunkers.Markdown;
using PanoramicData.Chunker.Extensions;
using PanoramicData.Chunker.Models;
using Xunit;

namespace PanoramicData.Chunker.Tests.Unit.Extensions;

/// <summary>
/// Tests for ChunkTreeExtensions.
/// </summary>
public class ChunkTreeExtensionsTests
{
	private readonly List<ChunkerBase> _testChunks;

	public ChunkTreeExtensionsTests()
	{
		_testChunks = CreateTestHierarchy();
	}

	private static List<ChunkerBase> CreateTestHierarchy()
	{
		var chunks = new List<ChunkerBase>();

		// Root section (depth 0)
		var section1 = new MarkdownSectionChunk
		{
			Id = Guid.NewGuid(),
			SpecificType = "Section",
			HeadingLevel = 1,
			HeadingText = "Introduction",
			Depth = 0,
			SequenceNumber = 0,
			AncestorIds = [],
			Metadata = new ChunkMetadata
			{
				DocumentType = "Markdown",
				Hierarchy = "Introduction"
			}
		};
		chunks.Add(section1);

		// Child paragraph (depth 1)
		var para1 = new MarkdownParagraphChunk
		{
			Id = Guid.NewGuid(),
			ParentId = section1.Id,
			SpecificType = "Paragraph",
			Content = "First paragraph",
			Depth = 1,
			SequenceNumber = 1,
			AncestorIds = [section1.Id],
			Metadata = new ChunkMetadata
			{
				DocumentType = "Markdown",
				Hierarchy = "Introduction > Paragraph"
			}
		};
		chunks.Add(para1);

		// Another child paragraph (depth 1)
		var para2 = new MarkdownParagraphChunk
		{
			Id = Guid.NewGuid(),
			ParentId = section1.Id,
			SpecificType = "Paragraph",
			Content = "Second paragraph",
			Depth = 1,
			SequenceNumber = 2,
			AncestorIds = [section1.Id],
			Metadata = new ChunkMetadata
			{
				DocumentType = "Markdown",
				Hierarchy = "Introduction > Paragraph"
			}
		};
		chunks.Add(para2);

		// Second root section (depth 0)
		var section2 = new MarkdownSectionChunk
		{
			Id = Guid.NewGuid(),
			SpecificType = "Section",
			HeadingLevel = 1,
			HeadingText = "Methods",
			Depth = 0,
			SequenceNumber = 3,
			AncestorIds = [],
			Metadata = new ChunkMetadata
			{
				DocumentType = "Markdown",
				Hierarchy = "Methods"
			}
		};
		chunks.Add(section2);

		// Subsection (depth 1)
		var subsection = new MarkdownSectionChunk
		{
			Id = Guid.NewGuid(),
			ParentId = section2.Id,
			SpecificType = "Subsection",
			HeadingLevel = 2,
			HeadingText = "API Methods",
			Depth = 1,
			SequenceNumber = 4,
			AncestorIds = [section2.Id],
			Metadata = new ChunkMetadata
			{
				DocumentType = "Markdown",
				Hierarchy = "Methods > API Methods"
			}
		};
		chunks.Add(subsection);

		// Code block (depth 2)
		var code = new MarkdownCodeBlockChunk
		{
			Id = Guid.NewGuid(),
			ParentId = subsection.Id,
			SpecificType = "Code",
			Content = "public void Method() { }",
			Language = "csharp",
			Depth = 2,
			SequenceNumber = 5,
			AncestorIds = [section2.Id, subsection.Id],
			Metadata = new ChunkMetadata
			{
				DocumentType = "Markdown",
				Hierarchy = "Methods > API Methods > Code"
			}
		};
		chunks.Add(code);

		// Paragraph under subsection (depth 2)
		var para3 = new MarkdownParagraphChunk
		{
			Id = Guid.NewGuid(),
			ParentId = subsection.Id,
			SpecificType = "Paragraph",
			Content = "Description of method",
			Depth = 2,
			SequenceNumber = 6,
			AncestorIds = [section2.Id, subsection.Id],
			Metadata = new ChunkMetadata
			{
				DocumentType = "Markdown",
				Hierarchy = "Methods > API Methods > Paragraph"
			}
		};
		chunks.Add(para3);

		return chunks;
	}

	#region BuildTree Tests

	[Fact]
	public void BuildTree_ShouldPopulateChildrenCollections()
	{
		// Act
		var roots = _testChunks.BuildTree().ToList();

		// Assert
		_ = roots.Should().HaveCount(2);
		_ = roots.Should().AllBeOfType<MarkdownSectionChunk>();

		var section1 = (MarkdownSectionChunk)roots[0];
		_ = section1.Children.Should().HaveCount(2);
		_ = section1.Children.Should().AllBeOfType<MarkdownParagraphChunk>();

		var section2 = (MarkdownSectionChunk)roots[1];
		_ = section2.Children.Should().ContainSingle();
		_ = section2.Children[0].Should().BeOfType<MarkdownSectionChunk>();
	}

	[Fact]
	public void BuildTree_ShouldReturnOnlyRootChunks()
	{
		// Act
		var roots = _testChunks.BuildTree().ToList();

		// Assert
		_ = roots.Should().HaveCount(2);
		_ = roots.Should().OnlyContain(c => !c.ParentId.HasValue);
	}

	[Fact]
	public void BuildTree_ShouldHandleEmptyList()
	{
		// Arrange
		var emptyList = new List<ChunkerBase>();

		// Act
		var roots = emptyList.BuildTree().ToList();

		// Assert
		_ = roots.Should().BeEmpty();
	}

	#endregion

	#region FlattenTree Tests

	[Fact]
	public void FlattenTree_ShouldReturnAllChunks()
	{
		// Arrange
		var roots = _testChunks.BuildTree().ToList();

		// Act
		var flattened = roots.FlattenTree().ToList();

		// Assert
		_ = flattened.Should().HaveCount(7);
	}

	[Fact]
	public void FlattenTree_ShouldMaintainSequenceOrder()
	{
		// Arrange
		var roots = _testChunks.BuildTree().ToList();

		// Act
		var flattened = roots.FlattenTree().ToList();

		// Assert
		_ = flattened.Should().BeInAscendingOrder(c => c.SequenceNumber);
	}

	[Fact]
	public void FlattenTree_ShouldHandleEmptyList()
	{
		// Arrange
		var emptyList = new List<ChunkerBase>();

		// Act
		var flattened = emptyList.FlattenTree().ToList();

		// Assert
		_ = flattened.Should().BeEmpty();
	}

	#endregion

	#region TraverseDepthFirst Tests

	[Fact]
	public void TraverseDepthFirst_ShouldVisitAllChunksInDepthFirstOrder()
	{
		// Arrange
		var roots = _testChunks.BuildTree().ToList();
		var section1 = roots[0];
		var visited = new List<string>();

		// Act
		section1.TraverseDepthFirst(c => visited.Add(c.SpecificType));

		// Assert
		_ = visited.Should().HaveCount(3);
		_ = visited[0].Should().Be("Section");
		_ = visited[1].Should().Be("Paragraph");
		_ = visited[2].Should().Be("Paragraph");
	}

	[Fact]
	public void TraverseDepthFirst_ShouldVisitRootFirst()
	{
		// Arrange
		var roots = _testChunks.BuildTree().ToList();
		var section2 = roots[1];
		var visited = new List<ChunkerBase>();

		// Act
		section2.TraverseDepthFirst(c => visited.Add(c));

		// Assert
		_ = visited.First().Should().Be(section2);
	}

	[Fact]
	public void TraverseDepthFirst_ShouldVisitDeepestNodesBeforeSiblings()
	{
		// Arrange
		var roots = _testChunks.BuildTree().ToList();
		var section2 = roots[1];
		var visited = new List<string>();

		// Act
		section2.TraverseDepthFirst(c => visited.Add($"{c.SpecificType}-{c.SequenceNumber}"));

		// Assert
		_ = visited.Should().HaveCount(4);
		// Should visit: section2, subsection, code, para3
		_ = visited[0].Should().Be("Section-3");
		_ = visited[1].Should().Be("Subsection-4");
		_ = visited[2].Should().Be("Code-5");
		_ = visited[3].Should().Be("Paragraph-6");
	}

	#endregion

	#region TraverseBreadthFirst Tests

	[Fact]
	public void TraverseBreadthFirst_ShouldVisitAllChunksInBreadthFirstOrder()
	{
		// Arrange
		var roots = _testChunks.BuildTree().ToList();
		var section2 = roots[1];
		var visited = new List<string>();

		// Act
		section2.TraverseBreadthFirst(c => visited.Add(c.SpecificType));

		// Assert
		_ = visited.Should().HaveCount(4);
		_ = visited[0].Should().Be("Section"); // Root
		_ = visited[1].Should().Be("Subsection"); // Level 1
		_ = visited[2].Should().Be("Code"); // Level 2
		_ = visited[3].Should().Be("Paragraph"); // Level 2
	}

	[Fact]
	public void TraverseBreadthFirst_ShouldVisitAllChildrenBeforeGrandchildren()
	{
		// Arrange
		var roots = _testChunks.BuildTree().ToList();
		var section2 = roots[1];
		var depths = new List<int>();

		// Act
		section2.TraverseBreadthFirst(c => depths.Add(c.Depth));

		// Assert
		_ = depths.Should().Equal(0, 1, 2, 2);
	}

	#endregion

	#region GetPathFromRoot Tests

	[Fact]
	public void GetPathFromRoot_ShouldReturnPathIncludingChunk()
	{
		// Arrange
		var code = _testChunks.OfType<MarkdownCodeBlockChunk>().First();

		// Act
		var path = code.GetPathFromRoot(_testChunks);

		// Assert
		_ = path.Should().HaveCount(3);
		_ = path[0].Should().BeOfType<MarkdownSectionChunk>(); // section2
		_ = path[1].Should().BeOfType<MarkdownSectionChunk>(); // subsection
		_ = path[2].Should().BeOfType<MarkdownCodeBlockChunk>(); // code
	}

	[Fact]
	public void GetPathFromRoot_ForRootChunk_ShouldReturnSingleItem()
	{
		// Arrange
		var section1 = _testChunks.First();

		// Act
		var path = section1.GetPathFromRoot(_testChunks);

		// Assert
		_ = path.Should().ContainSingle();
		_ = path.Should().HaveElementAt(0, section1);
	}

	[Fact]
	public void GetPathFromRoot_ShouldBeOrderedFromRootToChunk()
	{
		// Arrange
		var para3 = _testChunks.OfType<MarkdownParagraphChunk>().Last();

		// Act
		var path = para3.GetPathFromRoot(_testChunks);

		// Assert
		_ = path.Should().BeInAscendingOrder(c => c.Depth);
	}

	#endregion

	#region GetHierarchyPath Tests

	[Fact]
	public void GetHierarchyPath_ShouldReturnFormattedPath()
	{
		// Arrange
		var code = _testChunks.OfType<MarkdownCodeBlockChunk>().First();

		// Act
		var path = code.GetHierarchyPath(_testChunks);

		// Assert
		_ = path.Should().Be("Section > Subsection > Code");
	}

	[Fact]
	public void GetHierarchyPath_WithCustomSeparator_ShouldUseCustomSeparator()
	{
		// Arrange
		var code = _testChunks.OfType<MarkdownCodeBlockChunk>().First();

		// Act
		var path = code.GetHierarchyPath(_testChunks, " / ");

		// Assert
		_ = path.Should().Be("Section / Subsection / Code");
	}

	[Fact]
	public void GetHierarchyPath_ForRootChunk_ShouldReturnSingleType()
	{
		// Arrange
		var section1 = _testChunks.First();

		// Act
		var path = section1.GetHierarchyPath(_testChunks);

		// Assert
		_ = path.Should().Be("Section");
	}

	#endregion

	#region FilterTreeByPredicate Tests

	[Fact]
	public void FilterTreeByPredicate_ShouldIncludeMatchingChunksAndAncestors()
	{
		// Act
		var filtered = _testChunks.FilterTreeByPredicate(c => c.SpecificType == "Code").ToList();

		// Assert
		_ = filtered.Should().HaveCount(3); // section2, subsection, code
		_ = filtered.Should().Contain(c => c.SpecificType == "Code");
		_ = filtered.Should().Contain(c => c.SpecificType == "Section");
		_ = filtered.Should().Contain(c => c.SpecificType == "Subsection");
	}

	[Fact]
	public void FilterTreeByPredicate_ShouldExcludeUnrelatedBranches()
	{
		// Act
		var filtered = _testChunks.FilterTreeByPredicate(c => c.SpecificType == "Code").ToList();

		// Assert
		_ = filtered.Should().NotContain(c => c.SpecificType == "Paragraph");
	}

	[Fact]
	public void FilterTreeByPredicate_WithNoMatches_ShouldReturnEmpty()
	{
		// Act
		var filtered = _testChunks.FilterTreeByPredicate(c => c.SpecificType == "NonExistent").ToList();

		// Assert
		_ = filtered.Should().BeEmpty();
	}

	[Fact]
	public void FilterTreeByPredicate_WithMultipleMatches_ShouldIncludeAllBranches()
	{
		// Act
		var filtered = _testChunks.FilterTreeByPredicate(c => c.SpecificType == "Paragraph").ToList();

		// Assert
		_ = filtered.Should().HaveCountGreaterThan(3); // Multiple paragraphs and their ancestors
	}

	#endregion

	#region CloneSubtree Tests

	[Fact]
	public void CloneSubtree_WithDescendants_ShouldIncludeAllDescendants()
	{
		// Arrange
		var section2 = _testChunks.First(c => c.SequenceNumber == 3);

		// Act
		var subtree = section2.CloneSubtree(_testChunks, includeDescendants: true);

		// Assert
		_ = subtree.Should().HaveCount(4); // section2, subsection, code, para3
	}

	[Fact]
	public void CloneSubtree_WithoutDescendants_ShouldReturnOnlyRoot()
	{
		// Arrange
		var section2 = _testChunks.First(c => c.SequenceNumber == 3);

		// Act
		var subtree = section2.CloneSubtree(_testChunks, includeDescendants: false);

		// Assert
		_ = subtree.Should().ContainSingle();
		_ = subtree.Should().HaveElementAt(0, section2);
	}

	[Fact]
	public void CloneSubtree_ForLeafNode_ShouldReturnSingleItem()
	{
		// Arrange
		var code = _testChunks.OfType<MarkdownCodeBlockChunk>().First();

		// Act
		var subtree = code.CloneSubtree(_testChunks, includeDescendants: true);

		// Assert
		_ = subtree.Should().ContainSingle();
		_ = subtree.Should().HaveElementAt(0, code);
	}

	#endregion

	#region CountDescendants Tests

	[Fact]
	public void CountDescendants_ShouldReturnCorrectCount()
	{
		// Arrange
		var section2 = _testChunks.First(c => c.SequenceNumber == 3);

		// Act
		var count = section2.CountDescendants(_testChunks);

		// Assert
		_ = count.Should().Be(3); // subsection, code, para3
	}

	[Fact]
	public void CountDescendants_ForLeafNode_ShouldReturnZero()
	{
		// Arrange
		var code = _testChunks.OfType<MarkdownCodeBlockChunk>().First();

		// Act
		var count = code.CountDescendants(_testChunks);

		// Assert
		_ = count.Should().Be(0);
	}

	[Fact]
	public void CountDescendants_ForIntermediateNode_ShouldCountAllLevels()
	{
		// Arrange
		var subsection = _testChunks.First(c => c.SpecificType == "Subsection");

		// Act
		var count = subsection.CountDescendants(_testChunks);

		// Assert
		_ = count.Should().Be(2); // code, para3
	}

	#endregion

	#region HasDescendants Tests

	[Fact]
	public void HasDescendants_ForParentNode_ShouldReturnTrue()
	{
		// Arrange
		var section1 = _testChunks.First();

		// Act
		var hasDescendants = section1.HasDescendants(_testChunks);

		// Assert
		_ = hasDescendants.Should().BeTrue();
	}

	[Fact]
	public void HasDescendants_ForLeafNode_ShouldReturnFalse()
	{
		// Arrange
		var code = _testChunks.OfType<MarkdownCodeBlockChunk>().First();

		// Act
		var hasDescendants = code.HasDescendants(_testChunks);

		// Assert
		_ = hasDescendants.Should().BeFalse();
	}

	#endregion

	#region IsAncestorOf Tests

	[Fact]
	public void IsAncestorOf_WhenIsAncestor_ShouldReturnTrue()
	{
		// Arrange
		var section2 = _testChunks.First(c => c.SequenceNumber == 3);
		var code = _testChunks.OfType<MarkdownCodeBlockChunk>().First();

		// Act
		var isAncestor = section2.IsAncestorOf(code);

		// Assert
		_ = isAncestor.Should().BeTrue();
	}

	[Fact]
	public void IsAncestorOf_WhenNotAncestor_ShouldReturnFalse()
	{
		// Arrange
		var section1 = _testChunks.First();
		var code = _testChunks.OfType<MarkdownCodeBlockChunk>().First();

		// Act
		var isAncestor = section1.IsAncestorOf(code);

		// Assert
		_ = isAncestor.Should().BeFalse();
	}

	[Fact]
	public void IsAncestorOf_ForSelf_ShouldReturnFalse()
	{
		// Arrange
		var section1 = _testChunks.First();

		// Act
		var isAncestor = section1.IsAncestorOf(section1);

		// Assert
		_ = isAncestor.Should().BeFalse();
	}

	#endregion

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
