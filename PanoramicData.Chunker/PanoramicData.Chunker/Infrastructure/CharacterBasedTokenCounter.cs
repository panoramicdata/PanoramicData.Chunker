using System.Runtime.InteropServices;
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
	public Task<int> CountTokensAsync(string text, CancellationToken cancellationToken) => Task.FromResult(CountTokens(text));

	/// <summary>
	/// Split text at token boundaries to fit within maxTokens.
	/// </summary>
	public IEnumerable<string> SplitIntoTokenBatches(string text, int maxTokens, [Optional, DefaultParameterValue(0)] int overlap)
	{
		if (string.IsNullOrEmpty(text))
		{
			yield break;
		}

		var maxChars = maxTokens * CharsPerToken;
		var overlapChars = overlap * CharsPerToken;
		var currentBatch = new List<string>();
		var currentLength = 0;

		foreach (var sentence in SentenceRegex().Split(text).Where(s => !string.IsNullOrWhiteSpace(s)))
		{
			if (currentLength + sentence.Length > maxChars && currentBatch.Count > 0)
			{
				yield return string.Concat(currentBatch);
				(currentBatch, currentLength) = StartNextBatch(currentBatch, overlap > 0, overlapChars);
			}

			if (sentence.Length > maxChars)
			{
				if (currentBatch.Count > 0)
				{
					yield return string.Concat(currentBatch);
					currentBatch.Clear();
					currentLength = 0;
				}

				foreach (var piece in SplitLongSentence(sentence, maxChars, overlapChars))
				{
					yield return piece;
				}
			}
			else
			{
				currentBatch.Add(sentence);
				currentLength += sentence.Length;
			}
		}

		if (currentBatch.Count > 0)
		{
			yield return string.Concat(currentBatch);
		}
	}

	private static (List<string> Batch, int Length) StartNextBatch(List<string> previous, bool keepOverlap, int overlapChars)
	{
		if (!keepOverlap)
		{
			return ([], 0);
		}

		var overlapText = string.Concat(previous);
		if (overlapText.Length > overlapChars)
		{
			overlapText = overlapText[^overlapChars..];
		}

		return ([overlapText], overlapText.Length);
	}

	private static IEnumerable<string> SplitLongSentence(string sentence, int maxChars, int overlapChars)
	{
		for (var i = 0; i < sentence.Length; i += maxChars - overlapChars)
		{
			yield return sentence.Substring(i, Math.Min(maxChars, sentence.Length - i));
		}
	}
	[GeneratedRegex(@"(?<=[.!?])\s+", RegexOptions.Compiled)]
	private static partial Regex SentenceRegex();
}
