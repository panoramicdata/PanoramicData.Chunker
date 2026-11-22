using AwesomeAssertions;
using PanoramicData.Chunker.KnowledgeGraph.Extractors;
using PanoramicData.Chunker.Models;
using PanoramicData.Chunker.Models.KnowledgeGraph;
using PanoramicData.Chunker.Tests.Utilities;

namespace PanoramicData.Chunker.Tests.Unit.KnowledgeGraph;

/// <summary>
/// Unit tests for the CooccurrenceRelationshipExtractor class.
/// </summary>
public class CooccurrenceRelationshipExtractorTests(ITestOutputHelper output) : BaseTest(output)
{
	[Fact]
	public void Constructor_WithDefaultParameters_ShouldInitialize()
	{
		// Arrange & Act
		var extractor = new CooccurrenceRelationshipExtractor();

		// Assert
		extractor.Should().NotBeNull();
		extractor.Name.Should().Be("CooccurrenceRelationshipExtractor");
		extractor.Version.Should().Be("1.0");
	}

	[Fact]
	public void Constructor_WithCustomParameters_ShouldInitialize()
	{
		// Arrange & Act
		var extractor = new CooccurrenceRelationshipExtractor(maxDistance: 300, minConfidence: 0.5);

		// Assert
		extractor.Should().NotBeNull();
	}

	[Fact]
	public void SupportedRelationshipTypes_ShouldIncludeMentions()
	{
		// Arrange
		var extractor = new CooccurrenceRelationshipExtractor();

		// Act
		var types = extractor.SupportedRelationshipTypes;

		// Assert
		types.Should().Contain(RelationshipType.Mentions);
		types.Should().Contain(RelationshipType.CooccursWith);
		types.Should().Contain(RelationshipType.RelatedTo);
	}

	[Fact]
	public async Task ExtractRelationshipsAsync_WithNoEntities_ShouldReturnEmpty()
	{
		// Arrange
		var extractor = new CooccurrenceRelationshipExtractor();
		var entities = new List<Entity>();
		var chunks = new List<ChunkerBase>
		{
			new TestContentChunk { Content = "Test content" }
		};

		// Act
		var relationships = await extractor.ExtractRelationshipsAsync(entities, chunks, CancellationToken);

		// Assert
		relationships.Should().BeEmpty();
	}

	[Fact]
	public async Task ExtractRelationshipsAsync_WithNoChunks_ShouldReturnEmpty()
	{
		// Arrange
		var extractor = new CooccurrenceRelationshipExtractor();
		var entities = new List<Entity>
		{
			new() { Name = "Entity 1", Type = EntityType.Person, NormalizedName = "entity 1" },
			new() { Name = "Entity 2", Type = EntityType.Organization, NormalizedName = "entity 2" }
		};
		var chunks = new List<ChunkerBase>();

		// Act
		var relationships = await extractor.ExtractRelationshipsAsync(entities, chunks, CancellationToken);

		// Assert
		relationships.Should().BeEmpty();
	}

	[Fact]
	public async Task ExtractRelationshipsAsync_WithOneEntity_ShouldReturnEmpty()
	{
		// Arrange
		var extractor = new CooccurrenceRelationshipExtractor();
		var chunkId = Guid.NewGuid();
		var entity = CreateEntity("Microsoft", EntityType.Organization, (chunkId, 0));

		var chunks = new List<ChunkerBase>
		{
			new TestContentChunk { Id = chunkId, Content = "Microsoft is a technology company." }
		};

		// Act
		var relationships = await extractor.ExtractRelationshipsAsync([entity], chunks, CancellationToken);

		// Assert
		relationships.Should().BeEmpty();
	}

	[Fact]
	public async Task ExtractRelationshipsAsync_WithCloseEntities_ShouldExtractRelationship()
	{
		// Arrange
		var extractor = new CooccurrenceRelationshipExtractor(maxDistance: 500, minConfidence: 0.3);
		var chunkId = Guid.NewGuid();

		var entity1 = CreateEntity("Microsoft", EntityType.Organization, (chunkId, 0));

		var entity2 = CreateEntity("Azure", EntityType.Product, (chunkId, 28));

		var chunks = new List<ChunkerBase>
		{
			new TestContentChunk { Id = chunkId, Content = "Microsoft announced their new Azure service today." }
		};

		// Act
		var relationships = await extractor.ExtractRelationshipsAsync(
			[entity1, entity2],
			chunks, CancellationToken);

		// Assert
		relationships.Should().ContainSingle();
		var rel = relationships[0];
		rel.Type.Should().Be(RelationshipType.Mentions);
		rel.Bidirectional.Should().BeTrue();
		(rel.Confidence >= 0.3).Should().BeTrue();
		_output.WriteLine($"Relationship: {rel}");
	}

