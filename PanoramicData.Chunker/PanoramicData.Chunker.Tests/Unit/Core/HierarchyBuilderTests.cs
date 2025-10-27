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
		Assert.Equal(0, root.Depth);
		Assert.Equal(1, child1.Depth);
		Assert.Equal(2, child2.Depth);
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
		_ = Assert.Single(child1.AncestorIds);
		Assert.Equal(root.Id, child1.AncestorIds[0]);
		Assert.Equal(2, child2.AncestorIds.Length);
		Assert.Equal(root.Id, child2.AncestorIds[0]);
		Assert.Equal(child1.Id, child2.AncestorIds[1]);
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
		Assert.Equal(2, root.Children.Count);
		_ = root.Children.Should().Contain(child1);
		Assert.Contains(child2, root.Children);
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
		_ = Assert.Single(roots);
		Assert.Contains(root, roots);
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
		Assert.Equal(2, leaves.Count);
		Assert.Contains(child1, leaves);
		Assert.Contains(child2, leaves);
		Assert.DoesNotContain(root, leaves);
	}

	// Test helper classes
	private class TestContentChunk : ContentChunk { }
	private class TestStructuralChunk : StructuralChunk { }
}
