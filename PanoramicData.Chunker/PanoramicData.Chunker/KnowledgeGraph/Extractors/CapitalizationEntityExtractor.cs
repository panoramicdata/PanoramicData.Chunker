using System.Runtime.InteropServices;
using PanoramicData.Chunker.Configuration;
using PanoramicData.Chunker.Interfaces.KnowledgeGraph;
using PanoramicData.Chunker.Models;
using PanoramicData.Chunker.Models.KnowledgeGraph;

namespace PanoramicData.Chunker.KnowledgeGraph.Extractors;

/// <summary>
/// Entity extractor that identifies proper nouns based on capitalization patterns.
/// Uses JSON configuration for dictionaries and extraction rules, enabling domain customization.
/// </summary>
/// <remarks>
/// Phase 12 Enhancements:
/// - Proper noun dictionary loaded from JSON configuration
/// - Title prefixes, organizational suffixes configurable
/// - Extraction rules (confidence, thresholds) editable without recompilation
/// - Domain-specific customization supported
/// 
/// Configuration:
/// - Default config: Configuration/EntityPatterns.json
/// - Custom config: Can be loaded from any JSON file
/// 
/// This extractor uses enhanced heuristics:
/// - Detects capitalized words including at sentence starts when they match known patterns
/// - Extracts multi-word proper nouns (e.g., "Plinian Society", "University of Edinburgh")
/// - Recognizes title prefixes (Professor, Captain, HMS, Dr., etc.)
/// - Uses proper noun dictionary for improved accuracy
/// - Filters out acronyms and ALL-CAPS text
/// - Allows lowercase connectors like "of", "the", "and" in entity names
/// 
/// Best used in combination with other extractors for comprehensive entity extraction.
/// </remarks>
public class CapitalizationEntityExtractor : IEntityExtractor
{
	private readonly int _minOccurrences;
	private readonly int _minWordLength;
	private readonly double _baseConfidence;
	private readonly EntityPatternsConfiguration _config;

	// Cached lookups for performance
	private readonly HashSet<string> _properNounDictionary;
	private readonly HashSet<string> _titlePrefixes;
	private readonly HashSet<string> _organizationalSuffixes;
	private readonly HashSet<string> _allowedConnectors;
	private readonly HashSet<string> _sentenceStarters;

	/// <inheritdoc/>
	public string Name => "CapitalizationEntityExtractor";

	/// <inheritdoc/>
	public string Version => "2.0"; // Phase 12: JSON-based configuration

	/// <inheritdoc/>
	public IReadOnlyList<EntityType> SupportedEntityTypes { get; } = [EntityType.ProperNoun];

	/// <summary>
	/// Initializes a new instance of the <see cref="CapitalizationEntityExtractor"/> class
	/// with default configuration from JSON file.
	/// </summary>
	/// <param name="minOccurrences">Minimum number of times a term must appear (overrides config if specified).</param>
	/// <param name="minWordLength">Minimum word length to consider (overrides config if specified).</param>
	/// <param name="baseConfidence">Base confidence score (overrides config if specified).</param>
	public CapitalizationEntityExtractor(
		[Optional] int? minOccurrences,
		[Optional] int? minWordLength,
		[Optional] double? baseConfidence)
		: this(LoadDefaultConfigSync(), minOccurrences, minWordLength, baseConfidence)
	{
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="CapitalizationEntityExtractor"/> class
	/// with custom configuration from a JSON file.
	/// </summary>
	/// <param name="configFilePath">Path to custom configuration JSON file.</param>
	/// <param name="minOccurrences">Minimum number of times a term must appear (overrides config if specified).</param>
	/// <param name="minWordLength">Minimum word length to consider (overrides config if specified).</param>
	/// <param name="baseConfidence">Base confidence score (overrides config if specified).</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	public static async Task<CapitalizationEntityExtractor> CreateAsync(
		string configFilePath,
		[Optional] int? minOccurrences,
		[Optional] int? minWordLength,
		[Optional] double? baseConfidence,
		[Optional] CancellationToken cancellationToken)
	{
		var config = await EntityPatternLoader.LoadPatternsAsync(configFilePath, cancellationToken);
		return new CapitalizationEntityExtractor(config, minOccurrences, minWordLength, baseConfidence);
	}

