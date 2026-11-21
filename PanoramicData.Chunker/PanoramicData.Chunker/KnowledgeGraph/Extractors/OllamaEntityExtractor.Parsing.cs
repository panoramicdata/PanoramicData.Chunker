using PanoramicData.Chunker.Models.KnowledgeGraph;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace PanoramicData.Chunker.KnowledgeGraph.Extractors;

public partial class OllamaEntityExtractor
{
	private List<Entity> ParseNerResponse(string response, string sourceText, Guid chunkId)
	{
		var json = ExtractEntitiesJson(response);
		if (json == null)
		{
			return [];
		}

		try
		{
			using var doc = JsonDocument.Parse(json);

			return doc.RootElement.TryGetProperty("entities", out var entitiesArray)
				? [.. entitiesArray.EnumerateArray()
					.Select(element => CreateEntity(element, sourceText, chunkId))
					.OfType<Entity>()]
				: [];
		}
		catch (JsonException ex)
		{
			Console.WriteLine($"Failed to parse NER response as JSON: {ex.Message}");
			Console.WriteLine($"Response: {json}");
			return [];
		}
	}

	private static string? ExtractEntitiesJson(string response)
	{
		var jsonMatch = Regex.Match(response, @"\{.*\}", RegexOptions.Singleline);
		if (jsonMatch.Success)
		{
			return jsonMatch.Value;
		}

		// Fall back to just the entities array
		var arrayMatch = Regex.Match(response, @"""entities"":\s*\[(.*?)\]", RegexOptions.Singleline);
		return arrayMatch.Success ? $"{{\"entities\":[{arrayMatch.Groups[1].Value}]}}" : null;
	}

	private Entity? CreateEntity(JsonElement element, string sourceText, Guid chunkId)
	{
		if (!element.TryGetProperty("name", out var nameElement) || !element.TryGetProperty("type", out var typeElement))
		{
			return null;
		}

		var name = nameElement.GetString();
		if (string.IsNullOrWhiteSpace(name))
		{
			return null;
		}

		var typeStr = typeElement.GetString();

		var entity = new Entity(MapEntityType(typeStr ?? "UNKNOWN"), name, confidence: 0.9)
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
			},
			Aliases = GenerateAliases(name)
		};

		var position = sourceText.IndexOf(name, StringComparison.OrdinalIgnoreCase);
		if (position >= 0)
		{
			entity.AddSource(chunkId, position, ExtractorHelpers.GetSnippet(sourceText, position, name.Length, 100));
		}

		return entity;
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

	private static readonly string[] TitlePrefixes = ["Professor", "Captain", "Dr.", "Dr", "Sir", "Lord", "Mr.", "Mr"];

	private static List<string> GenerateAliases(string name)
	{
		var aliases = ExtractorHelpers.CreateAliasSet(name);

		if (name.StartsWith("HMS ", StringComparison.OrdinalIgnoreCase) || name.StartsWith("USS ", StringComparison.OrdinalIgnoreCase))
		{
			aliases.Add(name[4..]);
		}

		var lastName = GetLastName(name);
		if (lastName != null)
		{
			aliases.Add(lastName);
		}

		ExtractorHelpers.AddTitleAliases(aliases, name, TitlePrefixes);
		return ExtractorHelpers.FinishAliases(aliases, name);
	}

	private static string? GetLastName(string name)
	{
		var words = name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
		return words.Length > 1 && words[^1].Length > 2 ? words[^1] : null;
	}

	private class OllamaResponse
	{
		public string? Response { get; set; }
		public bool Done { get; set; }
	}
}
