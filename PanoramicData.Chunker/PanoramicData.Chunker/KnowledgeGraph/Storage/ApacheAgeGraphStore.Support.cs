using Microsoft.Extensions.Logging;
using Npgsql;
using PanoramicData.Chunker.Models.KnowledgeGraph;
using System.Text.Json;

namespace PanoramicData.Chunker.KnowledgeGraph.Storage;

public partial class ApacheAgeGraphStore
{
	private const string SelectEntitiesSql = "SELECT id, name, type, confidence, frequency, properties FROM age_entities WHERE graph_id = @graphId";

	private const string SelectRelationshipsSql = "SELECT id, from_entity_id, to_entity_id, type, weight, confidence, properties FROM age_relationships WHERE graph_id = @graphId";

	private const string SelectEntitiesByTypeSql = SelectEntitiesSql + " AND type = @type";

	private const string SelectRelationshipsByTypeSql = SelectRelationshipsSql + " AND type = @type";

	private async Task<NpgsqlConnection> OpenInitializedAsync(CancellationToken cancellationToken)
	{
		var connection = new NpgsqlConnection(_connectionString);
		try
		{
			await connection.OpenAsync(cancellationToken);
			await InitializeAgeAsync(connection, cancellationToken);
			return connection;
		}
		catch
		{
			await connection.DisposeAsync();
			throw;
		}
	}

	private async Task WithConnectionAsync(
		Func<NpgsqlConnection, Task> work,
		Action<Exception> logError,
		CancellationToken cancellationToken)
		=> await WithConnectionAsync<object?>(
			async connection =>
			{
				await work(connection);
				return null;
			},
			logError,
			cancellationToken);

	private async Task<T> WithConnectionAsync<T>(
		Func<NpgsqlConnection, Task<T>> work,
		Action<Exception> logError,
		CancellationToken cancellationToken)
	{
		await using var connection = await OpenInitializedAsync(cancellationToken);

		try
		{
			return await work(connection);
		}
		catch (Exception ex)
		{
			logError(ex);
			throw;
		}
	}

	private async Task InitializeAgeAsync(NpgsqlConnection connection, CancellationToken cancellationToken)
	{
		try
		{
			await using (var load = new NpgsqlCommand("LOAD 'age';", connection))
			{
				await load.ExecuteNonQueryAsync(cancellationToken);
			}

			await using var setPath = new NpgsqlCommand("SET search_path = ag_catalog, public;", connection);
			await setPath.ExecuteNonQueryAsync(cancellationToken);
		}
		catch (Exception ex)
		{
			_logger.LogWarning(ex, "Could not initialize Apache AGE - it may not be installed");
		}
	}

	private static NpgsqlCommand Bind(NpgsqlCommand cmd, params (string Name, object Value)[] parameters)
	{
		foreach (var (name, value) in parameters)
		{
			cmd.Parameters.AddWithValue(name, value);
		}

		return cmd;
	}

	private static async Task<int> ExecuteAsync(NpgsqlCommand command, CancellationToken cancellationToken)
	{
		await using var cmd = command;
		return await cmd.ExecuteNonQueryAsync(cancellationToken);
	}

	private static async Task<List<T>> ReadAllAsync<T>(NpgsqlCommand cmd, Func<NpgsqlDataReader, T> read, CancellationToken cancellationToken)
	{
		var rows = new List<T>();
		await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
		while (await reader.ReadAsync(cancellationToken))
		{
			rows.Add(read(reader));
		}

		return rows;
	}

	private static Entity ReadEntity(NpgsqlDataReader reader)
	{
		var entity = new Entity(
			Enum.Parse<EntityType>(reader.GetString(2)),
			reader.GetString(1),
			reader.GetDouble(3))
		{
			Id = reader.GetGuid(0),
			Frequency = reader.GetInt32(4)
		};

		ReadProperties(reader, 5, entity.Properties);
		return entity;
	}

	private static Relationship ReadRelationship(NpgsqlDataReader reader)
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

		ReadProperties(reader, 6, relationship.Properties);
		return relationship;
	}

	private static void ReadProperties(NpgsqlDataReader reader, int ordinal, Dictionary<string, object> target)
	{
		if (reader.IsDBNull(ordinal))
		{
			return;
		}

		var props = JsonSerializer.Deserialize<Dictionary<string, object>>(reader.GetString(ordinal));
		if (props == null)
		{
			return;
		}

		foreach (var kvp in props)
		{
			target[kvp.Key] = kvp.Value;
		}
	}

	private const string CreateMetadataTablesSql = @"
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

	private static async Task CreateMetadataTableIfNotExistsAsync(NpgsqlConnection connection, CancellationToken cancellationToken)
		=> await ExecuteAsync(new NpgsqlCommand(CreateMetadataTablesSql, connection), cancellationToken);

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

		await using var cmd = Bind(new NpgsqlCommand(sql, connection), ("id", graphId));
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
		await using var cmd = Bind(new NpgsqlCommand(SelectEntitiesSql, connection), ("graphId", graphId));
		return await ReadAllAsync(cmd, ReadEntity, cancellationToken);
	}

	private static async Task<List<Relationship>> LoadRelationshipsAsync(NpgsqlConnection connection, Guid graphId, CancellationToken cancellationToken)
	{
		await using var cmd = Bind(new NpgsqlCommand(SelectRelationshipsSql, connection), ("graphId", graphId));
		return await ReadAllAsync(cmd, ReadRelationship, cancellationToken);
	}
}
