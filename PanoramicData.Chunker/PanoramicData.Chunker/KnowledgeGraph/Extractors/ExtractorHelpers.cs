using PanoramicData.Chunker.Models;
using PanoramicData.Chunker.Models.KnowledgeGraph;

namespace PanoramicData.Chunker.KnowledgeGraph.Extractors;

/// <summary>
/// Entity pair found within one chunk, with the positions of each entity.
/// </summary>
internal sealed record EntityPair(
	Entity Entity1,
	Entity Entity2,
	List<int> Positions1,
	List<int> Positions2,
	int MinDistance,
	string Content);

/// <summary>
/// Shared logic for the relationship extractors.
/// </summary>
internal static partial class ExtractorHelpers
{
	public static string GetChunkContent(ChunkerBase chunk)
		=> chunk is ContentChunk contentChunk ? contentChunk.Content : string.Empty;

	/// <summary>
	/// Yields every entity pair that appears in the chunk within <paramref name="maxDistance"/> characters.
	/// </summary>
	public static IEnumerable<EntityPair> GetEntityPairs(List<Entity> entities, ChunkerBase chunk, int maxDistance)
	{
		var chunkEntities = entities
			.Where(e => e.Sources.Any(s => s.ChunkId == chunk.Id))
			.ToList();

		var content = GetChunkContent(chunk);
		if (chunkEntities.Count < 2 || string.IsNullOrWhiteSpace(content))
		{
			yield break;
		}

		for (var i = 0; i < chunkEntities.Count; i++)
		{
			for (var j = i + 1; j < chunkEntities.Count; j++)
			{
				var positions1 = GetPositions(chunkEntities[i], chunk.Id);
				var positions2 = GetPositions(chunkEntities[j], chunk.Id);
				var minDistance = CalculateMinDistance(positions1, positions2);

				if (minDistance <= maxDistance)
				{
					yield return new EntityPair(chunkEntities[i], chunkEntities[j], positions1, positions2, minDistance, content);
				}
			}
		}
	}

	public static int CalculateMinDistance(List<int> positions1, List<int> positions2)
		=> positions1.SelectMany(_ => positions2, (pos1, pos2) => Math.Abs(pos1 - pos2)).Prepend(int.MaxValue).Min();

	public static string GetContext(string text, int startPos, int endPos, int contextSize)
	{
		if (startPos > endPos)
		{
			(startPos, endPos) = (endPos, startPos);
		}

		var start = Math.Max(0, startPos - contextSize);
		var end = Math.Min(text.Length, endPos + contextSize);

		if (start >= text.Length || end <= 0 || start >= end)
		{
			return string.Empty;
		}

		var prefix = start > 0 ? "..." : string.Empty;
		var suffix = end < text.Length ? "..." : string.Empty;
		return prefix + text[start..end] + suffix;
	}

	public static void NormalizeWeights(List<Relationship> relationships)
	{
		if (relationships.Count == 0)
		{
			return;
		}

		var maxWeight = relationships.Max(r => r.Weight);
		if (maxWeight <= 0)
		{
			return;
		}

		foreach (var relationship in relationships)
		{
			relationship.Weight /= maxWeight;
		}
	}

	public static double InterpolateConfidence(int distance, int maxDistance, double minConfidence)
	{
		if (distance <= 50)
		{
			return 1.0;
		}

		if (distance >= maxDistance)
		{
			return minConfidence;
		}

		return 1.0 - ((double)distance / maxDistance * (1.0 - minConfidence));
	}

	private static List<int> GetPositions(Entity entity, Guid chunkId)
		=> [.. entity.Sources.Where(s => s.ChunkId == chunkId).Select(s => s.Position)];
}
