using PanoramicData.Chunker.Models;
using PanoramicData.Chunker.Infrastructure;

namespace PanoramicData.Chunker.Tests.Unit.Core;

/// <summary>
/// Tests for ChunkerBase and core data models.
/// </summary>
public class ChunkerBaseTests
{
	[Fact]
	public void ChunkerBase_ShouldGenerateUniqueId()
	{
		// Arrange & Act
		var chunk1 = new TestContentChunk();
		var chunk2 = new TestContentChunk();

		// Assert
		Assert.NotEqual(Guid.Empty, chunk1.Id);
		Assert.NotEqual(Guid.Empty, chunk2.Id);
		Assert.NotEqual(chunk1.Id, chunk2.Id);
	}

	[Fact]
	public void ChunkerBase_ShouldInitializeWithDefaultValues()
	{
		// Arrange & Act
		var chunk = new TestContentChunk();

		// Assert
		Assert.NotEqual(Guid.Empty, chunk.Id);
		Assert.Null(chunk.ParentId);
		Assert.Equal(string.Empty, chunk.SpecificType);
		Assert.NotNull(chunk.Metadata);
		Assert.Equal(0, chunk.Depth);
		Assert.Equal(0, chunk.SequenceNumber);
		Assert.Empty(chunk.AncestorIds);
		Assert.Null(chunk.QualityMetrics);
	}

	[Fact]
	public void StructuralChunk_ShouldInitializeWithEmptyChildren()
	{
		// Arrange & Act
		var chunk = new TestStructuralChunk();

		// Assert
		Assert.NotNull(chunk.Children);
		Assert.Empty(chunk.Children);
	}

	[Fact]
	public void ContentChunk_ShouldAllowContentAssignment()
	{
		// Arrange
		var chunk = new TestContentChunk();
		const string content = "Test content";

		// Act
		chunk.Content = content;

		// Assert
		Assert.Equal(content, chunk.Content);
	}

	// Test helper classes
	private class TestContentChunk : ContentChunk { }
	private class TestStructuralChunk : StructuralChunk { }
}
