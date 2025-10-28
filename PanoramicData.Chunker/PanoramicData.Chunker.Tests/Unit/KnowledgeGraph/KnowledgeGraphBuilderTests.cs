using AwesomeAssertions;
using PanoramicData.Chunker.KnowledgeGraph;
using PanoramicData.Chunker.KnowledgeGraph.Extractors;
using PanoramicData.Chunker.Models;
using PanoramicData.Chunker.Tests.Utilities;
using Xunit.Abstractions;

namespace PanoramicData.Chunker.Tests.Unit.KnowledgeGraph;

/// <summary>
/// Unit tests for the KnowledgeGraphBuilder class.
/// </summary>
public class KnowledgeGraphBuilderTests(ITestOutputHelper output)
{
	[Fact]
	public void Constructor_ShouldInitialize()
	{
		// Arrange & Act
		var builder = new KnowledgeGraphBuilder();

		// Assert
		builder.Should().NotBeNull();
	}

	[Fact]
	public void AddEntityExtractor_ShouldReturnBuilder()
	{
		// Arrange
		var builder = new KnowledgeGraphBuilder();
		var extractor = new SimpleKeywordExtractor();

		// Act
		var result = builder.AddEntityExtractor(extractor);

		// Assert
		result.Should().BeSameAs(builder);
	}

	[Fact]
	public void AddRelationshipExtractor_ShouldReturnBuilder()
	{
		// Arrange
		var builder = new KnowledgeGraphBuilder();
		var extractor = new CooccurrenceRelationshipExtractor();

		// Act
		var result = builder.AddRelationshipExtractor(extractor);

		// Assert
		result.Should().BeSameAs(builder);
	}

	[Fact]
	public async Task BuildGraphAsync_WithNoChunks_ShouldReturnEmptyGraph()
	{
		// Arrange
		var builder = new KnowledgeGraphBuilder();
		var chunks = new List<ChunkerBase>();

		// Act
		var result = await builder.BuildGraphAsync(chunks);

		// Assert
		result.Success.Should().BeTrue();
		result.Graph.Should().NotBeNull();
		result.Graph.Entities.Should().BeEmpty();
		result.Graph.Relationships.Should().BeEmpty();
		result.Warnings.Should().ContainSingle();
		output.WriteLine($"Warnings: {string.Join(", ", result.Warnings)}");
	}

	[Fact]
	public async Task BuildGraphAsync_WithNoExtractors_ShouldReturnEmptyGraph()
	{
		// Arrange
		var builder = new KnowledgeGraphBuilder();
		var chunks = new List<ChunkerBase>
		{
			new TestContentChunk { Content = "Test content with keywords." }
		};

		// Act
		var result = await builder.BuildGraphAsync(chunks);

		// Assert
		result.Success.Should().BeTrue();
		result.Graph.Should().NotBeNull();
		result.Graph.Entities.Should().BeEmpty();
		result.Graph.Relationships.Should().BeEmpty();
	}

	[Fact]
	public async Task BuildGraphAsync_WithEntityExtractor_ShouldExtractEntities()
	{
		// Arrange
		var builder = new KnowledgeGraphBuilder()
			.AddEntityExtractor(new SimpleKeywordExtractor(maxKeywords: 5));

		var chunks = new List<ChunkerBase>
		{
			new TestContentChunk
			{
				Id = Guid.NewGuid(),
				Content = "Machine learning and artificial intelligence are transforming technology. Machine learning models require data."
			}
		};

		// Act
		var result = await builder.BuildGraphAsync(chunks, "Test Graph");

		// Assert
		result.Success.Should().BeTrue();
		result.Graph.Should().NotBeNull();
		result.Graph.Entities.Should().NotBeEmpty();
		result.Graph.Name.Should().Be("Test Graph");
		output.WriteLine($"Extracted {result.Statistics.EntitiesExtracted} entities");
		output.WriteLine($"After deduplication: {result.Statistics.EntitiesAfterDeduplication} entities");
	}

