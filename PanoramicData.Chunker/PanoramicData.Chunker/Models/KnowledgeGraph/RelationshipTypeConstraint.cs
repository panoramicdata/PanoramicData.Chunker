namespace PanoramicData.Chunker.Models.KnowledgeGraph;

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
