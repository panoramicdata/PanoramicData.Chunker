using System.Runtime.InteropServices;
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
		[Optional] CancellationToken cancellationToken)
	{
		var relationships = ExtractorHelpers.CollectRelationships(entities, chunks, maxDistance, AddCooccurrence, cancellationToken);
		return await Task.FromResult(relationships);
	}

	private void AddCooccurrence(
		EntityPair pair,
		Guid chunkId,
		List<Relationship> relationships,
		Dictionary<string, Relationship> relationshipMap)
	{
		var confidence = ExtractorHelpers.InterpolateConfidence(pair.MinDistance, maxDistance, minConfidence);
		if (confidence < minConfidence)
		{
			return;
		}

		var key = GetRelationshipKey(pair.Entity1.Id, pair.Entity2.Id);
		if (relationshipMap.TryGetValue(key, out var relationship))
		{
			relationship.Weight += 1.0;
			relationship.Confidence = Math.Max(relationship.Confidence, confidence);
		}
		else
		{
			relationship = CreateRelationship(pair, confidence);
			relationshipMap[key] = relationship;
			relationships.Add(relationship);
		}

		relationship.AddEvidence(
			chunkId,
			ExtractorHelpers.GetContext(pair.Content, pair.Positions1.Min(), pair.Positions2.Max(), 100),
			confidence);

		relationship.Properties["MinDistance"] = relationship.Properties.TryGetValue("MinDistance", out var existing)
			? Math.Min((int)existing, pair.MinDistance)
			: pair.MinDistance;
	}

	private Relationship CreateRelationship(EntityPair pair, double confidence) => new(
		pair.Entity1.Id,
		pair.Entity2.Id,
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

	private static string GetRelationshipKey(Guid entityId1, Guid entityId2)
	{
		// Same key regardless of order
		var ids = new[] { entityId1, entityId2 }.OrderBy(id => id).ToArray();
		return $"{ids[0]}_{ids[1]}";
	}
}