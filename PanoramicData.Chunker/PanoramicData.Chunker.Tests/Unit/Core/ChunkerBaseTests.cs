using PanoramicData.Chunker.Models;
using FluentAssertions;

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
		chunk1.Id.Should().NotBe(Guid.Empty);
		chunk2.Id.Should().NotBe(Guid.Empty);
		chunk1.Id.Should().NotBe(chunk2.Id);
	}

	[Fact]
	public void ChunkerBase_ShouldInitializeWithDefaultValues()
	{
		// Arrange & Act
		var chunk = new TestContentChunk();

		// Assert
		chunk.Id.Should().NotBe(Guid.Empty);
		chunk.ParentId.Should().BeNull();
		chunk.SpecificType.Should().Be(string.Empty);
		chunk.Metadata.Should().NotBeNull();
		chunk.Depth.Should().Be(0);
		chunk.SequenceNumber.Should().Be(0);
		chunk.AncestorIds.Should().BeEmpty();
		chunk.QualityMetrics.Should().BeNull();
	}

	[Fact]
	public void StructuralChunk_ShouldInitializeWithEmptyChildren()
	{
		// Arrange & Act
		var chunk = new TestStructuralChunk();

		// Assert
		chunk.Children.Should().NotBeNull();
		chunk.Children.Should().BeEmpty();
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
		chunk.Content.Should().Be(content);
	}

	// Test helper classes
	private class TestContentChunk : ContentChunk { }
	private class TestStructuralChunk : StructuralChunk { }
}
