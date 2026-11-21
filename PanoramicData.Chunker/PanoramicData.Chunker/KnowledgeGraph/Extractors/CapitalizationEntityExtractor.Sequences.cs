
namespace PanoramicData.Chunker.KnowledgeGraph.Extractors;

public partial class CapitalizationEntityExtractor
{
	private List<string> ExtractCapitalizedSequences(string text)
	{
		var results = new List<string>();

		foreach (var sentence in text.Split(['.', '!', '?'], StringSplitOptions.RemoveEmptyEntries))
		{
			var words = sentence.Split([' ', '\t', '\n', '\r'], StringSplitOptions.RemoveEmptyEntries);
			var index = 0;

			while (index < words.Length)
			{
				var word = CleanWord(words[index]);
				var isCandidate = IsCapitalized(word) && !IsAcronym(word) && !IsAllCaps(word)
					&& (index != 0 || ShouldExtractAtSentenceStart(word, words, index));

				if (!isCandidate)
				{
					index++;
					continue;
				}

				var sequence = ExtractMultiWordProperNoun(words, index);
				results.Add(sequence);
				index += sequence.Split(' ').Length;
			}
		}

		return [.. results.Distinct(StringComparer.OrdinalIgnoreCase)];
	}
	/// <summary>
	/// Phase 12: Determines if a capitalized word at sentence start should be extracted as a proper noun.
	/// </summary>
	private bool ShouldExtractAtSentenceStart(string firstWord, string[] words, int index)
	{
		// Title prefix (e.g. "Professor Jameson") or a known proper noun
		if (_titlePrefixes.Contains(firstWord) || _properNounDictionary.Contains(firstWord))
		{
			return true;
		}

		if (IsMultiWordStart(words, index))
		{
			return true;
		}

		// "Society was founded..." is likely not the entity name
		if (_organizationalSuffixes.Contains(firstWord))
		{
			return false;
		}

		// Part of a multi-word capitalized sequence; single words are too often false positives
		return PeekAheadForMultiWordSequence(words, index) >= 2;
	}

	private bool IsMultiWordStart(string[] words, int index)
	{
		if (index + 1 >= words.Length)
		{
			return false;
		}

		var nextWord = CleanWord(words[index + 1]);

		// e.g. "Edinburgh University"
		if (IsProperNounCandidate(nextWord) && (_properNounDictionary.Contains(nextWord) || _organizationalSuffixes.Contains(nextWord)))
		{
			return true;
		}

		// e.g. "University of Edinburgh"
		return _allowedConnectors.Contains(nextWord)
			&& index + 2 < words.Length
			&& IsProperNounCandidate(CleanWord(words[index + 2]));
	}
}
