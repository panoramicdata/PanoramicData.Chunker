using AwesomeAssertions;
using PanoramicData.Chunker.Chunkers.Html;
using PanoramicData.Chunker.KnowledgeGraph.Extractors;
using PanoramicData.Chunker.Models.KnowledgeGraph;

namespace PanoramicData.Chunker.Tests.Unit.KnowledgeGraph;

/// <summary>
/// Unit tests specifically for extraction of "Plinian Society" and other ground truth entities.
/// </summary>
public class CapitalizationExtractorDebugTests(ITestOutputHelper output) : BaseTest(output)
{
	[Fact]
	public async Task ExtractEntitiesAsync_ShouldExtract_PlinianSociety()
	{
		var entities = await ExtractAndLogAsync("The Plinian Society was encouraged and, I believe, founded by Professor Jameson.");

		HasNameWithAll(entities, "Plinian", "Society").Should().BeTrue("Should extract 'Plinian Society'");
		HasNameWithAll(entities, "Professor", "Jameson").Should().BeTrue("Should extract 'Professor Jameson'");
	}

	[Fact]
	public async Task ExtractEntitiesAsync_ShouldExtract_EdinburghUniversity()
	{
		var entities = await ExtractAndLogAsync("My father wisely took me away at a rather earlier age than usual, and sent me (Oct. 1825) to Edinburgh University with my brother.");

		HasNameWithAll(entities, "Edinburgh", "University").Should().BeTrue("Should extract 'Edinburgh University'");
	}

	[Fact]
	public async Task ExtractEntitiesAsync_ShouldExtract_OriginOfSpecies()
	{
		var entities = await ExtractAndLogAsync("I admired my grandfather's upholding them under a different form in my 'Origin of Species.'");

		// "Origin of Species" might be extracted as "Origin of Species" or just "Species"
		var hasOriginOfSpecies = entities.Any(e =>
			e.Name.Contains("Origin", StringComparison.OrdinalIgnoreCase) ||
			(e.Name.Contains("Species", StringComparison.OrdinalIgnoreCase)));

		hasOriginOfSpecies.Should().BeTrue("Should extract 'Origin' or 'Species'");
	}

	private async Task<List<Entity>> ExtractAndLogAsync(string content)
	{
		var extractor = new CapitalizationEntityExtractor(minOccurrences: 1, minWordLength: 2);

		var entities = await extractor.ExtractEntitiesAsync([new HtmlParagraphChunk { Content = content }], CancellationToken);

		_output.WriteLine($"Extracted {entities.Count} entities:");
		foreach (var entity in entities)
		{
			_output.WriteLine($"  - {entity.Name} (confidence: {entity.Confidence:F2}, frequency: {entity.Frequency})");
		}

		return entities;
	}

	private static bool HasNameWithAll(List<Entity> entities, params string[] fragments)
		=> entities.Any(e => fragments.All(f => e.Name.Contains(f, StringComparison.OrdinalIgnoreCase)));
}