	/// <summary>
	/// Internal constructor that accepts pre-loaded configuration.
	/// </summary>
	private CapitalizationEntityExtractor(
		EntityPatternsConfiguration config,
		int? minOccurrences,
		int? minWordLength,
		double? baseConfidence)
	{
		_config = config;
		var rules = config.ExtractionRules;

		_minOccurrences = Pick(minOccurrences, rules?.MinOccurrences, 1);
		_minWordLength = Pick(minWordLength, rules?.MinWordLength, 2);
		_baseConfidence = Pick(baseConfidence, rules?.BaseConfidence, 0.7);

		_properNounDictionary = config.GetAllProperNouns();
		_titlePrefixes = config.GetAllTitlePrefixes();
		_organizationalSuffixes = ToSet(config.OrganizationalSuffixes);
		_allowedConnectors = ToSet(config.AllowedConnectors);
		_sentenceStarters = ToSet(config.SentenceStarters);
	}

	private static HashSet<string> ToSet(IEnumerable<string>? values)
		=> new(values ?? [], StringComparer.OrdinalIgnoreCase);

	private static T Pick<T>(T? overrideValue, T? configured, T fallback)
		where T : struct
		=> overrideValue ?? configured ?? fallback;

	/// <summary>
	/// Loads default configuration synchronously (for backward compatibility).
	/// </summary>
	private static EntityPatternsConfiguration LoadDefaultConfigSync() => EntityPatternLoader.LoadDefaultPatternsAsync().GetAwaiter().GetResult();

	/// <inheritdoc/>
	public async Task<List<Entity>> ExtractEntitiesAsync(
		IEnumerable<ChunkerBase> chunks,
		CancellationToken cancellationToken)
	{
		var capitalizedTerms = new Dictionary<string, EntityCandidate>(StringComparer.OrdinalIgnoreCase);

		foreach (var chunk in chunks)
		{
			cancellationToken.ThrowIfCancellationRequested();

			var content = GetChunkContent(chunk);
			if (string.IsNullOrWhiteSpace(content))
			{
				continue;
			}

			// Find capitalized word sequences (including sentence starts with proper noun indicators)
			var candidates = ExtractCapitalizedSequences(content);

			foreach (var term in candidates)
			{
				if (!capitalizedTerms.TryGetValue(term, out var candidate))
				{
					candidate = new EntityCandidate { Term = term };
					capitalizedTerms[term] = candidate;
				}

				candidate.Sources.Add(new EntitySource
				{
					ChunkId = chunk.Id,
					Position = content.IndexOf(term, StringComparison.Ordinal),
					Context = GetContext(content, term)
				});
			}
		}

		// Filter: must appear at least N times and meet length requirement
		var entities = capitalizedTerms.Values
			.Where(c => c.Frequency >= _minOccurrences && c.Term.Length >= _minWordLength)
			.Select(CreateEntity)
			.ToList();

		return await Task.FromResult(entities);
	}

	private Entity CreateEntity(EntityCandidate candidate) => new(
		EntityType.ProperNoun,
		candidate.Term,
		confidence: CalculateConfidence(candidate))
	{
		Frequency = candidate.Frequency,
		Sources = candidate.Sources,
		Metadata = new EntityMetadata
		{
			ExtractorName = Name,
			ExtractorVersion = Version,
			ExtractedAt = DateTimeOffset.UtcNow,
			ExtractionDetails = new Dictionary<string, object>
			{
				["extraction_method"] = "capitalization_enhanced",
				["word_count"] = candidate.Term.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length,
				["has_title"] = HasTitlePrefix(candidate.Term),
				["in_dictionary"] = IsInDictionary(candidate.Term),
				["config_version"] = _config.Version ?? "unknown"
			}
		}
	};
	/// <inheritdoc/>
	public async Task<List<Entity>> ExtractEntitiesAsync(
		ChunkerBase chunk,
		CancellationToken cancellationToken)
		=> await ExtractEntitiesAsync([chunk], cancellationToken);

	private static string GetChunkContent(ChunkerBase chunk)
	{
		if (chunk is ContentChunk contentChunk)
		{
			return contentChunk.Content;
		}

		return string.Empty;
	}

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
