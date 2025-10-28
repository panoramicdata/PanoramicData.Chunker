using PanoramicData.Chunker.Models.KnowledgeGraph;

namespace PanoramicData.Chunker.Interfaces.KnowledgeGraph;

/// <summary>
/// Interface for persisting and retrieving knowledge graphs from storage.
/// </summary>
public interface IGraphStore
{
	/// <summary>
	/// Saves a knowledge graph to storage.
	/// </summary>
	/// <param name="graph">The knowledge graph to save.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	Task SaveGraphAsync(Graph graph, CancellationToken cancellationToken = default);

	/// <summary>
	/// Loads a knowledge graph from storage by ID.
	/// </summary>
	/// <param name="graphId">The graph ID.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>The knowledge graph, or null if not found.</returns>
	Task<Graph?> LoadGraphAsync(Guid graphId, CancellationToken cancellationToken = default);

	/// <summary>
	/// Loads a knowledge graph from storage by name.
	/// </summary>
	/// <param name="name">The graph name.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>The knowledge graph, or null if not found.</returns>
	Task<Graph?> LoadGraphByNameAsync(string name, CancellationToken cancellationToken = default);

	/// <summary>
	/// Deletes a knowledge graph from storage.
	/// </summary>
	/// <param name="graphId">The graph ID.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	Task DeleteGraphAsync(Guid graphId, CancellationToken cancellationToken = default);

	/// <summary>
	/// Checks if a knowledge graph exists in storage.
	/// </summary>
	/// <param name="graphId">The graph ID.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>True if the graph exists.</returns>
	Task<bool> GraphExistsAsync(Guid graphId, CancellationToken cancellationToken = default);

	/// <summary>
	/// Lists all knowledge graphs in storage.
	/// </summary>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>List of graph metadata.</returns>
	Task<List<GraphMetadata>> ListGraphsAsync(CancellationToken cancellationToken = default);

	/// <summary>
	/// Saves an entity to storage.
	/// </summary>
	/// <param name="graphId">The graph ID.</param>
	/// <param name="entity">The entity to save.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	Task SaveEntityAsync(Guid graphId, Entity entity, CancellationToken cancellationToken = default);

	/// <summary>
	/// Saves a relationship to storage.
	/// </summary>
	/// <param name="graphId">The graph ID.</param>
	/// <param name="relationship">The relationship to save.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	Task SaveRelationshipAsync(Guid graphId, Relationship relationship, CancellationToken cancellationToken = default);

	/// <summary>
	/// Queries entities by type.
	/// </summary>
	/// <param name="graphId">The graph ID.</param>
	/// <param name="entityType">The entity type.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>List of matching entities.</returns>
	Task<List<Entity>> QueryEntitiesByTypeAsync(Guid graphId, EntityType entityType, CancellationToken cancellationToken = default);

	/// <summary>
	/// Queries relationships by type.
	/// </summary>
	/// <param name="graphId">The graph ID.</param>
	/// <param name="relationshipType">The relationship type.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>List of matching relationships.</returns>
	Task<List<Relationship>> QueryRelationshipsByTypeAsync(Guid graphId, RelationshipType relationshipType, CancellationToken cancellationToken = default);
}
