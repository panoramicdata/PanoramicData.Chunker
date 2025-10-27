using PanoramicData.Chunker.Models;
using AwesomeAssertions;

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
		_ = chunk1.Id.Should().NotBe(Guid.Empty);
		_ = chunk2.Id.Should().NotBe(Guid.Empty);
		_ = chunk1.Id.Should().NotBe(chunk2.Id);
	}

	[Fact]
	public void ChunkerBase_ShouldInitializeWithDefaultValues()
	{
		// Arrange & Act
		var chunk = new TestContentChunk();

		// Assert
		_ = chunk.Id.Should().NotBe(Guid.Empty);
		_ = chunk.ParentId.Should().BeNull();
		_ = chunk.SpecificType.Should().Be(string.Empty);
		_ = chunk.Metadata.Should().NotBeNull();
		_ = chunk.Depth.Should().Be(0);
		_ = chunk.SequenceNumber.Should().Be(0);
		_ = chunk.AncestorIds.Should().BeEmpty();
		_ = chunk.QualityMetrics.Should().BeNull();
	}

	[Fact]
	public void StructuralChunk_ShouldInitializeWithEmptyChildren()
	{
		// Arrange & Act
		var chunk = new TestStructuralChunk();

		// Assert
		_ = chunk.Children.Should().NotBeNull();
		_ = chunk.Children.Should().BeEmpty();
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
		_ = chunk.Content.Should().Be(content);
	}

	// Test helper classes
	private class TestContentChunk : ContentChunk { }
	private class TestStructuralChunk : StructuralChunk { }
}
