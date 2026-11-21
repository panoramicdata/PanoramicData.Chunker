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
public partial class CapitalizationEntityExtractor : IEntityExtractor
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

			var content = ExtractorHelpers.GetChunkContent(chunk);
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
}
