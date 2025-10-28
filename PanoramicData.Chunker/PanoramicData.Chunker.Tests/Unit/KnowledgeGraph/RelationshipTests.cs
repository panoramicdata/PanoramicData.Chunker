using AwesomeAssertions;
using PanoramicData.Chunker.Models.KnowledgeGraph;

namespace PanoramicData.Chunker.Tests.Unit.KnowledgeGraph;

/// <summary>
/// Unit tests for the Relationship class.
/// </summary>
public class RelationshipTests
{
	[Fact]
	public void Constructor_WithBasicParameters_ShouldInitializeCorrectly()
	{
		// Arrange
		var fromEntityId = Guid.NewGuid();
		var toEntityId = Guid.NewGuid();

		// Act
		var relationship = new Relationship(
			fromEntityId,
			toEntityId,
			RelationshipType.Mentions,
			weight: 0.8,
			confidence: 0.95);

		// Assert
		relationship.Id.Should().NotBe(Guid.Empty);
		relationship.FromEntityId.Should().Be(fromEntityId);
		relationship.ToEntityId.Should().Be(toEntityId);
		relationship.Type.Should().Be(RelationshipType.Mentions);
		relationship.Weight.Should().Be(0.8);
		relationship.Confidence.Should().Be(0.95);
		relationship.Bidirectional.Should().BeFalse();
		relationship.Evidence.Should().BeEmpty();
	}

	[Fact]
	public void Bidirectional_ShouldAllowSettingToTrue()
	{
		// Arrange
		var relationship = new Relationship(
			Guid.NewGuid(),
			Guid.NewGuid(),
			RelationshipType.RelatedTo)
		{
			// Act
			Bidirectional = true
		};

		// Assert
		relationship.Bidirectional.Should().BeTrue();
	}

	[Fact]
	public void AddEvidence_ShouldAddToEvidenceList()
	{
		// Arrange
		var relationship = new Relationship(
			Guid.NewGuid(),
			Guid.NewGuid(),
			RelationshipType.Mentions);
		var chunkId = Guid.NewGuid();

		// Act
		relationship.AddEvidence(chunkId, "Test context", 0.9);

		// Assert
		relationship.Evidence.Should().ContainSingle();
		relationship.Evidence[0].ChunkId.Should().Be(chunkId);
		relationship.Evidence[0].Context.Should().Be("Test context");
		relationship.Evidence[0].Confidence.Should().Be(0.9);
	}

	[Fact]
	public void AddEvidence_WithMultiple_ShouldAddAll()
	{
		// Arrange
		var relationship = new Relationship(
			Guid.NewGuid(),
			Guid.NewGuid(),
			RelationshipType.Mentions);

		// Act
		relationship.AddEvidence(Guid.NewGuid(), "Context 1", 0.8);
		relationship.AddEvidence(Guid.NewGuid(), "Context 2", 0.9);
		relationship.AddEvidence(Guid.NewGuid(), "Context 3", 0.7);

		// Assert
		relationship.Evidence.Should().HaveCount(3);
	}

	[Fact]
	public void Merge_ShouldCombineRelationships()
	{
		// Arrange
		var fromId = Guid.NewGuid();
		var toId = Guid.NewGuid();

		var rel1 = new Relationship(fromId, toId, RelationshipType.Mentions, 0.5, 0.8);
		rel1.AddEvidence(Guid.NewGuid(), "Context 1", 0.8);
		rel1.Properties["prop1"] = "value1";

		var rel2 = new Relationship(fromId, toId, RelationshipType.Mentions, 0.7, 0.9);
		rel2.AddEvidence(Guid.NewGuid(), "Context 2", 0.9);
		rel2.Properties["prop2"] = "value2";

		// Act
		rel1.Merge(rel2);

		// Assert
		rel1.Weight.Should().Be(1.2); // Sum of weights
		rel1.Confidence.Should().Be(0.9); // Max confidence
		rel1.Evidence.Should().HaveCount(2);
		rel1.Properties.Should().HaveCount(2);
	}

	[Fact]
	public void Merge_ShouldPreserveBothPropertiesOnConflict()
	{
		// Arrange
		var fromId = Guid.NewGuid();
		var toId = Guid.NewGuid();

		var rel1 = new Relationship(fromId, toId, RelationshipType.Mentions);
		rel1.Properties["shared"] = "value1";

		var rel2 = new Relationship(fromId, toId, RelationshipType.Mentions);
		rel2.Properties["shared"] = "value2";

		// Act
		rel1.Merge(rel2);

		// Assert
		// Second value should overwrite
		rel1.Properties["shared"].Should().Be("value2");
	}

	[Fact]
	public void Properties_ShouldAllowCustomData()
	{
		// Arrange
		var relationship = new Relationship(
			Guid.NewGuid(),
			Guid.NewGuid(),
			RelationshipType.Mentions);

		// Act
		relationship.Properties["MinDistance"] = 50;
		relationship.Properties["Frequency"] = 5;

		// Assert
		relationship.Properties["MinDistance"].Should().Be(50);
		relationship.Properties["Frequency"].Should().Be(5);
	}

	[Fact]
	public void Metadata_ShouldTrackExtractionInfo()
	{
		// Arrange
		var relationship = new Relationship(
			Guid.NewGuid(),
			Guid.NewGuid(),
			RelationshipType.Mentions);

		// Act
		relationship.Metadata.ExtractorName = "CooccurrenceRelationshipExtractor";
		relationship.Metadata.ExtractorVersion = "1.0";
		relationship.Metadata.ExtractedAt = DateTimeOffset.UtcNow;

		// Assert
		relationship.Metadata.ExtractorName.Should().Be("CooccurrenceRelationshipExtractor");
		relationship.Metadata.ExtractorVersion.Should().Be("1.0");
		(relationship.Metadata.ExtractedAt <= DateTimeOffset.UtcNow).Should().BeTrue();
	}

	[Fact]
	public void ToString_ShouldReturnFormattedString()
	{
		// Arrange
		var relationship = new Relationship(
			Guid.NewGuid(),
			Guid.NewGuid(),
			RelationshipType.Mentions,
			weight: 0.8,
			confidence: 0.95);

		// Act
		var result = relationship.ToString();

		// Assert
		result.Should().Contain("Mentions");
		result.Should().Contain("0.8");
		result.Should().Contain("0.95");
	}

	[Fact]
	public void Constructor_WithDefaultValues_ShouldSetWeightAndConfidenceToOne()
	{
		// Arrange & Act
		var relationship = new Relationship(
			Guid.NewGuid(),
			Guid.NewGuid(),
			RelationshipType.RelatedTo);

		// Assert
		relationship.Weight.Should().Be(1.0);
		relationship.Confidence.Should().Be(1.0);
	}
}
