namespace PanoramicData.Chunker.Models.KnowledgeGraph;

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
