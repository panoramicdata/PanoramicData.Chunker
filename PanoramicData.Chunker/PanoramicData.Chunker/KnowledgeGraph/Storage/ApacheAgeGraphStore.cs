using Microsoft.Extensions.Logging;
using PanoramicData.Chunker.Interfaces.KnowledgeGraph;
using PanoramicData.Chunker.Models.KnowledgeGraph;
using System.Text.Json;

namespace PanoramicData.Chunker.KnowledgeGraph.Storage;

/// <summary>
/// Apache AGE native implementation of IGraphStore using Cypher queries.
/// This is a simplified initial implementation focusing on basic graph operations.
/// </summary>
public partial class ApacheAgeGraphStore(
	string connectionString,
	ILogger<ApacheAgeGraphStore> logger) : IGraphStore
{
	private readonly string _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
	private readonly ILogger<ApacheAgeGraphStore> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

	public async Task SaveGraphAsync(Graph graph, CancellationToken cancellationToken)
	{
		ArgumentNullException.ThrowIfNull(graph);

		_logger.LogInformation("Saving graph '{GraphName}' ({GraphId}) with {EntityCount} entities to Apache AGE",
			graph.Name, graph.Id, graph.Entities.Count);

		await WithConnectionAsync(
			async connection =>
			{
				// For now, save to a metadata table (simple approach)
				await CreateMetadataTableIfNotExistsAsync(connection, cancellationToken);
				await SaveGraphMetadataAsync(connection, graph, cancellationToken);

				foreach (var entity in graph.Entities)
				{
					await SaveEntityAsync(graph.Id, entity, cancellationToken);
				}

				foreach (var relationship in graph.Relationships)
				{
					await SaveRelationshipAsync(graph.Id, relationship, cancellationToken);
				}

				_logger.LogInformation("Successfully saved graph '{GraphName}' to Apache AGE", graph.Name);
			},
			ex => _logger.LogError(ex, "Error saving graph '{GraphName}' to Apache AGE", graph.Name),
			cancellationToken);
	}

	public async Task<Graph?> LoadGraphAsync(Guid graphId, CancellationToken cancellationToken)
	{
		_logger.LogInformation("Loading graph {GraphId} from Apache AGE", graphId);

		return await WithConnectionAsync<Graph?>(
			async connection =>
			{
				var graph = await LoadGraphMetadataAsync(connection, graphId, cancellationToken);
				if (graph == null)
				{
					return null;
				}

				foreach (var entity in await LoadEntitiesAsync(connection, graphId, cancellationToken))
				{
					graph.AddEntity(entity);
				}

				foreach (var relationship in await LoadRelationshipsAsync(connection, graphId, cancellationToken))
				{
					graph.AddRelationship(relationship);
				}

				graph.BuildIndexes();

				_logger.LogInformation("Loaded graph '{GraphName}' with {EntityCount} entities",
					graph.Name, graph.Entities.Count);

				return graph;
			},
			ex => _logger.LogError(ex, "Error loading graph {GraphId}", graphId),
			cancellationToken);
	}

	public async Task<Graph?> LoadGraphByNameAsync(string name, CancellationToken cancellationToken)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(name);

		_logger.LogInformation("Loading graph by name '{GraphName}'", name);

		return await WithConnectionAsync<Graph?>(
			async connection =>
			{
				await using var cmd = CreateCommand(connection, "SELECT id FROM age_graph_metadata WHERE name = @name", ("name", name));
				await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
				if (await reader.ReadAsync(cancellationToken))
				{
					var graphId = reader.GetGuid(0);
					await reader.CloseAsync();
					return await LoadGraphAsync(graphId, cancellationToken);
				}

				_logger.LogWarning("Graph '{GraphName}' not found", name);
				return null;
			},
			ex => _logger.LogError(ex, "Error loading graph by name '{GraphName}'", name),
			cancellationToken);
	}

	public async Task DeleteGraphAsync(Guid graphId, CancellationToken cancellationToken)
	{
		_logger.LogInformation("Deleting graph {GraphId}", graphId);

		await WithConnectionAsync(
			async connection =>
			{
				// Relationships first (FK constraints), then entities, then the graph itself
				await ExecuteAsync(connection, "DELETE FROM age_relationships WHERE graph_id = @graphId", cancellationToken, ("graphId", graphId));
				await ExecuteAsync(connection, "DELETE FROM age_entities WHERE graph_id = @graphId", cancellationToken, ("graphId", graphId));

				var rowsAffected = await ExecuteAsync(connection, "DELETE FROM age_graph_metadata WHERE id = @graphId", cancellationToken, ("graphId", graphId));
				if (rowsAffected == 0)
				{
					_logger.LogWarning("Graph {GraphId} not found for deletion", graphId);
				}

				_logger.LogInformation("Successfully deleted graph {GraphId}", graphId);
			},
			ex => _logger.LogError(ex, "Error deleting graph {GraphId}", graphId),
			cancellationToken);
	}

	public async Task<bool> GraphExistsAsync(Guid graphId, CancellationToken cancellationToken)
		=> await WithConnectionAsync(
			async connection =>
			{
				await using var cmd = CreateCommand(connection, "SELECT EXISTS(SELECT 1 FROM age_graph_metadata WHERE id = @graphId)", ("graphId", graphId));
				var result = await cmd.ExecuteScalarAsync(cancellationToken);
				return result is bool exists && exists;
			},
			ex => _logger.LogError(ex, "Error checking if graph {GraphId} exists", graphId),
			cancellationToken);

	public async Task<List<GraphMetadata>> ListGraphsAsync(CancellationToken cancellationToken)
	{
		_logger.LogInformation("Listing all graphs");

		return await WithConnectionAsync(
			async connection =>
			{
				await using var cmd = CreateCommand(connection, "SELECT metadata FROM age_graph_metadata");
				var all = await ReadAllAsync(cmd, reader => JsonSerializer.Deserialize<GraphMetadata>(reader.GetString(0)), cancellationToken);
				var graphs = all.OfType<GraphMetadata>().ToList();

				_logger.LogInformation("Found {GraphCount} graphs", graphs.Count);
				return graphs;
			},
			ex => _logger.LogError(ex, "Error listing graphs"),
			cancellationToken);
	}

	public async Task SaveEntityAsync(Guid graphId, Entity entity, CancellationToken cancellationToken)
	{
		await using var connection = await OpenInitializedAsync(cancellationToken);

		// Use simple INSERT for now - will enhance to use Cypher MERGE later
		var sql = @"
			INSERT INTO age_entities (id, graph_id, name, type, confidence, frequency, properties)
			VALUES (@id, @graphId, @name, @type, @confidence, @frequency, @properties::jsonb)
			ON CONFLICT (id) DO UPDATE SET
				name = EXCLUDED.name,
				confidence = EXCLUDED.confidence,
				frequency = EXCLUDED.frequency,
				properties = EXCLUDED.properties";

		await ExecuteAsync(
			connection,
			sql,
			cancellationToken,
			("id", entity.Id),
			("graphId", graphId),
			("name", entity.Name),
			("type", entity.Type.ToString()),
			("confidence", entity.Confidence),
			("frequency", entity.Frequency),
			("properties", JsonSerializer.Serialize(entity.Properties)));
	}

	public async Task SaveRelationshipAsync(Guid graphId, Relationship relationship, CancellationToken cancellationToken)
	{
		await using var connection = await OpenInitializedAsync(cancellationToken);

		var sql = @"
			INSERT INTO age_relationships (id, graph_id, from_entity_id, to_entity_id, type, weight, confidence, properties)
			VALUES (@id, @graphId, @fromId, @toId, @type, @weight, @confidence, @properties::jsonb)
			ON CONFLICT (id) DO UPDATE SET
				weight = EXCLUDED.weight,
				confidence = EXCLUDED.confidence,
				properties = EXCLUDED.properties";

		await ExecuteAsync(
			connection,
			sql,
			cancellationToken,
			("id", relationship.Id),
			("graphId", graphId),
			("fromId", relationship.FromEntityId),
			("toId", relationship.ToEntityId),
			("type", relationship.Type.ToString()),
			("weight", relationship.Weight),
			("confidence", relationship.Confidence),
			("properties", JsonSerializer.Serialize(relationship.Properties)));
	}

	public async Task<List<Entity>> QueryEntitiesByTypeAsync(Guid graphId, EntityType entityType, CancellationToken cancellationToken)
	{
		_logger.LogInformation("Querying entities of type {EntityType} in graph {GraphId}", entityType, graphId);

		return await WithConnectionAsync(
			async connection =>
			{
				await using var cmd = CreateCommand(
					connection,
					$"{SelectEntitiesSql} AND type = @type",
					("graphId", graphId),
					("type", entityType.ToString()));
				var entities = await ReadAllAsync(cmd, ReadEntity, cancellationToken);

				_logger.LogInformation("Found {EntityCount} entities of type {EntityType}", entities.Count, entityType);
				return entities;
			},
			ex => _logger.LogError(ex, "Error querying entities by type {EntityType} in graph {GraphId}", entityType, graphId),
			cancellationToken);
	}

	public async Task<List<Relationship>> QueryRelationshipsByTypeAsync(Guid graphId, RelationshipType relationshipType, CancellationToken cancellationToken)
	{
		_logger.LogInformation("Querying relationships of type {RelationshipType} in graph {GraphId}", relationshipType, graphId);

		return await WithConnectionAsync(
			async connection =>
			{
				await using var cmd = CreateCommand(
					connection,
					$"{SelectRelationshipsSql} AND type = @type",
					("graphId", graphId),
					("type", relationshipType.ToString()));
				var relationships = await ReadAllAsync(cmd, ReadRelationship, cancellationToken);

				_logger.LogInformation("Found {RelationshipCount} relationships of type {RelationshipType}", relationships.Count, relationshipType);
				return relationships;
			},
			ex => _logger.LogError(ex, "Error querying relationships by type {RelationshipType} in graph {GraphId}", relationshipType, graphId),
			cancellationToken);
	}
}
