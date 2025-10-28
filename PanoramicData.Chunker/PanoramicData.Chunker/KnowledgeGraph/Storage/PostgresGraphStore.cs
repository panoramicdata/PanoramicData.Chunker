using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PanoramicData.Chunker.Interfaces.KnowledgeGraph;
using PanoramicData.Chunker.Models.KnowledgeGraph;

namespace PanoramicData.Chunker.KnowledgeGraph.Storage;

/// <summary>
/// PostgreSQL + EF Core implementation of IGraphStore for persisting knowledge graphs.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="PostgresGraphStore"/> class.
/// </remarks>
/// <param name="context">The database context.</param>
/// <param name="logger">The logger.</param>
public class PostgresGraphStore(
	KnowledgeGraphDbContext context,
	ILogger<PostgresGraphStore> logger) : IGraphStore
{
	private readonly KnowledgeGraphDbContext _context = context ?? throw new ArgumentNullException(nameof(context));
	private readonly ILogger<PostgresGraphStore> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

	/// <inheritdoc/>
	public async Task SaveGraphAsync(Graph graph, CancellationToken cancellationToken)
	{
		ArgumentNullException.ThrowIfNull(graph);

		_logger.LogInformation("Saving graph '{GraphName}' ({GraphId}) with {EntityCount} entities and {RelationshipCount} relationships",
			graph.Name, graph.Id, graph.Entities.Count, graph.Relationships.Count);

		try
		{
			// Check if graph exists
			var existingGraph = await _context.Graphs.FindAsync([graph.Id], cancellationToken);

			if (existingGraph == null)
			{
				// Create new graph
				_context.Graphs.Add(graph);
			}
			else
			{
				// Update existing graph
				existingGraph.Name = graph.Name;
				existingGraph.Metadata = graph.Metadata;
				existingGraph.Schema = graph.Schema;
				existingGraph.Statistics = graph.Statistics;
				_context.Graphs.Update(existingGraph);
			}

			// Save graph metadata first
			await _context.SaveChangesAsync(cancellationToken);

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

			_logger.LogInformation("Successfully saved graph '{GraphName}' ({GraphId})", graph.Name, graph.Id);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error saving graph '{GraphName}' ({GraphId})", graph.Name, graph.Id);
			throw;
		}
	}

	/// <inheritdoc/>
	public async Task<Graph?> LoadGraphAsync(Guid graphId, CancellationToken cancellationToken)
	{
		_logger.LogInformation("Loading graph {GraphId}", graphId);

		try
		{
			var graph = await _context.Graphs.FindAsync([graphId], cancellationToken);

			if (graph == null)
			{
				_logger.LogWarning("Graph {GraphId} not found", graphId);
				return null;
			}

			// Load entities and relationships
			graph.Entities = await _context.Entities
				.Where(e => _context.Relationships.Any(r => r.FromEntityId == e.Id || r.ToEntityId == e.Id))
				.ToListAsync(cancellationToken);

			graph.Relationships = await _context.Relationships
				.ToListAsync(cancellationToken);

			// Build indexes
			graph.BuildIndexes();

			_logger.LogInformation("Successfully loaded graph '{GraphName}' ({GraphId}) with {EntityCount} entities and {RelationshipCount} relationships",
				graph.Name, graphId, graph.Entities.Count, graph.Relationships.Count);

			return graph;
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error loading graph {GraphId}", graphId);
			throw;
		}
	}

	/// <inheritdoc/>
	public async Task<Graph?> LoadGraphByNameAsync(string name, CancellationToken cancellationToken)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(name);

		_logger.LogInformation("Loading graph by name '{GraphName}'", name);

		try
		{
			var graph = await _context.Graphs
				.FirstOrDefaultAsync(g => g.Name == name, cancellationToken);

			if (graph == null)
			{
				_logger.LogWarning("Graph '{GraphName}' not found", name);
				return null;
			}

			return await LoadGraphAsync(graph.Id, cancellationToken);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error loading graph by name '{GraphName}'", name);
			throw;
		}
	}

	/// <inheritdoc/>
	public async Task DeleteGraphAsync(Guid graphId, CancellationToken cancellationToken)
	{
		_logger.LogInformation("Deleting graph {GraphId}", graphId);

		try
		{
			var graph = await _context.Graphs.FindAsync([graphId], cancellationToken);

			if (graph == null)
			{
				_logger.LogWarning("Graph {GraphId} not found for deletion", graphId);
				return;
			}

			// Delete all relationships first (foreign key constraints)
			var relationships = await _context.Relationships
				.ToListAsync(cancellationToken);
			_context.Relationships.RemoveRange(relationships);

			// Delete all entities
			var entities = await _context.Entities
				.ToListAsync(cancellationToken);
			_context.Entities.RemoveRange(entities);

			// Delete the graph
			_context.Graphs.Remove(graph);

			await _context.SaveChangesAsync(cancellationToken);

			_logger.LogInformation("Successfully deleted graph {GraphId}", graphId);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error deleting graph {GraphId}", graphId);
			throw;
		}
	}

	/// <inheritdoc/>
	public async Task<bool> GraphExistsAsync(Guid graphId, CancellationToken cancellationToken)
		=> await _context.Graphs.AnyAsync(g => g.Id == graphId, cancellationToken);

	/// <inheritdoc/>
	public async Task<List<GraphMetadata>> ListGraphsAsync(CancellationToken cancellationToken)
	{
		_logger.LogInformation("Listing all graphs");

		try
		{
			var graphs = await _context.Graphs
				.Select(g => g.Metadata)
				.ToListAsync(cancellationToken);

			_logger.LogInformation("Found {GraphCount} graphs", graphs.Count);

			return graphs;
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error listing graphs");
			throw;
		}
	}

	/// <inheritdoc/>
	public async Task SaveEntityAsync(Guid graphId, Entity entity, CancellationToken cancellationToken)
	{
		ArgumentNullException.ThrowIfNull(entity);

		try
		{
			var existingEntity = await _context.Entities.FindAsync([entity.Id], cancellationToken);

			if (existingEntity == null)
			{
				_context.Entities.Add(entity);
			}
			else
			{
				_context.Entry(existingEntity).CurrentValues.SetValues(entity);
			}

			await _context.SaveChangesAsync(cancellationToken);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error saving entity {EntityId} in graph {GraphId}", entity.Id, graphId);
			throw;
		}
	}

	/// <inheritdoc/>
	public async Task SaveRelationshipAsync(Guid graphId, Relationship relationship, CancellationToken cancellationToken)
	{
		ArgumentNullException.ThrowIfNull(relationship);

		try
		{
			var existingRelationship = await _context.Relationships.FindAsync([relationship.Id], cancellationToken);

			if (existingRelationship == null)
			{
				_context.Relationships.Add(relationship);
			}
			else
			{
				_context.Entry(existingRelationship).CurrentValues.SetValues(relationship);
			}

			await _context.SaveChangesAsync(cancellationToken);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error saving relationship {RelationshipId} in graph {GraphId}", relationship.Id, graphId);
			throw;
		}
	}

	/// <inheritdoc/>
	public async Task<List<Entity>> QueryEntitiesByTypeAsync(Guid graphId, EntityType entityType, CancellationToken cancellationToken)
	{
		_logger.LogInformation("Querying entities of type {EntityType} in graph {GraphId}", entityType, graphId);

		try
		{
			var entities = await _context.Entities
				.Where(e => e.Type == entityType)
				.ToListAsync(cancellationToken);

			_logger.LogInformation("Found {EntityCount} entities of type {EntityType}", entities.Count, entityType);

			return entities;
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error querying entities by type {EntityType} in graph {GraphId}", entityType, graphId);
			throw;
		}
	}

	/// <inheritdoc/>
	public async Task<List<Relationship>> QueryRelationshipsByTypeAsync(Guid graphId, RelationshipType relationshipType, CancellationToken cancellationToken)
	{
		_logger.LogInformation("Querying relationships of type {RelationshipType} in graph {GraphId}", relationshipType, graphId);

		try
		{
			var relationships = await _context.Relationships
				.Where(r => r.Type == relationshipType)
				.ToListAsync(cancellationToken);

			_logger.LogInformation("Found {RelationshipCount} relationships of type {RelationshipType}", relationships.Count, relationshipType);

			return relationships;
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error querying relationships by type {RelationshipType} in graph {GraphId}", relationshipType, graphId);
			throw;
		}
	}
}
