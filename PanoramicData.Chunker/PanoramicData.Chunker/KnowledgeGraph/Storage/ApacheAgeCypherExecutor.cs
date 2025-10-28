using Microsoft.Extensions.Logging;
using Npgsql;
using PanoramicData.Chunker.Interfaces.KnowledgeGraph;
using PanoramicData.Chunker.Models.KnowledgeGraph;
using System.Text.Json;

namespace PanoramicData.Chunker.KnowledgeGraph.Storage;

/// <summary>
/// Apache AGE implementation for executing Cypher queries against PostgreSQL.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="ApacheAgeCypherExecutor"/> class.
/// </remarks>
/// <param name="connectionString">PostgreSQL connection string.</param>
/// <param name="graphName">Apache AGE graph name.</param>
/// <param name="logger">Logger instance.</param>
public class ApacheAgeCypherExecutor(
	string connectionString,
	string graphName,
	ILogger<ApacheAgeCypherExecutor> logger) : ICypherQueryExecutor
{
	private readonly string _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
	private readonly string _graphName = graphName ?? throw new ArgumentNullException(nameof(graphName));
	private readonly ILogger<ApacheAgeCypherExecutor> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

	/// <inheritdoc/>
	public async Task<IEnumerable<T>> ExecuteQueryAsync<T>(
		string cypherQuery,
		Dictionary<string, object>? parameters,
		CancellationToken cancellationToken)
	{
		_logger.LogInformation("Executing Cypher query: {Query}", cypherQuery);

		await using var connection = new NpgsqlConnection(_connectionString);
		await connection.OpenAsync(cancellationToken);

		// Set the search path to include the AGE extension
		await using (var cmd = new NpgsqlCommand("SET search_path = ag_catalog, \"$user\", public;", connection))
		{
			await cmd.ExecuteNonQueryAsync(cancellationToken);
		}

		// Build the Cypher query with parameters
		var query = BuildCypherQuery(cypherQuery, parameters);

		await using var command = new NpgsqlCommand(query, connection);
		await using var reader = await command.ExecuteReaderAsync(cancellationToken);

		var results = new List<T>();
		while (await reader.ReadAsync(cancellationToken))
		{
			// Apache AGE returns results as JSON, parse them
			var jsonResult = reader.GetString(0);
			var result = JsonSerializer.Deserialize<T>(jsonResult);
			if (result != null)
			{
				results.Add(result);
			}
		}

		_logger.LogInformation("Query returned {Count} results", results.Count);
		return results;
	}

	/// <inheritdoc/>
	public async Task<IEnumerable<Dictionary<string, object>>> ExecuteQueryRawAsync(
		string cypherQuery,
		Dictionary<string, object>? parameters,
		CancellationToken cancellationToken)
	{
		_logger.LogInformation("Executing raw Cypher query: {Query}", cypherQuery);

		await using var connection = new NpgsqlConnection(_connectionString);
		await connection.OpenAsync(cancellationToken);

		// Set the search path to include the AGE extension
		await using (var cmd = new NpgsqlCommand("SET search_path = ag_catalog, \"$user\", public;", connection))
		{
			await cmd.ExecuteNonQueryAsync(cancellationToken);
		}

		// Build the Cypher query
		var query = BuildCypherQuery(cypherQuery, parameters);

		await using var command = new NpgsqlCommand(query, connection);
		await using var reader = await command.ExecuteReaderAsync(cancellationToken);

		var results = new List<Dictionary<string, object>>();
		while (await reader.ReadAsync(cancellationToken))
		{
			var row = new Dictionary<string, object>();
			for (var i = 0; i < reader.FieldCount; i++)
			{
				var value = reader.GetValue(i);
				row[reader.GetName(i)] = value;
			}
			results.Add(row);
		}

		_logger.LogInformation("Raw query returned {Count} rows", results.Count);
		return results;
	}

	/// <inheritdoc/>
	public async Task<List<Guid>> FindShortestPathAsync(
		Guid fromEntityId,
		Guid toEntityId,
		int maxHops,
		CancellationToken cancellationToken)
	{
		_logger.LogInformation("Finding shortest path from {From} to {To}", fromEntityId, toEntityId);

		var cypherQuery = $@"
			MATCH path = shortestPath(
				(from:Entity {{Id: '{fromEntityId}'}})-[*..{maxHops}]-(to:Entity {{Id: '{toEntityId}'}})
			)
			RETURN [node IN nodes(path) | node.Id] AS entityIds
		";

		var results = await ExecuteQueryAsync<List<Guid>>(cypherQuery, null, cancellationToken);
		var path = results.FirstOrDefault() ?? [];

		_logger.LogInformation("Shortest path found with {Length} entities", path.Count);
		return path;
	}

	/// <inheritdoc/>
	public async Task<List<Entity>> GetNeighborsAsync(
		Guid entityId,
		int depth,
		List<RelationshipType>? relationshipTypes,
		CancellationToken cancellationToken)
	{
		_logger.LogInformation("Getting neighbors for entity {EntityId} at depth {Depth}", entityId, depth);

		var relationshipFilter = relationshipTypes != null && relationshipTypes.Count > 0
			? $":{string.Join("|", relationshipTypes.Select(t => t.ToString()))}"
			: "";

		var cypherQuery = $@"
			MATCH (start:Entity {{Id: '{entityId}'}})-[{relationshipFilter}*1..{depth}]-(neighbor:Entity)
			RETURN DISTINCT neighbor
		";

		var results = await ExecuteQueryAsync<Entity>(cypherQuery, null, cancellationToken);
		var neighbors = results.ToList();

		_logger.LogInformation("Found {Count} neighbors", neighbors.Count);
		return neighbors;
	}

	/// <inheritdoc/>
	public async Task<CypherMatchResult> ExecutePatternMatchAsync(
		string pattern,
		string? whereClause,
		CancellationToken cancellationToken)
	{
		_logger.LogInformation("Executing pattern match: {Pattern}", pattern);

		var where = string.IsNullOrEmpty(whereClause) ? "" : $"WHERE {whereClause}";
		var cypherQuery = $@"
			MATCH {pattern}
			{where}
			RETURN *
		";

		var rawResults = await ExecuteQueryRawAsync(cypherQuery, null, cancellationToken);

		var result = new CypherMatchResult();

		// Parse results into entities and relationships
		foreach (var row in rawResults)
		{
			foreach (var kvp in row)
			{
				if (kvp.Value is string json && !string.IsNullOrEmpty(json))
				{
					try
					{
						// Try to deserialize as Entity
						var entity = JsonSerializer.Deserialize<Entity>(json);
						if (entity != null && !result.Entities.Any(e => e.Id == entity.Id))
						{
							result.Entities.Add(entity);
						}
					}
					catch
					{
						try
						{
							// Try to deserialize as Relationship
							var relationship = JsonSerializer.Deserialize<Relationship>(json);
							if (relationship != null && !result.Relationships.Any(r => r.Id == relationship.Id))
							{
								result.Relationships.Add(relationship);
							}
						}
						catch
						{
							// Store as additional data if not an entity or relationship
							result.AdditionalData[kvp.Key] = kvp.Value;
						}
					}
				}
			}
		}

		_logger.LogInformation("Pattern match found {EntityCount} entities and {RelationshipCount} relationships",
			result.Entities.Count, result.Relationships.Count);

		return result;
	}

	/// <summary>
	/// Builds a Cypher query for Apache AGE with parameters.
	/// </summary>
	private string BuildCypherQuery(string cypherQuery, Dictionary<string, object>? parameters)
	{
		// Apache AGE requires wrapping Cypher queries in a SELECT statement
		var query = $"SELECT * FROM ag_catalog.cypher('{_graphName}', $$\n{cypherQuery}\n$$) AS (result agtype);";

		// Replace parameters in the query (simple implementation - should be enhanced for production)
		if (parameters != null)
		{
			foreach (var param in parameters)
			{
				var value = param.Value switch
				{
					string s => $"'{s}'",
					Guid g => $"'{g}'",
					int i => i.ToString(),
					double d => d.ToString(),
					bool b => b.ToString().ToLower(),
					_ => JsonSerializer.Serialize(param.Value)
				};

				query = query.Replace($"${param.Key}", value);
			}
		}

		return query;
	}
}