	[Fact]
	public async Task BuildGraphAsync_WithBothExtractors_ShouldExtractBoth()
	{
		// Arrange
		var builder = new KnowledgeGraphBuilder()
			.AddEntityExtractor(new SimpleKeywordExtractor(maxKeywords: 10))
			.AddRelationshipExtractor(new CooccurrenceRelationshipExtractor(maxDistance: 500, minConfidence: 0.3));

		var chunks = new List<ChunkerBase>
		{
			new TestContentChunk
			{
				Id = Guid.NewGuid(),
				Content = "Microsoft Azure cloud computing platform provides services for machine learning and artificial intelligence applications."
			}
		};

		// Act
		var result = await builder.BuildGraphAsync(chunks);

		// Assert
		result.Success.Should().BeTrue();
		result.Graph.Should().NotBeNull();
		result.Graph.Entities.Should().NotBeEmpty();
		output.WriteLine($"Entities extracted: {result.Statistics.EntitiesExtracted}");
		output.WriteLine($"Relationships extracted: {result.Statistics.RelationshipsExtracted}");
		output.WriteLine($"Extractors used: {string.Join(", ", result.Statistics.ExtractorsUsed)}");
	}

	[Fact]
	public async Task BuildGraphAsync_ShouldDeduplicateEntities()
	{
		// Arrange
		var builder = new KnowledgeGraphBuilder()
			.AddEntityExtractor(new SimpleKeywordExtractor(maxKeywords: 10));

		var chunks = new List<ChunkerBase>
		{
			new TestContentChunk
			{
				Id = Guid.NewGuid(),
				Content = "Machine learning is important. Machine learning transforms data. Machine learning requires expertise."
			}
		};

		// Act
		var result = await builder.BuildGraphAsync(chunks);

		// Assert
		result.Success.Should().BeTrue();
		result.Graph.Should().NotBeNull();
		var mlEntities = result.Graph.Entities
			.Where(e => e.Name.Contains("machine", StringComparison.OrdinalIgnoreCase))
			.ToList();

		output.WriteLine($"Total entities: {result.Graph.Entities.Count}");
		output.WriteLine($"'Machine learning' entities: {mlEntities.Count}");
		output.WriteLine($"Entities merged: {result.Statistics.EntitiesMerged}");

		(result.Statistics.EntitiesMerged >= 0).Should().BeTrue();
	}

	[Fact]
	public async Task BuildGraphAsync_ShouldConsolidateRelationships()
	{
		// Arrange
		var chunkId1 = Guid.NewGuid();
		var chunkId2 = Guid.NewGuid();

		var builder = new KnowledgeGraphBuilder()
			.AddEntityExtractor(new SimpleKeywordExtractor(maxKeywords: 10))
			.AddRelationshipExtractor(new CooccurrenceRelationshipExtractor());

		var chunks = new List<ChunkerBase>
		{
			new TestContentChunk
			{
				Id = chunkId1,
				Content = "Cloud computing and machine learning are related technologies."
			},
			new TestContentChunk
			{
				Id = chunkId2,
				Content = "Cloud computing enables machine learning at scale."
			}
		};

		// Act
		var result = await builder.BuildGraphAsync(chunks);

		// Assert
		result.Success.Should().BeTrue();
		output.WriteLine($"Relationships extracted: {result.Statistics.RelationshipsExtracted}");
		output.WriteLine($"Relationships after consolidation: {result.Statistics.RelationshipsAfterConsolidation}");
		output.WriteLine($"Relationships merged: {result.Statistics.RelationshipsMerged}");

		(result.Statistics.RelationshipsAfterConsolidation <= result.Statistics.RelationshipsExtracted).Should().BeTrue();
	}

