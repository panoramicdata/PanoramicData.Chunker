using AwesomeAssertions;
using Microsoft.Extensions.DependencyInjection;
using PanoramicData.Chunker.Interfaces.KnowledgeGraph;
using PanoramicData.Chunker.Models.KnowledgeGraph;
using PanoramicData.Chunker.Tests.Fixtures;

namespace PanoramicData.Chunker.Tests.Integration.KnowledgeGraph;

/// <summary>
/// Integration tests for PostgresGraphStore using xUnit v3 Fixture pattern.
/// </summary>
public class PostgresGraphStoreTests : IClassFixture<PostgresKnowledgeGraphFixture>
{
	private readonly PostgresKnowledgeGraphFixture _fixture;
	private readonly IGraphStore _graphStore;

	private readonly static CancellationToken _cancellationToken = TestContext.Current.CancellationToken;

	public PostgresGraphStoreTests(PostgresKnowledgeGraphFixture fixture)
	{
		_fixture = fixture ?? throw new ArgumentNullException(nameof(fixture));
		_graphStore = _fixture.Services.GetRequiredService<IGraphStore>();
	}

	[Fact]
	public async Task SaveGraphAsync_WithNewGraph_ShouldSaveSuccessfully()
	{
		// Arrange
		await _fixture.CleanDatabaseAsync();

		var graph = new Graph("Test Graph")
		{
			Metadata = new GraphMetadata
			{
				Description = "Integration test graph",
				Version = "1.0"
			}
		};

		var entity1 = new Entity(EntityType.Person, "John Doe", 0.95);
		var entity2 = new Entity(EntityType.Organization, "Acme Corp", 0.90);
		graph.AddEntity(entity1);
		graph.AddEntity(entity2);

		var relationship = new Relationship(entity1.Id, entity2.Id, RelationshipType.WorksFor, 1.0, 0.95);
		graph.AddRelationship(relationship);

		graph.ComputeStatistics();

		// Act
		await _graphStore.SaveGraphAsync(graph, _cancellationToken);

		// Assert
		var loaded = await _graphStore.LoadGraphAsync(graph.Id, _cancellationToken);
		loaded.Should().NotBeNull();
		loaded!.Name.Should().Be("Test Graph");
		loaded.Entities.Should().HaveCount(2);
		loaded.Relationships.Should().ContainSingle();
		loaded.Statistics.TotalEntities.Should().Be(2);
		loaded.Statistics.TotalRelationships.Should().Be(1);
	}

	[Fact]
	public async Task LoadGraphAsync_WithNonExistentId_ShouldReturnNull()
	{
		// Arrange
		await _fixture.CleanDatabaseAsync();
		var nonExistentId = Guid.NewGuid();

		// Act
		var result = await _graphStore.LoadGraphAsync(nonExistentId, _cancellationToken);

		// Assert
		result.Should().BeNull();
	}

	[Fact]
	public async Task LoadGraphByNameAsync_WithExistingName_ShouldReturnGraph()
	{
		// Arrange
		await _fixture.CleanDatabaseAsync();

		var graph = new Graph("Named Test Graph");
		var entity = new Entity(EntityType.Keyword, "test", 0.85);
		graph.AddEntity(entity);

		await _graphStore.SaveGraphAsync(graph, _cancellationToken);

		// Act
		var loaded = await _graphStore.LoadGraphByNameAsync("Named Test Graph", _cancellationToken);

		// Assert
		loaded.Should().NotBeNull();
		loaded!.Id.Should().Be(graph.Id);
		loaded.Name.Should().Be("Named Test Graph");
		loaded.Entities.Should().ContainSingle();
	}

