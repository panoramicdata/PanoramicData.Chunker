using PanoramicData.Chunker.Interfaces.KnowledgeGraph;
using PanoramicData.Chunker.Models;
using PanoramicData.Chunker.Models.KnowledgeGraph;

namespace PanoramicData.Chunker.KnowledgeGraph.Extractors;

/// <summary>
/// Hybrid entity extractor that combines TF-IDF keyword extraction with capitalization-based proper noun detection.
/// Provides better coverage than using either method alone.
/// </summary>
/// <remarks>
/// This extractor runs two complementary extraction strategies:
/// 1. <see cref="SimpleKeywordExtractor"/> - Extracts significant keywords using TF-IDF
/// 2. <see cref="CapitalizationEntityExtractor"/> - Detects proper nouns by capitalization
/// 
/// The results are merged with proper nouns taking precedence when there are conflicts.
/// This approach ensures both general topics and specific named entities are captured.
/// 
/// Benefits:
/// - Catches rare proper nouns (e.g., "Plinian Society") missed by TF-IDF alone
/// - Maintains good keyword extraction for general terms
/// - No external dependencies required
/// - Fast processing suitable for large documents
/// 
/// Typical use case: Knowledge graph extraction where both topics and entities are needed.
/// </remarks>
/// <remarks>
/// Initializes a new instance of the <see cref="HybridEntityExtractor"/> class with custom extractors.
/// </remarks>
/// <param name="keywordExtractor">The keyword extractor to use.</param>
/// <param name="capitalizationExtractor">The capitalization extractor to use.</param>
public class HybridEntityExtractor(
	SimpleKeywordExtractor keywordExtractor,
	CapitalizationEntityExtractor capitalizationExtractor) : IEntityExtractor
{
	private readonly SimpleKeywordExtractor _keywordExtractor = keywordExtractor ?? throw new ArgumentNullException(nameof(keywordExtractor));
	private readonly CapitalizationEntityExtractor _capitalizationExtractor = capitalizationExtractor ?? throw new ArgumentNullException(nameof(capitalizationExtractor));

	/// <summary>
	/// Initializes a new instance of the <see cref="HybridEntityExtractor"/> class with default settings.
	/// </summary>
	public HybridEntityExtractor()
		: this(
			keywordExtractor: new SimpleKeywordExtractor(maxKeywords: 50, minWordLength: 3, minConfidence: 0.0),
			capitalizationExtractor: new CapitalizationEntityExtractor(minOccurrences: 1, minWordLength: 2))
	{
	}

	/// <inheritdoc/>
	public string Name => "HybridEntityExtractor";

	/// <inheritdoc/>
	public string Version => "1.0";

	/// <inheritdoc/>
	public IReadOnlyList<EntityType> SupportedEntityTypes { get; } =
	[
		EntityType.Keyword,
		EntityType.ProperNoun
	];

	/// <inheritdoc/>
	public async Task<List<Entity>> ExtractEntitiesAsync(
		IEnumerable<ChunkerBase> chunks,
		CancellationToken cancellationToken)
	{
		var chunkList = chunks.ToList();

		// Extract using both methods in parallel for efficiency
		var keywordsTask = _keywordExtractor.ExtractEntitiesAsync(chunkList, cancellationToken);
		var properNounsTask = _capitalizationExtractor.ExtractEntitiesAsync(chunkList, cancellationToken);

		await Task.WhenAll(keywordsTask, properNounsTask);

		var keywords = await keywordsTask;
		var properNouns = await properNounsTask;

		// Merge results (proper nouns take precedence for naming, but we combine sources)
		var merged = MergeEntities(keywords, properNouns);

		return merged;
	}

	/// <inheritdoc/>
	public async Task<List<Entity>> ExtractEntitiesAsync(
		ChunkerBase chunk,
		CancellationToken cancellationToken)
		=> await ExtractEntitiesAsync([chunk], cancellationToken);

	private static List<Entity> MergeEntities(List<Entity> keywords, List<Entity> properNouns)
	{
		var merged = new Dictionary<string, Entity>(StringComparer.OrdinalIgnoreCase);

		// Step 1: Add all keywords first
		foreach (var entity in keywords)
		{
			merged[entity.Name] = entity;
		}

		// Step 2: Add/merge proper nouns (these take precedence)
		foreach (var entity in properNouns)
		{
			if (merged.TryGetValue(entity.Name, out var existing))
			{
				// Entity exists as keyword - merge them
				var mergedEntity = MergeEntity(existing, entity);
				merged[entity.Name] = mergedEntity;
			}
			else
			{
				// New proper noun - add it with aliases
				entity.Aliases = GenerateNameAliases(entity.Name);
				merged[entity.Name] = entity;
			}
		}

		// Return sorted by confidence (highest first)
		return [.. merged.Values.OrderByDescending(e => e.Confidence)];
	}

	private static Entity MergeEntity(Entity keyword, Entity properNoun)
	{
		// Create new entity based on proper noun (better type classification)
		var merged = new Entity(
			properNoun.Type,  // Use ProperNoun type
			properNoun.Name,   // Use proper noun's name (preserves capitalization)
			confidence: Math.Max(keyword.Confidence, properNoun.Confidence) * 1.1) // Boost confidence when both extractors agree
		{
			Frequency = keyword.Frequency + properNoun.Frequency,
			Metadata = new EntityMetadata
			{
				ExtractorName = "HybridEntityExtractor",
				ExtractorVersion = "1.0",
				ExtractedAt = DateTimeOffset.UtcNow,
				ExtractionDetails = new Dictionary<string, object>
				{
					["merged_from"] = new[] { "keyword", "proper_noun" },
					["keyword_confidence"] = keyword.Confidence,
					["proper_noun_confidence"] = properNoun.Confidence
				}
			}
		};

		// Combine sources from both entities
		merged.Sources.AddRange(keyword.Sources);
		merged.Sources.AddRange(properNoun.Sources);

		// Generate name aliases for better matching
		merged.Aliases = GenerateNameAliases(merged.Name);

		// Cap confidence at 1.0
		if (merged.Confidence > 1.0)
		{
			merged.Confidence = 1.0;
		}

		return merged;
	}

	/// <summary>
	/// Generates name aliases for an entity to handle variations in text.
	/// </summary>
	/// <param name="name">The entity name.</param>
	/// <returns>List of alias variations.</returns>
	private static List<string> GenerateNameAliases(string name)
	{
		var aliases = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

		// Remove quotes if present (e.g., "'Beagle'" → "Beagle")
		if (name.Contains('\'') || name.Contains('"'))
		{
			aliases.Add(name.Replace("'", "").Replace("\"", ""));
		}

		// HMS prefix variations (e.g., "HMS Beagle" → "Beagle")
		if (name.StartsWith("HMS ", StringComparison.OrdinalIgnoreCase))
		{
			aliases.Add(name[4..]); // Remove "HMS "
		}

		// Multi-word: Add last word as alias (e.g., "Robert Grant" → "Grant")
		// But skip if it's a common word or very short
		if (name.Contains(' '))
		{
			var words = name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
			if (words.Length > 1)
			{
				var lastName = words[^1];
				// Only add if it's substantial (not "of", "the", etc.)
				if (lastName.Length > 2 && char.IsUpper(lastName[0]))
				{
					aliases.Add(lastName);
				}

				// Also add first word if it's a person's first name
				var firstName = words[0];
				if (firstName.Length > 2 && char.IsUpper(firstName[0]))
				{
					aliases.Add(firstName);
				}
			}
		}

		// Title prefixes: "Professor Jameson" → "Jameson"
		var titlePrefixes = new[] { "Professor", "Captain", "Dr.", "Dr", "Sir", "Lord", "Mr.", "Mr", "Mrs.", "Mrs", "Miss", "Ms.", "Ms" };
		foreach (var prefix in titlePrefixes)
		{
			if (name.StartsWith(prefix + " ", StringComparison.OrdinalIgnoreCase))
			{
				aliases.Add(name[(prefix.Length + 1)..]);
			}
		}

		// Remove the original name from aliases (we don't need it as an alias of itself)
		aliases.Remove(name);

		return [.. aliases.Where(a => !string.IsNullOrWhiteSpace(a))];
	}
}
