namespace PanoramicData.Chunker.Interfaces;

/// <summary>
/// Interface for token counting implementations.
/// </summary>
public interface ITokenCounter
{
	/// <summary>
	/// Count tokens in the given text.
	/// </summary>
	/// <param name="text">The text to count tokens in.</param>
	/// <returns>The number of tokens.</returns>
	int CountTokens(string text);

	/// <summary>
	/// Async version for potentially expensive counting operations.
	/// </summary>
	/// <param name="text">The text to count tokens in.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>The number of tokens.</returns>
	Task<int> CountTokensAsync(string text, CancellationToken cancellationToken = default);

	/// <summary>
	/// Split text at token boundaries to fit within maxTokens.
	/// </summary>
	/// <param name="text">The text to split.</param>
	/// <param name="maxTokens">Maximum tokens per segment.</param>
	/// <param name="overlap">Number of tokens to overlap.</param>
	/// <returns>Enumerable of text segments.</returns>
	IEnumerable<string> SplitIntoTokenBatches(string text, int maxTokens, int overlap = 0);
}
