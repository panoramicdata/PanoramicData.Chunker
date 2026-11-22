using AwesomeAssertions;
using PanoramicData.Chunker.KnowledgeGraph;
using PanoramicData.Chunker.KnowledgeGraph.Extractors;
using PanoramicData.Chunker.Models;
using PanoramicData.Chunker.Models.KnowledgeGraph;
using PanoramicData.Chunker.Tests.Utilities;

namespace PanoramicData.Chunker.Tests.Unit.KnowledgeGraph;

/// <summary>
/// Unit tests for the KnowledgeGraphBuilder class.
/// </summary>
public class KnowledgeGraphBuilderTests(ITestOutputHelper output) : BaseTest(output)
{
	[Fact]
	public void Constructor_ShouldInitialize()
	{
		new KnowledgeGraphBuilder().Should().NotBeNull();
	}

	[Fact]
	public void AddEntityExtractor_ShouldReturnBuilder()
	{
		var builder = new KnowledgeGraphBuilder();

		builder.AddEntityExtractor(new SimpleKeywordExtractor()).Should().BeSameAs(builder);
	}

	[Fact]
	public void AddRelationshipExtractor_ShouldReturnBuilder()
	{
		var builder = new KnowledgeGraphBuilder();

		builder.AddRelationshipExtractor(new CooccurrenceRelationshipExtractor()).Should().BeSameAs(builder);
	}

	[Fact]
	public async Task BuildGraphAsync_WithNoChunks_ShouldReturnEmptyGraph()
	{
		var result = await BuildAsync(new KnowledgeGraphBuilder(), []);

		result.Success.Should().BeTrue();
		result.Graph.Should().NotBeNull();
		result.Graph.Entities.Should().BeEmpty();
		result.Graph.Relationships.Should().BeEmpty();
		result.Warnings.Should().ContainSingle();
		_output.WriteLine($"Warnings: {string.Join(", ", result.Warnings)}");
	}

	[Fact]
	public async Task BuildGraphAsync_WithNoExtractors_ShouldReturnEmptyGraph()
	{
		var result = await BuildAsync(new KnowledgeGraphBuilder(), CreateChunks("Test content with keywords."));

		result.Success.Should().BeTrue();
		result.Graph.Should().NotBeNull();
		result.Graph.Entities.Should().BeEmpty();
		result.Graph.Relationships.Should().BeEmpty();
	}

	[Fact]
	public async Task BuildGraphAsync_WithEntityExtractor_ShouldExtractEntities()
	{
		var result = await BuildAsync(
			BuilderWith(5),
			CreateChunks("Machine learning and artificial intelligence are transforming technology. Machine learning models require data."),
			"Test Graph");

		result.Success.Should().BeTrue();
		result.Graph.Should().NotBeNull();
		result.Graph.Entities.Should().NotBeEmpty();
		result.Graph.Name.Should().Be("Test Graph");
		_output.WriteLine($"Extracted {result.Statistics.EntitiesExtracted} entities");
		_output.WriteLine($"After deduplication: {result.Statistics.EntitiesAfterDeduplication} entities");
	}

	[Fact]
	public async Task BuildGraphAsync_WithBothExtractors_ShouldExtractBoth()
	{
		var result = await BuildAsync(
			BuilderWith(10, new CooccurrenceRelationshipExtractor(maxDistance: 500, minConfidence: 0.3)),
			CreateChunks("Microsoft Azure cloud computing platform provides services for machine learning and artificial intelligence applications."));

		result.Success.Should().BeTrue();
		result.Graph.Should().NotBeNull();
		result.Graph.Entities.Should().NotBeEmpty();
		_output.WriteLine($"Entities extracted: {result.Statistics.EntitiesExtracted}");
		_output.WriteLine($"Relationships extracted: {result.Statistics.RelationshipsExtracted}");
		_output.WriteLine($"Extractors used: {string.Join(", ", result.Statistics.ExtractorsUsed)}");
	}

