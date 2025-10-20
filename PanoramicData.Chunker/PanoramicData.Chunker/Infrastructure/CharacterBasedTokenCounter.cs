using PanoramicData.Chunker.Interfaces;
using System.Text.RegularExpressions;

namespace PanoramicData.Chunker.Infrastructure;

/// <summary>
/// Character-based token counter (default implementation).
/// Estimates tokens as characters / 4.
/// </summary>
public partial class CharacterBasedTokenCounter : ITokenCounter
{
	private const int CharsPerToken = 4;

	/// <summary>
	/// Count tokens in the given text.
	/// </summary>
	public int CountTokens(string text)
	{
		if (string.IsNullOrEmpty(text))
		{
			return 0;
		}

		// Simple approximation: ~4 characters per token
		return (int)Math.Ceiling(text.Length / (double)CharsPerToken);
	}

	/// <summary>
	/// Async version (just wraps synchronous version).
	/// </summary>
	public Task<int> CountTokensAsync(string text, CancellationToken cancellationToken = default)
	{
		return Task.FromResult(CountTokens(text));
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

		var maxChars = maxTokens * CharsPerToken;
		var overlapChars = overlap * CharsPerToken;

		// Try to split at sentence boundaries
		var sentences = SentenceRegex().Split(text);
		var currentBatch = new List<string>();
		var currentLength = 0;

		foreach (var sentence in sentences)
		{
			if (string.IsNullOrWhiteSpace(sentence))
			{
				continue;
			}

			var sentenceLength = sentence.Length;

			// If adding this sentence would exceed the limit, yield current batch
			if (currentLength + sentenceLength > maxChars && currentBatch.Count > 0)
			{
				yield return string.Join("", currentBatch);

				// Keep overlap
				if (overlap > 0 && currentBatch.Count > 0)
				{
					var overlapText = string.Join("", currentBatch);
					if (overlapText.Length > overlapChars)
					{
						overlapText = overlapText[^overlapChars..];
					}
					currentBatch = [overlapText];
					currentLength = overlapText.Length;
				}
				else
				{
					currentBatch.Clear();
					currentLength = 0;
				}
			}

			// If a single sentence is too long, split it
			if (sentenceLength > maxChars)
			{
				// Yield what we have
				if (currentBatch.Count > 0)
				{
					yield return string.Join("", currentBatch);
					currentBatch.Clear();
					currentLength = 0;
				}

				// Split long sentence into chunks
				for (var i = 0; i < sentenceLength; i += maxChars - overlapChars)
				{
					var chunkLength = Math.Min(maxChars, sentenceLength - i);
					yield return sentence.Substring(i, chunkLength);
				}
			}
			else
			{
				currentBatch.Add(sentence);
				currentLength += sentenceLength;
			}
		}

		// Yield remaining batch
		if (currentBatch.Count > 0)
		{
			yield return string.Join("", currentBatch);
		}
	}

	[GeneratedRegex(@"(?<=[.!?])\s+", RegexOptions.Compiled)]
	private static partial Regex SentenceRegex();
}
