using System.Text.Json.Serialization;

namespace PanoramicData.Chunker.Models.Llm;

/// <summary>
/// Represents a preliminary named entity extracted from text.
/// </summary>
public record PreliminaryEntity
{
	/// <summary>
	/// The text of the entity.
	/// </summary>
	[JsonPropertyName("text")]
	public required string Text { get; init; }

	/// <summary>
	/// The type of entity (e.g., "Person", "Organization", "Location").
	/// </summary>
	[JsonPropertyName("type")]
	public required string Type { get; init; }

	/// <summary>
	/// Confidence score for the entity extraction (0.0-1.0).
	/// </summary>
	[JsonPropertyName("confidence")]
	public double Confidence { get; init; } = 1.0;

	/// <summary>
	/// Start position of the entity in the original text.
	/// </summary>
	[JsonPropertyName("startPosition")]
	public int? StartPosition { get; init; }

	/// <summary>
	/// End position of the entity in the original text.
	/// </summary>
	[JsonPropertyName("endPosition")]
	public int? EndPosition { get; init; }
}
