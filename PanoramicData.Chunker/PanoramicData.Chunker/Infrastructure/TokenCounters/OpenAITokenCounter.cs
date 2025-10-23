using PanoramicData.Chunker.Interfaces;
using SharpToken;

namespace PanoramicData.Chunker.Infrastructure.TokenCounters;

/// <summary>
/// OpenAI token counter using SharpToken library.
/// Supports CL100K (GPT-4, GPT-3.5-turbo), P50K (GPT-3), and R50K (GPT-2) encodings.
/// </summary>
public class OpenAITokenCounter : ITokenCounter
{
	private readonly GptEncoding _encoding;
	private readonly string _modelName;

	/// <summary>
	/// Initializes a new instance of the <see cref="OpenAITokenCounter"/> class with CL100K encoding (GPT-4, GPT-3.5-turbo).
	/// </summary>
	public OpenAITokenCounter() : this("cl100k_base")
	{
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="OpenAITokenCounter"/> class with the specified encoding.
	/// </summary>
	/// <param name="encodingName">Encoding name: "cl100k_base" (GPT-4), "p50k_base" (GPT-3), or "r50k_base" (GPT-2).</param>
	public OpenAITokenCounter(string encodingName)
	{
		_encoding = GptEncoding.GetEncoding(encodingName);
		_modelName = encodingName;
	}

	/// <summary>
	/// Creates a token counter for GPT-4 and GPT-3.5-turbo models (CL100K encoding).
	/// </summary>
	public static OpenAITokenCounter ForGpt4() => new("cl100k_base");

	/// <summary>
	/// Creates a token counter for GPT-3 models (P50K encoding).
	/// </summary>
	public static OpenAITokenCounter ForGpt3() => new("p50k_base");

	/// <summary>
	/// Creates a token counter for GPT-2 models (R50K encoding).
	/// </summary>
	public static OpenAITokenCounter ForGpt2() => new("r50k_base");

	/// <summary>
	/// Count tokens in the given text.
	/// </summary>
	public int CountTokens(string text)
	{
		if (string.IsNullOrEmpty(text))
		{
			return 0;
		}

		try
		{
			var tokens = _encoding.Encode(text);
			return tokens.Count;
		}
		catch (Exception)
		{
			// Fallback to character-based estimation if encoding fails
			return (int)Math.Ceiling(text.Length / 4.0);
		}
	}

	/// <summary>
	/// Async version (wraps synchronous version as encoding is CPU-bound).
	/// </summary>
	public Task<int> CountTokensAsync(string text, CancellationToken cancellationToken = default)
	{
		return Task.Run(() => CountTokens(text), cancellationToken);
	}

	/// <summary>
	/// Split text at token boundaries to fit within maxTokens.
	/// </summary>
	public IEnumerable<string> SplitIntoTokenBatches(string text, int maxTokens, int overlap = 0)
	{
		if (string.IsNullOrEmpty(text))
		{
			yield break;
		}

		if (overlap >= maxTokens)
		{
			throw new ArgumentException("Overlap must be less than maxTokens", nameof(overlap));
		}

		// Encode the entire text first
		var allTokens = _encoding.Encode(text);
		var totalTokens = allTokens.Count;

		if (totalTokens <= maxTokens)
		{
			yield return text;
			yield break;
		}

		// Calculate stride (how many tokens to advance each iteration)
		var stride = maxTokens - overlap;

		// Split into batches
		for (var start = 0; start < totalTokens; start += stride)
		{
			var end = Math.Min(start + maxTokens, totalTokens);
			var batchTokens = allTokens.GetRange(start, end - start);

			// Decode tokens back to text
			var batchText = _encoding.Decode(batchTokens);
			yield return batchText;

			// If we've reached the end, stop
			if (end >= totalTokens)
			{
				break;
			}
		}
	}

	/// <summary>
	/// Gets the encoding name.
	/// </summary>
	public string EncodingName => _modelName;
}
