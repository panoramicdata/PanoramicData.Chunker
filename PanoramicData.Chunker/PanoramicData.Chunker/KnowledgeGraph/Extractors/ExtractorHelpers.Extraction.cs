using PanoramicData.Chunker.Models;
using PanoramicData.Chunker.Models.KnowledgeGraph;

namespace PanoramicData.Chunker.KnowledgeGraph.Extractors;

internal static partial class ExtractorHelpers
{
	/// <summary>
	/// Runs the shared relationship extraction loop, handing each nearby entity pair to <paramref name="addPair"/>.
	/// </summary>
	public static List<Relationship> CollectRelationships(
		IEnumerable<Entity> entities,
		IEnumerable<ChunkerBase> chunks,
		int maxDistance,
		Action<EntityPair, Guid, List<Relationship>, Dictionary<string, Relationship>> addPair,
		CancellationToken cancellationToken)
	{
		var entityList = entities.ToList();
		var chunkList = chunks.ToList();

		if (entityList.Count < 2 || chunkList.Count == 0)
		{
			return [];
		}

		var relationships = new List<Relationship>();
		var relationshipMap = new Dictionary<string, Relationship>();

		foreach (var chunk in chunkList)
		{
			cancellationToken.ThrowIfCancellationRequested();

			foreach (var pair in GetEntityPairs(entityList, chunk, maxDistance))
			{
				addPair(pair, chunk.Id, relationships, relationshipMap);
			}
		}

		NormalizeWeights(relationships);

		return relationships;
	}

	public static string GetSnippet(string text, int position, int length, int contextSize)
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

	public static HashSet<string> CreateAliasSet(string name)
	{
		var aliases = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

		if (name.Contains('\'') || name.Contains('"'))
		{
			aliases.Add(name.Replace("'", "").Replace("\"", ""));
		}

		return aliases;
	}

	public static void AddTitleAliases(HashSet<string> aliases, string name, string[] titlePrefixes)
	{
		foreach (var prefix in titlePrefixes.Where(prefix => name.StartsWith(prefix + " ", StringComparison.OrdinalIgnoreCase)))
		{
			aliases.Add(name[(prefix.Length + 1)..]);
		}
	}

	public static List<string> FinishAliases(HashSet<string> aliases, string name)
	{
		aliases.Remove(name);
		return [.. aliases.Where(a => !string.IsNullOrWhiteSpace(a))];
	}
}
