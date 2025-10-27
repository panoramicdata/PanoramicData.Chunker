namespace PanoramicData.Chunker.Configuration;

/// <summary>
/// Configuration options for Ollama LLM provider.
/// </summary>
public record OllamaOptions
{
	/// <summary>
	/// Base URL for the Ollama API.
	/// </summary>
	public string BaseUrl { get; init; } = "http://localhost:11434";

	/// <summary>
	/// Default model to use for generation.
	/// </summary>
	public string DefaultModel { get; init; } = "llama3";

	/// <summary>
	/// Default temperature for generation (0.0-1.0).
	/// </summary>
	public double Temperature { get; init; } = 0.7;

	/// <summary>
	/// Default maximum tokens for generation.
	/// </summary>
	public int MaxTokens { get; init; } = 500;

	/// <summary>
	/// Timeout for API requests in seconds.
	/// </summary>
	public int TimeoutSeconds { get; init; } = 30;

	/// <summary>
	/// Number of retry attempts for failed requests.
	/// </summary>
	public int RetryAttempts { get; init; } = 3;
}
