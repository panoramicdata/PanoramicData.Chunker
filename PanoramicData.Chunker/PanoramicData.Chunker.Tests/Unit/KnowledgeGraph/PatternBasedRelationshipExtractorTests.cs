using AwesomeAssertions;
using PanoramicData.Chunker.Chunkers.Html;
using PanoramicData.Chunker.KnowledgeGraph.Extractors;
using PanoramicData.Chunker.Models;
using PanoramicData.Chunker.Models.KnowledgeGraph;

namespace PanoramicData.Chunker.Tests.Unit.KnowledgeGraph;

/// <summary>
/// Unit tests for PatternBasedRelationshipExtractor.
/// </summary>
public class PatternBasedRelationshipExtractorTests(ITestOutputHelper output) : BaseTest(output)
{
	[Fact]
	public async Task ExtractRelationshipsAsync_WithFoundedPattern_ShouldDetectFoundedRelationship()
	{
		// Arrange
		var darwinEntity = new Entity(EntityType.Person, "Darwin", 1.0);
		var societyEntity = new Entity(EntityType.Organization, "Plinian Society", 1.0);

		// Act
		var relationships = await ExtractFromChunkAsync(
			new PatternBasedRelationshipExtractor(),
			"Darwin founded the Plinian Society in 1823.",
			(darwinEntity, 0),
			(societyEntity, 20));

		// Assert
		relationships.Should().NotBeEmpty();
		var foundedRel = relationships.FirstOrDefault(r => r.Type == RelationshipType.Founded);
		foundedRel.Should().NotBeNull("Should detect Founded relationship");
		foundedRel!.FromEntityId.Should().Be(darwinEntity.Id);
		foundedRel.ToEntityId.Should().Be(societyEntity.Id);
		foundedRel.Confidence.Should().BeGreaterThanOrEqualTo(0.9);
	}

	[Fact]
	public async Task ExtractRelationshipsAsync_WithMemberOfPattern_ShouldDetectMemberRelationship()
	{
		// Arrange
		var darwinEntity = new Entity(EntityType.Person, "Charles Darwin", 1.0);
		var societyEntity = new Entity(EntityType.Organization, "Plinian Society", 1.0);

		// Act
		var relationships = await ExtractFromChunkAsync(
			new PatternBasedRelationshipExtractor(),
			"Charles Darwin was a member of the Plinian Society.",
			(darwinEntity, 0),
			(societyEntity, 40));

		// Assert
		relationships.Should().NotBeEmpty();
		var memberRel = relationships.FirstOrDefault(r => r.Type == RelationshipType.MemberOf);
		memberRel.Should().NotBeNull("Should detect MemberOf relationship");
		memberRel!.FromEntityId.Should().Be(darwinEntity.Id);
		memberRel.ToEntityId.Should().Be(societyEntity.Id);
	}

	[Fact]
	public async Task ExtractRelationshipsAsync_WithMultiplePatterns_ShouldDetectMultipleTypes()
	{
		// Arrange
		var extractor = new PatternBasedRelationshipExtractor();

		var jamesonEntity = new Entity(EntityType.Person, "Professor Jameson", 1.0);
		var darwinEntity = new Entity(EntityType.Person, "Darwin", 1.0);
		var societyEntity = new Entity(EntityType.Organization, "Plinian Society", 1.0);
		var universityEntity = new Entity(EntityType.Organization, "Edinburgh University", 1.0);

		var chunk1 = CreateChunk("The Plinian Society was founded by Professor Jameson at Edinburgh University.");
		var chunk2 = CreateChunk("Darwin was a member of the Plinian Society who regularly attended meetings.");

		jamesonEntity.AddSource(chunk1.Id, chunk1.Content.IndexOf("Professor Jameson", StringComparison.Ordinal), chunk1.Content);
		societyEntity.AddSource(chunk1.Id, 4, chunk1.Content);
		societyEntity.AddSource(chunk2.Id, 23, chunk2.Content);
		universityEntity.AddSource(chunk1.Id, chunk1.Content.IndexOf("Edinburgh University", StringComparison.Ordinal), chunk1.Content);
		darwinEntity.AddSource(chunk2.Id, 0, chunk2.Content);

		var entities = new List<Entity> { jamesonEntity, darwinEntity, societyEntity, universityEntity };
		var chunks = new List<ChunkerBase> { chunk1, chunk2 };

		// Act
		var relationships = await extractor.ExtractRelationshipsAsync(entities, chunks, CancellationToken);

		// Assert
		relationships.Should().NotBeEmpty();

		var relationshipTypes = relationships.Select(r => r.Type).Distinct().ToList();
		relationshipTypes.Should().Contain(RelationshipType.Founded, "Should detect Founded relationship");
		relationshipTypes.Should().Contain(RelationshipType.MemberOf, "Should detect MemberOf relationship");
		relationshipTypes.Should().Contain(RelationshipType.LocatedIn, "Should detect LocatedIn relationship");

		_output.WriteLine($"Extracted {relationships.Count} relationships with {relationshipTypes.Count} distinct types:");
		foreach (var relType in relationshipTypes.OrderBy(t => t.ToString()))
		{
			var count = relationships.Count(r => r.Type == relType);
			_output.WriteLine($"  - {relType}: {count}");
		}

		relationshipTypes.Count.Should().BeGreaterThan(2, "Should extract at least 3 different relationship types");
	}

