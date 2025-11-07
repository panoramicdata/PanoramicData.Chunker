using AwesomeAssertions;
using PanoramicData.Chunker.Tests.Helpers;

namespace PanoramicData.Chunker.Tests.Unit.Helpers;

public class GroundTruthLoaderTests
{
	[Fact]
	public void Load_ValidFile_ShouldLoadAllRelationships()
	{
		// Act
		var groundTruth = GroundTruthLoader.Load("TestData/Darwin-GroundTruth.txt");

		// Assert
		groundTruth.Should().NotBeEmpty("Ground truth file should contain relationships");
		groundTruth.Should().HaveCountGreaterThanOrEqualTo(50, "Should have at least 50 relationships");
	}

	[Fact]
	public void Load_ValidFile_ShouldParseAllFields()
	{
		// Act
		var groundTruth = GroundTruthLoader.Load("TestData/Darwin-GroundTruth.txt");

		// Assert - check first relationship
		var first = groundTruth.First();
		first.Entity1.Should().NotBeNullOrEmpty();
		first.Entity2.Should().NotBeNullOrEmpty();
		first.RelationType.Should().NotBeNullOrEmpty();
		first.Confidence.Should().BeInRange(0.0, 1.0);
		first.Section.Should().NotBeNullOrEmpty();
		first.Notes.Should().NotBeNullOrEmpty();
	}

	[Fact]
	public void Load_ValidFile_ShouldHaveExpectedRelationship()
	{
		// Act
		var groundTruth = GroundTruthLoader.Load("TestData/Darwin-GroundTruth.txt");

		// Assert - verify key relationship exists
		var plinianRelationship = groundTruth.FirstOrDefault(r =>
			r.Entity1.Contains("Jameson", StringComparison.OrdinalIgnoreCase) &&
			r.Entity2.Contains("Plinian", StringComparison.OrdinalIgnoreCase) &&
			r.RelationType == "Founded");

		plinianRelationship.Should().NotBeNull("Should include Jameson founding Plinian Society");
		plinianRelationship!.Confidence.Should().Be(1.0, "This relationship is explicitly stated");
	}

	[Fact]
	public void GetStatistics_ShouldProvideAccurateStats()
	{
		// Arrange
		var groundTruth = GroundTruthLoader.Load("TestData/Darwin-GroundTruth.txt");

		// Act
		var stats = GroundTruthLoader.GetStatistics(groundTruth);

		// Assert
		stats.TotalRelationships.Should().Be(groundTruth.Count);
		stats.UniqueEntity1Count.Should().BeGreaterThan(0);
		stats.UniqueEntity2Count.Should().BeGreaterThan(0);
		stats.UniqueRelationshipTypes.Should().BeGreaterThan(5, "Should have diverse relationship types");
		stats.AverageConfidence.Should().BeInRange(0.8, 1.0, "Most relationships should be high confidence");
		stats.ConfidenceDistribution.Should().NotBeEmpty();
		stats.RelationshipTypeDistribution.Should().NotBeEmpty();
		stats.SectionDistribution.Should().NotBeEmpty();
	}

	[Fact]
	public void Load_NonExistentFile_ShouldThrowFileNotFoundException()
	{
		// Act & Assert
		var act = () => GroundTruthLoader.Load("NonExistent.txt");
		act.Should().Throw<FileNotFoundException>();
	}

	[Fact]
	public void ToString_ShouldGenerateReadableOutput()
	{
		// Arrange
		var groundTruth = GroundTruthLoader.Load("TestData/Darwin-GroundTruth.txt");

		// Act
		var relationship = groundTruth.First();
		var output = relationship.ToString();

		// Assert
		output.Should().Contain(relationship.Entity1);
		output.Should().Contain(relationship.Entity2);
		output.Should().Contain(relationship.RelationType);
		output.Should().Contain("confidence");
	}
}
