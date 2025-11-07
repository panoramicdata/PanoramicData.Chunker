using Ollama.Api;
using PanoramicData.Chunker.Interfaces.KnowledgeGraph;
using PanoramicData.Chunker.Models;
using PanoramicData.Chunker.Models.KnowledgeGraph;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace PanoramicData.Chunker.KnowledgeGraph.Extractors;

/// <summary>
/// LLM-powered entity extractor using Ollama for Named Entity Recognition (NER).
/// Provides state-of-the-art entity extraction with proper type classification.
/// </summary>
/// <remarks>
/// This extractor uses local Ollama models for NER, offering:
/// - High accuracy entity detection
/// - Proper entity type classification
/// - Multi-word entity support
/// - Context-aware extraction
/// - No external API costs
/// 
/// Requires: Ollama running locally with a capable model (e.g., llama3, mistral, phi3)
/// </remarks>
/// <param name="ollamaEndpoint">Ollama API endpoint (default: http://localhost:11434)</param>
/// <param name="modelName">Model to use for extraction (default: llama3.2)</param>
/// <param name="temperature">Temperature for generation (default: 0.1 for deterministic)</param>
/// <param name="maxTokensPerChunk">Maximum tokens to send per chunk (default: 2000)</param>
public class OllamaEntityExtractor(
	string ollamaEndpoint = "http://localhost:11434",
	string modelName = "llama3.2",
	double temperature = 0.1,
	int maxTokensPerChunk = 2000) : IEntityExtractor
{
	private readonly OllamaClient _client = new(new OllamaClientOptions
	{
		Uri = new Uri(ollamaEndpoint)
	});
	private readonly string _modelName = modelName;
	private readonly double _temperature = temperature;
	private readonly int _maxTokensPerChunk = maxTokensPerChunk;

	/// <inheritdoc/>
	public string Name => "OllamaEntityExtractor";

	/// <inheritdoc/>
	public string Version => "1.0";

	/// <inheritdoc/>
	public IReadOnlyList<EntityType> SupportedEntityTypes { get; } =
	[
		EntityType.Person,
		EntityType.Organization,
		EntityType.Location,
		EntityType.Date,
		EntityType.Event,
		EntityType.Work,
		EntityType.Product,
		EntityType.ProperNoun
	];

	/// <inheritdoc/>
	public async Task<List<Entity>> ExtractEntitiesAsync(
		IEnumerable<ChunkerBase> chunks,
		CancellationToken cancellationToken)
	{
		var allEntities = new Dictionary<string, Entity>(StringComparer.Ordinal);

		foreach (var chunk in chunks)
		{
			cancellationToken.ThrowIfCancellationRequested();

			var content = GetChunkContent(chunk);
			if (string.IsNullOrWhiteSpace(content) || content.Length < 10)
			{
				continue;
			}

			// Truncate if too long
			if (content.Length > _maxTokensPerChunk * 4) // Rough estimate: 4 chars per token
			{
				content = content[..(_maxTokensPerChunk * 4)];
			}

			try
			{
				var extractedEntities = await ExtractFromTextAsync(content, chunk.Id, cancellationToken);

				// Merge entities
				foreach (var entity in extractedEntities)
				{
					if (allEntities.TryGetValue(entity.Name, out var existing))
					{
						// Merge
						existing.Frequency++;
						existing.Sources.AddRange(entity.Sources);
						existing.Confidence = Math.Max(existing.Confidence, entity.Confidence);
					}
					else
					{
						allEntities[entity.Name] = entity;
					}
				}
			}
			catch (Exception ex)
			{
				// Log but continue processing
				Console.WriteLine($"Error extracting entities from chunk {chunk.Id}: {ex.Message}");
			}
		}

		return [.. allEntities.Values.OrderByDescending(e => e.Confidence)];
	}

	/// <inheritdoc/>
	public async Task<List<Entity>> ExtractEntitiesAsync(
		ChunkerBase chunk,
		CancellationToken cancellationToken)
		=> await ExtractEntitiesAsync([chunk], cancellationToken);

	private async Task<List<Entity>> ExtractFromTextAsync(
		string text,
		Guid chunkId,
		CancellationToken cancellationToken)
	{
		var prompt = BuildNerPrompt(text);

		// Use Ollama.Api - GenerateAsync with GenerateRequest
		var request = new Ollama.Api.Models.GenerateRequest
		{
			Model = _modelName,
			Prompt = prompt,
			Stream = false,
			Options = new Ollama.Api.Models.GenerateOptions
			{
				Temperature = (float)_temperature,
				NumPredict = 1000
			}
		};

		var response = await _client.Generate.GenerateAsync(request, cancellationToken);

		if (response == null || string.IsNullOrWhiteSpace(response.Response))
		{
			return [];
		}

		return ParseNerResponse(response.Response, text, chunkId);
	}

	private static string BuildNerPrompt(string text) =>
		// Use $$ for raw string interpolation with JSON
		$$"""
You are a Named Entity Recognition (NER) expert. Extract all entities from the following text and classify them.

ENTITY TYPES:
- PERSON: People's names (e.g., "Charles Darwin", "Professor Jameson")
- ORGANIZATION: Organizations, societies, institutions (e.g., "Plinian Society", "Edinburgh University")
- LOCATION: Places, cities, countries (e.g., "Edinburgh", "Galapagos Islands")
- WORK: Books, publications, creative works (e.g., "Origin of Species", "Voyage of the Beagle")
- EVENT: Historical events, voyages, expeditions
- DATE: Dates and time periods

TEXT:
{{text}}

INSTRUCTIONS:
1. Extract ALL entities, including rare ones that appear only once
2. Preserve exact capitalization and punctuation (e.g., "'Beagle'", "HMS Beagle")
3. Include titles with names (e.g., "Professor Jameson", "Captain FitzRoy")
4. Include full multi-word entities (e.g., "Plinian Society", not just "Society")
5. Output ONLY valid JSON in this exact format:

{
  "entities": [
    {"name": "Charles Darwin", "type": "PERSON"},
    {"name": "Plinian Society", "type": "ORGANIZATION"},
    {"name": "Edinburgh University", "type": "ORGANIZATION"}
  ]
}

OUTPUT (JSON only, no explanation):
""";

	private List<Entity> ParseNerResponse(string response, string sourceText, Guid chunkId)
	{
		var entities = new List<Entity>();

		try
		{
			// Try to extract JSON from response
			var jsonMatch = Regex.Match(response, @"\{.*\}", RegexOptions.Singleline);
			if (!jsonMatch.Success)
			{
				// Try to find just the entities array
				jsonMatch = Regex.Match(response, @"""entities"":\s*\[(.*?)\]", RegexOptions.Singleline);
				if (jsonMatch.Success)
				{
					response = $"{{\"entities\":[{jsonMatch.Groups[1].Value}]}}";
				}
				else
				{
					return entities;
				}
			}
			else
			{
				response = jsonMatch.Value;
			}

			var doc = JsonDocument.Parse(response);
			if (!doc.RootElement.TryGetProperty("entities", out var entitiesArray))
			{
				return entities;
			}

			foreach (var entityElement in entitiesArray.EnumerateArray())
			{
				if (!entityElement.TryGetProperty("name", out var nameElement) ||
					!entityElement.TryGetProperty("type", out var typeElement))
				{
					continue;
				}

				var name = nameElement.GetString();
				var typeStr = typeElement.GetString();

				if (string.IsNullOrWhiteSpace(name))
				{
					continue;
				}

				var entityType = MapEntityType(typeStr ?? "UNKNOWN");

				// Find position in source text
				var position = sourceText.IndexOf(name, StringComparison.OrdinalIgnoreCase);
				if (position < 0)
				{
					// Try case-sensitive
					position = sourceText.IndexOf(name, StringComparison.Ordinal);
				}

				var entity = new Entity(entityType, name, confidence: 0.9)
				{
					Frequency = 1,
					Metadata = new EntityMetadata
					{
						ExtractorName = Name,
						ExtractorVersion = Version,
						ExtractedAt = DateTimeOffset.UtcNow,
						ExtractionDetails = new Dictionary<string, object>
						{
							["extraction_method"] = "llm_ner",
							["model"] = _modelName,
							["llm_classified_type"] = typeStr ?? "UNKNOWN"
						}
					}
				};

				if (position >= 0)
				{
					entity.AddSource(chunkId, position, GetContext(sourceText, position, name.Length));
				}

				// Generate aliases
				entity.Aliases = GenerateAliases(name);

				entities.Add(entity);
			}
		}
		catch (JsonException ex)
		{
			Console.WriteLine($"Failed to parse NER response as JSON: {ex.Message}");
			Console.WriteLine($"Response: {response}");
		}

		return entities;
	}

	private static EntityType MapEntityType(string llmType) => llmType.ToUpperInvariant() switch
	{
		"PERSON" => EntityType.Person,
		"ORGANIZATION" or "ORG" => EntityType.Organization,
		"LOCATION" or "LOC" or "GPE" => EntityType.Location,
		"DATE" or "TIME" => EntityType.Date,
		"EVENT" => EntityType.Event,
		"WORK" or "WORK_OF_ART" => EntityType.Work,
		"PRODUCT" => EntityType.Product,
		_ => EntityType.ProperNoun
	};

	private static List<string> GenerateAliases(string name)
	{
		var aliases = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

		// Remove quotes
		if (name.Contains('\'') || name.Contains('"'))
		{
			aliases.Add(name.Replace("'", "").Replace("\"", ""));
		}

		// HMS/USS prefix
		if (name.StartsWith("HMS ", StringComparison.OrdinalIgnoreCase) ||
			name.StartsWith("USS ", StringComparison.OrdinalIgnoreCase))
		{
			aliases.Add(name[4..]);
		}

		// Multi-word: last name
		if (name.Contains(' '))
		{
			var words = name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
			if (words.Length > 1 && words[^1].Length > 2)
			{
				aliases.Add(words[^1]);
			}
		}

		// Title removal
		var titlePrefixes = new[] { "Professor", "Captain", "Dr.", "Dr", "Sir", "Lord", "Mr.", "Mr" };
		foreach (var prefix in titlePrefixes)
		{
			if (name.StartsWith(prefix + " ", StringComparison.OrdinalIgnoreCase))
			{
				aliases.Add(name[(prefix.Length + 1)..]);
			}
		}

		aliases.Remove(name);
		return [.. aliases.Where(a => !string.IsNullOrWhiteSpace(a))];
	}

	private static string GetChunkContent(ChunkerBase chunk)
	{
		if (chunk is ContentChunk contentChunk)
		{
			return contentChunk.Content;
		}
		return string.Empty;
	}

	private static string GetContext(string text, int position, int length, int contextSize = 100)
	{
		var start = Math.Max(0, position - contextSize);
		var end = Math.Min(text.Length, position + length + contextSize);
		var context = text[start..end];

		if (start > 0) context = "..." + context;
		if (end < text.Length) context += "...";

		return context;
	}

	private class OllamaResponse
	{
		public string? Response { get; set; }
		public bool Done { get; set; }
	}
}
