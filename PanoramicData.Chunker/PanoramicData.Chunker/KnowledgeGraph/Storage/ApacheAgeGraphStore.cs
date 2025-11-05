using Microsoft.Extensions.Logging;
using Npgsql;
using PanoramicData.Chunker.Interfaces.KnowledgeGraph;
using PanoramicData.Chunker.Models.KnowledgeGraph;
using System.Text.Json;

namespace PanoramicData.Chunker.KnowledgeGraph.Storage;

/// <summary>
/// Apache AGE native implementation of IGraphStore using Cypher queries.
/// This is a simplified initial implementation focusing on basic graph operations.
/// </summary>
public class ApacheAgeGraphStore(
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

		await using var connection = new NpgsqlConnection(_connectionString);
		await connection.OpenAsync(cancellationToken);

		try
		{
			// Initialize AGE
			await InitializeAgeAsync(connection, cancellationToken);

			// For now, save to a metadata table (simple approach)
			// We'll enhance this to use pure Cypher later
			await CreateMetadataTableIfNotExistsAsync(connection, cancellationToken);
			await SaveGraphMetadataAsync(connection, graph, cancellationToken);

			// Save entities
			foreach (var entity in graph.Entities)
			{
				await SaveEntityAsync(graph.Id, entity, cancellationToken);
			}

			// Save relationships
			foreach (var relationship in graph.Relationships)
			{
				await SaveRelationshipAsync(graph.Id, relationship, cancellationToken);
			}

			_logger.LogInformation("Successfully saved graph '{GraphName}' to Apache AGE", graph.Name);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error saving graph '{GraphName}' to Apache AGE", graph.Name);
			throw;
		}
	}

	public async Task<Graph?> LoadGraphAsync(Guid graphId, CancellationToken cancellationToken)
	{
		_logger.LogInformation("Loading graph {GraphId} from Apache AGE", graphId);

		await using var connection = new NpgsqlConnection(_connectionString);
		await connection.OpenAsync(cancellationToken);

		try
		{
			await InitializeAgeAsync(connection, cancellationToken);

			// Load graph metadata
			var graph = await LoadGraphMetadataAsync(connection, graphId, cancellationToken);
			if (graph == null)
			{
				return null;
			}

			// Load entities (simplified - from regular table for now)
			var entities = await LoadEntitiesAsync(connection, graphId, cancellationToken);
			foreach (var entity in entities)
			{
				graph.AddEntity(entity);
			}

			// Load relationships
			var relationships = await LoadRelationshipsAsync(connection, graphId, cancellationToken);
			foreach (var rel in relationships)
			{
				graph.AddRelationship(rel);
			}

			graph.BuildIndexes();

			_logger.LogInformation("Loaded graph '{GraphName}' with {EntityCount} entities",
				graph.Name, graph.Entities.Count);

			return graph;
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error loading graph {GraphId}", graphId);
			throw;
		}
	}

	public async Task<Graph?> LoadGraphByNameAsync(string name, CancellationToken cancellationToken)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(name);

		_logger.LogInformation("Loading graph by name '{GraphName}'", name);

		await using var connection = new NpgsqlConnection(_connectionString);
		await connection.OpenAsync(cancellationToken);

		try
		{
			await InitializeAgeAsync(connection, cancellationToken);

			// Find graph by name
			var sql = "SELECT id FROM age_graph_metadata WHERE name = @name";
			await using var cmd = new NpgsqlCommand(sql, connection);
			cmd.Parameters.AddWithValue("name", name);

			await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
			if (await reader.ReadAsync(cancellationToken))
			{
				var graphId = reader.GetGuid(0);
				await reader.CloseAsync();
				return await LoadGraphAsync(graphId, cancellationToken);
			}

			_logger.LogWarning("Graph '{GraphName}' not found", name);
			return null;
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error loading graph by name '{GraphName}'", name);
			throw;
		}
	}

	public async Task DeleteGraphAsync(Guid graphId, CancellationToken cancellationToken)
	{
		_logger.LogInformation("Deleting graph {GraphId}", graphId);

		await using var connection = new NpgsqlConnection(_connectionString);
		await connection.OpenAsync(cancellationToken);

		try
		{
			await InitializeAgeAsync(connection, cancellationToken);

			// Delete relationships first (FK constraints)
			var deleteRelsSql = "DELETE FROM age_relationships WHERE graph_id = @graphId";
			await using (var cmd = new NpgsqlCommand(deleteRelsSql, connection))
			{
				cmd.Parameters.AddWithValue("graphId", graphId);
				await cmd.ExecuteNonQueryAsync(cancellationToken);
			}

			// Delete entities
			var deleteEntitiesSql = "DELETE FROM age_entities WHERE graph_id = @graphId";
			await using (var cmd = new NpgsqlCommand(deleteEntitiesSql, connection))
			{
				cmd.Parameters.AddWithValue("graphId", graphId);
				await cmd.ExecuteNonQueryAsync(cancellationToken);
			}

			// Delete graph metadata
			var deleteGraphSql = "DELETE FROM age_graph_metadata WHERE id = @graphId";
			await using (var cmd = new NpgsqlCommand(deleteGraphSql, connection))
			{
				cmd.Parameters.AddWithValue("graphId", graphId);
				var rowsAffected = await cmd.ExecuteNonQueryAsync(cancellationToken);

				if (rowsAffected == 0)
				{
					_logger.LogWarning("Graph {GraphId} not found for deletion", graphId);
				}
			}

			_logger.LogInformation("Successfully deleted graph {GraphId}", graphId);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error deleting graph {GraphId}", graphId);
			throw;
		}
	}

	public async Task<bool> GraphExistsAsync(Guid graphId, CancellationToken cancellationToken)
	{
		await using var connection = new NpgsqlConnection(_connectionString);
		await connection.OpenAsync(cancellationToken);

		try
		{
			await InitializeAgeAsync(connection, cancellationToken);

			var sql = "SELECT EXISTS(SELECT 1 FROM age_graph_metadata WHERE id = @graphId)";
			await using var cmd = new NpgsqlCommand(sql, connection);
			cmd.Parameters.AddWithValue("graphId", graphId);

			var result = await cmd.ExecuteScalarAsync(cancellationToken);
			return result is bool exists && exists;
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error checking if graph {GraphId} exists", graphId);
			throw;
		}
	}

	public async Task<List<GraphMetadata>> ListGraphsAsync(CancellationToken cancellationToken)
	{
		_logger.LogInformation("Listing all graphs");

		await using var connection = new NpgsqlConnection(_connectionString);
		await connection.OpenAsync(cancellationToken);

		try
		{
			await InitializeAgeAsync(connection, cancellationToken);

			var sql = "SELECT metadata FROM age_graph_metadata";
			await using var cmd = new NpgsqlCommand(sql, connection);

			var graphs = new List<GraphMetadata>();
			await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
			while (await reader.ReadAsync(cancellationToken))
			{
				var metadataJson = reader.GetString(0);
				var metadata = JsonSerializer.Deserialize<GraphMetadata>(metadataJson);
				if (metadata != null)
				{
					graphs.Add(metadata);
				}
			}

			_logger.LogInformation("Found {GraphCount} graphs", graphs.Count);
			return graphs;
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error listing graphs");
			throw;
		}
	}

	public async Task SaveEntityAsync(Guid graphId, Entity entity, CancellationToken cancellationToken)
	{
		await using var connection = new NpgsqlConnection(_connectionString);
		await connection.OpenAsync(cancellationToken);
		await InitializeAgeAsync(connection, cancellationToken);

		// Use simple INSERT for now - will enhance to use Cypher MERGE later
		var sql = @"
			INSERT INTO age_entities (id, graph_id, name, type, confidence, frequency, properties)
			VALUES (@id, @graphId, @name, @type, @confidence, @frequency, @properties::jsonb)
			ON CONFLICT (id) DO UPDATE SET
				name = EXCLUDED.name,
				confidence = EXCLUDED.confidence,
				frequency = EXCLUDED.frequency,
				properties = EXCLUDED.properties";

		await using var cmd = new NpgsqlCommand(sql, connection);
		cmd.Parameters.AddWithValue("id", entity.Id);
		cmd.Parameters.AddWithValue("graphId", graphId);
		cmd.Parameters.AddWithValue("name", entity.Name);
		cmd.Parameters.AddWithValue("type", entity.Type.ToString());
		cmd.Parameters.AddWithValue("confidence", entity.Confidence);
		cmd.Parameters.AddWithValue("frequency", entity.Frequency);
		cmd.Parameters.AddWithValue("properties", JsonSerializer.Serialize(entity.Properties));

		await cmd.ExecuteNonQueryAsync(cancellationToken);
	}

	public async Task SaveRelationshipAsync(Guid graphId, Relationship relationship, CancellationToken cancellationToken)
	{
		await using var connection = new NpgsqlConnection(_connectionString);
		await connection.OpenAsync(cancellationToken);
		await InitializeAgeAsync(connection, cancellationToken);

		var sql = @"
			INSERT INTO age_relationships (id, graph_id, from_entity_id, to_entity_id, type, weight, confidence, properties)
			VALUES (@id, @graphId, @fromId, @toId, @type, @weight, @confidence, @properties::jsonb)
			ON CONFLICT (id) DO UPDATE SET
				weight = EXCLUDED.weight,
				confidence = EXCLUDED.confidence,
				properties = EXCLUDED.properties";

		await using var cmd = new NpgsqlCommand(sql, connection);
		cmd.Parameters.AddWithValue("id", relationship.Id);
		cmd.Parameters.AddWithValue("graphId", graphId);
		cmd.Parameters.AddWithValue("fromId", relationship.FromEntityId);
		cmd.Parameters.AddWithValue("toId", relationship.ToEntityId);
		cmd.Parameters.AddWithValue("type", relationship.Type.ToString());
		cmd.Parameters.AddWithValue("weight", relationship.Weight);
		cmd.Parameters.AddWithValue("confidence", relationship.Confidence);
		cmd.Parameters.AddWithValue("properties", JsonSerializer.Serialize(relationship.Properties));

		await cmd.ExecuteNonQueryAsync(cancellationToken);
	}

	public async Task<List<Entity>> QueryEntitiesByTypeAsync(Guid graphId, EntityType entityType, CancellationToken cancellationToken)
	{
		_logger.LogInformation("Querying entities of type {EntityType} in graph {GraphId}", entityType, graphId);

		await using var connection = new NpgsqlConnection(_connectionString);
		await connection.OpenAsync(cancellationToken);

		try
		{
			await InitializeAgeAsync(connection, cancellationToken);

			var sql = "SELECT id, name, type, confidence, frequency, properties FROM age_entities WHERE graph_id = @graphId AND type = @type";
			await using var cmd = new NpgsqlCommand(sql, connection);
			cmd.Parameters.AddWithValue("graphId", graphId);
			cmd.Parameters.AddWithValue("type", entityType.ToString());

			var entities = new List<Entity>();
			await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
			while (await reader.ReadAsync(cancellationToken))
			{
				var entity = new Entity(
					Enum.Parse<EntityType>(reader.GetString(2)),
					reader.GetString(1),
					reader.GetDouble(3))
				{
					Id = reader.GetGuid(0),
					Frequency = reader.GetInt32(4)
				};

				if (!reader.IsDBNull(5))
				{
					var propsJson = reader.GetString(5);
					var props = JsonSerializer.Deserialize<Dictionary<string, object>>(propsJson);
					if (props != null)
					{
						foreach (var kvp in props)
						{
							entity.Properties[kvp.Key] = kvp.Value;
						}
					}
				}

				entities.Add(entity);
			}

			_logger.LogInformation("Found {EntityCount} entities of type {EntityType}", entities.Count, entityType);
			return entities;
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error querying entities by type {EntityType} in graph {GraphId}", entityType, graphId);
			throw;
		}
	}

	public async Task<List<Relationship>> QueryRelationshipsByTypeAsync(Guid graphId, RelationshipType relationshipType, CancellationToken cancellationToken)
	{
		_logger.LogInformation("Querying relationships of type {RelationshipType} in graph {GraphId}", relationshipType, graphId);

		await using var connection = new NpgsqlConnection(_connectionString);
		await connection.OpenAsync(cancellationToken);

		try
		{
			await InitializeAgeAsync(connection, cancellationToken);

			var sql = "SELECT id, from_entity_id, to_entity_id, type, weight, confidence, properties FROM age_relationships WHERE graph_id = @graphId AND type = @type";
			await using var cmd = new NpgsqlCommand(sql, connection);
			cmd.Parameters.AddWithValue("graphId", graphId);
			cmd.Parameters.AddWithValue("type", relationshipType.ToString());

			var relationships = new List<Relationship>();
			await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
			while (await reader.ReadAsync(cancellationToken))
			{
				var relationship = new Relationship(
					reader.GetGuid(1),
					reader.GetGuid(2),
					Enum.Parse<RelationshipType>(reader.GetString(3)),
					reader.GetDouble(4),
					reader.GetDouble(5))
				{
					Id = reader.GetGuid(0)
				};

				if (!reader.IsDBNull(6))
				{
					var propsJson = reader.GetString(6);
					var props = JsonSerializer.Deserialize<Dictionary<string, object>>(propsJson);
					if (props != null)
					{
						foreach (var kvp in props)
						{
							relationship.Properties[kvp.Key] = kvp.Value;
						}
					}
				}

				relationships.Add(relationship);
			}

			_logger.LogInformation("Found {RelationshipCount} relationships of type {RelationshipType}", relationships.Count, relationshipType);
			return relationships;
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error querying relationships by type {RelationshipType} in graph {GraphId}", relationshipType, graphId);
			throw;
		}
	}

	// Helper methods

	private async Task InitializeAgeAsync(NpgsqlConnection connection, CancellationToken cancellationToken)
	{
		try
		{
			await ExecuteSqlAsync(connection, "LOAD 'age';", cancellationToken);
			await ExecuteSqlAsync(connection, "SET search_path = ag_catalog, public;", cancellationToken);
		}
		catch (Exception ex)
		{
			_logger.LogWarning(ex, "Could not initialize Apache AGE - it may not be installed");
		}
	}

	private static async Task CreateMetadataTableIfNotExistsAsync(NpgsqlConnection connection, CancellationToken cancellationToken)
	{
		var sql = @"
			CREATE TABLE IF NOT EXISTS age_graph_metadata (
				id UUID PRIMARY KEY,
				name TEXT NOT NULL,
				description TEXT,
				version TEXT,
				created_at TIMESTAMPTZ NOT NULL,
				metadata JSONB
			);

			CREATE TABLE IF NOT EXISTS age_entities (
				id UUID PRIMARY KEY,
				graph_id UUID NOT NULL,
				name TEXT NOT NULL,
				type TEXT NOT NULL,
				confidence DOUBLE PRECISION NOT NULL,
				frequency INTEGER NOT NULL,
				properties JSONB
			);

			CREATE TABLE IF NOT EXISTS age_relationships (
				id UUID PRIMARY KEY,
				graph_id UUID NOT NULL,
				from_entity_id UUID NOT NULL,
				to_entity_id UUID NOT NULL,
				type TEXT NOT NULL,
				weight DOUBLE PRECISION NOT NULL,
				confidence DOUBLE PRECISION NOT NULL,
				properties JSONB
			);

			CREATE INDEX IF NOT EXISTS idx_age_entities_graph ON age_entities(graph_id);
			CREATE INDEX IF NOT EXISTS idx_age_entities_name ON age_entities(name);
			CREATE INDEX IF NOT EXISTS idx_age_relationships_graph ON age_relationships(graph_id);
			CREATE INDEX IF NOT EXISTS idx_age_relationships_from ON age_relationships(from_entity_id);
			CREATE INDEX IF NOT EXISTS idx_age_relationships_to ON age_relationships(to_entity_id);
		";

		await ExecuteSqlAsync(connection, sql, cancellationToken);
	}

	private static async Task SaveGraphMetadataAsync(NpgsqlConnection connection, Graph graph, CancellationToken cancellationToken)
	{
		var sql = @"
			INSERT INTO age_graph_metadata (id, name, description, version, created_at, metadata)
			VALUES (@id, @name, @description, @version, @createdAt, @metadata::jsonb)
			ON CONFLICT (id) DO UPDATE SET
				name = EXCLUDED.name,
				description = EXCLUDED.description,
				version = EXCLUDED.version,
				metadata = EXCLUDED.metadata";

		await using var cmd = new NpgsqlCommand(sql, connection);
		cmd.Parameters.AddWithValue("id", graph.Id);
		cmd.Parameters.AddWithValue("name", graph.Name);
		cmd.Parameters.AddWithValue("description", graph.Metadata.Description ?? (object)DBNull.Value);
		cmd.Parameters.AddWithValue("version", graph.Metadata.Version ?? (object)DBNull.Value);
		cmd.Parameters.AddWithValue("createdAt", graph.Metadata.CreatedAt);
		cmd.Parameters.AddWithValue("metadata", JsonSerializer.Serialize(graph.Metadata));

		await cmd.ExecuteNonQueryAsync(cancellationToken);
	}

	private static async Task<Graph?> LoadGraphMetadataAsync(NpgsqlConnection connection, Guid graphId, CancellationToken cancellationToken)
	{
		var sql = "SELECT id, name, description, version, created_at, metadata FROM age_graph_metadata WHERE id = @id";

		await using var cmd = new NpgsqlCommand(sql, connection);
		cmd.Parameters.AddWithValue("id", graphId);

		await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
		if (await reader.ReadAsync(cancellationToken))
		{
			var name = reader.GetString(1);
			var metadataJson = reader.GetString(5);
			var metadata = JsonSerializer.Deserialize<GraphMetadata>(metadataJson) ?? new GraphMetadata();

			return new Graph(name)
			{
				Id = graphId,
				Metadata = metadata
			};
		}

		return null;
	}

	private static async Task<List<Entity>> LoadEntitiesAsync(NpgsqlConnection connection, Guid graphId, CancellationToken cancellationToken)
	{
		var entities = new List<Entity>();
		var sql = "SELECT id, name, type, confidence, frequency, properties FROM age_entities WHERE graph_id = @graphId";

		await using var cmd = new NpgsqlCommand(sql, connection);
		cmd.Parameters.AddWithValue("graphId", graphId);

		await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
		while (await reader.ReadAsync(cancellationToken))
		{
			var entity = new Entity(
				Enum.Parse<EntityType>(reader.GetString(2)),
				reader.GetString(1),
				reader.GetDouble(3))
			{
				Id = reader.GetGuid(0),
				Frequency = reader.GetInt32(4)
			};

			if (!reader.IsDBNull(5))
			{
				var propsJson = reader.GetString(5);
				var props = JsonSerializer.Deserialize<Dictionary<string, object>>(propsJson);
				if (props != null)
				{
					foreach (var kvp in props)
					{
						entity.Properties[kvp.Key] = kvp.Value;
					}
				}
			}

			entities.Add(entity);
		}

		return entities;
	}

	private static async Task<List<Relationship>> LoadRelationshipsAsync(NpgsqlConnection connection, Guid graphId, CancellationToken cancellationToken)
	{
		var relationships = new List<Relationship>();
		var sql = "SELECT id, from_entity_id, to_entity_id, type, weight, confidence, properties FROM age_relationships WHERE graph_id = @graphId";

		await using var cmd = new NpgsqlCommand(sql, connection);
		cmd.Parameters.AddWithValue("graphId", graphId);

		await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
		while (await reader.ReadAsync(cancellationToken))
		{
			var relationship = new Relationship(
				reader.GetGuid(1),
				reader.GetGuid(2),
				Enum.Parse<RelationshipType>(reader.GetString(3)),
				reader.GetDouble(4),
				reader.GetDouble(5))
			{
				Id = reader.GetGuid(0)
			};

			if (!reader.IsDBNull(6))
			{
				var propsJson = reader.GetString(6);
				var props = JsonSerializer.Deserialize<Dictionary<string, object>>(propsJson);
				if (props != null)
				{
					foreach (var kvp in props)
					{
						relationship.Properties[kvp.Key] = kvp.Value;
					}
				}
			}

			relationships.Add(relationship);
		}

		return relationships;
	}

	private static async Task ExecuteSqlAsync(NpgsqlConnection connection, string sql, CancellationToken cancellationToken)
	{
		await using var cmd = new NpgsqlCommand(sql, connection);
		await cmd.ExecuteNonQueryAsync(cancellationToken);
	}
}
