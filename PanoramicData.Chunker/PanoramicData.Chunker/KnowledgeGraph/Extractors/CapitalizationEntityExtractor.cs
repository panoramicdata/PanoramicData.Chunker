using PanoramicData.Chunker.Interfaces.KnowledgeGraph;
using PanoramicData.Chunker.Models;
using PanoramicData.Chunker.Models.KnowledgeGraph;

namespace PanoramicData.Chunker.KnowledgeGraph.Extractors;

/// <summary>
/// Entity extractor that identifies proper nouns based on capitalization patterns.
/// Extracts capitalized word sequences that likely represent names of people, organizations, places, etc.
/// </summary>
/// <remarks>
/// This extractor uses simple heuristics:
/// - Detects capitalized words that are NOT at sentence starts
/// - Extracts multi-word proper nouns (e.g., "Plinian Society", "University of Edinburgh")
/// - Filters out acronyms and ALL-CAPS text
/// - Allows lowercase connectors like "of", "the", "and" in entity names
///
/// Limitations:
/// - May miss entities in all-lowercase text
/// - Can produce false positives for sentence-initial words
/// - No entity type classification (all marked as ProperNoun)
///
/// Best used in combination with other extractors for comprehensive entity extraction.
/// </remarks>
/// <remarks>
/// Initializes a new instance of the <see cref="CapitalizationEntityExtractor"/> class.
/// </remarks>
/// <param name="minOccurrences">Minimum number of times a term must appear to be considered (default: 1).</param>
/// <param name="minWordLength">Minimum word length to consider (default: 2).</param>
/// <param name="baseConfidence">Base confidence score for extracted entities (default: 0.7).</param>
public class CapitalizationEntityExtractor(
	int minOccurrences = 1,
	int minWordLength = 2,
	double baseConfidence = 0.7) : IEntityExtractor
{
	private static readonly HashSet<string> _allowedConnectors = new(StringComparer.OrdinalIgnoreCase)
	{
		"of", "the", "and", "in", "at", "on", "for", "de", "del", "la", "le"
	};

	/// <inheritdoc/>
	public string Name => "CapitalizationEntityExtractor";

	/// <inheritdoc/>
	public string Version => "1.0";

	/// <inheritdoc/>
	public IReadOnlyList<EntityType> SupportedEntityTypes { get; } = [EntityType.ProperNoun];

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

			// Find capitalized word sequences (excluding sentence starts)
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
			.Where(c => c.Frequency >= minOccurrences && c.Term.Length >= minWordLength)
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
						["extraction_method"] = "capitalization",
						["word_count"] = c.Term.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length
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

			// Skip first word (may be capitalized as sentence start)
			for (var i = 1; i < words.Length; i++)
			{
				var word = CleanWord(words[i]);

				// Check if capitalized AND not an acronym AND not all-caps
				if (IsCapitalized(word) && !IsAcronym(word) && !IsAllCaps(word))
				{
					// Check if multi-word proper noun (e.g., "Plinian Society")
					var sequence = ExtractMultiWordProperNoun(words, i);
					if (sequence.Split(' ').Length >= 1) // At least one word
					{
						results.Add(sequence);
						i += sequence.Split(' ').Length - 1;  // Skip processed words
					}
				}
			}
		}

		return [.. results.Distinct(StringComparer.OrdinalIgnoreCase)];
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
		if (word.Length < minWordLength)
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

	private double CalculateConfidence(EntityCandidate candidate)
	{
		// Base confidence
		var confidence = baseConfidence;

		// Boost confidence if appears multiple times
		if (candidate.Frequency > 1)
		{
			confidence += Math.Min(0.2, candidate.Frequency * 0.05);
		}

		// Boost confidence for multi-word proper nouns (more likely to be actual entities)
		var wordCount = candidate.Term.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;
		if (wordCount > 1)
		{
			confidence += 0.1;
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
