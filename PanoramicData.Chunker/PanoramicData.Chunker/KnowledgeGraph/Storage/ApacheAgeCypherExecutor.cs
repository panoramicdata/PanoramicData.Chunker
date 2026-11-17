using Microsoft.Extensions.Logging;
using Npgsql;
using PanoramicData.Chunker.Interfaces.KnowledgeGraph;
using PanoramicData.Chunker.Models.KnowledgeGraph;
using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

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
public partial class ApacheAgeCypherExecutor(
	string connectionString,
	string graphName,
	ILogger<ApacheAgeCypherExecutor> logger) : ICypherQueryExecutor
{
	private readonly string _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
	private readonly string _graphName = graphName ?? throw new ArgumentNullException(nameof(graphName));
	private readonly ILogger<ApacheAgeCypherExecutor> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

	[GeneratedRegex("^[A-Za-z_][A-Za-z0-9_]*$")]
	private static partial Regex GraphNameRegex();

	/// <inheritdoc/>
	public async Task<IEnumerable<T>> ExecuteQueryAsync<T>(
		string cypherQuery,
		Dictionary<string, object>? parameters,
		CancellationToken cancellationToken)
	{
		_logger.LogInformation("Executing Cypher query: {Query}", cypherQuery);

		await using var connection = await OpenConnectionAsync(cancellationToken);
		await using var command = CreateCommand(connection, BuildCypherQuery(cypherQuery, parameters));
		await using var reader = await command.ExecuteReaderAsync(cancellationToken);

		var results = new List<T>();
		while (await reader.ReadAsync(cancellationToken))
		{
			// Apache AGE returns results as JSON, parse them
			var result = JsonSerializer.Deserialize<T>(reader.GetString(0));
			if (!EqualityComparer<T>.Default.Equals(result, default))
			{
				results.Add(result!);
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

		await using var connection = await OpenConnectionAsync(cancellationToken);
		await using var command = CreateCommand(connection, BuildCypherQuery(cypherQuery, parameters));
		await using var reader = await command.ExecuteReaderAsync(cancellationToken);

		var results = new List<Dictionary<string, object>>();
		while (await reader.ReadAsync(cancellationToken))
		{
			var row = new Dictionary<string, object>();
			for (var i = 0; i < reader.FieldCount; i++)
			{
				row[reader.GetName(i)] = reader.GetValue(i);
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
				(from:Entity {{Id: '{fromEntityId}'}})-[*..{maxHops.ToString(CultureInfo.InvariantCulture)}]-(to:Entity {{Id: '{toEntityId}'}})
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
			MATCH (start:Entity {{Id: '{entityId}'}})-[{relationshipFilter}*1..{depth.ToString(CultureInfo.InvariantCulture)}]-(neighbor:Entity)
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
		foreach (var kvp in rawResults.SelectMany(row => row))
		{
			if (kvp.Value is string json && !string.IsNullOrEmpty(json))
			{
				AddParsedResult(result, kvp.Key, json);
			}
		}

		_logger.LogInformation("Pattern match found {EntityCount} entities and {RelationshipCount} relationships",
			result.Entities.Count, result.Relationships.Count);

		return result;
	}

	private static void AddParsedResult(CypherMatchResult result, string key, string json)
	{
		try
		{
			var entity = JsonSerializer.Deserialize<Entity>(json);
			if (entity != null && !result.Entities.Any(e => e.Id == entity.Id))
			{
				result.Entities.Add(entity);
			}
		}
		catch (JsonException)
		{
			AddRelationshipOrData(result, key, json);
		}
	}

	private static void AddRelationshipOrData(CypherMatchResult result, string key, string json)
	{
		try
		{
			var relationship = JsonSerializer.Deserialize<Relationship>(json);
			if (relationship != null && !result.Relationships.Any(r => r.Id == relationship.Id))
			{
				result.Relationships.Add(relationship);
			}
		}
		catch (JsonException)
		{
			result.AdditionalData[key] = json;
		}
	}

	private async Task<NpgsqlConnection> OpenConnectionAsync(CancellationToken cancellationToken)
	{
		var connection = new NpgsqlConnection(_connectionString);
		try
		{
			await connection.OpenAsync(cancellationToken);

			// Set the search path to include the AGE extension
			await using var cmd = new NpgsqlCommand("SET search_path = ag_catalog, \"$user\", public;", connection);
			await cmd.ExecuteNonQueryAsync(cancellationToken);
			return connection;
		}
		catch
		{
			await connection.DisposeAsync();
			throw;
		}
	}

	private static NpgsqlCommand CreateCommand(NpgsqlConnection connection, string query)
	{
		var command = connection.CreateCommand();
		command.CommandText = query;
		return command;
	}

	/// <summary>
	/// Builds a Cypher query for Apache AGE, inlining escaped parameter values.
	/// </summary>
	private string BuildCypherQuery(string cypherQuery, Dictionary<string, object>? parameters)
	{
		if (!GraphNameRegex().IsMatch(_graphName))
		{
			throw new InvalidOperationException($"Invalid Apache AGE graph name '{_graphName}'. Use letters, digits and underscores only.");
		}

		// Apache AGE requires a constant graph name, so the validated name is part of the statement text
		var query = string.Concat("SELECT * FROM ag_catalog.cypher('", _graphName, "', $$\n", cypherQuery, "\n$$) AS (result agtype);");

		if (parameters != null)
		{
			foreach (var param in parameters)
			{
				query = query.Replace($"${param.Key}", FormatParameterValue(param.Value), StringComparison.Ordinal);
			}
		}

		return query;
	}

	private static string FormatParameterValue(object? value) => value switch
	{
		string s => $"'{EscapeCypherString(s)}'",
		Guid g => $"'{g}'",
		int i => i.ToString(CultureInfo.InvariantCulture),
		double d => d.ToString(CultureInfo.InvariantCulture),
		bool b => b ? "true" : "false",
		_ => JsonSerializer.Serialize(value).Replace("$", "\\u0024", StringComparison.Ordinal)
	};

	private static string EscapeCypherString(string value)
	{
		var builder = new StringBuilder(value.Length);
		foreach (var c in value)
		{
			_ = c switch
			{
				'\\' => builder.Append("\\\\"),
				'\'' => builder.Append("\\'"),
				'$' => builder.Append("\\u0024"),
				_ => builder.Append(c)
			};
		}

		return builder.ToString();
	}
}
