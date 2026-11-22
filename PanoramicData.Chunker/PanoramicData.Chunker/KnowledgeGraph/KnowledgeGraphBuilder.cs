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
		string graphName,
		CancellationToken cancellationToken)
	{
		var result = new KnowledgeGraphResult
		{
			StartedAt = DateTimeOffset.UtcNow
		};

		try
		{
			await BuildCoreAsync(chunks, graphName, result, cancellationToken);
		}
		catch (OperationCanceledException)
		{
			Fail(result, "Graph building was cancelled.");
		}
		catch (Exception ex)
		{
			Fail(result, $"Error building graph: {ex.Message}");
		}

		return result;
	}

	private async Task BuildCoreAsync(
		IEnumerable<ChunkerBase> chunks,
		string graphName,
		KnowledgeGraphResult result,
		CancellationToken cancellationToken)
	{
		var chunkList = chunks.ToList();
		result.Statistics.ChunksProcessed = chunkList.Count;

		var graph = new Graph(graphName);

		if (chunkList.Count == 0)
		{
			CompleteEarly(result, graph, "No chunks provided for graph extraction.");
			return;
		}

		var allEntities = await ExtractEntitiesAsync(chunkList, result, cancellationToken);

		if (allEntities.Count == 0)
		{
			CompleteEarly(result, graph, "No entities extracted from chunks.");
			return;
		}

		var resolvedEntities = ResolveEntities(allEntities, graph, result);
		var allRelationships = await ExtractRelationshipsAsync(resolvedEntities, chunkList, result, cancellationToken);

		AddConsolidatedRelationships(allRelationships, graph, result);
		BuildIndexes(graph, result);

		foreach (var error in graph.Validate().Errors)
		{
			result.AddError(error);
		}

		result.Graph = graph;
		result.Success = true;
		result.Complete();
	}

	private static void CompleteEarly(KnowledgeGraphResult result, Graph graph, string warning)
	{
		result.AddWarning(warning);
		result.Success = true;
		result.Graph = graph;
		result.Complete();
	}

	private static void Fail(KnowledgeGraphResult result, string error)
	{
		result.AddError(error);
		result.Success = false;
		result.Complete();
	}

	private async Task<List<Entity>> ExtractEntitiesAsync(
		List<ChunkerBase> chunkList,
		KnowledgeGraphResult result,
		CancellationToken cancellationToken)
	{
		var stopwatch = Stopwatch.StartNew();
		var allEntities = new List<Entity>();

		foreach (var extractor in _entityExtractors)
		{
			cancellationToken.ThrowIfCancellationRequested();

			allEntities.AddRange(await extractor.ExtractEntitiesAsync(chunkList, cancellationToken));
			result.Statistics.ExtractorsUsed.Add(extractor.Name);
		}

		result.Statistics.EntitiesExtracted = allEntities.Count;
		stopwatch.Stop();
		result.Statistics.EntityExtractionTime = stopwatch.Elapsed;

		return allEntities;
	}

	private List<Entity> ResolveEntities(List<Entity> allEntities, Graph graph, KnowledgeGraphResult result)
	{
		var stopwatch = Stopwatch.StartNew();
		var resolvedEntities = _entityResolver.Resolve(allEntities);
		result.Statistics.EntitiesAfterDeduplication = resolvedEntities.Count;
		result.Statistics.EntitiesMerged = allEntities.Count - resolvedEntities.Count;
		stopwatch.Stop();
		result.Statistics.GraphBuildingTime += stopwatch.Elapsed;

		foreach (var entity in resolvedEntities)
		{
			graph.AddEntity(entity);
		}

		return resolvedEntities;
	}

	private async Task<List<Relationship>> ExtractRelationshipsAsync(
		List<Entity> resolvedEntities,
		List<ChunkerBase> chunkList,
		KnowledgeGraphResult result,
		CancellationToken cancellationToken)
	{
		var stopwatch = Stopwatch.StartNew();
		var allRelationships = new List<Relationship>();

		foreach (var extractor in _relationshipExtractors)
		{
			cancellationToken.ThrowIfCancellationRequested();

			if (!result.Statistics.ExtractorsUsed.Contains(extractor.Name))
			{
				result.Statistics.ExtractorsUsed.Add(extractor.Name);
			}

			allRelationships.AddRange(await extractor.ExtractRelationshipsAsync(resolvedEntities, chunkList, cancellationToken));
		}

		result.Statistics.RelationshipsExtracted = allRelationships.Count;
		stopwatch.Stop();
		result.Statistics.RelationshipExtractionTime = stopwatch.Elapsed;

		return allRelationships;
	}

	private static void AddConsolidatedRelationships(List<Relationship> allRelationships, Graph graph, KnowledgeGraphResult result)
	{
		var stopwatch = Stopwatch.StartNew();
		var consolidatedRelationships = ConsolidateRelationships(allRelationships);
		result.Statistics.RelationshipsAfterConsolidation = consolidatedRelationships.Count;
		result.Statistics.RelationshipsMerged = allRelationships.Count - consolidatedRelationships.Count;
		stopwatch.Stop();
		result.Statistics.GraphBuildingTime += stopwatch.Elapsed;

		foreach (var relationship in consolidatedRelationships)
		{
			graph.AddRelationship(relationship);
		}
	}

	private static void BuildIndexes(Graph graph, KnowledgeGraphResult result)
	{
		var stopwatch = Stopwatch.StartNew();
		graph.BuildIndexes();
		graph.ComputeStatistics();
		stopwatch.Stop();
		result.Statistics.GraphBuildingTime += stopwatch.Elapsed;
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