	[Fact]
	public async Task DeleteGraphAsync_WithExistingGraph_ShouldDeleteSuccessfully()
	{
		// Arrange
		await _fixture.CleanDatabaseAsync();

		var graph = new Graph("Graph To Delete");
		var entity = new Entity(EntityType.Concept, "TestConcept", 0.80);
		graph.AddEntity(entity);

		await _graphStore.SaveGraphAsync(graph, _cancellationToken);

		// Verify it exists
		var exists = await _graphStore.GraphExistsAsync(graph.Id, _cancellationToken);
		exists.Should().BeTrue();

		// Act
		await _graphStore.DeleteGraphAsync(graph.Id, _cancellationToken);

		// Assert
		var stillExists = await _graphStore.GraphExistsAsync(graph.Id, _cancellationToken);
		stillExists.Should().BeFalse();

		var loaded = await _graphStore.LoadGraphAsync(graph.Id, _cancellationToken);
		loaded.Should().BeNull();
	}

	[Fact]
	public async Task GraphExistsAsync_WithExistingGraph_ShouldReturnTrue()
	{
		// Arrange
		await _fixture.CleanDatabaseAsync();

		var graph = new Graph("Existence Test");
		await _graphStore.SaveGraphAsync(graph, _cancellationToken);

		// Act
		var exists = await _graphStore.GraphExistsAsync(graph.Id, _cancellationToken);

		// Assert
		exists.Should().BeTrue();
	}

	[Fact]
	public async Task GraphExistsAsync_WithNonExistentGraph_ShouldReturnFalse()
	{
		// Arrange
		await _fixture.CleanDatabaseAsync();
		var nonExistentId = Guid.NewGuid();

		// Act
		var exists = await _graphStore.GraphExistsAsync(nonExistentId, _cancellationToken);

		// Assert
		exists.Should().BeFalse();
	}

	[Fact]
	public async Task SaveEntityAsync_WithNewEntity_ShouldSaveSuccessfully()
	{
		// Arrange
		await _fixture.CleanDatabaseAsync();

		var graph = new Graph("Entity Test Graph");
		await _graphStore.SaveGraphAsync(graph, _cancellationToken);

		var entity = new Entity(EntityType.Location, "New York", 0.92);
		entity.AddSource(Guid.NewGuid(), 10, "Located in New York");

		// Act
		await _graphStore.SaveEntityAsync(graph.Id, entity, _cancellationToken);

		// Assert
		var entities = await _graphStore.QueryEntitiesByTypeAsync(graph.Id, EntityType.Location, _cancellationToken);
		entities.Should().ContainSingle();
		entities[0].Name.Should().Be("New York");
		entities[0].Confidence.Should().Be(0.92);
		entities[0].Sources.Should().ContainSingle();
	}

	[Fact]
	public async Task SaveRelationshipAsync_WithNewRelationship_ShouldSaveSuccessfully()
	{
		// Arrange
		await _fixture.CleanDatabaseAsync();

		var graph = new Graph("Relationship Test Graph");
		await _graphStore.SaveGraphAsync(graph, _cancellationToken);

		var entity1 = new Entity(EntityType.Person, "Alice", 0.95);
		var entity2 = new Entity(EntityType.Person, "Bob", 0.93);
		await _graphStore.SaveEntityAsync(graph.Id, entity1, _cancellationToken);
		await _graphStore.SaveEntityAsync(graph.Id, entity2, _cancellationToken);

		var relationship = new Relationship(entity1.Id, entity2.Id, RelationshipType.RelatedTo, 0.8, 0.90);
		relationship.AddEvidence(Guid.NewGuid(), "Alice and Bob worked together", 0.90);

		// Act
		await _graphStore.SaveRelationshipAsync(graph.Id, relationship, _cancellationToken);

		// Assert
		var relationships = await _graphStore.QueryRelationshipsByTypeAsync(graph.Id, RelationshipType.RelatedTo, _cancellationToken);
		relationships.Should().ContainSingle();
		relationships[0].FromEntityId.Should().Be(entity1.Id);
		relationships[0].ToEntityId.Should().Be(entity2.Id);
		relationships[0].Weight.Should().Be(0.8);
		relationships[0].Evidence.Should().ContainSingle();
	}

