using AwesomeAssertions;
using PanoramicData.Chunker.Chunkers.PlainText;
using PanoramicData.Chunker.Models;

namespace PanoramicData.Chunker.Tests.Unit.Chunkers;

public partial class PlainTextDocumentChunkerTests
{
	[Fact]
	public async Task ChunkAsync_WithUnderlinedHeading_ShouldDetectHeading()
	{
		var result = await ChunkTextAsync("Main Heading\n============\n\nContent here.");

		_ = result.Success.Should().BeTrue();
		var sections = result.Chunks.OfType<PlainTextSectionChunk>().ToList();
		_ = sections.Should().ContainSingle();
		_ = sections[0].HeadingText.Should().Be("Main Heading");
		_ = sections[0].HeadingLevel.Should().Be(1);
		_ = sections[0].HeadingType.Should().Be(HeadingHeuristic.Underlined);
		_ = sections[0].Confidence.Should().BeGreaterThan(0.9);
	}

	[Fact]
	public async Task ChunkAsync_WithDashUnderlinedHeading_ShouldDetectLevel2()
	{
		var sections = await ChunksOfAsync<PlainTextSectionChunk>("Sub Heading\n-----------\n\nContent here.");

		_ = sections.Should().ContainSingle();
		_ = sections[0].HeadingLevel.Should().Be(2);
		_ = sections[0].HeadingType.Should().Be(HeadingHeuristic.Underlined);
	}

	[Fact]
	public async Task ChunkAsync_WithNumberedSection_ShouldDetectHeading()
	{
		var sections = await ChunksOfAsync<PlainTextSectionChunk>("1. Introduction\n\nContent here.\n\n1.1 Background\n\nMore content.");

		_ = sections.Should().HaveCount(2);
		_ = sections[0].HeadingText.Should().Be("Introduction");
		_ = sections[0].HeadingLevel.Should().Be(1);
		_ = sections[0].HeadingType.Should().Be(HeadingHeuristic.Numbered);
		_ = sections[1].HeadingText.Should().Be("Background");
		_ = sections[1].HeadingLevel.Should().Be(2);
	}

	[Fact]
	public async Task ChunkAsync_WithDeepNumberedSection_ShouldDetectCorrectLevel()
	{
		var sections = await ChunksOfAsync<PlainTextSectionChunk>("1.1.1 Deep Section\n\nContent here.");

		_ = sections.Should().ContainSingle();
		_ = sections[0].HeadingLevel.Should().Be(3);
	}

	[Fact]
	public async Task ChunkAsync_WithAllCapsHeading_ShouldDetectHeading()
	{
		var sections = await ChunksOfAsync<PlainTextSectionChunk>("INTRODUCTION\n\nThis is the introduction content.");

		_ = sections.Should().ContainSingle();
		_ = sections[0].HeadingText.Should().Be("INTRODUCTION");
		_ = sections[0].HeadingLevel.Should().Be(1);
		_ = sections[0].HeadingType.Should().Be(HeadingHeuristic.AllCaps);
	}

	[Fact]
	public async Task ChunkAsync_WithPrefixedHeading_ShouldDetectHeading()
	{
		var sections = await ChunksOfAsync<PlainTextSectionChunk>("# Main Heading\n\nContent.\n\n## Sub Heading\n\nMore content.");

		_ = sections.Should().HaveCount(2);
		_ = sections[0].HeadingLevel.Should().Be(1);
		_ = sections[0].HeadingType.Should().Be(HeadingHeuristic.Prefixed);
		_ = sections[1].HeadingLevel.Should().Be(2);
	}

	[Fact]
	public async Task ChunkAsync_WithShortAllCaps_ShouldNotDetectAsHeading()
	{
		var sections = await ChunksOfAsync<PlainTextSectionChunk>("This mentions USA in the text.\n\nMore content.");

		_ = sections.Should().BeEmpty();
	}

	[Fact]
	public async Task ChunkAsync_WithLongAllCaps_ShouldNotDetectAsHeading()
	{
		var sections = await ChunksOfAsync<PlainTextSectionChunk>("THIS IS A VERY LONG LINE THAT EXCEEDS ONE HUNDRED CHARACTERS AND SHOULD NOT BE DETECTED AS A HEADING BECAUSE IT IS TOO LONG\n\nContent.");

		_ = sections.Should().BeEmpty();
	}

	[Fact]
	public async Task ChunkAsync_WithNestedHeadings_ShouldBuildHierarchy()
	{
		var sections = await ChunksOfAsync<PlainTextSectionChunk>("Main\n====\n\nContent 1\n\nSub\n---\n\nContent 2");

		_ = sections.Should().HaveCount(2);

		var mainSection = sections.First(s => s.HeadingLevel == 1);
		var subSection = sections.First(s => s.HeadingLevel == 2);

		_ = mainSection.ParentId.Should().BeNull();
		_ = subSection.ParentId.Should().Be(mainSection.Id);
	}

	[Fact]
	public async Task ChunkAsync_WithContentUnderHeading_ShouldAssignParent()
	{
		var result = await ChunkTextAsync("Heading\n=======\n\nParagraph content.");

		var section = result.Chunks.OfType<PlainTextSectionChunk>().First();
		var paragraph = result.Chunks.OfType<PlainTextParagraphChunk>().First();

		_ = paragraph.ParentId.Should().Be(section.Id);
	}
}
