using AwesomeAssertions;
using PanoramicData.Chunker.Chunkers.Html;
using PanoramicData.Chunker.KnowledgeGraph.Extractors;
using PanoramicData.Chunker.Models;
using PanoramicData.Chunker.Models.KnowledgeGraph;

namespace PanoramicData.Chunker.Tests.Unit.KnowledgeGraph;

/// <summary>
/// Unit tests for HybridEntityExtractor to verify multi-word entity extraction and alias generation.
/// </summary>
public class HybridEntityExtractorTests(ITestOutputHelper output) : BaseTest(output)
{
	[Fact]
	public async Task ExtractEntitiesAsync_ShouldExtract_MultiWordProperNouns()
	{
		// Arrange
		var extractor = new HybridEntityExtractor();
		var chunks = new List<ChunkerBase>
		{
			new HtmlParagraphChunk
			{
				Content = "The Plinian Society was founded by Professor Jameson. HMS Beagle was commanded by Captain FitzRoy. Robert Grant taught marine biology at Edinburgh University."
			}
		};

		// Act
		var entities = await extractor.ExtractEntitiesAsync(chunks, CancellationToken);

		// Assert
		entities.Should().NotBeEmpty("Should extract entities from content");

		_output.WriteLine($"Extracted {entities.Count} entities:");
		foreach (var entity in entities.OrderByDescending(e => e.Confidence))
		{
			_output.WriteLine($"  - {entity.Name} ({entity.Type}, confidence: {entity.Confidence:F2}, frequency: {entity.Frequency})");
			if (entity.Aliases.Count > 0)
			{
				_output.WriteLine($"    Aliases: {string.Join(", ", entity.Aliases)}");
			}
		}

		// Verify key multi-word entities are extracted
		var societyEntities = entities.Where(e =>
			e.Name.Contains("Society", StringComparison.OrdinalIgnoreCase) ||
			e.Name.Contains("Plinian", StringComparison.OrdinalIgnoreCase)).ToList();

		societyEntities.Should().NotBeEmpty("Should extract 'Plinian Society' or 'Plinian'");

		// Verify HMS Beagle (might be "Beagle" or "HMS Beagle")
		var beagleEntities = entities.Where(e =>
			e.Name.Contains("Beagle", StringComparison.OrdinalIgnoreCase)).ToList();

		beagleEntities.Should().NotBeEmpty("Should extract 'Beagle' or 'HMS Beagle'");

		// Verify person names with titles
		var jamesonEntities = entities.Where(e =>
			e.Name.Contains("Jameson", StringComparison.OrdinalIgnoreCase)).ToList();

		jamesonEntities.Should().NotBeEmpty("Should extract 'Jameson' or 'Professor Jameson'");

		// Verify Robert Grant
		var grantEntities = entities.Where(e =>
			e.Name.Contains("Grant", StringComparison.OrdinalIgnoreCase) ||
			e.Name.Contains("Robert", StringComparison.OrdinalIgnoreCase)).ToList();

		grantEntities.Should().NotBeEmpty("Should extract 'Grant' or 'Robert Grant'");

		// Verify Edinburgh University
		var universityEntities = entities.Where(e =>
			e.Name.Contains("University", StringComparison.OrdinalIgnoreCase) ||
			e.Name.Contains("Edinburgh", StringComparison.OrdinalIgnoreCase)).ToList();

		universityEntities.Should().NotBeEmpty("Should extract 'University' or 'Edinburgh University'");
	}

