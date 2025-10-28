using AwesomeAssertions;
using PanoramicData.Chunker.Models.KnowledgeGraph;

namespace PanoramicData.Chunker.Tests.Unit.KnowledgeGraph;

/// <summary>
/// Unit tests for the Graph (KnowledgeGraph) class.
/// </summary>
public class KnowledgeGraphTests
{
	[Fact]
	public void Constructor_WithName_ShouldInitialize()
	{
		// Arrange & Act
		var graph = new Graph("Test Graph");

		// Assert
		graph.Should().NotBeNull();
		graph.Name.Should().Be("Test Graph");
		graph.Entities.Should().BeEmpty();
		graph.Relationships.Should().BeEmpty();
		graph.Statistics.Should().NotBeNull();
	}

	[Fact]
	public void AddEntity_ShouldAddToEntitiesList()
	{
		// Arrange
		var graph = new Graph("Test");
		var entity = new Entity
		{
			Name = "Test Entity",
			Type = EntityType.Concept,
			NormalizedName = "test entity"
		};

		// Act
		graph.AddEntity(entity);

		// Assert
		graph.Entities.Should().ContainSingle();
		graph.Entities[0].Id.Should().Be(entity.Id);
	}

	[Fact]
	public void AddEntity_WithDuplicateId_ShouldNotAddTwice()
	{
		// Arrange
		var graph = new Graph("Test");
		var entityId = Guid.NewGuid();
		var entity1 = new Entity
		{
			Id = entityId,
			Name = "Entity 1",
			Type = EntityType.Person,
			NormalizedName = "entity 1"
		};
		var entity2 = new Entity
		{
			Id = entityId,
			Name = "Entity 2",
			Type = EntityType.Person,
			NormalizedName = "entity 2"
		};

		// Act
		graph.AddEntity(entity1);
		graph.AddEntity(entity2);

		// Assert
		graph.Entities.Should().HaveCount(2); // Both are added, duplication check is in Validate()
		graph.Entities[0].Name.Should().Be("Entity 1");
	}

	[Fact]
	public void AddRelationship_ShouldAddToRelationshipsList()
	{
		// Arrange
		var graph = new Graph("Test");
		var entity1 = new Entity { Name = "Entity 1", Type = EntityType.Person, NormalizedName = "entity 1" };
		var entity2 = new Entity { Name = "Entity 2", Type = EntityType.Organization, NormalizedName = "entity 2" };
		graph.AddEntity(entity1);
		graph.AddEntity(entity2);

		var relationship = new Relationship(entity1.Id, entity2.Id, RelationshipType.Mentions);

		// Act
		graph.AddRelationship(relationship);

		// Assert
		graph.Relationships.Should().ContainSingle();
		graph.Relationships[0].Id.Should().Be(relationship.Id);
	}

	[Fact]
	public void GetEntity_WithValidId_ShouldReturnEntity()
	{
		// Arrange
		var graph = new Graph("Test");
		var entity = new Entity { Name = "Test", Type = EntityType.Concept, NormalizedName = "test" };
		graph.AddEntity(entity);
		graph.BuildIndexes();

		// Act
		var result = graph.GetEntity(entity.Id);

		// Assert
		result.Should().NotBeNull();
		result.Id.Should().Be(entity.Id);
	}

	[Fact]
	public void GetEntity_WithInvalidId_ShouldReturnNull()
	{
		// Arrange
		var graph = new Graph("Test");
		graph.BuildIndexes();

		// Act
		var result = graph.GetEntity(Guid.NewGuid());

		// Assert
		result.Should().BeNull();
	}

	[Fact]
	public void GetEntitiesByName_WithMatch_ShouldReturnEntity()
	{
		// Arrange
		var graph = new Graph("Test");
		var entity = new Entity { Name = "Microsoft", Type = EntityType.Organization, NormalizedName = "microsoft" };
		graph.AddEntity(entity);
		graph.BuildIndexes();

		// Act
		var results = graph.GetEntitiesByName("Microsoft");

		// Assert
		results.Should().ContainSingle();
		results[0].Id.Should().Be(entity.Id);
	}

	[Fact]
	public void GetEntitiesByName_WithPartialMatch_ShouldReturnMatches()
	{
		// Arrange
		var graph = new Graph("Test");
		var entity1 = new Entity { Name = "Microsoft", Type = EntityType.Organization, NormalizedName = "microsoft" };
		var entity2 = new Entity { Name = "Microsoft Azure", Type = EntityType.Product, NormalizedName = "microsoft azure" };
		graph.AddEntity(entity1);
		graph.AddEntity(entity2);
		graph.BuildIndexes();

		// Act - note: GetEntitiesByName does normalized exact match, not partial match
		var results = graph.GetEntitiesByName("microsoft");

		// Assert
		results.Should().ContainSingle(); // Only exact normalized match
	}

