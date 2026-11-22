using AwesomeAssertions;
using PanoramicData.Chunker.Chunkers.Markdown;
using PanoramicData.Chunker.Extensions;
using PanoramicData.Chunker.Models;
using PanoramicData.Chunker.Tests.Utilities;
using Xunit;

namespace PanoramicData.Chunker.Tests.Unit.Extensions;

/// <summary>
/// Tests for ChunkTreeExtensions.
/// </summary>
public partial class ChunkTreeExtensionsTests
{
	private readonly List<ChunkerBase> _testChunks;

	public ChunkTreeExtensionsTests()
	{
		_testChunks = CreateTestHierarchy();
	}

	private static List<ChunkerBase> CreateTestHierarchy()
	{
		var section1 = MarkdownChunkBuilder.Section("Introduction", 1, 0);
		var para1 = MarkdownChunkBuilder.Paragraph("First paragraph", 1, section1);
		var para2 = MarkdownChunkBuilder.Paragraph("Second paragraph", 2, section1);
		var section2 = MarkdownChunkBuilder.Section("Methods", 1, 3);
		var subsection = MarkdownChunkBuilder.Section("API Methods", 2, 4, "Subsection", section2);
		var code = MarkdownChunkBuilder.Code("public void Method() { }", "csharp", 5, subsection);
		var para3 = MarkdownChunkBuilder.Paragraph("Description of method", 6, subsection);

		return [section1, para1, para2, section2, subsection, code, para3];
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
}