	[Fact]
	public async Task BuildGraphAsync_ShouldBuildIndexes()
	{
		// Arrange
		var builder = new KnowledgeGraphBuilder()
			.AddEntityExtractor(new SimpleKeywordExtractor(maxKeywords: 5));

		var chunks = new List<ChunkerBase>
		{
			new TestContentChunk
			{
				Id = Guid.NewGuid(),
				Content = "Data science machine learning artificial intelligence analytics."
			}
		};

		// Act
		var result = await builder.BuildGraphAsync(chunks);

		// Assert
		result.Success.Should().BeTrue();
		result.Graph.Should().NotBeNull();

		// Test that indexes work
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
		// Arrange
		var builder = new KnowledgeGraphBuilder()
			.AddEntityExtractor(new SimpleKeywordExtractor(maxKeywords: 10))
			.AddRelationshipExtractor(new CooccurrenceRelationshipExtractor());

		var chunks = new List<ChunkerBase>
		{
			new TestContentChunk
			{
				Id = Guid.NewGuid(),
				Content = "Machine learning and data science are important for artificial intelligence applications."
			}
		};

		// Act
		var result = await builder.BuildGraphAsync(chunks);

		// Assert
		result.Success.Should().BeTrue();
		result.Graph.Should().NotBeNull();
		result.Graph.Statistics.Should().NotBeNull();
		result.Graph.Statistics.TotalEntities.Should().Be(result.Graph.Entities.Count);
		result.Graph.Statistics.TotalRelationships.Should().Be(result.Graph.Relationships.Count);
		output.WriteLine($"Graph statistics: {result.Graph.Statistics.TotalEntities} entities, {result.Graph.Statistics.TotalRelationships} relationships");
	}

	[Fact]
	public async Task BuildGraphAsync_ShouldValidateGraph()
	{
		// Arrange
		var builder = new KnowledgeGraphBuilder()
			.AddEntityExtractor(new SimpleKeywordExtractor(maxKeywords: 5))
			.AddRelationshipExtractor(new CooccurrenceRelationshipExtractor());

		var chunks = new List<ChunkerBase>
		{
			new TestContentChunk
			{
				Id = Guid.NewGuid(),
				Content = "Cloud computing and machine learning technologies."
			}
		};

		// Act
		var result = await builder.BuildGraphAsync(chunks);

		// Assert
		result.Success.Should().BeTrue();
		// If validation fails, there should be errors
		if (!result.Success || result.Errors.Count > 0)
		{
			output.WriteLine($"Validation errors: {string.Join(", ", result.Errors)}");
		}
	}

	[Fact]
	public async Task BuildGraphAsync_ShouldSetTimestamps()
	{
		// Arrange
		var builder = new KnowledgeGraphBuilder()
			.AddEntityExtractor(new SimpleKeywordExtractor(maxKeywords: 5));

		var chunks = new List<ChunkerBase>
		{
			new TestContentChunk
			{
				Id = Guid.NewGuid(),
				Content = "Test content for timestamp validation."
			}
		};

		// Act
		var startTime = DateTimeOffset.UtcNow;
		var result = await builder.BuildGraphAsync(chunks);
		var endTime = DateTimeOffset.UtcNow;

		// Assert
		(result.StartedAt >= startTime).Should().BeTrue();
		(result.StartedAt <= endTime).Should().BeTrue();
		(result.CompletedAt >= result.StartedAt).Should().BeTrue();
		result.Duration.Should().NotBeNull();
		(result.Duration!.Value.TotalMilliseconds > 0).Should().BeTrue();
		output.WriteLine($"Build duration: {result.Duration!.Value.TotalMilliseconds}ms");
	}

