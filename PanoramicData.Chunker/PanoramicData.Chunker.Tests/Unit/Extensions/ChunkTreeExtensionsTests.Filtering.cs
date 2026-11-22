using AwesomeAssertions;
using PanoramicData.Chunker.Chunkers.Markdown;
using PanoramicData.Chunker.Extensions;
using PanoramicData.Chunker.Models;
using PanoramicData.Chunker.Tests.Utilities;
using Xunit;

namespace PanoramicData.Chunker.Tests.Unit.Extensions;

public partial class ChunkTreeExtensionsTests
{
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
}
