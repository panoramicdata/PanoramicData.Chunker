using PanoramicData.Chunker.Models.LLM;

namespace PanoramicData.Chunker.Interfaces;

/// <summary>
/// Abstraction for LLM providers (Ollama, OpenAI, Azure, etc.).
/// </summary>
public interface ILlmProvider
{
	/// <summary>
	/// Generates text completion from a prompt.
	/// </summary>
	/// <param name="request">The LLM request.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>The LLM response.</returns>
	Task<LLMResponse> GenerateAsync(
		LLMRequest request,
		CancellationToken cancellationToken = default);

	/// <summary>
	/// Generates text completions for multiple prompts (batch).
	/// </summary>
	/// <param name="requests">The LLM requests.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>The LLM responses.</returns>
	Task<IEnumerable<LLMResponse>> GenerateBatchAsync(
		IEnumerable<LLMRequest> requests,
		CancellationToken cancellationToken = default);

	/// <summary>
	/// Checks if the LLM provider is available/reachable.
	/// </summary>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>True if available, false otherwise.</returns>
	Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default);

	/// <summary>
	/// Gets the name of the provider (e.g., "Ollama", "OpenAI").
	/// </summary>
	string ProviderName { get; }
}
