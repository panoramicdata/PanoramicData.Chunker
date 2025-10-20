namespace PanoramicData.Chunker.Interfaces;

/// <summary>
/// Interface for LLM operations (summaries, keywords).
/// </summary>
public interface ILlmProvider
{
	/// <summary>
	/// Generate a summary of the content.
	/// </summary>
	/// <param name="content">The content to summarize.</param>
	/// <param name="maxTokens">Maximum length of summary in tokens.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>The generated summary.</returns>
	Task<string> GenerateSummaryAsync(
		string content,
		int maxTokens = 100,
		CancellationToken cancellationToken = default);

	/// <summary>
	/// Extract keywords from the content.
	/// </summary>
	/// <param name="content">The content to analyze.</param>
	/// <param name="maxKeywords">Maximum number of keywords to extract.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>List of extracted keywords.</returns>
	Task<List<string>> ExtractKeywordsAsync(
		string content,
		int maxKeywords = 5,
		CancellationToken cancellationToken = default);
}
