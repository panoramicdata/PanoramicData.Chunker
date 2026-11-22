using AwesomeAssertions;
using PanoramicData.Chunker.Chunkers.PlainText;

namespace PanoramicData.Chunker.Tests.Unit.Chunkers;

public partial class PlainTextDocumentChunkerTests
{
	[Fact]
	public async Task ChunkAsync_WithBulletList_ShouldDetectListItems()
	{
		var listItems = await ChunksOfAsync<PlainTextListItemChunk>("Items:\n- First item\n- Second item\n- Third item");

		_ = listItems.Should().HaveCount(3);
		_ = listItems.Should().AllSatisfy(item =>
		{
			_ = item.ListType.Should().Be("bullet");
			_ = item.Marker.Should().Be("-");
			_ = item.IsOrdered.Should().BeFalse();
		});
	}

	[Fact]
	public async Task ChunkAsync_WithNumberedList_ShouldDetectListItems()
	{
		var listItems = await ChunksOfAsync<PlainTextListItemChunk>("Steps:\n1. First step\n2. Second step\n3. Third step");

		_ = listItems.Should().HaveCount(3);
		_ = listItems.Should().AllSatisfy(item =>
		{
			_ = item.ListType.Should().Be("numbered");
			_ = item.IsOrdered.Should().BeTrue();
		});
	}

	[Fact]
	public async Task ChunkAsync_WithAsteriskBullets_ShouldDetectListItems()
	{
		await AssertMarkersAsync("* First\n* Second\n* Third", "*");
	}

	[Fact]
	public async Task ChunkAsync_WithUnicodeBullets_ShouldDetectListItems()
	{
		await AssertMarkersAsync("• First\n• Second\n• Third", "•");
	}

	[Fact]
	public async Task ChunkAsync_WithNestedList_ShouldDetectNestingLevels()
	{
		var listItems = await ChunksOfAsync<PlainTextListItemChunk>("- Level 0\n  - Level 1\n    - Level 2");

		_ = listItems.Should().HaveCount(3);
		_ = listItems.Select(item => item.NestingLevel).Should().Equal(0, 1, 2);
	}

	[Fact]
	public async Task ChunkAsync_WithFencedCodeBlock_ShouldDetectCode()
	{
		var codeBlocks = await ChunksOfAsync<PlainTextCodeBlockChunk>("Text before\n\n```csharp\npublic class Test {}\n```\n\nText after");

		_ = codeBlocks.Should().ContainSingle();
		_ = codeBlocks[0].IsFenced.Should().BeTrue();
		_ = codeBlocks[0].Language.Should().Be("csharp");
		_ = codeBlocks[0].Content.Should().Contain("public class Test");
	}

	[Fact]
	public async Task ChunkAsync_WithFencedCodeBlockNoLanguage_ShouldDetectCode()
	{
		var codeBlocks = await ChunksOfAsync<PlainTextCodeBlockChunk>("```\nsome code\n```");

		_ = codeBlocks.Should().ContainSingle();
		_ = codeBlocks[0].IsFenced.Should().BeTrue();
		_ = codeBlocks[0].Language.Should().BeNull();
	}

	[Fact]
	public async Task ChunkAsync_WithIndentedCode_ShouldDetectCode()
	{
		var codeBlocks = await ChunksOfAsync<PlainTextCodeBlockChunk>("Normal text\n\n    public class Test\n    {\n      // code\n    }\n\nBack to normal");

		_ = codeBlocks.Should().ContainSingle();
		_ = codeBlocks[0].IsFenced.Should().BeFalse();
		_ = codeBlocks[0].IndentationLevel.Should().Be(4);
	}

	[Fact]
	public async Task ChunkAsync_WithCodeIndicators_ShouldPopulateIndicators()
	{
		var codeBlocks = await ChunksOfAsync<PlainTextCodeBlockChunk>("    public class Test\n    {\n        public void Method() { }\n    }");

		_ = codeBlocks.Should().ContainSingle();
		_ = codeBlocks[0].CodeIndicators.Should().Contain("public");
		_ = codeBlocks[0].CodeIndicators.Should().Contain("class");
	}

	private async Task AssertMarkersAsync(string text, string marker)
	{
		var listItems = await ChunksOfAsync<PlainTextListItemChunk>(text);

		_ = listItems.Should().HaveCount(3);
		_ = listItems.Should().AllSatisfy(item => item.Marker.Should().Be(marker));
	}
}