	[Fact]
	public async Task ExtractRelationshipsAsync_WithDistantEntities_ShouldNotExtract()
	{
		// Arrange
		var extractor = new CooccurrenceRelationshipExtractor(maxDistance: 50, minConfidence: 0.5);
		var chunkId = Guid.NewGuid();

		var entity1 = CreateEntity("Entity1", EntityType.Concept, (chunkId, 0));

		var entity2 = CreateEntity("Entity2", EntityType.Concept, (chunkId, 200));

		var chunks = new List<ChunkerBase>
		{
			new TestContentChunk
			{
				Id = chunkId,
				Content = "Entity1" + new string(' ', 185) + "Entity2"
			}
		};

		// Act
		var relationships = await extractor.ExtractRelationshipsAsync(
			[entity1, entity2],
			chunks, CancellationToken);

		// Assert
		relationships.Should().BeEmpty();
	}

	[Fact]
	public async Task ExtractRelationshipsAsync_WithMultipleChunks_ShouldConsolidateRelationships()
	{
		// Arrange
		var extractor = new CooccurrenceRelationshipExtractor();
		var chunkId1 = Guid.NewGuid();
		var chunkId2 = Guid.NewGuid();

		var entity1 = CreateEntity("Microsoft", EntityType.Organization, (chunkId1, 0), (chunkId2, 0));

		var entity2 = CreateEntity("Azure", EntityType.Product, (chunkId1, 20), (chunkId2, 25));

		var chunks = new List<ChunkerBase>
		{
			new TestContentChunk { Id = chunkId1, Content = "Microsoft launched Azure in 2010." },
			new TestContentChunk { Id = chunkId2, Content = "Microsoft continues to Azure investments." }
		};

		// Act
		var relationships = await extractor.ExtractRelationshipsAsync(
			[entity1, entity2],
			chunks, CancellationToken);

		// Assert
		relationships.Should().ContainSingle();
		var rel = relationships[0];
		rel.Evidence.Should().HaveCount(2);
		(rel.Weight > 0).Should().BeTrue();
		_output.WriteLine($"Consolidated relationship weight: {rel.Weight}");
	}

	[Fact]
	public async Task ExtractRelationshipsAsync_WithThreeEntities_ShouldExtractMultipleRelationships()
	{
		// Arrange
		var extractor = new CooccurrenceRelationshipExtractor();
		var chunkId = Guid.NewGuid();

		var entity1 = CreateEntity("E1", EntityType.Person, (chunkId, 0));

		var entity2 = CreateEntity("E2", EntityType.Person, (chunkId, 10));

		var entity3 = CreateEntity("E3", EntityType.Person, (chunkId, 20));

		var chunks = new List<ChunkerBase>
		{
			new TestContentChunk { Id = chunkId, Content = "E1 met with E2 and later E3 joined." }
		};

		// Act
		var relationships = await extractor.ExtractRelationshipsAsync(
			[entity1, entity2, entity3],
			chunks, CancellationToken);

		// Assert
		relationships.Should().HaveCount(3); // E1-E2, E1-E3, E2-E3
		_output.WriteLine($"Extracted {relationships.Count} relationships from 3 entities");
	}

	[Fact]
	public async Task ExtractRelationshipsAsync_ShouldNormalizeWeights()
	{
		// Arrange
		var extractor = new CooccurrenceRelationshipExtractor();
		var chunkId1 = Guid.NewGuid();
		var chunkId2 = Guid.NewGuid();
		var chunkId3 = Guid.NewGuid();

		var entity1 = CreateEntity("E1", EntityType.Person, (chunkId1, 0), (chunkId2, 0), (chunkId3, 0));

		var entity2 = CreateEntity("E2", EntityType.Person, (chunkId1, 10), (chunkId2, 10), (chunkId3, 10));

		var entity3 = CreateEntity("E3", EntityType.Person, (chunkId1, 20));

		var chunks = new List<ChunkerBase>
		{
			new TestContentChunk { Id = chunkId1, Content = "E1 works with E2 and E3." },
			new TestContentChunk { Id = chunkId2, Content = "E1 and E2 collaborated." },
			new TestContentChunk { Id = chunkId3, Content = "E1 met E2 yesterday." }
		};

		// Act
		var relationships = await extractor.ExtractRelationshipsAsync(
			[entity1, entity2, entity3],
			chunks, CancellationToken);

		// Assert
		var maxWeight = relationships.Max(r => r.Weight);
		maxWeight.Should().Be(1.0); // Weights should be normalized to 1.0
		_output.WriteLine($"Max weight after normalization: {maxWeight}");
	}

