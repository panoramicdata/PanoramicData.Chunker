using PanoramicData.Chunker.Models.KnowledgeGraph;

namespace PanoramicData.Chunker.KnowledgeGraph.Extractors;

public partial class PatternBasedRelationshipExtractor
{
	private static (Entity First, Entity Second, int FirstPos, int SecondPos) OrderByPosition(Entity entity1, Entity entity2, int pos1, int pos2)
		=> pos1 < pos2 ? (entity1, entity2, pos1, pos2) : (entity2, entity1, pos2, pos1);

	private void AddOrUpdateRelationship(
		Dictionary<string, Relationship> relationshipMap,
		List<Relationship> relationships,
		DetectedRelationship detected,
		Guid chunkId)
	{
		var key = GetRelationshipKey(detected.FromEntityId, detected.ToEntityId, detected.Type);

		if (!relationshipMap.TryGetValue(key, out var relationship))
		{
			relationship = new Relationship(
				detected.FromEntityId,
				detected.ToEntityId,
				detected.Type,
				weight: 1.0,
				confidence: detected.Confidence)
			{
				Bidirectional = !detected.IsDirectional,
				Metadata = new RelationshipMetadata
				{
					ExtractorName = Name,
					ExtractorVersion = Version,
					ExtractedAt = DateTimeOffset.UtcNow
				}
			};

			relationship.Properties["DetectionMethod"] = "PatternMatching";
			if (!string.IsNullOrWhiteSpace(detected.PatternName))
			{
				relationship.Properties["PatternName"] = detected.PatternName;
			}

			relationshipMap[key] = relationship;
			relationships.Add(relationship);
		}
		else
		{
			// Update existing relationship
			relationship.Weight += 1.0;
			relationship.Confidence = Math.Max(relationship.Confidence, detected.Confidence);
		}

		// Add evidence
		relationship.AddEvidence(chunkId, detected.Context, detected.Confidence);
	}

	private static string GetTextBetween(string content, int start, int end, int entityLength)
	{
		var extractStart = start + entityLength;
		var extractEnd = end;

		if (extractStart >= extractEnd || extractStart >= content.Length)
		{
			return string.Empty;
		}

		extractEnd = Math.Min(extractEnd, content.Length);
		return content[extractStart..extractEnd].Trim();
	}

	private static string GetRelationshipKey(Guid entityId1, Guid entityId2, RelationshipType type) =>
		// For directional relationships, order matters
		$"{entityId1}_{entityId2}_{type}";
}