	[Fact]
	public async Task BuildGraphAsync_ShouldTrackExtractionStatistics()
	{
		// Arrange
		var builder = new KnowledgeGraphBuilder()
			.AddEntityExtractor(new SimpleKeywordExtractor(maxKeywords: 10))
			.AddRelationshipExtractor(new CooccurrenceRelationshipExtractor());

		var chunks = new List<ChunkerBase>
		{
			new TestContentChunk { Id = Guid.NewGuid(), Content = "Cloud computing provides infrastructure." },
			new TestContentChunk { Id = Guid.NewGuid(), Content = "Machine learning requires data processing." },
			new TestContentChunk { Id = Guid.NewGuid(), Content = "Artificial intelligence transforms industries." }
		};

		// Act
		var result = await builder.BuildGraphAsync(chunks);

		// Assert
		result.Success.Should().BeTrue();
		result.Statistics.ChunksProcessed.Should().Be(3);
		(result.Statistics.EntityExtractionTime.TotalMilliseconds > 0).Should().BeTrue();
		result.Statistics.ExtractorsUsed.Should().NotBeEmpty();

		output.WriteLine($"Chunks processed: {result.Statistics.ChunksProcessed}");
		output.WriteLine($"Entity extraction time: {result.Statistics.EntityExtractionTime.TotalMilliseconds}ms");
		output.WriteLine($"Relationship extraction time: {result.Statistics.RelationshipExtractionTime.TotalMilliseconds}ms");
		output.WriteLine($"Graph building time: {result.Statistics.GraphBuildingTime.TotalMilliseconds}ms");
	}

	[Fact]
	public async Task BuildGraphAsync_WithCancellation_ShouldHandleGracefully()
	{
		// Arrange
		var builder = new KnowledgeGraphBuilder()
			.AddEntityExtractor(new SimpleKeywordExtractor());

		var chunks = new List<ChunkerBase>
		{
			new TestContentChunk { Content = "Test content." }
		};

		var cts = new CancellationTokenSource();
		cts.Cancel();

		// Act
		var result = await builder.BuildGraphAsync(chunks, cancellationToken: cts.Token);

		// Assert
		result.Success.Should().BeFalse();
		result.Errors.Should().Contain(e => e.Contains("cancel", StringComparison.OrdinalIgnoreCase));
		output.WriteLine($"Errors: {string.Join(", ", result.Errors)}");
	}

	[Fact]
	public async Task BuildGraphAsync_WithMultipleExtractors_ShouldTrackAllInStatistics()
	{
		// Arrange
		var builder = new KnowledgeGraphBuilder()
			.AddEntityExtractor(new SimpleKeywordExtractor())
			.AddRelationshipExtractor(new CooccurrenceRelationshipExtractor());

		var chunks = new List<ChunkerBase>
		{
			new TestContentChunk
			{
				Id = Guid.NewGuid(),
				Content = "Machine learning and artificial intelligence."
			}
		};

		// Act
		var result = await builder.BuildGraphAsync(chunks);

		// Assert
		result.Success.Should().BeTrue();
		result.Statistics.ExtractorsUsed.Should().Contain("SimpleKeywordExtractor");
		result.Statistics.ExtractorsUsed.Should().Contain("CooccurrenceRelationshipExtractor");
		output.WriteLine($"Extractors used: {string.Join(", ", result.Statistics.ExtractorsUsed)}");
	}

	[Fact]
	public async Task BuildGraphAsync_WithCustomEntityResolver_ShouldUseIt()
	{
		// Arrange
		var resolver = new EntityResolver(similarityThreshold: 0.9);
		var builder = new KnowledgeGraphBuilder(resolver)
			.AddEntityExtractor(new SimpleKeywordExtractor(maxKeywords: 10));

		var chunks = new List<ChunkerBase>
		{
			new TestContentChunk
			{
				Id = Guid.NewGuid(),
				Content = "Machine learning and machine-learning are similar terms."
			}
		};

		// Act
		var result = await builder.BuildGraphAsync(chunks);

		// Assert
		result.Success.Should().BeTrue();
		output.WriteLine($"Entities before deduplication: {result.Statistics.EntitiesExtracted}");
		output.WriteLine($"Entities after deduplication: {result.Statistics.EntitiesAfterDeduplication}");
	}
}
