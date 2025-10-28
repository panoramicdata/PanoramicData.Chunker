using PanoramicData.Chunker.Models.KnowledgeGraph;

namespace PanoramicData.Chunker.Interfaces.KnowledgeGraph;

/// <summary>
/// Interface for executing Cypher queries against the Knowledge Graph using Apache AGE.
/// </summary>
public interface ICypherQueryExecutor
{
	/// <summary>
	/// Executes a Cypher query and returns the results.
	/// </summary>
	/// <typeparam name="T">The expected result type.</typeparam>
	/// <param name="cypherQuery">The Cypher query to execute.</param>
	/// <param name="parameters">Query parameters.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>Query results.</returns>
	Task<IEnumerable<T>> ExecuteQueryAsync<T>(
		string cypherQuery,
		Dictionary<string, object>? parameters,
		CancellationToken cancellationToken);

	/// <summary>
	/// Executes a Cypher query and returns raw results as dictionaries.
	/// </summary>
	/// <param name="cypherQuery">The Cypher query to execute.</param>
	/// <param name="parameters">Query parameters.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>Query results as dictionaries.</returns>
	Task<IEnumerable<Dictionary<string, object>>> ExecuteQueryRawAsync(
		string cypherQuery,
		Dictionary<string, object>? parameters,
		CancellationToken cancellationToken);

	/// <summary>
	/// Finds the shortest path between two entities.
	/// </summary>
	/// <param name="fromEntityId">Source entity ID.</param>
	/// <param name="toEntityId">Target entity ID.</param>
	/// <param name="maxHops">Maximum number of hops.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>List of entity IDs representing the path.</returns>
	Task<List<Guid>> FindShortestPathAsync(
		Guid fromEntityId,
		Guid toEntityId,
		int maxHops,
		CancellationToken cancellationToken);

	/// <summary>
	/// Gets all neighbors of an entity within a specified depth.
	/// </summary>
	/// <param name="entityId">The entity ID.</param>
	/// <param name="depth">The traversal depth.</param>
	/// <param name="relationshipTypes">Optional filter for relationship types.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>List of neighboring entities.</returns>
	Task<List<Entity>> GetNeighborsAsync(
		Guid entityId,
		int depth,
		List<RelationshipType>? relationshipTypes,
		CancellationToken cancellationToken);

	/// <summary>
	/// Executes a pattern match query (e.g., "(person)-[:WORKS_FOR]->(company)").
	/// </summary>
	/// <param name="pattern">The Cypher pattern to match.</param>
	/// <param name="whereClause">Optional WHERE clause.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>Matched entities and relationships.</returns>
	Task<CypherMatchResult> ExecutePatternMatchAsync(
		string pattern,
		string? whereClause,
		CancellationToken cancellationToken);
}

/// <summary>
/// Result of a Cypher pattern match query.
/// </summary>
public class CypherMatchResult
{
	/// <summary>
	/// Gets or sets the matched entities.
	/// </summary>
	public List<Entity> Entities { get; set; } = [];

	/// <summary>
	/// Gets or sets the matched relationships.
	/// </summary>
	public List<Relationship> Relationships { get; set; } = [];

	/// <summary>
	/// Gets or sets additional result data.
	/// </summary>
	public Dictionary<string, object> AdditionalData { get; set; } = [];
}
