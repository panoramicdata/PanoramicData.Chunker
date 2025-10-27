namespace PanoramicData.Chunker.Models.LLM;

/// <summary>
/// Represents a preliminary named entity extracted from text.
/// </summary>
public record PreliminaryEntity
{
	/// <summary>
	/// The text of the entity.
	/// </summary>
	public required string Text { get; init; }

	/// <summary>
	/// The type of entity (e.g., "Person", "Organization", "Location").
	/// </summary>
	public required string Type { get; init; }

	/// <summary>
	/// Confidence score for the entity extraction (0.0-1.0).
	/// </summary>
	public double Confidence { get; init; } = 1.0;

	/// <summary>
	/// Start position of the entity in the original text.
	/// </summary>
	public int? StartPosition { get; init; }

	/// <summary>
	/// End position of the entity in the original text.
	/// </summary>
	public int? EndPosition { get; init; }
}