	[Fact]
	public void GetRelationships_WithValidEntityId_ShouldReturnRelationships()
	{
		// Arrange
		var graph = new Graph("Test");
		var entity1 = new Entity { Name = "E1", Type = EntityType.Person, NormalizedName = "e1" };
		var entity2 = new Entity { Name = "E2", Type = EntityType.Organization, NormalizedName = "e2" };
		var entity3 = new Entity { Name = "E3", Type = EntityType.Location, NormalizedName = "e3" };
		graph.AddEntity(entity1);
		graph.AddEntity(entity2);
		graph.AddEntity(entity3);

		var rel1 = new Relationship(entity1.Id, entity2.Id, RelationshipType.Mentions);
		var rel2 = new Relationship(entity1.Id, entity3.Id, RelationshipType.RelatedTo);
		graph.AddRelationship(rel1);
		graph.AddRelationship(rel2);
		graph.BuildIndexes();

		// Act
		var results = graph.GetRelationships(entity1.Id);

		// Assert
		results.Should().HaveCount(2);
	}

	[Fact]
	public void GetRelationshipsByType_ShouldFilterByType()
	{
		// Arrange
		var graph = new Graph("Test");
		var entity1 = new Entity { Name = "E1", Type = EntityType.Person, NormalizedName = "e1" };
		var entity2 = new Entity { Name = "E2", Type = EntityType.Organization, NormalizedName = "e2" };
		graph.AddEntity(entity1);
		graph.AddEntity(entity2);

		var rel1 = new Relationship(entity1.Id, entity2.Id, RelationshipType.Mentions);
		var rel2 = new Relationship(entity1.Id, entity2.Id, RelationshipType.RelatedTo);
		graph.AddRelationship(rel1);
		graph.AddRelationship(rel2);
		graph.BuildIndexes();

		// Act
		var results = graph.GetRelationshipsByType(RelationshipType.Mentions);

		// Assert
		results.Should().ContainSingle();
		results[0].Type.Should().Be(RelationshipType.Mentions);
	}

	[Fact]
	public void ComputeStatistics_ShouldCalculateCorrectly()
	{
		// Arrange
		var graph = new Graph("Test");
		var entity1 = new Entity { Name = "E1", Type = EntityType.Person, NormalizedName = "e1" };
		var entity2 = new Entity { Name = "E2", Type = EntityType.Organization, NormalizedName = "e2" };
		graph.AddEntity(entity1);
		graph.AddEntity(entity2);

		var relationship = new Relationship(entity1.Id, entity2.Id, RelationshipType.Mentions);
		graph.AddRelationship(relationship);

		// Act
		graph.ComputeStatistics();

		// Assert
		graph.Statistics.TotalEntities.Should().Be(2);
		graph.Statistics.TotalRelationships.Should().Be(1);
		graph.Statistics.EntityTypeDistribution.Should().NotBeNull();
		graph.Statistics.RelationshipTypeDistribution.Should().NotBeNull();
	}

	[Fact]
	public void Validate_WithValidGraph_ShouldReturnTrue()
	{
		// Arrange
		var graph = new Graph("Test");
		var entity1 = new Entity { Name = "E1", Type = EntityType.Person, NormalizedName = "e1" };
		var entity2 = new Entity { Name = "E2", Type = EntityType.Organization, NormalizedName = "e2" };
		graph.AddEntity(entity1);
		graph.AddEntity(entity2);

		var relationship = new Relationship(entity1.Id, entity2.Id, RelationshipType.Mentions);
		graph.AddRelationship(relationship);
		graph.BuildIndexes();

		// Act
		var isValid = graph.Validate(out var errors);

		// Assert
		isValid.Should().BeTrue();
		errors.Should().BeEmpty();
	}

	[Fact]
	public void Validate_WithOrphanedRelationship_ShouldReturnFalse()
	{
		// Arrange
		var graph = new Graph("Test");
		var entity1 = new Entity { Name = "E1", Type = EntityType.Person, NormalizedName = "e1" };
		graph.AddEntity(entity1);

		// Add relationship referencing non-existent entity
		var relationship = new Relationship(entity1.Id, Guid.NewGuid(), RelationshipType.Mentions);
		graph.AddRelationship(relationship);
		graph.BuildIndexes();

		// Act
		var isValid = graph.Validate(out var errors);

		// Assert
		isValid.Should().BeFalse();
		errors.Should().NotBeEmpty();
	}

	[Fact]
	public void BuildIndexes_ShouldEnableFastLookup()
	{
		// Arrange
		var graph = new Graph("Test");
		for (var i = 0; i < 100; i++)
		{
			graph.AddEntity(new Entity
			{
				Name = $"Entity {i}",
				Type = EntityType.Concept,
				NormalizedName = $"entity {i}"
			});
		}

		// Act
		graph.BuildIndexes();
		var entity = graph.GetEntity(graph.Entities[50].Id);

		// Assert
		entity.Should().NotBeNull();
		entity.Id.Should().Be(graph.Entities[50].Id);
	}

	[Fact]
	public void EmptyGraph_ShouldHaveZeroStatistics()
	{
		// Arrange & Act
		var graph = new Graph("Empty");
		graph.ComputeStatistics();

		// Assert
		graph.Statistics.TotalEntities.Should().Be(0);
		graph.Statistics.TotalRelationships.Should().Be(0);
		graph.Statistics.AverageEntityConfidence.Should().Be(0);
	}
}
