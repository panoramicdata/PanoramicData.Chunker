namespace PanoramicData.Chunker.Configuration;

/// <summary>
/// Configuration for entity extraction patterns loaded from JSON.
/// </summary>
public class EntityPatternsConfiguration
{
	/// <summary>
	/// Version of the pattern configuration.
	/// </summary>
	public string Version { get; set; } = "1.0";

	/// <summary>
	/// Description of this pattern set.
	/// </summary>
	public string? Description { get; set; }

	/// <summary>
	/// Date when patterns were last updated.
	/// </summary>
	public string? LastUpdated { get; set; }

	/// <summary>
	/// Proper noun dictionary organized by category.
	/// </summary>
	public ProperNounDictionary? ProperNounDictionary { get; set; }

	/// <summary>
	/// Title prefixes organized by category.
	/// </summary>
	public Dictionary<string, List<string>>? TitlePrefixes { get; set; }

	/// <summary>
	/// Organizational suffixes that indicate entities.
	/// </summary>
	public List<string>? OrganizationalSuffixes { get; set; }

	/// <summary>
	/// Allowed connector words in multi-word entities.
	/// </summary>
	public List<string>? AllowedConnectors { get; set; }

	/// <summary>
	/// Words that commonly start sentences (to filter out).
	/// </summary>
	public List<string>? SentenceStarters { get; set; }

	/// <summary>
	/// Extraction rules and parameters.
	/// </summary>
	public ExtractionRules? ExtractionRules { get; set; }

	/// <summary>
	/// Training notes and instructions.
	/// </summary>
	public EntityTrainingNotes? TrainingNotes { get; set; }

	/// <summary>
	/// Gets all proper nouns as a flat list.
	/// </summary>
	public HashSet<string> GetAllProperNouns()
	{
		var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

		if (ProperNounDictionary?.People != null)
		{
			foreach (var person in ProperNounDictionary.People)
			{
				result.Add(person);
			}
		}

		if (ProperNounDictionary?.Places != null)
		{
			foreach (var place in ProperNounDictionary.Places)
			{
				result.Add(place);
			}
		}

		if (ProperNounDictionary?.Organizations != null)
		{
			foreach (var org in ProperNounDictionary.Organizations)
			{
				result.Add(org);
			}
		}

		return result;
	}

	/// <summary>
	/// Gets all title prefixes as a flat list.
	/// </summary>
	public HashSet<string> GetAllTitlePrefixes()
	{
		var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

		if (TitlePrefixes != null)
		{
			foreach (var category in TitlePrefixes.Values)
			{
				foreach (var title in category)
				{
					result.Add(title);
				}
			}
		}

		return result;
	}
}

/// <summary>
/// Proper noun dictionary organized by type.
/// </summary>
public class ProperNounDictionary
{
	/// <summary>
	/// Person names (first names, last names).
	/// </summary>
	public List<string>? People { get; set; }

	/// <summary>
	/// Place names (cities, countries, geographic features).
	/// </summary>
	public List<string>? Places { get; set; }

	/// <summary>
	/// Organization names and fragments.
	/// </summary>
	public List<string>? Organizations { get; set; }
}

/// <summary>
/// Extraction rules and parameters.
/// </summary>
public class ExtractionRules
{
	/// <summary>
	/// Minimum word length to consider.
	/// </summary>
	public int MinWordLength { get; set; } = 2;

	/// <summary>
	/// Minimum occurrences required.
	/// </summary>
	public int MinOccurrences { get; set; } = 1;

	/// <summary>
	/// Base confidence score.
	/// </summary>
	public double BaseConfidence { get; set; } = 0.7;

	/// <summary>
	/// Confidence boost amounts for various conditions.
	/// </summary>
	public ConfidenceBoosts? ConfidenceBoosts { get; set; }
}

/// <summary>
/// Confidence boost values for different entity characteristics.
/// </summary>
public class ConfidenceBoosts
{
	/// <summary>
	/// Boost for entities in the dictionary.
	/// </summary>
	public double InDictionary { get; set; } = 0.15;

	/// <summary>
	/// Boost for entities with title prefixes.
	/// </summary>
	public double HasTitle { get; set; } = 0.10;

	/// <summary>
	/// Boost for multi-word entities.
	/// </summary>
	public double MultiWord { get; set; } = 0.10;

	/// <summary>
	/// Boost for organizational suffixes.
	/// </summary>
	public double OrganizationalSuffix { get; set; } = 0.10;

	/// <summary>
	/// Boost per occurrence frequency.
	/// </summary>
	public double PerFrequency { get; set; } = 0.05;

	/// <summary>
	/// Maximum boost from frequency.
	/// </summary>
	public double MaxFrequencyBoost { get; set; } = 0.20;
}

/// <summary>
/// Training notes for entity extraction.
/// </summary>
public class EntityTrainingNotes
{
	/// <summary>
	/// General training instructions.
	/// </summary>
	public string? Instructions { get; set; }

	/// <summary>
	/// Dictionary maintenance guidelines.
	/// </summary>
	public string? DictionaryMaintenance { get; set; }

	/// <summary>
	/// Category classification guidelines.
	/// </summary>
	public string? CategoryClassification { get; set; }

	/// <summary>
	/// Performance monitoring guidelines.
	/// </summary>
	public string? PerformanceMonitoring { get; set; }
}
