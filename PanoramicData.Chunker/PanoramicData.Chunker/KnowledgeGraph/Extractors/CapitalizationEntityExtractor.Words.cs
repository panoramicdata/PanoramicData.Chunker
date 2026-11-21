
namespace PanoramicData.Chunker.KnowledgeGraph.Extractors;

public partial class CapitalizationEntityExtractor
{
	private bool IsProperNounCandidate(string word) => IsCapitalized(word) && !IsAcronym(word);

	/// <summary>
	/// Phase 12: Peeks ahead to count how many capitalized words follow in sequence.
	/// </summary>
	private int PeekAheadForMultiWordSequence(string[] words, int startIndex)
	{
		var count = 1;

		for (var i = startIndex + 1; i < words.Length && i < startIndex + 5; i++)
		{
			var word = CleanWord(words[i]);

			if (IsProperNounCandidate(word) && !IsAllCaps(word))
			{
				count++;
			}
			else if (!_allowedConnectors.Contains(word))
			{
				break;
			}
		}

		return count;
	}
	private string ExtractMultiWordProperNoun(string[] words, int startIndex)
	{
		var sequence = new List<string> { CleanWord(words[startIndex]) };

		// Look ahead for more capitalized words (up to 5 words total)
		for (var i = startIndex + 1; i < words.Length && i < startIndex + 5; i++)
		{
			var word = CleanWord(words[i]);

			if (IsCapitalized(word) && !IsAcronym(word))
			{
				sequence.Add(word);
			}
			else if (_allowedConnectors.Contains(word))
			{
				// Allow lowercase connectors in entity names
				// e.g., "University of Edinburgh", "Society of Arts"
				sequence.Add(word);
			}
			else
			{
				break;
			}
		}

		return string.Join(" ", sequence);
	}

	private static string CleanWord(string word) =>
		// Remove common punctuation but preserve hyphens in compound words
		word.Trim(',', ';', ':', '"', '\'', ')', '(', '[', ']', '{', '}');

	private bool IsCapitalized(string word)
	{
		if (string.IsNullOrEmpty(word))
		{
			return false;
		}

		// Must start with uppercase letter
		if (!char.IsUpper(word[0]))
		{
			return false;
		}

		// Must be long enough
		if (word.Length < _minWordLength)
		{
			return false;
		}

		return true;
	}

	private static bool IsAcronym(string word) =>
		// Short all-caps sequences are likely acronyms (e.g., "USA", "NASA", "HTTP")
		word.Length >= 2 && word.Length <= 5 && word.All(char.IsUpper);

	private static bool IsAllCaps(string word)
	{
		// Longer all-caps text is usually emphasis, not a proper noun
		if (word.Length <= 1)
		{
			return false;
		}

		return word.All(c => !char.IsLetter(c) || char.IsUpper(c));
	}
}
