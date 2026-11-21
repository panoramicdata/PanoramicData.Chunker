using PanoramicData.Chunker.Configuration;
using PanoramicData.Chunker.Models.KnowledgeGraph;

namespace PanoramicData.Chunker.KnowledgeGraph.Extractors;

public partial class CapitalizationEntityExtractor
{
	/// <summary>
	/// Phase 12: Checks if entity name has a title prefix.
	/// </summary>
	private bool HasTitlePrefix(string name)
	{
		var firstWord = name.Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
		return firstWord != null && _titlePrefixes.Contains(firstWord);
	}

	/// <summary>
	/// Phase 12: Checks if entity name (or any word in it) is in the proper noun dictionary.
	/// </summary>
	private bool IsInDictionary(string name)
	{
		// Check full name
		if (_properNounDictionary.Contains(name))
		{
			return true;
		}

		// Check individual words
		var words = name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
		return words.Any(word => _properNounDictionary.Contains(word));
	}

	private double CalculateConfidence(EntityCandidate candidate)
	{
		// Base confidence
		var confidence = _baseConfidence;

		// Get boost configuration (with defaults)
		var boosts = _config.ExtractionRules?.ConfidenceBoosts ?? new ConfidenceBoosts();

		// Phase 12: Boost confidence if in dictionary
		if (IsInDictionary(candidate.Term))
		{
			confidence += boosts.InDictionary;
		}

		// Phase 12: Boost confidence if has title prefix
		if (HasTitlePrefix(candidate.Term))
		{
			confidence += boosts.HasTitle;
		}

		// Boost confidence if appears multiple times
		if (candidate.Frequency > 1)
		{
			var frequencyBoost = Math.Min(boosts.MaxFrequencyBoost, candidate.Frequency * boosts.PerFrequency);
			confidence += frequencyBoost;
		}

		// Boost confidence for multi-word proper nouns (more likely to be actual entities)
		var wordCount = candidate.Term.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;
		if (wordCount > 1)
		{
			confidence += boosts.MultiWord;
		}

		// Phase 12: Extra boost for organizational names
		var words = candidate.Term.Split(' ', StringSplitOptions.RemoveEmptyEntries);
		if (words.Any(w => _organizationalSuffixes.Contains(w)))
		{
			confidence += boosts.OrganizationalSuffix;
		}

		// Cap at 1.0
		return Math.Min(1.0, confidence);
	}

	private static string GetContext(string text, string term, int contextSize = 50)
	{
		var position = text.IndexOf(term, StringComparison.OrdinalIgnoreCase);
		if (position < 0)
		{
			return text.Length <= 100 ? text : text[..100] + "...";
		}

		var start = Math.Max(0, position - contextSize);
		var end = Math.Min(text.Length, position + term.Length + contextSize);
		var context = text[start..end];

		if (start > 0)
		{
			context = "..." + context;
		}
		if (end < text.Length)
		{
			context += "...";
		}

		return context;
	}

	/// <summary>
	/// Internal class to track entity candidates during extraction.
	/// </summary>
	private class EntityCandidate
	{
		public string Term { get; set; } = string.Empty;
		public List<EntitySource> Sources { get; set; } = [];
		public int Frequency => Sources.Count;
	}
}
