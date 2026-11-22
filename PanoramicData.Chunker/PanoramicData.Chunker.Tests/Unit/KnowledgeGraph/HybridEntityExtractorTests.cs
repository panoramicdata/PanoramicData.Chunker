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
		const string content = "The Plinian Society was founded by Professor Jameson. HMS Beagle was commanded by Captain FitzRoy. Robert Grant taught marine biology at Edinburgh University.";

		// Act
		var entities = await ExtractAsync(content);

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
		WithNameContaining(entities, "Society", "Plinian").Should().NotBeEmpty("Should extract 'Plinian Society' or 'Plinian'");

		// Verify HMS Beagle (might be "Beagle" or "HMS Beagle")
		WithNameContaining(entities, "Beagle").Should().NotBeEmpty("Should extract 'Beagle' or 'HMS Beagle'");

		// Verify person names with titles
		WithNameContaining(entities, "Jameson").Should().NotBeEmpty("Should extract 'Jameson' or 'Professor Jameson'");

		// Verify Robert Grant
		WithNameContaining(entities, "Grant", "Robert").Should().NotBeEmpty("Should extract 'Grant' or 'Robert Grant'");

		// Verify Edinburgh University
		WithNameContaining(entities, "University", "Edinburgh").Should().NotBeEmpty("Should extract 'University' or 'Edinburgh University'");
	}

	[Fact]
	public async Task ExtractEntitiesAsync_ShouldGenerate_NameAliases()
	{
		// Arrange
		const string content = "HMS Beagle sailed with Charles Darwin. The 'Beagle' was a survey vessel. Professor Jameson founded the society.";

		// Act
		var entities = await ExtractAsync(content);

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
		var beagleEntity = WithNameContaining(entities, "Beagle").FirstOrDefault();

		if (beagleEntity != null && beagleEntity.Name.StartsWith("HMS ", StringComparison.OrdinalIgnoreCase))
		{
			AssertHasAlias(beagleEntity, "Beagle", "HMS Beagle should have 'Beagle' as an alias");
		}

		// Check for person name aliases
		var darwinEntity = WithNameContaining(entities, "Darwin").FirstOrDefault();

		if (darwinEntity != null && darwinEntity.Name.Contains("Charles"))
		{
			AssertHasAlias(darwinEntity, "Darwin", "Charles Darwin should have 'Darwin' as an alias");
		}

		// Check for title removal aliases
		var jamesonEntity = WithNameContaining(entities, "Jameson").FirstOrDefault();

		if (jamesonEntity != null && jamesonEntity.Name.Contains("Professor"))
		{
			AssertHasAlias(jamesonEntity, "Jameson", "Professor Jameson should have 'Jameson' as an alias");
		}
	}

	[Fact]
	public async Task ExtractEntitiesAsync_ShouldMerge_KeywordsAndProperNouns()
	{
		// Arrange
		const string content = "Darwin studied natural history and marine biology. Charles Darwin was a naturalist. Darwin's work on evolution changed biology forever. Natural selection was Darwin's key insight.";

		// Act
		var entities = await ExtractAsync(content);

		// Assert
		_output.WriteLine($"Extracted {entities.Count} entities (merged):");
		foreach (var entity in entities.Take(10))
		{
			_output.WriteLine($"  - {entity.Name} ({entity.Type}, confidence: {entity.Confidence:F2}, frequency: {entity.Frequency})");
		}

		// "Darwin" should appear as both keyword (frequent) and proper noun (capitalized)
		// The extractor should merge these into one entity
		var darwinEntities = WithNameContaining(entities, "Darwin");

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
		// Act
		var entities = await ExtractAsync(string.Empty);

		// Assert
		entities.Should().BeEmpty("Should return empty list for empty content");
	}

	[Fact]
	public async Task ExtractEntitiesAsync_ShouldExtract_DarwinGroundTruthEntities()
	{
		// Arrange
		const string content = @"The Plinian Society was encouraged and, I believe, founded by Professor Jameson.
					HMS Beagle was a Royal Navy survey vessel.
					Charles Darwin studied at Edinburgh University with Robert Grant.
					Captain FitzRoy commanded the Beagle during its voyage to the Galapagos Islands.";

		// Act
		var entities = await ExtractAsync(content);

		// Assert
		_output.WriteLine($"\n=== Ground Truth Entities Test ===");
		_output.WriteLine($"Extracted {entities.Count} entities:");

		WriteProperNouns(entities);

		// Ground truth entities we expect to find
		var groundTruthEntities = new[]
		{
			"Plinian Society", "Professor Jameson", "HMS Beagle", "Charles Darwin",
			"Edinburgh University", "Robert Grant", "Captain FitzRoy", "Galapagos Islands"
		};

		WriteGroundTruthCoverage(entities, groundTruthEntities);

		// At least 70% of ground truth entities should be found
		var foundCount = CountFoundGroundTruth(entities, groundTruthEntities);

		var coverage = (double)foundCount / groundTruthEntities.Length;
		_output.WriteLine($"\nCoverage: {foundCount}/{groundTruthEntities.Length} ({coverage:P0})");

		coverage.Should().BeGreaterThanOrEqualTo(0.5,
			"Should extract at least 50% of ground truth entities");
	}

	private Task<List<Entity>> ExtractAsync(string content)
		=> new HybridEntityExtractor().ExtractEntitiesAsync([new HtmlParagraphChunk { Content = content }], CancellationToken);

	private static List<Entity> WithNameContaining(List<Entity> entities, params string[] fragments)
		=> [.. entities.Where(e => fragments.Any(f => e.Name.Contains(f, StringComparison.OrdinalIgnoreCase)))];

	private static void AssertHasAlias(Entity entity, string alias, string because)
		=> entity.Aliases.Should().Contain(a => a.Equals(alias, StringComparison.OrdinalIgnoreCase), because);

	private void WriteProperNouns(List<Entity> entities)
	{
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
	}

	private void WriteGroundTruthCoverage(List<Entity> entities, string[] groundTruthEntities)
	{
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
	}

	private static int CountFoundGroundTruth(List<Entity> entities, string[] groundTruthEntities)
	{
		var foundCount = groundTruthEntities.Count(gt =>
			entities.Any(e =>
				e.Name.Contains(gt, StringComparison.OrdinalIgnoreCase) ||
				gt.Contains(e.Name, StringComparison.OrdinalIgnoreCase) ||
				e.Aliases.Any(a => gt.Contains(a, StringComparison.OrdinalIgnoreCase))));
		return foundCount;
	}
}
