namespace PanoramicData.Chunker.Models.KnowledgeGraph;

/// <summary>
/// Defines the schema for a knowledge graph, including valid entity and relationship types.
/// </summary>
public class GraphSchema
{
	/// <summary>
	/// Gets or sets the name of this schema.
	/// </summary>
	public string Name { get; set; } = "Default";

	/// <summary>
	/// Gets or sets the version of this schema.
	/// </summary>
	public string Version { get; set; } = "1.0";

	/// <summary>
	/// Gets or sets the allowed entity types in this schema.
	/// If empty, all types are allowed.
	/// </summary>
	public List<EntityType> AllowedEntityTypes { get; set; } = [];

	/// <summary>
	/// Gets or sets the allowed relationship types in this schema.
	/// If empty, all types are allowed.
	/// </summary>
	public List<RelationshipType> AllowedRelationshipTypes { get; set; } = [];

	/// <summary>
	/// Gets or sets constraints for specific entity types.
	/// Key: EntityType, Value: Constraint definition
	/// </summary>
	public Dictionary<EntityType, EntityTypeConstraint> EntityConstraints { get; set; } = [];

	/// <summary>
	/// Gets or sets constraints for specific relationship types.
	/// Key: RelationshipType, Value: Constraint definition
	/// </summary>
	public Dictionary<RelationshipType, RelationshipTypeConstraint> RelationshipConstraints { get; set; } = [];

	/// <summary>
	/// Validates whether an entity type is allowed in this schema.
	/// </summary>
	/// <param name="type">The entity type to validate.</param>
	/// <returns>True if allowed.</returns>
	public bool IsEntityTypeAllowed(EntityType type) => AllowedEntityTypes.Count == 0 || AllowedEntityTypes.Contains(type);

	/// <summary>
	/// Validates whether a relationship type is allowed in this schema.
	/// </summary>
	/// <param name="type">The relationship type to validate.</param>
	/// <returns>True if allowed.</returns>
	public bool IsRelationshipTypeAllowed(RelationshipType type) => AllowedRelationshipTypes.Count == 0 || AllowedRelationshipTypes.Contains(type);
}

/// <summary>
/// Constraints for a specific entity type.
/// </summary>
public class EntityTypeConstraint
{
	/// <summary>
	/// Gets or sets required properties for this entity type.
	/// </summary>
	public List<string> RequiredProperties { get; set; } = [];

	/// <summary>
	/// Gets or sets optional properties for this entity type.
	/// </summary>
	public List<string> OptionalProperties { get; set; } = [];

	/// <summary>
	/// Gets or sets the minimum confidence score required for this entity type.
	/// </summary>
	public double MinConfidence { get; set; } = 0.0;

	/// <summary>
	/// Gets or sets whether this entity type allows aliases.
	/// </summary>
	public bool AllowAliases { get; set; } = true;
}

/// <summary>
/// Constraints for a specific relationship type.
/// </summary>
public class RelationshipTypeConstraint
{
	/// <summary>
	/// Gets or sets allowed source entity types for this relationship.
	/// If empty, all types are allowed.
	/// </summary>
	public List<EntityType> AllowedSourceTypes { get; set; } = [];

	/// <summary>
	/// Gets or sets allowed target entity types for this relationship.
	/// If empty, all types are allowed.
	/// </summary>
	public List<EntityType> AllowedTargetTypes { get; set; } = [];

	/// <summary>
	/// Gets or sets whether this relationship type is bidirectional by default.
	/// </summary>
	public bool IsBidirectional { get; set; }

	/// <summary>
	/// Gets or sets the minimum confidence score required for this relationship type.
	/// </summary>
	public double MinConfidence { get; set; } = 0.0;

	/// <summary>
	/// Gets or sets whether evidence is required for this relationship type.
	/// </summary>
	public bool RequiresEvidence { get; set; }
}
