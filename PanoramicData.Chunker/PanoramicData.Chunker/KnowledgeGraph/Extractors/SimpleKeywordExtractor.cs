using PanoramicData.Chunker.Interfaces.KnowledgeGraph;
using PanoramicData.Chunker.Models;
using PanoramicData.Chunker.Models.KnowledgeGraph;
using System.Text.RegularExpressions;

namespace PanoramicData.Chunker.KnowledgeGraph.Extractors;

/// <summary>
/// Simple keyword-based entity extractor using TF-IDF algorithm.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="SimpleKeywordExtractor"/> class.
/// </remarks>
/// <param name="maxKeywords">Maximum number of keywords to extract per chunk.</param>
/// <param name="minWordLength">Minimum word length to consider.</param>
/// <param name="minConfidence">Minimum confidence score for extraction.</param>
public partial class SimpleKeywordExtractor(int maxKeywords = 10, int minWordLength = 3, double minConfidence = 0.0) : IEntityExtractor
{
	private static readonly HashSet<string> _stopWords = new(StringComparer.OrdinalIgnoreCase)
	{
		"a", "an", "and", "are", "as", "at", "be", "been", "but", "by", "for", "from",
		"has", "have", "he", "in", "is", "it", "its", "of", "on", "that", "the", "to",
		"was", "were", "will", "with", "this", "they", "their", "them", "these", "those",
		"what", "when", "where", "which", "who", "whom", "why", "would", "could", "should",
		"can", "may", "might", "must", "shall", "or", "not", "no", "yes", "also", "any",
		"some", "such", "into", "than", "then", "there", "more", "much", "very", "so",
		"do", "does", "did", "doing", "done", "am", "been", "being", "had", "having",
		"if", "because", "while", "until", "since", "after", "before", "during", "through",
		"about", "above", "below", "between", "under", "over", "up", "down", "out", "off"
	};

	[GeneratedRegex(@"\b[a-zA-Z][a-zA-Z0-9]*\b")]
	private static partial Regex WordRegex();

	/// <inheritdoc/>
	public string Name => "SimpleKeywordExtractor";

	/// <inheritdoc/>
	public string Version => "1.0";

	/// <inheritdoc/>
	public IReadOnlyList<EntityType> SupportedEntityTypes { get; } = [EntityType.Keyword];

	/// <inheritdoc/>
	public async Task<List<Entity>> ExtractEntitiesAsync(
		IEnumerable<ChunkerBase> chunks,
		CancellationToken cancellationToken)
	{
		var chunkList = chunks.ToList();
		if (chunkList.Count == 0)
		{
			return [];
		}

		// Calculate term frequencies across all chunks
		var termFrequencies = CalculateTermFrequencies(chunkList);

		// Calculate document frequencies (how many chunks contain each term)
		var documentFrequencies = CalculateDocumentFrequencies(chunkList);

		// Calculate TF-IDF scores
		var tfidfScores = CalculateTfIdf(termFrequencies, documentFrequencies, chunkList.Count);

		// Extract top keywords as entities
		var entities = new List<Entity>();
		var entityMap = new Dictionary<string, Entity>(StringComparer.OrdinalIgnoreCase);

		foreach (var chunk in chunkList)
		{
			cancellationToken.ThrowIfCancellationRequested();

			var content = GetChunkContent(chunk);
			if (string.IsNullOrWhiteSpace(content))
			{
				continue;
			}

			var chunkTerms = ExtractTerms(content);
			var chunkTfIdf = CalculateChunkTfIdf(chunkTerms, documentFrequencies, chunkList.Count);

			// Get top keywords for this chunk
			var topKeywords = chunkTfIdf
				.Where(kvp => kvp.Value >= minConfidence)
				.OrderByDescending(kvp => kvp.Value)
				.Take(maxKeywords)
				.ToList();

			foreach (var (term, score) in topKeywords)
			{
				var normalizedTerm = term.ToLowerInvariant();

				if (!entityMap.TryGetValue(normalizedTerm, out var entity))
				{
					entity = new Entity(EntityType.Keyword, term, score)
					{
						Metadata = new EntityMetadata
						{
							ExtractorName = Name,
							ExtractorVersion = Version,
							ExtractedAt = DateTimeOffset.UtcNow
						}
					};
					entityMap[normalizedTerm] = entity;
					entities.Add(entity);
				}
				else
				{
					// Update frequency and confidence
					entity.Frequency++;
					entity.Confidence = Math.Max(entity.Confidence, score);
				}

				// Add source reference
				var position = content.IndexOf(term, StringComparison.OrdinalIgnoreCase);
				if (position >= 0)
				{
					entity.AddSource(chunk.Id, position, GetContext(content, position, term.Length));
				}
			}
		}

		// Update final frequencies
		foreach (var entity in entities)
		{
			if (entity.Frequency == 0)
			{
				entity.Frequency = 1;
			}
		}

		return await Task.FromResult(entities);
	}