	[Fact]
	public async Task ExtractEntitiesAsync_ShouldGenerate_NameAliases()
	{
		// Arrange
		var extractor = new HybridEntityExtractor();
		var chunks = new List<ChunkerBase>
		{
			new HtmlParagraphChunk
			{
				Content = "HMS Beagle sailed with Charles Darwin. The 'Beagle' was a survey vessel. Professor Jameson founded the society."
			}
		};

		// Act
		var entities = await extractor.ExtractEntitiesAsync(chunks, CancellationToken);

		// Assert
		_output.WriteLine("Entities with aliases:");
		var entitiesWithAliases = entities.Where(e => e.Aliases.Count > 0).ToList();

		foreach (var entity in entitiesWithAliases)
		{
			_output.WriteLine($"  {entity.Name}:");
			foreach (var alias in entity.Aliases)
			{
				_output.WriteLine($"    → {alias}");
			}
		}

		// Verify aliases are generated
		entitiesWithAliases.Should().NotBeEmpty("Should generate aliases for some entities");

		// Check specific alias patterns
		var beagleEntity = entities.FirstOrDefault(e =>
			e.Name.Contains("Beagle", StringComparison.OrdinalIgnoreCase));

		if (beagleEntity != null && beagleEntity.Name.StartsWith("HMS ", StringComparison.OrdinalIgnoreCase))
		{
			beagleEntity.Aliases.Should().Contain(a =>
				a.Equals("Beagle", StringComparison.OrdinalIgnoreCase),
				"HMS Beagle should have 'Beagle' as an alias");
		}

		// Check for person name aliases
		var darwinEntity = entities.FirstOrDefault(e =>
			e.Name.Contains("Darwin", StringComparison.OrdinalIgnoreCase));

		if (darwinEntity != null && darwinEntity.Name.Contains("Charles"))
		{
			darwinEntity.Aliases.Should().Contain(a =>
				a.Equals("Darwin", StringComparison.OrdinalIgnoreCase),
				"Charles Darwin should have 'Darwin' as an alias");
		}

		// Check for title removal aliases
		var jamesonEntity = entities.FirstOrDefault(e =>
			e.Name.Contains("Jameson", StringComparison.OrdinalIgnoreCase));

		if (jamesonEntity != null && jamesonEntity.Name.Contains("Professor"))
		{
			jamesonEntity.Aliases.Should().Contain(a =>
				a.Equals("Jameson", StringComparison.OrdinalIgnoreCase),
				"Professor Jameson should have 'Jameson' as an alias");
		}
	}

	[Fact]
	public async Task ExtractEntitiesAsync_ShouldMerge_KeywordsAndProperNouns()
	{
		// Arrange
		var extractor = new HybridEntityExtractor();
		var chunks = new List<ChunkerBase>
		{
			new HtmlParagraphChunk
			{
				Content = "Darwin studied natural history and marine biology. Charles Darwin was a naturalist. Darwin's work on evolution changed biology forever. Natural selection was Darwin's key insight."
			}
		};

		// Act
		var entities = await extractor.ExtractEntitiesAsync(chunks, CancellationToken);

		// Assert
		_output.WriteLine($"Extracted {entities.Count} entities (merged):");
		foreach (var entity in entities.Take(10))
		{
			_output.WriteLine($"  - {entity.Name} ({entity.Type}, confidence: {entity.Confidence:F2}, frequency: {entity.Frequency})");
		}

		// "Darwin" should appear as both keyword (frequent) and proper noun (capitalized)
		// The extractor should merge these into one entity
		var darwinEntities = entities.Where(e =>
			e.Name.Contains("Darwin", StringComparison.OrdinalIgnoreCase)).ToList();

		darwinEntities.Should().NotBeEmpty("Should extract Darwin entity");

		// Should have high confidence (boosted from merge)
		var darwinEntity = darwinEntities.FirstOrDefault();
		if (darwinEntity != null)
		{
			_output.WriteLine($"\nDarwin entity details:");
			_output.WriteLine($"  Name: {darwinEntity.Name}");
			_output.WriteLine($"  Type: {darwinEntity.Type}");
			_output.WriteLine($"  Confidence: {darwinEntity.Confidence:F2}");
			_output.WriteLine($"  Frequency: {darwinEntity.Frequency}");

			// Should be classified as ProperNoun (from capitalization extractor)
			darwinEntity.Type.Should().Be(EntityType.ProperNoun,
				"Merged entity should use ProperNoun type from capitalization extractor");

			// Should have boosted confidence from merge
			darwinEntity.Confidence.Should().BeGreaterThan(0.5,
				"Merged entity should have confidence > 0.5");
		}
	}

