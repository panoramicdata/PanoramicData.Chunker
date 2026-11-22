using AwesomeAssertions;
using PanoramicData.Chunker.Chunkers.Markdown;
using PanoramicData.Chunker.Extensions;
using PanoramicData.Chunker.Models;

namespace PanoramicData.Chunker.Tests.Unit.Extensions;

public partial class ChunkQueryExtensionsTests
{
	[Fact]
	public void GetNext_ShouldReturnNextChunkInSequence()
	{
		// Arrange
		var first = _testChunks.OrderBy(c => c.SequenceNumber).First();

		// Act
		var next = first.GetNext(_testChunks);

		// Assert
		_ = next.Should().NotBeNull();
		_ = next!.SequenceNumber.Should().Be(first.SequenceNumber + 1);
	}

	[Fact]
	public void GetNext_ForLastChunk_ShouldReturnNull()
	{
		// Arrange
		var last = _testChunks.OrderByDescending(c => c.SequenceNumber).First();

		// Act
		var next = last.GetNext(_testChunks);

		// Assert
		_ = next.Should().BeNull();
	}

	[Fact]
	public void GetPrevious_ShouldReturnPreviousChunkInSequence()
	{
		// Arrange
		var second = _testChunks.OrderBy(c => c.SequenceNumber).Skip(1).First();

		// Act
		var previous = second.GetPrevious(_testChunks);

		// Assert
		_ = previous.Should().NotBeNull();
		_ = previous!.SequenceNumber.Should().Be(second.SequenceNumber - 1);
	}

	[Fact]
	public void GetPrevious_ForFirstChunk_ShouldReturnNull()
	{
		// Arrange
		var first = _testChunks.OrderBy(c => c.SequenceNumber).First();

		// Act
		var previous = first.GetPrevious(_testChunks);

		// Assert
		_ = previous.Should().BeNull();
	}

	[Fact]
	public void GetContext_ShouldReturnSurroundingChunks()
	{
		// Arrange
		var middle = _testChunks.OrderBy(c => c.SequenceNumber).Skip(1).First();

		// Act
		var context = middle.GetContext(_testChunks, before: 1, after: 1, includeSelf: true).ToList();

		// Assert
		_ = context.Should().HaveCount(3);
		_ = context.Should().Contain(middle);
	}

	[Fact]
	public void GetContext_WithoutSelf_ShouldExcludeCurrentChunk()
	{
		// Arrange
		var middle = _testChunks.OrderBy(c => c.SequenceNumber).Skip(1).First();

		// Act
		var context = middle.GetContext(_testChunks, before: 1, after: 1, includeSelf: false).ToList();

		// Assert
		_ = context.Should().HaveCount(2);
		_ = context.Should().NotContain(middle);
	}

	[Fact]
	public void GroupByPage_ShouldGroupCorrectly()
	{
		// Arrange
		_testChunks[0].Metadata = new ChunkMetadata { PageNumber = 1 };
		_testChunks[1].Metadata = new ChunkMetadata { PageNumber = 1 };
		_testChunks[2].Metadata = new ChunkMetadata { PageNumber = 2 };

		// Act
		var groups = _testChunks.GroupByPage().ToList();

		// Assert
		_ = groups.Should().HaveCountGreaterThan(0);
		var page1Group = groups.FirstOrDefault(g => g.Key == 1);
		_ = page1Group.Should().NotBeNull();
		_ = page1Group!.Should().HaveCount(2);
	}

	[Fact]
	public void GroupByType_ShouldGroupCorrectly()
	{
		// Act
		var groups = _testChunks.GroupByType().ToList();

		// Assert
		_ = groups.Should().NotBeEmpty();
		_ = groups.Should().AllSatisfy(g => g.Should().NotBeEmpty());
	}
}