	private static string GetChunkContent(ChunkerBase chunk)
	{
		// Try to get content from different chunk types
		if (chunk is ContentChunk contentChunk)
		{
			return contentChunk.Content;
		}

		// For other chunk types, return empty string
		return string.Empty;
	}

	/// <inheritdoc/>
	public async Task<List<Entity>> ExtractEntitiesAsync(
		ChunkerBase chunk,
		CancellationToken cancellationToken) => await ExtractEntitiesAsync([chunk], cancellationToken);

	private Dictionary<string, int> CalculateTermFrequencies(List<ChunkerBase> chunks)
	{
		var frequencies = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

		foreach (var chunk in chunks)
		{
			var content = GetChunkContent(chunk);
			var terms = ExtractTerms(content);
			foreach (var term in terms)
			{
				frequencies[term] = frequencies.GetValueOrDefault(term) + 1;
			}
		}

		return frequencies;
	}

	private Dictionary<string, int> CalculateDocumentFrequencies(List<ChunkerBase> chunks)
	{
		var frequencies = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

		foreach (var chunk in chunks)
		{
			var content = GetChunkContent(chunk);
			var uniqueTerms = ExtractTerms(content).Distinct(StringComparer.OrdinalIgnoreCase);
			foreach (var term in uniqueTerms)
			{
				frequencies[term] = frequencies.GetValueOrDefault(term) + 1;
			}
		}

		return frequencies;
	}

	private static Dictionary<string, double> CalculateTfIdf(
		Dictionary<string, int> termFrequencies,
		Dictionary<string, int> documentFrequencies,
		int totalDocuments)
	{
		var tfidf = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);

		foreach (var (term, tf) in termFrequencies)
		{
			var df = documentFrequencies[term];
			var idf = Math.Log((double)totalDocuments / df);
			tfidf[term] = tf * idf;
		}

		// Normalize to 0-1 range
		if (tfidf.Count > 0)
		{
			var maxScore = tfidf.Values.Max();
			if (maxScore > 0)
			{
				foreach (var term in tfidf.Keys.ToList())
				{
					tfidf[term] /= maxScore;
				}
			}
		}

		return tfidf;
	}

	private static Dictionary<string, double> CalculateChunkTfIdf(
		List<string> terms,
		Dictionary<string, int> documentFrequencies,
		int totalDocuments)
	{
		var termFrequency = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
		foreach (var term in terms)
		{
			termFrequency[term] = termFrequency.GetValueOrDefault(term) + 1;
		}

		var tfidf = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
		foreach (var (term, tf) in termFrequency)
		{
			if (documentFrequencies.TryGetValue(term, out var df))
			{
				var idf = Math.Log((double)totalDocuments / df);
				tfidf[term] = tf * idf;
			}
		}

		// Normalize to 0-1 range
		if (tfidf.Count > 0)
		{
			var maxScore = tfidf.Values.Max();
			if (maxScore > 0)
			{
				foreach (var term in tfidf.Keys.ToList())
				{
					tfidf[term] /= maxScore;
				}
			}
		}

		return tfidf;
	}

	private List<string> ExtractTerms(string text)
	{
		if (string.IsNullOrWhiteSpace(text))
		{
			return [];
		}

		// Extract words (alphanumeric sequences)
		var words = WordRegex().Matches(text)
			.Cast<Match>()
			.Select(m => m.Value)
			.Where(w => w.Length >= minWordLength)
			.Where(w => !_stopWords.Contains(w))
			.ToList();

		return words;
	}

	private static string GetContext(string text, int position, int length, int contextSize = 50)
	{
		var start = Math.Max(0, position - contextSize);
		var end = Math.Min(text.Length, position + length + contextSize);
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
}