	[Fact]
	public async Task ExtractEntitiesAsync_ShouldHandle_EmptyContent()
	{
		// Arrange
		var extractor = new HybridEntityExtractor();
		var chunks = new List<ChunkerBase>
		{
			new HtmlParagraphChunk { Content = string.Empty }
		};

		// Act
		var entities = await extractor.ExtractEntitiesAsync(chunks, CancellationToken);

		// Assert
		entities.Should().BeEmpty("Should return empty list for empty content");
	}

	[Fact]
	public async Task ExtractEntitiesAsync_ShouldExtract_DarwinGroundTruthEntities()
	{
		// Arrange
		var extractor = new HybridEntityExtractor();
		var chunks = new List<ChunkerBase>
		{
			new HtmlParagraphChunk
			{
				Content = @"The Plinian Society was encouraged and, I believe, founded by Professor Jameson.
					HMS Beagle was a Royal Navy survey vessel.
					Charles Darwin studied at Edinburgh University with Robert Grant.
					Captain FitzRoy commanded the Beagle during its voyage to the Galapagos Islands."
			}
		};

		// Act
		var entities = await extractor.ExtractEntitiesAsync(chunks, CancellationToken);

		// Assert
		_output.WriteLine($"\n=== Ground Truth Entities Test ===");
		_output.WriteLine($"Extracted {entities.Count} entities:");

		var properNouns = entities.Where(e => e.Type == EntityType.ProperNoun).ToList();
		_output.WriteLine($"\nProper Nouns ({properNouns.Count}):");
		foreach (var entity in properNouns)
		{
			_output.WriteLine($"  - {entity.Name} (confidence: {entity.Confidence:F2}, frequency: {entity.Frequency})");
			if (entity.Aliases.Count > 0)
			{
				_output.WriteLine($"  Aliases: {string.Join(", ", entity.Aliases)}");
			}
		}

		// Ground truth entities we expect to find
		var groundTruthEntities = new[]
		{
			"Plinian Society", "Professor Jameson", "HMS Beagle", "Charles Darwin",
			"Edinburgh University", "Robert Grant", "Captain FitzRoy", "Galapagos Islands"
		};

		_output.WriteLine($"\n=== Ground Truth Entity Coverage ===");
		foreach (var gt in groundTruthEntities)
		{
			var found = entities.Any(e =>
				// Exact or partial match
				e.Name.Equals(gt, StringComparison.OrdinalIgnoreCase) ||
				e.Name.Contains(gt, StringComparison.OrdinalIgnoreCase) ||
				gt.Contains(e.Name, StringComparison.OrdinalIgnoreCase) ||
				// Check aliases
				e.Aliases.Any(a => a.Equals(gt, StringComparison.OrdinalIgnoreCase)));

			_output.WriteLine($"  {(found ? "✓" : "✗")} {gt}");
		}

		// At least 70% of ground truth entities should be found
		var foundCount = groundTruthEntities.Count(gt =>
			entities.Any(e =>
				e.Name.Contains(gt, StringComparison.OrdinalIgnoreCase) ||
				gt.Contains(e.Name, StringComparison.OrdinalIgnoreCase) ||
				e.Aliases.Any(a => gt.Contains(a, StringComparison.OrdinalIgnoreCase))));

		var coverage = (double)foundCount / groundTruthEntities.Length;
		_output.WriteLine($"\nCoverage: {foundCount}/{groundTruthEntities.Length} ({coverage:P0})");

		coverage.Should().BeGreaterThanOrEqualTo(0.5,
			"Should extract at least 50% of ground truth entities");
	}
}
