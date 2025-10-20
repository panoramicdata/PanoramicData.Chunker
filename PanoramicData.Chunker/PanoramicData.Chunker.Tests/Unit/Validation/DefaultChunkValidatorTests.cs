using PanoramicData.Chunker.Infrastructure;
using PanoramicData.Chunker.Models;

namespace PanoramicData.Chunker.Tests.Unit.Validation;

/// <summary>
/// Tests for DefaultChunkValidator.
/// </summary>
public class DefaultChunkValidatorTests
{
	private readonly DefaultChunkValidator _validator = new();

	[Fact]
	public async Task ValidateAsync_WithValidChunks_ShouldSucceed()
	{
		// Arrange
		var root = new TestStructuralChunk { Id = Guid.NewGuid() };
		var child = new TestContentChunk { Id = Guid.NewGuid(), ParentId = root.Id, Depth = 1 };

		var chunks = new List<ChunkerBase> { root, child };

		// Act
		var result = await _validator.ValidateAsync(chunks);

		// Assert
		Assert.True(result.IsValid);
		Assert.Empty(result.Issues);
	}

	[Fact]
	public async Task ValidateAsync_WithOrphanedChunk_ShouldFail()
	{
		// Arrange
		var chunk = new TestContentChunk 
		{ 
			Id = Guid.NewGuid(), 
			ParentId = Guid.NewGuid() // Non-existent parent
		};

		var chunks = new List<ChunkerBase> { chunk };

		// Act
		var result = await _validator.ValidateAsync(chunks);

		// Assert
		Assert.False(result.IsValid);
		Assert.True(result.HasOrphanedChunks);
		Assert.NotEmpty(result.Issues);
		Assert.Contains(result.Issues, i => i.Code == "ORPHANED_CHUNK");
	}

	[Fact]
	public async Task ValidateAsync_WithCircularReference_ShouldFail()
	{
		// Arrange
		var chunk1 = new TestStructuralChunk { Id = Guid.NewGuid() };
		var chunk2 = new TestStructuralChunk { Id = Guid.NewGuid(), ParentId = chunk1.Id };
		chunk1.ParentId = chunk2.Id; // Circular reference

		var chunks = new List<ChunkerBase> { chunk1, chunk2 };

		// Act
		var result = await _validator.ValidateAsync(chunks);

		// Assert
		Assert.False(result.IsValid);
		Assert.True(result.HasCircularReferences);
	}

	[Fact]
	public async Task ValidateAsync_WithIncorrectDepth_ShouldFail()
	{
		// Arrange
		var root = new TestStructuralChunk { Id = Guid.NewGuid(), Depth = 0 };
		var child = new TestContentChunk 
		{ 
			Id = Guid.NewGuid(), 
			ParentId = root.Id, 
			Depth = 5 // Should be 1
		};

		var chunks = new List<ChunkerBase> { root, child };

		// Act
		var result = await _validator.ValidateAsync(chunks);

		// Assert
		Assert.False(result.IsValid);
		Assert.True(result.HasInvalidHierarchy);
		Assert.Contains(result.Issues, i => i.Code == "DEPTH_MISMATCH");
	}

	// Test helper classes
	private class TestContentChunk : ContentChunk { }
	private class TestStructuralChunk : StructuralChunk { }
}
