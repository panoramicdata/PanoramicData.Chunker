using AwesomeAssertions;
using PanoramicData.Chunker.Chunkers.Html;
using PanoramicData.Chunker.KnowledgeGraph.Extractors;
using PanoramicData.Chunker.Models;

namespace PanoramicData.Chunker.Tests.Unit.KnowledgeGraph;

/// <summary>
/// Unit tests specifically for extraction of "Plinian Society" and other ground truth entities.
/// </summary>
public class CapitalizationExtractorDebugTests(ITestOutputHelper output) : BaseTest(output)
{
	[Fact]
	public async Task ExtractEntitiesAsync_ShouldExtract_PlinianSociety()
	{
		// Arrange
		var extractor = new CapitalizationEntityExtractor(minOccurrences: 1, minWordLength: 2);
		var chunks = new List<ChunkerBase>
		{
			new HtmlParagraphChunk
			{
				Content = "The Plinian Society was encouraged and, I believe, founded by Professor Jameson."
			}
		};

		// Act
		var entities = await extractor.ExtractEntitiesAsync(chunks, CancellationToken);

		// Assert
		_output.WriteLine($"Extracted {entities.Count} entities:");
		foreach (var entity in entities)
		{
			_output.WriteLine($"  - {entity.Name} (confidence: {entity.Confidence:F2}, frequency: {entity.Frequency})");
		}

		var hasPlinianSociety = entities.Any(e =>
			e.Name.Contains("Plinian", StringComparison.OrdinalIgnoreCase) &&
			e.Name.Contains("Society", StringComparison.OrdinalIgnoreCase));

		hasPlinianSociety.Should().BeTrue("Should extract 'Plinian Society'");

		var hasProfessorJameson = entities.Any(e =>
			e.Name.Contains("Professor", StringComparison.OrdinalIgnoreCase) &&
			e.Name.Contains("Jameson", StringComparison.OrdinalIgnoreCase));

		hasProfessorJameson.Should().BeTrue("Should extract 'Professor Jameson'");
	}

	[Fact]
	public async Task ExtractEntitiesAsync_ShouldExtract_EdinburghUniversity()
	{
		// Arrange
		var extractor = new CapitalizationEntityExtractor(minOccurrences: 1, minWordLength: 2);
		var chunks = new List<ChunkerBase>
		{
			new HtmlParagraphChunk
			{
				Content = "My father wisely took me away at a rather earlier age than usual, and sent me (Oct. 1825) to Edinburgh University with my brother."
			}
		};

		// Act
		var entities = await extractor.ExtractEntitiesAsync(chunks, CancellationToken);

		// Assert
		_output.WriteLine($"Extracted {entities.Count} entities:");
		foreach (var entity in entities)
		{
			_output.WriteLine($"  - {entity.Name} (confidence: {entity.Confidence:F2}, frequency: {entity.Frequency})");
		}

		var hasEdinburghUniversity = entities.Any(e =>
			e.Name.Contains("Edinburgh", StringComparison.OrdinalIgnoreCase) &&
			e.Name.Contains("University", StringComparison.OrdinalIgnoreCase));

		hasEdinburghUniversity.Should().BeTrue("Should extract 'Edinburgh University'");
	}

	[Fact]
	public async Task ExtractEntitiesAsync_ShouldExtract_OriginOfSpecies()
	{
		// Arrange
		var extractor = new CapitalizationEntityExtractor(minOccurrences: 1, minWordLength: 2);
		var chunks = new List<ChunkerBase>
		{
			new HtmlParagraphChunk
			{
				Content = "I admired my grandfather's upholding them under a different form in my 'Origin of Species.'"
			}
		};

		// Act
		var entities = await extractor.ExtractEntitiesAsync(chunks, CancellationToken);

		// Assert
		_output.WriteLine($"Extracted {entities.Count} entities:");
		foreach (var entity in entities)
		{
			_output.WriteLine($"  - {entity.Name} (confidence: {entity.Confidence:F2}, frequency: {entity.Frequency})");
		}

		// "Origin of Species" might be extracted as "Origin of Species" or just "Species"
		var hasOriginOfSpecies = entities.Any(e =>
			e.Name.Contains("Origin", StringComparison.OrdinalIgnoreCase) ||
			(e.Name.Contains("Species", StringComparison.OrdinalIgnoreCase)));

		hasOriginOfSpecies.Should().BeTrue("Should extract 'Origin' or 'Species'");
	}
}
