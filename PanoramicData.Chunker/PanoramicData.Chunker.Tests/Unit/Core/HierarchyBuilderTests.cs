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
		// Arrange
		var root = new TestStructuralChunk { Id = Guid.NewGuid() };
		var child1 = new TestContentChunk { Id = Guid.NewGuid(), ParentId = root.Id };
		var child2 = new TestContentChunk { Id = Guid.NewGuid(), ParentId = child1.Id };

		var chunks = new List<ChunkerBase> { root, child1, child2 };

		// Act
		HierarchyBuilder.BuildHierarchy(chunks);

		// Assert
		root.Depth.Should().Be(0);
		child1.Depth.Should().Be(1);
		child2.Depth.Should().Be(2);
	}

	[Fact]
	public void BuildHierarchy_ShouldSetCorrectAncestorIds()
	{
		// Arrange
		var root = new TestStructuralChunk { Id = Guid.NewGuid() };
		var child1 = new TestContentChunk { Id = Guid.NewGuid(), ParentId = root.Id };
		var child2 = new TestContentChunk { Id = Guid.NewGuid(), ParentId = child1.Id };

		var chunks = new List<ChunkerBase> { root, child1, child2 };

		// Act
		HierarchyBuilder.BuildHierarchy(chunks);

		// Assert
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
		// Arrange
		var root = new TestStructuralChunk { Id = Guid.NewGuid() };
		var child1 = new TestContentChunk { Id = Guid.NewGuid(), ParentId = root.Id };
		var child2 = new TestContentChunk { Id = Guid.NewGuid(), ParentId = root.Id };

		var chunks = new List<ChunkerBase> { root, child1, child2 };

		// Act
		HierarchyBuilder.PopulateChildren(chunks);

		// Assert
		root.Children.Should().HaveCount(2);
		_ = root.Children.Should().Contain(child1);
		root.Children.Should().Contain(child2);
	}

	[Fact]
	public void GetRootChunks_ShouldReturnOnlyRoots()
	{
		// Arrange
		var root = new TestStructuralChunk { Id = Guid.NewGuid() };
		var child = new TestContentChunk { Id = Guid.NewGuid(), ParentId = root.Id };

		var chunks = new List<ChunkerBase> { root, child };

		// Act
		var roots = HierarchyBuilder.GetRootChunks(chunks).ToList();

		// Assert
		roots.Should().ContainSingle();
		roots.Should().Contain(root);
	}

	[Fact]
	public void GetLeafChunks_ShouldReturnOnlyLeaves()
	{
		// Arrange
		var root = new TestStructuralChunk { Id = Guid.NewGuid() };
		var child1 = new TestContentChunk { Id = Guid.NewGuid(), ParentId = root.Id };
		var child2 = new TestContentChunk { Id = Guid.NewGuid(), ParentId = root.Id };

		var chunks = new List<ChunkerBase> { root, child1, child2 };

		// Act
		var leaves = HierarchyBuilder.GetLeafChunks(chunks).ToList();

		// Assert
		leaves.Should().HaveCount(2);
		leaves.Should().Contain(child1);
		leaves.Should().Contain(child2);
		leaves.Should().NotContain(root);
	}

	// Test helper classes
	private class TestContentChunk : ContentChunk { }
	private class TestStructuralChunk : StructuralChunk { }
}
