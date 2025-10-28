using AwesomeAssertions;
using PanoramicData.Chunker.Models.KnowledgeGraph;

namespace PanoramicData.Chunker.Tests.Unit.KnowledgeGraph;

/// <summary>
/// Unit tests for the Entity class.
/// </summary>
public class EntityTests
{
	[Fact]
	public void Constructor_WithTypeAndName_ShouldInitializeCorrectly()
	{
		// Arrange & Act
		var entity = new Entity(EntityType.Person, "John Doe", 0.95);

		// Assert
		entity.Id.Should().NotBe(Guid.Empty);
		entity.Type.Should().Be(EntityType.Person);
		entity.Name.Should().Be("John Doe");
		entity.NormalizedName.Should().Be("john doe");
		entity.Confidence.Should().Be(0.95);
		entity.Frequency.Should().Be(0);
		entity.Sources.Should().BeEmpty();
		entity.Aliases.Should().BeEmpty();
	}

	[Fact]
	public void AddSource_ShouldAddToSourcesList()
	{
		// Arrange
		var entity = new Entity(EntityType.Keyword, "test");
		var chunkId = Guid.NewGuid();

		// Act
		entity.AddSource(chunkId, 10, "This is a test context");

		// Assert
		entity.Sources.Should().ContainSingle();
		entity.Sources[0].ChunkId.Should().Be(chunkId);
		entity.Sources[0].Position.Should().Be(10);
		entity.Sources[0].Context.Should().Be("This is a test context");
	}

	[Fact]
	public void AddAlias_ShouldAddToAliasesList()
	{
		// Arrange
		var entity = new Entity(EntityType.Person, "John Doe");

		// Act
		entity.AddAlias("J. Doe");
		entity.AddAlias("Doe, John");

		// Assert
		entity.Aliases.Should().HaveCount(2);
		entity.Aliases.Should().Contain("J. Doe");
		entity.Aliases.Should().Contain("Doe, John");
	}

	[Fact]
	public void AddAlias_WithDuplicate_ShouldNotAddDuplicate()
	{
		// Arrange
		var entity = new Entity(EntityType.Person, "John Doe");

		// Act
		entity.AddAlias("J. Doe");
		entity.AddAlias("J. Doe"); // Duplicate

		// Assert
		entity.Aliases.Should().ContainSingle();
	}

	[Fact]
	public void Merge_ShouldCombineEntities()
	{
		// Arrange
		var entity1 = new Entity(EntityType.Keyword, "test", 0.8)
		{
			Frequency = 5
		};
		entity1.AddSource(Guid.NewGuid(), 10, "context1");
		entity1.AddAlias("testing");

		var entity2 = new Entity(EntityType.Keyword, "test", 0.9)
		{
			Frequency = 3
		};
		entity2.AddSource(Guid.NewGuid(), 20, "context2");
		entity2.AddAlias("tests");

		// Act
		entity1.Merge(entity2);

		// Assert
		entity1.Confidence.Should().Be(0.9); // Takes higher confidence
		entity1.Frequency.Should().Be(8); // Sums frequencies
		entity1.Sources.Should().HaveCount(2); // Combines sources
		entity1.Aliases.Should().HaveCount(2); // Combines aliases
		entity1.Aliases.Should().Contain("testing");
		entity1.Aliases.Should().Contain("tests");
	}

	[Fact]
	public void ToString_ShouldReturnFormattedString()
	{
		// Arrange
		var entity = new Entity(EntityType.Person, "John Doe", 0.95);

		// Act
		var result = entity.ToString();

		// Assert
		result.Should().Contain("Person");
		result.Should().Contain("John Doe");
		result.Should().Contain("0.95");
	}

	[Fact]
	public void Properties_ShouldAllowCustomData()
	{
		// Arrange
		var entity = new Entity(EntityType.Organization, "Acme Corp");

		// Act
		entity.Properties["industry"] = "Technology";
		entity.Properties["founded"] = 2020;

		// Assert
		entity.Properties["industry"].Should().Be("Technology");
		entity.Properties["founded"].Should().Be(2020);
	}

	[Fact]
	public void Metadata_ShouldTrackExtractionInfo()
	{
		// Arrange
		var entity = new Entity(EntityType.Keyword, "test");

		// Act
		entity.Metadata.ExtractorName = "SimpleKeywordExtractor";
		entity.Metadata.ExtractorVersion = "1.0";
		entity.Metadata.ExtractedAt = DateTimeOffset.UtcNow;

		// Assert
		entity.Metadata.ExtractorName.Should().Be("SimpleKeywordExtractor");
		entity.Metadata.ExtractorVersion.Should().Be("1.0");
		(entity.Metadata.ExtractedAt <= DateTimeOffset.UtcNow).Should().BeTrue();
	}
}
