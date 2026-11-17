using AwesomeAssertions;
using PanoramicData.Chunker.Configuration;

namespace PanoramicData.Chunker.Tests.Unit.Core;

public class KnowledgeGraphOptionsTests
{
	[Fact]
	public void Validate_DefaultOptions_HaveNoErrors()
	{
		_ = new KnowledgeGraphOptions().Validate().Should().BeEmpty();
	}

	[Fact]
	public void Validate_ReportsEveryInvalidSetting()
	{
		var options = new KnowledgeGraphOptions
		{
			MinEntityConfidence = 1.5,
			MinRelationshipConfidence = -0.1,
			MaxKeywordsPerChunk = 0,
			MinWordLength = 0,
			MaxCooccurrenceDistance = 0,
			EntitySimilarityThreshold = 2.0,
			EnableGraphStorage = true,
			PostgresConnectionString = null,
			GraphName = " "
		};

		var errors = options.Validate();

		_ = errors.Should().HaveCount(8);
		_ = errors.Should().Contain(e => e.StartsWith("PostgresConnectionString"));
		_ = errors.Should().Contain("GraphName cannot be empty.");
	}
}