	[Fact]
	public async Task ExtractRelationshipsAsync_WithProximityOnly_ShouldCreateMentionsRelationship()
	{
		// Arrange
		var extractor = new PatternBasedRelationshipExtractor(
			maxDistance: 500,
			minConfidence: 0.3,
			enablePatternMatching: false,  // Disable patterns
			enableProximityRelationships: true);

		var entity1 = new Entity(EntityType.Person, "Darwin", 1.0);
		var entity2 = new Entity(EntityType.Organization, "HMS Beagle", 1.0);

		// Act
		var relationships = await ExtractFromChunkAsync(extractor, "Darwin sailed on HMS Beagle.", (entity1, 0), (entity2, 18));

		// Assert
		relationships.Should().NotBeEmpty();
		var proximityRel = relationships.First();
		proximityRel.Type.Should().BeOneOf(RelationshipType.Mentions, RelationshipType.CooccursWith);
	}

	[Fact]
	public async Task ExtractRelationshipsAsync_WithNoEntities_ShouldReturnEmpty()
	{
		// Arrange
		var extractor = new PatternBasedRelationshipExtractor();
		var entities = new List<Entity>();
		var chunks = new List<ChunkerBase> { CreateChunk("Some content") };

		// Act
		var relationships = await extractor.ExtractRelationshipsAsync(entities, chunks, CancellationToken);

		// Assert
		relationships.Should().BeEmpty();
	}

	[Fact]
	public async Task ExtractRelationshipsAsync_WithDistantEntities_ShouldNotCreateRelationship()
	{
		// Arrange
		var entity1 = new Entity(EntityType.Person, "Darwin", 1.0);
		var entity2 = new Entity(EntityType.Organization, "Society", 1.0);

		// Act
		var relationships = await ExtractFromChunkAsync(
			new PatternBasedRelationshipExtractor(maxDistance: 50),
			"Darwin was a famous naturalist. Many years later, he joined a scientific Society.",
			(entity1, 0),
			(entity2, 75));

		// Assert
		relationships.Should().BeEmpty("Entities are too far apart");
	}

	[Fact]
	public async Task ExtractRelationshipsAsync_WithWorksForPattern_ShouldDetectWorkRelationship()
	{
		// Arrange
		var personEntity = new Entity(EntityType.Person, "John Smith", 1.0);
		var companyEntity = new Entity(EntityType.Organization, "Microsoft", 1.0);

		// Act
		var relationships = await ExtractFromChunkAsync(
			new PatternBasedRelationshipExtractor(),
			"John Smith works for Microsoft.",
			(personEntity, 0),
			(companyEntity, 26));

		// Assert
		relationships.Should().NotBeEmpty();
		var worksForRel = relationships.FirstOrDefault(r => r.Type == RelationshipType.WorksFor);
		worksForRel.Should().NotBeNull("Should detect WorksFor relationship");
	}

	[Fact]
	public async Task ExtractRelationshipsAsync_WithInfluencesPattern_ShouldDetectInfluence()
	{
		// Arrange
		const string content = "The theory of evolution influenced modern biologists greatly.";
		var theoryEntity = new Entity(EntityType.Concept, "theory of evolution", 1.0);
		var personEntity = new Entity(EntityType.Person, "modern biologists", 1.0);

		// Act
		var relationships = await ExtractFromChunkAsync(
			new PatternBasedRelationshipExtractor(),
			content,
			(theoryEntity, 4),
			(personEntity, content.IndexOf("modern biologists", StringComparison.Ordinal)));

		// Assert
		relationships.Should().NotBeEmpty();
		var influencesRel = relationships.FirstOrDefault(r => r.Type == RelationshipType.Influences);
		influencesRel.Should().NotBeNull("Should detect Influences relationship");
	}

	[Fact]
	public async Task ExtractRelationshipsAsync_ShouldNormalizeWeights()
	{
		// Arrange
		var extractor = new PatternBasedRelationshipExtractor();

		var entity1 = new Entity(EntityType.Person, "Darwin", 1.0);
		var entity2 = new Entity(EntityType.Organization, "Society", 1.0);

		// Add entity in multiple chunks to increase relationship weight
		var chunk1 = CreateChunk("Darwin founded the Society.");
		var chunk2 = CreateChunk("Darwin joined the Society.");

		entity1.AddSource(chunk1.Id, 0);
		entity1.AddSource(chunk2.Id, 0);
		entity2.AddSource(chunk1.Id, 23);
		entity2.AddSource(chunk2.Id, 19);

		var entities = new List<Entity> { entity1, entity2 };
		var chunks = new List<ChunkerBase> { chunk1, chunk2 };

		// Act
		var relationships = await extractor.ExtractRelationshipsAsync(entities, chunks, CancellationToken);

		// Assert
		relationships.Should().NotBeEmpty();
		relationships.Should().OnlyContain(r => r.Weight >= 0.0 && r.Weight <= 1.0, "Weights should be normalized between 0 and 1");
	}

	private static HtmlParagraphChunk CreateChunk(string content) => new()
	{
		Id = Guid.NewGuid(),
		Content = content,
		QualityMetrics = new ChunkQualityMetrics()
	};

	private Task<List<Relationship>> ExtractFromChunkAsync(
		PatternBasedRelationshipExtractor extractor,
		string content,
		(Entity Entity, int Position) first,
		(Entity Entity, int Position) second)
	{
		var chunk = CreateChunk(content);

		first.Entity.AddSource(chunk.Id, first.Position, chunk.Content);
		second.Entity.AddSource(chunk.Id, second.Position, chunk.Content);

		return extractor.ExtractRelationshipsAsync(
			[first.Entity, second.Entity],
			[chunk],
			CancellationToken);
	}
}
