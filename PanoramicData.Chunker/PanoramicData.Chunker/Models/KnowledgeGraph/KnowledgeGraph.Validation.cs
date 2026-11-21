namespace PanoramicData.Chunker.Models.KnowledgeGraph;

public partial class Graph
{
	/// <summary>
	/// Validates the integrity of the knowledge graph.
	/// </summary>
	/// <returns>True if the graph is valid.</returns>
	public bool Validate(out List<string> errors)
	{
		errors = [];

		// Check for duplicate entity IDs
		var entityIds = new HashSet<Guid>();
		foreach (var entity in Entities)
		{
			if (!entityIds.Add(entity.Id))
			{
				errors.Add($"Duplicate entity ID: {entity.Id}");
			}
		}

		// Check for duplicate relationship IDs
		var relationshipIds = new HashSet<Guid>();
		foreach (var relationship in Relationships)
		{
			if (!relationshipIds.Add(relationship.Id))
			{
				errors.Add($"Duplicate relationship ID: {relationship.Id}");
			}
		}

		// Check for relationships with invalid entity references
		foreach (var relationship in Relationships)
		{
			if (!entityIds.Contains(relationship.FromEntityId))
			{
				errors.Add($"Relationship {relationship.Id} references non-existent entity: {relationship.FromEntityId}");
			}
			if (!entityIds.Contains(relationship.ToEntityId))
			{
				errors.Add($"Relationship {relationship.Id} references non-existent entity: {relationship.ToEntityId}");
			}
		}

		return errors.Count == 0;
	}
}