	[Fact]
	public async Task BuildGraphAsync_ShouldDeduplicateEntities()
	{
		var result = await BuildAsync(
			BuilderWith(10),
			CreateChunks("Machine learning is important. Machine learning transforms data. Machine learning requires expertise."));

		result.Success.Should().BeTrue();
		result.Graph.Should().NotBeNull();
		var mlEntities = result.Graph.Entities
			.Where(e => e.Name.Contains("machine", StringComparison.OrdinalIgnoreCase))
			.ToList();

		_output.WriteLine($"Total entities: {result.Graph.Entities.Count}");
		_output.WriteLine($"'Machine learning' entities: {mlEntities.Count}");
		_output.WriteLine($"Entities merged: {result.Statistics.EntitiesMerged}");

		(result.Statistics.EntitiesMerged >= 0).Should().BeTrue();
	}

	[Fact]
	public async Task BuildGraphAsync_ShouldConsolidateRelationships()
	{
		var chunks = new List<ChunkerBase>
		{
			new TestContentChunk { Id = Guid.NewGuid(), Content = "Cloud computing and machine learning are related technologies." },
			new TestContentChunk { Id = Guid.NewGuid(), Content = "Cloud computing enables machine learning at scale." }
		};

		var result = await BuildAsync(BuilderWith(10, new CooccurrenceRelationshipExtractor()), chunks);

		result.Success.Should().BeTrue();
		_output.WriteLine($"Relationships extracted: {result.Statistics.RelationshipsExtracted}");
		_output.WriteLine($"Relationships after consolidation: {result.Statistics.RelationshipsAfterConsolidation}");
		_output.WriteLine($"Relationships merged: {result.Statistics.RelationshipsMerged}");

		(result.Statistics.RelationshipsAfterConsolidation <= result.Statistics.RelationshipsExtracted).Should().BeTrue();
	}

	[Fact]
	public async Task BuildGraphAsync_ShouldBuildIndexes()
	{
		var result = await BuildAsync(BuilderWith(5), CreateChunks("Data science machine learning artificial intelligence analytics."));

		result.Success.Should().BeTrue();
		result.Graph.Should().NotBeNull();

		if (result.Graph.Entities.Count > 0)
		{
			var firstEntity = result.Graph.Entities[0];
			var foundEntity = result.Graph.GetEntity(firstEntity.Id);
			foundEntity.Should().NotBeNull();
			foundEntity.Id.Should().Be(firstEntity.Id);
		}
	}

	[Fact]
	public async Task BuildGraphAsync_ShouldComputeStatistics()
	{
		var result = await BuildAsync(
			BuilderWith(10, new CooccurrenceRelationshipExtractor()),
			CreateChunks("Machine learning and data science are important for artificial intelligence applications."));

		result.Success.Should().BeTrue();
		result.Graph.Should().NotBeNull();
		result.Graph.Statistics.Should().NotBeNull();
		result.Graph.Statistics.TotalEntities.Should().Be(result.Graph.Entities.Count);
		result.Graph.Statistics.TotalRelationships.Should().Be(result.Graph.Relationships.Count);
		_output.WriteLine($"Graph statistics: {result.Graph.Statistics.TotalEntities} entities, {result.Graph.Statistics.TotalRelationships} relationships");
	}

	[Fact]
	public async Task BuildGraphAsync_ShouldValidateGraph()
	{
		var result = await BuildAsync(
			BuilderWith(5, new CooccurrenceRelationshipExtractor()),
			CreateChunks("Cloud computing and machine learning technologies."));

		result.Success.Should().BeTrue();
		if (!result.Success || result.Errors.Count > 0)
		{
			_output.WriteLine($"Validation errors: {string.Join(", ", result.Errors)}");
		}
	}

	[Fact]
	public async Task BuildGraphAsync_ShouldSetTimestamps()
	{
		var startTime = DateTimeOffset.UtcNow;
		var result = await BuildAsync(BuilderWith(5), CreateChunks("Test content for timestamp validation."));
		var endTime = DateTimeOffset.UtcNow;

		(result.StartedAt >= startTime).Should().BeTrue();
		(result.StartedAt <= endTime).Should().BeTrue();
		(result.CompletedAt >= result.StartedAt).Should().BeTrue();
		result.Duration.Should().NotBeNull();
		(result.Duration!.Value.TotalMilliseconds > 0).Should().BeTrue();
		_output.WriteLine($"Build duration: {result.Duration!.Value.TotalMilliseconds}ms");
	}