	[Fact]
	public async Task QueryEntitiesByTypeAsync_WithMultipleTypes_ShouldReturnCorrectEntities()
	{
		// Arrange
		await _fixture.CleanDatabaseAsync();

		var graph = new Graph("Query Test Graph");
		await _graphStore.SaveGraphAsync(graph, _cancellationToken);

		var person1 = new Entity(EntityType.Person, "Charlie", 0.95);
		var person2 = new Entity(EntityType.Person, "Diana", 0.90);
		var org = new Entity(EntityType.Organization, "TechCorp", 0.88);
		var location = new Entity(EntityType.Location, "San Francisco", 0.92);

		await _graphStore.SaveEntityAsync(graph.Id, person1, _cancellationToken);
		await _graphStore.SaveEntityAsync(graph.Id, person2, _cancellationToken);
		await _graphStore.SaveEntityAsync(graph.Id, org, _cancellationToken);
		await _graphStore.SaveEntityAsync(graph.Id, location, _cancellationToken);

		// Act
		var people = await _graphStore.QueryEntitiesByTypeAsync(graph.Id, EntityType.Person, _cancellationToken);
		var organizations = await _graphStore.QueryEntitiesByTypeAsync(graph.Id, EntityType.Organization, _cancellationToken);

		// Assert
		people.Should().HaveCount(2);
		people.Should().Contain(e => e.Name == "Charlie");
		people.Should().Contain(e => e.Name == "Diana");

		organizations.Should().ContainSingle();
		organizations[0].Name.Should().Be("TechCorp");
	}

	[Fact]
	public async Task QueryRelationshipsByTypeAsync_WithMultipleTypes_ShouldReturnCorrectRelationships()
	{
		// Arrange
		await _fixture.CleanDatabaseAsync();

		var graph = new Graph("Relationship Query Test");
		await _graphStore.SaveGraphAsync(graph, _cancellationToken);

		var entity1 = new Entity(EntityType.Person, "Eve", 0.95);
		var entity2 = new Entity(EntityType.Organization, "StartupCo", 0.90);
		var entity3 = new Entity(EntityType.Person, "Frank", 0.92);

		await _graphStore.SaveEntityAsync(graph.Id, entity1, _cancellationToken);
		await _graphStore.SaveEntityAsync(graph.Id, entity2, _cancellationToken);
		await _graphStore.SaveEntityAsync(graph.Id, entity3, _cancellationToken);

		var worksFor = new Relationship(entity1.Id, entity2.Id, RelationshipType.WorksFor, 1.0, 0.95);
		var relatedTo = new Relationship(entity1.Id, entity3.Id, RelationshipType.RelatedTo, 0.7, 0.85);
		var mentions = new Relationship(entity2.Id, entity3.Id, RelationshipType.Mentions, 0.6, 0.80);

		await _graphStore.SaveRelationshipAsync(graph.Id, worksFor, _cancellationToken);
		await _graphStore.SaveRelationshipAsync(graph.Id, relatedTo, _cancellationToken);
		await _graphStore.SaveRelationshipAsync(graph.Id, mentions, _cancellationToken);

		// Act
		var worksForRels = await _graphStore.QueryRelationshipsByTypeAsync(graph.Id, RelationshipType.WorksFor, _cancellationToken);
		var mentionsRels = await _graphStore.QueryRelationshipsByTypeAsync(graph.Id, RelationshipType.Mentions, _cancellationToken);

		// Assert
		worksForRels.Should().ContainSingle();
		worksForRels[0].FromEntityId.Should().Be(entity1.Id);
		worksForRels[0].ToEntityId.Should().Be(entity2.Id);

		mentionsRels.Should().ContainSingle();
		mentionsRels[0].Type.Should().Be(RelationshipType.Mentions);
	}

