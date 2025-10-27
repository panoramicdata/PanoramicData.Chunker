namespace PanoramicData.Chunker.Models.LLM;

/// <summary>
/// Represents a response from an LLM provider.
/// </summary>
public record LLMResponse
{
	/// <summary>
	/// The generated text.
	/// </summary>
	public required string Text { get; init; }

	/// <summary>
	/// The model used for generation.
	/// </summary>
	public required string Model { get; init; }

	/// <summary>
	/// Total tokens used (prompt + completion).
	/// </summary>
	public int TokensUsed { get; init; }

	/// <summary>
	/// Duration of the generation request.
	/// </summary>
	public TimeSpan Duration { get; init; }

	/// <summary>
	/// Indicates if the request was successful.
	/// </summary>
	public bool Success { get; init; }

	/// <summary>
	/// Error message if the request failed.
	/// </summary>
	public string? ErrorMessage { get; init; }
}
