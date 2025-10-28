using PanoramicData.Chunker.Interfaces.KnowledgeGraph;
using PanoramicData.Chunker.Models;
using PanoramicData.Chunker.Models.KnowledgeGraph;
using System.Diagnostics;

namespace PanoramicData.Chunker.KnowledgeGraph;

/// <summary>
/// Builds a knowledge graph from document chunks using entity and relationship extractors.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="KnowledgeGraphBuilder"/> class.
/// </remarks>
/// <param name="entityResolver">The entity resolver for deduplication.</param>
public class KnowledgeGraphBuilder(IEntityResolver? entityResolver = null)
{
	private readonly List<IEntityExtractor> _entityExtractors = [];
	private readonly List<IRelationshipExtractor> _relationshipExtractors = [];
	private readonly IEntityResolver _entityResolver = entityResolver ?? new EntityResolver();

	/// <summary>
	/// Adds an entity extractor to the builder.
	/// </summary>
	/// <param name="extractor">The entity extractor to add.</param>
	/// <returns>This builder for method chaining.</returns>
	public KnowledgeGraphBuilder AddEntityExtractor(IEntityExtractor extractor)
	{
		_entityExtractors.Add(extractor);
		return this;
	}

	/// <summary>
	/// Adds a relationship extractor to the builder.
	/// </summary>
	/// <param name="extractor">The relationship extractor to add.</param>
	/// <returns>This builder for method chaining.</returns>
	public KnowledgeGraphBuilder AddRelationshipExtractor(IRelationshipExtractor extractor)
	{
		_relationshipExtractors.Add(extractor);
		return this;
	}

	/// <summary>
	/// Builds a knowledge graph from the given chunks.
	/// </summary>
	/// <param name="chunks">The chunks to process.</param>
	/// <param name="graphName">Optional name for the graph.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>The knowledge graph result.</returns>
	public async Task<KnowledgeGraphResult> BuildGraphAsync(
		IEnumerable<ChunkerBase> chunks,
		string graphName = "Document Graph",
		CancellationToken cancellationToken = default)
	{
		var result = new KnowledgeGraphResult
		{
			StartedAt = DateTimeOffset.UtcNow
		};

		try
		{
			var chunkList = chunks.ToList();
			result.Statistics.ChunksProcessed = chunkList.Count;

			if (chunkList.Count == 0)
			{
				result.AddWarning("No chunks provided for graph extraction.");
				result.Success = true;
				result.Graph = new Graph(graphName);
				result.Complete();
				return result;
			}

			var graph = new Graph(graphName);

			// Step 1: Extract entities
			var entityStopwatch = Stopwatch.StartNew();
			var allEntities = new List<Entity>();

			foreach (var extractor in _entityExtractors)
			{
				cancellationToken.ThrowIfCancellationRequested();

				var entities = await extractor.ExtractEntitiesAsync(chunkList, cancellationToken);
				allEntities.AddRange(entities);
				result.Statistics.ExtractorsUsed.Add(extractor.Name);
			}

			result.Statistics.EntitiesExtracted = allEntities.Count;
			entityStopwatch.Stop();
			result.Statistics.EntityExtractionTime = entityStopwatch.Elapsed;

			if (allEntities.Count == 0)
			{
				result.AddWarning("No entities extracted from chunks.");
				result.Success = true;
				result.Graph = graph;
				result.Complete();
				return result;
			}

			// Step 2: Resolve and deduplicate entities
			var resolveStopwatch = Stopwatch.StartNew();
			var resolvedEntities = _entityResolver.Resolve(allEntities);
			result.Statistics.EntitiesAfterDeduplication = resolvedEntities.Count;
			result.Statistics.EntitiesMerged = allEntities.Count - resolvedEntities.Count;
			resolveStopwatch.Stop();
			result.Statistics.GraphBuildingTime += resolveStopwatch.Elapsed;

			// Add entities to graph
			foreach (var entity in resolvedEntities)
			{
				graph.AddEntity(entity);
			}

			// Step 3: Extract relationships
			var relationshipStopwatch = Stopwatch.StartNew();
			var allRelationships = new List<Relationship>();

			foreach (var extractor in _relationshipExtractors)
			{
				cancellationToken.ThrowIfCancellationRequested();

				var relationships = await extractor.ExtractRelationshipsAsync(
					resolvedEntities,
					chunkList,
					cancellationToken);

				allRelationships.AddRange(relationships);

				if (!result.Statistics.ExtractorsUsed.Contains(extractor.Name))
				{
					result.Statistics.ExtractorsUsed.Add(extractor.Name);
				}
			}

			result.Statistics.RelationshipsExtracted = allRelationships.Count;
			relationshipStopwatch.Stop();
			result.Statistics.RelationshipExtractionTime = relationshipStopwatch.Elapsed;

			// Step 4: Consolidate relationships
			var consolidateStopwatch = Stopwatch.StartNew();
			var consolidatedRelationships = ConsolidateRelationships(allRelationships);
			result.Statistics.RelationshipsAfterConsolidation = consolidatedRelationships.Count;
			result.Statistics.RelationshipsMerged = allRelationships.Count - consolidatedRelationships.Count;
			consolidateStopwatch.Stop();
			result.Statistics.GraphBuildingTime += consolidateStopwatch.Elapsed;

			// Add relationships to graph
			foreach (var relationship in consolidatedRelationships)
			{
				graph.AddRelationship(relationship);
			}

			// Step 5: Build indexes and compute statistics
			var indexStopwatch = Stopwatch.StartNew();
			graph.BuildIndexes();
			graph.ComputeStatistics();
			indexStopwatch.Stop();
			result.Statistics.GraphBuildingTime += indexStopwatch.Elapsed;

			// Step 6: Validate graph
			if (!graph.Validate(out var errors))
			{
				foreach (var error in errors)
				{
					result.AddError(error);
				}
			}

			result.Graph = graph;
			result.Success = true;
			result.Complete();
		}
		catch (OperationCanceledException)
		{
			result.AddError("Graph building was cancelled.");
			result.Success = false;
			result.Complete();
		}
		catch (Exception ex)
		{
			result.AddError($"Error building graph: {ex.Message}");
			result.Success = false;
			result.Complete();
		}

		return result;
	}

	private static List<Relationship> ConsolidateRelationships(List<Relationship> relationships)
	{
		if (relationships.Count == 0)
		{
			return [];
		}

		var consolidated = new Dictionary<string, Relationship>();

		foreach (var relationship in relationships)
		{
			var key = GetRelationshipKey(relationship);

			if (!consolidated.TryGetValue(key, out var existing))
			{
				consolidated[key] = relationship;
			}
			else
			{
				// Merge relationships
				existing.Merge(relationship);
			}
		}

		return [.. consolidated.Values];
	}

	private static string GetRelationshipKey(Relationship relationship)
	{
		// Create a consistent key for relationships
		if (relationship.Bidirectional)
		{
			// Order IDs consistently for bidirectional relationships
			var ids = new[] { relationship.FromEntityId, relationship.ToEntityId }
				.OrderBy(id => id)
				.ToArray();
			return $"{ids[0]}_{ids[1]}_{relationship.Type}";
		}
		else
		{
			// Directional relationships maintain order
			return $"{relationship.FromEntityId}_{relationship.ToEntityId}_{relationship.Type}";
		}
	}
}