	[Fact]
	public async Task ListGraphsAsync_WithMultipleGraphs_ShouldReturnAllMetadata()
	{
		// Arrange
		await _fixture.CleanDatabaseAsync();

		var graph1 = new Graph("Graph 1") { Metadata = new GraphMetadata { Description = "First graph" } };
		var graph2 = new Graph("Graph 2") { Metadata = new GraphMetadata { Description = "Second graph" } };
		var graph3 = new Graph("Graph 3") { Metadata = new GraphMetadata { Description = "Third graph" } };

		await _graphStore.SaveGraphAsync(graph1, _cancellationToken);
		await _graphStore.SaveGraphAsync(graph2, _cancellationToken);
		await _graphStore.SaveGraphAsync(graph3, _cancellationToken);

		// Act
		var metadata = await _graphStore.ListGraphsAsync(_cancellationToken);

		// Assert
		metadata.Should().HaveCount(3);
		metadata.Should().Contain(m => m.Description == "First graph");
		metadata.Should().Contain(m => m.Description == "Second graph");
		metadata.Should().Contain(m => m.Description == "Third graph");
	}

	[Fact]
	public async Task SaveGraphAsync_WithComplexGraph_ShouldPreserveAllProperties()
	{
		// Arrange
		await _fixture.CleanDatabaseAsync();

		var graph = new Graph("Complex Graph")
		{
			Metadata = new GraphMetadata
			{
				Description = "A complex test graph",
				Version = "2.0",
				CreatedAt = DateTimeOffset.UtcNow
			},
			Schema = new GraphSchema(),
			Statistics = new GraphStatistics
			{
				TotalEntities = 3,
				TotalRelationships = 2
			}
		};

		var entity1 = new Entity(EntityType.Person, "Grace Hopper", 0.98);
		entity1.AddAlias("Amazing Grace");
		entity1.AddSource(Guid.NewGuid(), 0, "Grace Hopper was a computer scientist");
		entity1.Properties["birthYear"] = 1906;
		entity1.Properties["field"] = "Computer Science";

		var entity2 = new Entity(EntityType.Concept, "COBOL", 0.95);
		entity2.AddSource(Guid.NewGuid(), 50, "Created the COBOL programming language");

		var entity3 = new Entity(EntityType.Organization, "US Navy", 0.92);

		graph.AddEntity(entity1);
		graph.AddEntity(entity2);
		graph.AddEntity(entity3);

		var rel1 = new Relationship(entity1.Id, entity2.Id, RelationshipType.RelatedTo, 1.0, 0.95);
		rel1.AddEvidence(Guid.NewGuid(), "Grace Hopper invented COBOL", 0.95);
		rel1.Properties["relationship"] = "Creator";

		var rel2 = new Relationship(entity1.Id, entity3.Id, RelationshipType.WorksFor, 0.9, 0.93);

		graph.AddRelationship(rel1);
		graph.AddRelationship(rel2);

		// Act
		await _graphStore.SaveGraphAsync(graph, _cancellationToken);
		var loaded = await _graphStore.LoadGraphAsync(graph.Id, _cancellationToken);

		// Assert
		loaded.Should().NotBeNull();
		loaded!.Name.Should().Be("Complex Graph");
		loaded.Metadata.Description.Should().Be("A complex test graph");
		loaded.Metadata.Version.Should().Be("2.0");

		var loadedEntity1 = loaded.GetEntitiesByName("Grace Hopper").Should().ContainSingle().Subject;
		loadedEntity1.Aliases.Should().Contain("Amazing Grace");
		loadedEntity1.Sources.Should().ContainSingle();
		loadedEntity1.Properties.Should().ContainKey("birthYear");
		loadedEntity1.Properties["field"].Should().Be("Computer Science");

		var loadedRel1 = loaded.GetRelationshipsByType(RelationshipType.RelatedTo).Should().ContainSingle().Subject;
		loadedRel1.Evidence.Should().ContainSingle();
		loadedRel1.Properties.Should().ContainKey("relationship");
		loadedRel1.Properties["relationship"].Should().Be("Creator");
	}
}
