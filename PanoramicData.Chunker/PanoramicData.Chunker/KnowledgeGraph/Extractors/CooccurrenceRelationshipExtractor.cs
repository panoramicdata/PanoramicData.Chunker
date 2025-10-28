using PanoramicData.Chunker.Interfaces.KnowledgeGraph;
using PanoramicData.Chunker.Models;
using PanoramicData.Chunker.Models.KnowledgeGraph;

namespace PanoramicData.Chunker.KnowledgeGraph.Extractors;

/// <summary>
/// Extracts relationships between entities based on their co-occurrence in chunks.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="CooccurrenceRelationshipExtractor"/> class.
/// </remarks>
/// <param name="maxDistance">Maximum character distance between entities to consider them related.</param>
/// <param name="minConfidence">Minimum confidence score for relationships.</param>
public class CooccurrenceRelationshipExtractor(int maxDistance = 500, double minConfidence = 0.3) : IRelationshipExtractor
{

	/// <inheritdoc/>
	public string Name => "CooccurrenceRelationshipExtractor";

	/// <inheritdoc/>
	public string Version => "1.0";

	/// <inheritdoc/>
	public IReadOnlyList<RelationshipType> SupportedRelationshipTypes { get; } =
	[
		RelationshipType.Mentions,
		RelationshipType.CooccursWith,
		RelationshipType.RelatedTo
	];

	/// <inheritdoc/>
	public async Task<List<Relationship>> ExtractRelationshipsAsync(
		IEnumerable<Entity> entities,
		IEnumerable<ChunkerBase> chunks,
		CancellationToken cancellationToken = default)
	{
		var entityList = entities.ToList();
		var chunkList = chunks.ToList();

		if (entityList.Count < 2 || chunkList.Count == 0)
		{
			return [];
		}

		// Build entity-to-chunks index
		var entityChunks = BuildEntityChunkIndex(entityList);

		// Extract relationships based on co-occurrence
		var relationships = new List<Relationship>();
		var relationshipMap = new Dictionary<string, Relationship>();

		foreach (var chunk in chunkList)
		{
			cancellationToken.ThrowIfCancellationRequested();

			// Find all entities that appear in this chunk
			var chunkEntities = entityList
				.Where(e => e.Sources.Any(s => s.ChunkId == chunk.Id))
				.ToList();

			if (chunkEntities.Count < 2)
			{
				continue;
			}

			var content = GetChunkContent(chunk);
			if (string.IsNullOrWhiteSpace(content))
			{
				continue;
			}

			// Create relationships for all pairs
			for (var i = 0; i < chunkEntities.Count; i++)
			{
				for (var j = i + 1; j < chunkEntities.Count; j++)
				{
					var entity1 = chunkEntities[i];
					var entity2 = chunkEntities[j];

					// Get positions in this chunk
					var positions1 = entity1.Sources
						.Where(s => s.ChunkId == chunk.Id)
						.Select(s => s.Position)
						.ToList();

					var positions2 = entity2.Sources
						.Where(s => s.ChunkId == chunk.Id)
						.Select(s => s.Position)
						.ToList();

					// Calculate minimum distance between entities in this chunk
					var minDistance = CalculateMinDistance(positions1, positions2);

					if (minDistance <= maxDistance)
					{
						// Calculate confidence based on distance (closer = higher confidence)
						var confidence = CalculateConfidence(minDistance);

						if (confidence >= minConfidence)
						{
							// Create or update relationship
							var relationshipKey = GetRelationshipKey(entity1.Id, entity2.Id);

							if (!relationshipMap.TryGetValue(relationshipKey, out var relationship))
							{
								relationship = new Relationship(
									entity1.Id,
									entity2.Id,
									RelationshipType.Mentions,
									weight: 1.0,
									confidence: confidence)
								{
									Bidirectional = true,
									Metadata = new RelationshipMetadata
									{
										ExtractorName = Name,
										ExtractorVersion = Version,
										ExtractedAt = DateTimeOffset.UtcNow
									}
								};
								relationshipMap[relationshipKey] = relationship;
								relationships.Add(relationship);
							}
							else
							{
								// Update existing relationship
								relationship.Weight += 1.0;
								relationship.Confidence = Math.Max(relationship.Confidence, confidence);
							}

							// Add evidence
							relationship.AddEvidence(
								chunk.Id,
								GetContext(content, positions1.Min(), positions2.Max()),
								confidence);

							// Store distance in properties
							if (!relationship.Properties.TryGetValue("MinDistance", out var value))
							{
								relationship.Properties["MinDistance"] = minDistance;
							}
							else
							{
								relationship.Properties["MinDistance"] = Math.Min(
									(int)value,
									minDistance);
							}
						}
					}
				}
			}
		}

		// Normalize relationship weights based on co-occurrence frequency
		if (relationships.Count > 0)
		{
			var maxWeight = relationships.Max(r => r.Weight);
			foreach (var relationship in relationships)
			{
				relationship.Weight /= maxWeight;
			}
		}

		return await Task.FromResult(relationships);
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

	private static Dictionary<Guid, List<Guid>> BuildEntityChunkIndex(List<Entity> entities)
	{
		var index = new Dictionary<Guid, List<Guid>>();

		foreach (var entity in entities)
		{
			var chunkIds = entity.Sources.Select(s => s.ChunkId).Distinct().ToList();
			index[entity.Id] = chunkIds;
		}

		return index;
	}

	private static int CalculateMinDistance(List<int> positions1, List<int> positions2)
	{
		var minDistance = int.MaxValue;

		foreach (var pos1 in positions1)
		{
			foreach (var pos2 in positions2)
			{
				var distance = Math.Abs(pos1 - pos2);
				minDistance = Math.Min(minDistance, distance);
			}
		}

		return minDistance;
	}

	private double CalculateConfidence(int distance)
	{
		// Confidence decreases with distance
		// Close entities (< 50 chars) get high confidence (0.9+)
		// Distant entities (near maxDistance) get low confidence (near minConfidence)

		if (distance <= 50)
		{
			return 1.0;
		}

		if (distance >= maxDistance)
		{
			return minConfidence;
		}

		// Linear interpolation
		var ratio = (double)distance / maxDistance;
		return 1.0 - (ratio * (1.0 - minConfidence));
	}

	private static string GetRelationshipKey(Guid entityId1, Guid entityId2)
	{
		// Create a consistent key regardless of order
		var ids = new[] { entityId1, entityId2 }.OrderBy(id => id).ToArray();
		return $"{ids[0]}_{ids[1]}";
	}

	private static string GetContext(string text, int startPos, int endPos, int contextSize = 100)
	{
		var start = Math.Max(0, startPos - contextSize);
		var end = Math.Min(text.Length, endPos + contextSize);
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
