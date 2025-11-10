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
		int? minOccurrences = null,
		int? minWordLength = null,
		double? baseConfidence = null)
	{
		// Load default configuration
		_config = LoadDefaultConfigSync();

		// Apply parameter overrides
		_minOccurrences = minOccurrences ?? _config.ExtractionRules?.MinOccurrences ?? 1;
		_minWordLength = minWordLength ?? _config.ExtractionRules?.MinWordLength ?? 2;
		_baseConfidence = baseConfidence ?? _config.ExtractionRules?.BaseConfidence ?? 0.7;

		// Build cached lookups
		_properNounDictionary = _config.GetAllProperNouns();
		_titlePrefixes = _config.GetAllTitlePrefixes();
		_organizationalSuffixes = new HashSet<string>(_config.OrganizationalSuffixes ?? [], StringComparer.OrdinalIgnoreCase);
		_allowedConnectors = new HashSet<string>(_config.AllowedConnectors ?? [], StringComparer.OrdinalIgnoreCase);
		_sentenceStarters = new HashSet<string>(_config.SentenceStarters ?? [], StringComparer.OrdinalIgnoreCase);
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
		int? minOccurrences = null,
		int? minWordLength = null,
		double? baseConfidence = null,
		CancellationToken cancellationToken = default)
	{
		var config = await EntityPatternLoader.LoadPatternsAsync(configFilePath, cancellationToken);
		return new CapitalizationEntityExtractor(config, minOccurrences, minWordLength, baseConfidence);
	}

	/// <summary>
	/// Internal constructor that accepts pre-loaded configuration.
	/// </summary>
	private CapitalizationEntityExtractor(
		EntityPatternsConfiguration config,
		int? minOccurrences = null,
		int? minWordLength = null,
		double? baseConfidence = null)
	{
		_config = config;

		// Apply parameter overrides
		_minOccurrences = minOccurrences ?? _config.ExtractionRules?.MinOccurrences ?? 1;
		_minWordLength = minWordLength ?? _config.ExtractionRules?.MinWordLength ?? 2;
		_baseConfidence = baseConfidence ?? _config.ExtractionRules?.BaseConfidence ?? 0.7;

		// Build cached lookups
		_properNounDictionary = _config.GetAllProperNouns();
		_titlePrefixes = _config.GetAllTitlePrefixes();
		_organizationalSuffixes = new HashSet<string>(_config.OrganizationalSuffixes ?? [], StringComparer.OrdinalIgnoreCase);
		_allowedConnectors = new HashSet<string>(_config.AllowedConnectors ?? [], StringComparer.OrdinalIgnoreCase);
		_sentenceStarters = new HashSet<string>(_config.SentenceStarters ?? [], StringComparer.OrdinalIgnoreCase);
	}

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

				candidate.Frequency++;
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
			.Select(c => new Entity(
				EntityType.ProperNoun,
				c.Term,
				confidence: CalculateConfidence(c))
			{
				Frequency = c.Frequency,
				Sources = c.Sources,
				Metadata = new EntityMetadata
				{
					ExtractorName = Name,
					ExtractorVersion = Version,
					ExtractedAt = DateTimeOffset.UtcNow,
					ExtractionDetails = new Dictionary<string, object>
					{
						["extraction_method"] = "capitalization_enhanced",
						["word_count"] = c.Term.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length,
						["has_title"] = HasTitlePrefix(c.Term),
						["in_dictionary"] = IsInDictionary(c.Term),
						["config_version"] = _config.Version ?? "unknown"
					}
				}
			})
			.ToList();

		return await Task.FromResult(entities);
	}

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
		var sentences = text.Split(['.', '!', '?'], StringSplitOptions.RemoveEmptyEntries);

		foreach (var sentence in sentences)
		{
			var words = sentence.Split([' ', '\t', '\n', '\r'], StringSplitOptions.RemoveEmptyEntries);

			// Process ALL words, with enhanced logic for first word
			for (var i = 0; i < words.Length; i++)
			{
				var word = CleanWord(words[i]);

				// Check if capitalized AND not an acronym AND not all-caps
				if (IsCapitalized(word) && !IsAcronym(word) && !IsAllCaps(word))
				{
					// Phase 12: Enhanced sentence-start handling
					if (i == 0)
					{
						// Check if this is a proper noun we should extract
						var shouldExtractAtSentenceStart = ShouldExtractAtSentenceStart(word, words, i);

						if (!shouldExtractAtSentenceStart)
						{
							continue;
						}

						// Extract the multi-word sequence
						var sentenceStartSequence = ExtractMultiWordProperNoun(words, i);
						results.Add(sentenceStartSequence);
						i += sentenceStartSequence.Split(' ').Length - 1;
						continue;
					}

					// Not first word - extract normally
					var midSentenceSequence = ExtractMultiWordProperNoun(words, i);
					results.Add(midSentenceSequence);
					i += midSentenceSequence.Split(' ').Length - 1;  // Skip processed words
				}
			}
		}

		return [.. results.Distinct(StringComparer.OrdinalIgnoreCase)];
	}

	/// <summary>
	/// Phase 12: Determines if a capitalized word at sentence start should be extracted as a proper noun.
	/// </summary>
	private bool ShouldExtractAtSentenceStart(string firstWord, string[] words, int index)
	{
		// Strategy 1: Title prefix (e.g., "Professor Jameson", "HMS Beagle")
		if (_titlePrefixes.Contains(firstWord))
		{
			return true;
		}

		// Strategy 2: In proper noun dictionary
		if (_properNounDictionary.Contains(firstWord))
		{
			return true;
		}

		// Strategy 3: Multi-word sequence with next word also capitalized or connector
		if (index + 1 < words.Length)
		{
			var nextWord = CleanWord(words[index + 1]);

			// Next word is capitalized proper noun
			if (IsCapitalized(nextWord) && !IsAcronym(nextWord))
			{
				// Check if it's a known pattern (e.g., "Edinburgh University")
				if (_properNounDictionary.Contains(nextWord) ||
					_organizationalSuffixes.Contains(nextWord))
				{
					return true;
				}

				// Or if first word is in dictionary (e.g., "Plinian Society")
				if (_properNounDictionary.Contains(firstWord))
				{
					return true;
				}
			}

			// Next word is a connector (e.g., "University of Edinburgh")
			if (_allowedConnectors.Contains(nextWord))
			{
				// Check if we have a third word that's capitalized
				if (index + 2 < words.Length)
				{
					var thirdWord = CleanWord(words[index + 2]);
					if (IsCapitalized(thirdWord) && !IsAcronym(thirdWord))
					{
						return true;
					}
				}
			}
		}

		// Strategy 4: Ends with organizational suffix (e.g., "Society", "University")
		// Look ahead to see if this becomes a multi-word org name
		if (_organizationalSuffixes.Contains(firstWord))
		{
			return false; // "Society was founded..." - likely not the entity name
		}

		// Phase 12: Be more lenient - if it's part of a multi-word capitalized sequence, extract it
		var peekAhead = PeekAheadForMultiWordSequence(words, index);
		if (peekAhead >= 2) // At least 2 words in sequence
		{
			return true;
		}

		// Default: Don't extract single words at sentence start (too many false positives)
		return false;
	}

	/// <summary>
	/// Phase 12: Peeks ahead to count how many capitalized words follow in sequence.
	/// </summary>
	private int PeekAheadForMultiWordSequence(string[] words, int startIndex)
	{
		var count = 1; // Start with current word

		for (var i = startIndex + 1; i < words.Length && i < startIndex + 5; i++)
		{
			var word = CleanWord(words[i]);

			if (IsCapitalized(word) && !IsAcronym(word) && !IsAllCaps(word))
			{
				count++;
			}
			else if (_allowedConnectors.Contains(word))
			{
				// Connector - continue counting
				continue;
			}
			else
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
		var words = name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
		if (words.Length == 0) return false;
		return _titlePrefixes.Contains(words[0]);
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
		public int Frequency { get; set; }
		public List<EntitySource> Sources { get; set; } = [];
	}
}
