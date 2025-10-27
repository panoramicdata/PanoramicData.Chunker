namespace PanoramicData.Chunker.Models.Llm;

/// <summary>
/// Represents a request to an LLM provider.
/// </summary>
public record LlmRequest
{
	/// <summary>
	/// The prompt to send to the LLM.
	/// </summary>
	public required string Prompt { get; init; }

	/// <summary>
	/// The model to use for generation.
	/// </summary>
	public required string Model { get; init; }

	/// <summary>
	/// Temperature for generation (0.0-1.0). Higher = more creative.
	/// </summary>
	public double Temperature { get; init; } = 0.7;

	/// <summary>
	/// Maximum number of tokens to generate.
	/// </summary>
	public int MaxTokens { get; init; } = 500;

	/// <summary>
	/// Optional system prompt to set context.
	/// </summary>
	public string? SystemPrompt { get; init; }
}