	[Fact]
	public async Task ExtractRelationshipsAsync_ShouldIncludeEvidence()
	{
		// Arrange
		var extractor = new CooccurrenceRelationshipExtractor();
		var chunkId = Guid.NewGuid();

		var entity1 = CreateEntity("Microsoft", EntityType.Organization, (chunkId, 0));

		var entity2 = CreateEntity("Azure", EntityType.Product, (chunkId, 20));

		var chunks = new List<ChunkerBase>
		{
			new TestContentChunk { Id = chunkId, Content = "Microsoft launched Azure cloud platform." }
		};

		// Act
		var relationships = await extractor.ExtractRelationshipsAsync(
			[entity1, entity2],
			chunks, CancellationToken);

		// Assert
		relationships.Should().ContainSingle();
		var rel = relationships[0];
		rel.Evidence.Should().NotBeEmpty();
		rel.Evidence[0].ChunkId.Should().Be(chunkId);
		rel.Evidence[0].Context.Should().Contain("Microsoft");
		rel.Evidence[0].Context.Should().Contain("Azure");
		_output.WriteLine($"Evidence context: {rel.Evidence[0].Context}");
	}

	[Fact]
	public async Task ExtractRelationshipsAsync_ShouldSetMetadata()
	{
		// Arrange
		var extractor = new CooccurrenceRelationshipExtractor();
		var chunkId = Guid.NewGuid();

		var entity1 = CreateEntity("E1", EntityType.Person, (chunkId, 0));

		var entity2 = CreateEntity("E2", EntityType.Person, (chunkId, 10));

		var chunks = new List<ChunkerBase>
		{
			new TestContentChunk { Id = chunkId, Content = "E1 met E2 today." }
		};

		// Act
		var relationships = await extractor.ExtractRelationshipsAsync(
			[entity1, entity2],
			chunks, CancellationToken);

		// Assert
		relationships.Should().ContainSingle();
		var rel = relationships[0];
		rel.Metadata.ExtractorName.Should().Be("CooccurrenceRelationshipExtractor");
		rel.Metadata.ExtractorVersion.Should().Be("1.0");
		(rel.Metadata.ExtractedAt <= DateTimeOffset.UtcNow).Should().BeTrue();
	}

	[Fact]
	public async Task ExtractRelationshipsAsync_ShouldStoreMinDistanceInProperties()
	{
		// Arrange
		var extractor = new CooccurrenceRelationshipExtractor();
		var chunkId = Guid.NewGuid();

		var entity1 = CreateEntity("E1", EntityType.Person, (chunkId, 0));

		var entity2 = CreateEntity("E2", EntityType.Person, (chunkId, 50));

		var chunks = new List<ChunkerBase>
		{
			new TestContentChunk { Id = chunkId, Content = "E1" + new string(' ', 46) + "E2" }
		};

		// Act
		var relationships = await extractor.ExtractRelationshipsAsync(
			[entity1, entity2],
			chunks, CancellationToken);

		// Assert
		relationships.Should().ContainSingle();
		var rel = relationships[0];
		rel.Properties.Should().ContainKey("MinDistance");
		var minDistance = (int)rel.Properties["MinDistance"];
		minDistance.Should().Be(50);
		_output.WriteLine($"Min distance: {minDistance}");
	}

	[Fact]
	public async Task ExtractRelationshipsAsync_WithCancellation_ShouldThrow()
	{
		// Arrange
		var extractor = new CooccurrenceRelationshipExtractor();
		var cts = new CancellationTokenSource();
		cts.Cancel();

		var entity1 = CreateEntity("E1", EntityType.Person);
		var entity2 = CreateEntity("E2", EntityType.Person);
		var chunks = new List<ChunkerBase>
		{
			new TestContentChunk { Content = "E1 and E2 are here." }
		};

		// Act
		var act = async () => await extractor.ExtractRelationshipsAsync([entity1, entity2], chunks, cts.Token);

		// Assert
		await act.Should().ThrowAsync<OperationCanceledException>();
	}

	private static Entity CreateEntity(string name, EntityType type, params (Guid ChunkId, int Position)[] sources)
	{
		var entity = new Entity
		{
			Name = name,
			Type = type,
			NormalizedName = name.ToLowerInvariant()
		};

		foreach (var (chunkId, position) in sources)
		{
			entity.Sources.Add(new EntitySource { ChunkId = chunkId, Position = position, Length = name.Length });
		}

		return entity;
	}
}