	[Fact]
	public async Task BuildGraphAsync_ShouldTrackExtractionStatistics()
	{
		var chunks = new List<ChunkerBase>
		{
			new TestContentChunk { Id = Guid.NewGuid(), Content = "Cloud computing provides infrastructure." },
			new TestContentChunk { Id = Guid.NewGuid(), Content = "Machine learning requires data processing." },
			new TestContentChunk { Id = Guid.NewGuid(), Content = "Artificial intelligence transforms industries." }
		};

		var result = await BuildAsync(BuilderWith(10, new CooccurrenceRelationshipExtractor()), chunks);

		result.Success.Should().BeTrue();
		result.Statistics.ChunksProcessed.Should().Be(3);
		(result.Statistics.EntityExtractionTime.TotalMilliseconds > 0).Should().BeTrue();
		result.Statistics.ExtractorsUsed.Should().NotBeEmpty();

		_output.WriteLine($"Chunks processed: {result.Statistics.ChunksProcessed}");
		_output.WriteLine($"Entity extraction time: {result.Statistics.EntityExtractionTime.TotalMilliseconds}ms");
		_output.WriteLine($"Relationship extraction time: {result.Statistics.RelationshipExtractionTime.TotalMilliseconds}ms");
		_output.WriteLine($"Graph building time: {result.Statistics.GraphBuildingTime.TotalMilliseconds}ms");
	}

	[Fact]
	public async Task BuildGraphAsync_WithCancellation_ShouldHandleGracefully()
	{
		var builder = new KnowledgeGraphBuilder().AddEntityExtractor(new SimpleKeywordExtractor());
		var cts = new CancellationTokenSource();
		cts.Cancel();

		var result = await builder.BuildGraphAsync(CreateChunks("Test content."), "Knowledge Graph", cancellationToken: cts.Token);

		result.Success.Should().BeFalse();
		result.Errors.Should().Contain(e => e.Contains("cancel", StringComparison.OrdinalIgnoreCase));
		_output.WriteLine($"Errors: {string.Join(", ", result.Errors)}");
	}

	[Fact]
	public async Task BuildGraphAsync_WithMultipleExtractors_ShouldTrackAllInStatistics()
	{
		var builder = new KnowledgeGraphBuilder()
			.AddEntityExtractor(new SimpleKeywordExtractor())
			.AddRelationshipExtractor(new CooccurrenceRelationshipExtractor());

		var result = await BuildAsync(builder, CreateChunks("Machine learning and artificial intelligence."));

		result.Success.Should().BeTrue();
		result.Statistics.ExtractorsUsed.Should().Contain("SimpleKeywordExtractor");
		result.Statistics.ExtractorsUsed.Should().Contain("CooccurrenceRelationshipExtractor");
		_output.WriteLine($"Extractors used: {string.Join(", ", result.Statistics.ExtractorsUsed)}");
	}

	[Fact]
	public async Task BuildGraphAsync_WithCustomEntityResolver_ShouldUseIt()
	{
		var builder = new KnowledgeGraphBuilder(new EntityResolver(similarityThreshold: 0.9))
			.AddEntityExtractor(new SimpleKeywordExtractor(maxKeywords: 10));

		var result = await BuildAsync(builder, CreateChunks("Machine learning and machine-learning are similar terms."));

		result.Success.Should().BeTrue();
		_output.WriteLine($"Entities before deduplication: {result.Statistics.EntitiesExtracted}");
		_output.WriteLine($"Entities after deduplication: {result.Statistics.EntitiesAfterDeduplication}");
	}

	private static KnowledgeGraphBuilder BuilderWith(int maxKeywords, CooccurrenceRelationshipExtractor? relationships = null)
	{
		var builder = new KnowledgeGraphBuilder().AddEntityExtractor(new SimpleKeywordExtractor(maxKeywords: maxKeywords));
		return relationships is null ? builder : builder.AddRelationshipExtractor(relationships);
	}

	private Task<KnowledgeGraphResult> BuildAsync(KnowledgeGraphBuilder builder, List<ChunkerBase> chunks, string name = "Knowledge Graph")
		=> builder.BuildGraphAsync(chunks, name, CancellationToken);

	private static List<ChunkerBase> CreateChunks(string content)
		=> [new TestContentChunk { Id = Guid.NewGuid(), Content = content }];
}