using AwesomeAssertions;
using PanoramicData.Chunker.Models;
using PanoramicData.Chunker.Utilities;

namespace PanoramicData.Chunker.Tests.Unit.Core;

/// <summary>
/// Tests for HierarchyBuilder utilities.
/// </summary>
public class HierarchyBuilderTests
{
	[Fact]
	public void BuildHierarchy_ShouldSetCorrectDepth()
	{
		var (root, child1, child2) = CreateChain();

		HierarchyBuilder.BuildHierarchy([root, child1, child2]);

		root.Depth.Should().Be(0);
		child1.Depth.Should().Be(1);
		child2.Depth.Should().Be(2);
	}

	[Fact]
	public void BuildHierarchy_ShouldSetCorrectAncestorIds()
	{
		var (root, child1, child2) = CreateChain();

		HierarchyBuilder.BuildHierarchy([root, child1, child2]);

		_ = root.AncestorIds.Should().BeEmpty();
		child1.AncestorIds.Should().ContainSingle();
		child1.AncestorIds[0].Should().Be(root.Id);
		child2.AncestorIds.Should().HaveCount(2);
		child2.AncestorIds[0].Should().Be(root.Id);
		child2.AncestorIds[1].Should().Be(child1.Id);
	}

	[Fact]
	public void PopulateChildren_ShouldAddChildrenToParents()
	{
		var (root, child1, child2) = CreateSiblings();

		HierarchyBuilder.PopulateChildren([root, child1, child2]);

		root.Children.Should().HaveCount(2);
		_ = root.Children.Should().Contain(child1);
		root.Children.Should().Contain(child2);
	}

	[Fact]
	public void GetRootChunks_ShouldReturnOnlyRoots()
	{
		var (root, child, _) = CreateSiblings();

		var roots = HierarchyBuilder.GetRootChunks([root, child]).ToList();

		roots.Should().ContainSingle();
		roots.Should().Contain(root);
	}

	[Fact]
	public void GetLeafChunks_ShouldReturnOnlyLeaves()
	{
		var (root, child1, child2) = CreateSiblings();

		var leaves = HierarchyBuilder.GetLeafChunks([root, child1, child2]).ToList();

		leaves.Should().HaveCount(2);
		leaves.Should().Contain(child1);
		leaves.Should().Contain(child2);
		leaves.Should().NotContain(root);
	}

	private static (TestStructuralChunk Root, TestContentChunk Child1, TestContentChunk Child2) CreateChain()
		=> CreateFamily((_, child1) => child1);

	private static (TestStructuralChunk Root, TestContentChunk Child1, TestContentChunk Child2) CreateSiblings()
		=> CreateFamily((root, _) => root);

	private static (TestStructuralChunk Root, TestContentChunk Child1, TestContentChunk Child2) CreateFamily(
		Func<ChunkerBase, ChunkerBase, ChunkerBase> secondChildParent)
	{
		var root = new TestStructuralChunk { Id = Guid.NewGuid() };
		var child1 = new TestContentChunk { Id = Guid.NewGuid(), ParentId = root.Id };
		var child2 = new TestContentChunk { Id = Guid.NewGuid(), ParentId = secondChildParent(root, child1).Id };
		return (root, child1, child2);
	}

	private class TestContentChunk : ContentChunk { }

	private class TestStructuralChunk : StructuralChunk { }
}
