using PanoramicData.Chunker.Interfaces.KnowledgeGraph;
using PanoramicData.Chunker.Models;
using PanoramicData.Chunker.Models.KnowledgeGraph;
using System.Text.RegularExpressions;

namespace PanoramicData.Chunker.KnowledgeGraph.Extractors;

/// <summary>
/// Extracts relationships between entities using pattern matching and linguistic analysis.
/// Uses regex patterns and contextual clues to identify specific relationship types.
/// </summary>
/// <remarks>
/// This extractor identifies relationships by analyzing the text between entities:
/// - Pattern matching for specific relationship indicators
/// - Verb analysis for action-based relationships
/// - Preposition analysis for spatial/organizational relationships
/// - Distance-based co-occurrence for general relatedness
/// 
/// Supports 15+ relationship types including: Founded, MemberOf, LocatedIn, WorksFor,
/// AuthorOf, PartOf, Creates, Uses, Collaborates, and more.
/// </remarks>
/// <remarks>
/// Initializes a new instance of the <see cref="PatternBasedRelationshipExtractor"/> class.
/// </remarks>
/// <param name="maxDistance">Maximum character distance between entities (default: 500).</param>
/// <param name="minConfidence">Minimum confidence score for relationships (default: 0.3).</param>
/// <param name="enablePatternMatching">Enable pattern-based relationship detection (default: true).</param>
/// <param name="enableProximityRelationships">Enable distance-based co-occurrence relationships (default: true).</param>
public partial class PatternBasedRelationshipExtractor(
	int maxDistance = 500,
	double minConfidence = 0.3,
	bool enablePatternMatching = true,
	bool enableProximityRelationships = true) : IRelationshipExtractor
{

	// Relationship patterns: (pattern, relationshipType, confidence, isDirectional)
	private readonly List<RelationshipPattern> _patterns = BuildPatterns();

	/// <inheritdoc/>
	public string Name => "PatternBasedRelationshipExtractor";

	/// <inheritdoc/>
	public string Version => "1.0";

	/// <inheritdoc/>
	public IReadOnlyList<RelationshipType> SupportedRelationshipTypes { get; } =
	[
		RelationshipType.Founded,
		RelationshipType.MemberOf,
		RelationshipType.LocatedIn,
		RelationshipType.WorksFor,
		RelationshipType.AuthorOf,
		RelationshipType.PartOf,
		RelationshipType.Creates,
		RelationshipType.Uses,
		RelationshipType.CollaboratesWith,
		RelationshipType.Owns,
		RelationshipType.Manages,
		RelationshipType.Influences,
		RelationshipType.Supports,
		RelationshipType.RelatedTo,
		RelationshipType.Mentions,
		RelationshipType.CooccursWith
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

			// Extract relationships for all entity pairs
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

					// Calculate minimum distance
					var minDistance = CalculateMinDistance(positions1, positions2);

					if (minDistance > maxDistance)
					{
						continue;
					}

					// Try pattern-based extraction first
					List<DetectedRelationship>? detectedRelationships = null;
					if (enablePatternMatching)
					{
						detectedRelationships = DetectPatternBasedRelationships(
							content,
							entity1,
							entity2,
							positions1,
							positions2);
					}

					// If no patterns matched and proximity is enabled, create co-occurrence relationship
					if ((detectedRelationships == null || detectedRelationships.Count == 0) && enableProximityRelationships)
					{
						var proximityConfidence = CalculateProximityConfidence(minDistance);
						if (proximityConfidence >= minConfidence)
						{
							detectedRelationships =
							[
								new DetectedRelationship
								{
									Type = minDistance < 100 ? RelationshipType.Mentions : RelationshipType.CooccursWith,
									FromEntityId = entity1.Id,
									ToEntityId = entity2.Id,
									Confidence = proximityConfidence,
									IsDirectional = false,
									Context = GetContext(content, positions1.Min(), positions2.Max())
								}
							];
						}
					}

					// Add detected relationships
					if (detectedRelationships != null)
					{
						foreach (var detected in detectedRelationships)
						{
							if (detected.Confidence < minConfidence)
							{
								continue;
							}

							AddOrUpdateRelationship(
								relationshipMap,
								relationships,
								detected,
								chunk.Id);
						}
					}
				}
			}
		}

		// Normalize relationship weights
		NormalizeRelationshipWeights(relationships);

		return await Task.FromResult(relationships);
	}

	private List<DetectedRelationship> DetectPatternBasedRelationships(
		string content,
		Entity entity1,
		Entity entity2,
		List<int> positions1,
		List<int> positions2)
	{
		var detected = new List<DetectedRelationship>();

		// Determine order (which entity comes first in text)
		var firstEntity = positions1.Min() < positions2.Min() ? entity1 : entity2;
		var secondEntity = firstEntity == entity1 ? entity2 : entity1;
		var firstPos = firstEntity == entity1 ? positions1.Min() : positions2.Min();
		var secondPos = secondEntity == entity2 ? positions2.Min() : positions1.Min();

		// Extract text between entities
		var betweenText = GetTextBetween(content, firstPos, secondPos, firstEntity.Name.Length);

		if (string.IsNullOrWhiteSpace(betweenText))
		{
			return detected;
		}

		// Check each pattern
		foreach (var pattern in _patterns)
		{
			var match = pattern.Regex.Match(betweenText);
			if (match.Success)
			{
				// Determine directionality
				var fromEntity = pattern.IsDirectional ? firstEntity : entity1;
				var toEntity = pattern.IsDirectional ? secondEntity : entity2;

				detected.Add(new DetectedRelationship
				{
					Type = pattern.Type,
					FromEntityId = fromEntity.Id,
					ToEntityId = toEntity.Id,
					Confidence = pattern.Confidence,
					IsDirectional = pattern.IsDirectional,
					Context = GetContext(content, firstPos, secondPos)
				});

				// If it's a high-confidence pattern, don't check other patterns
				if (pattern.Confidence >= 0.9)
				{
					break;
				}
			}
		}

		return detected;
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

	private static void NormalizeRelationshipWeights(List<Relationship> relationships)
	{
		if (relationships.Count == 0)
		{
			return;
		}

		var maxWeight = relationships.Max(r => r.Weight);
		if (maxWeight > 0)
		{
			foreach (var relationship in relationships)
			{
				relationship.Weight /= maxWeight;
			}
		}
	}

	private static List<RelationshipPattern> BuildPatterns() => [
			// Founded/Founder relationships
			new RelationshipPattern
			{
				Regex = FoundedPattern(),
				Type = RelationshipType.Founded,
				Confidence = 0.95,
				IsDirectional = true
			},

			// Member relationships
			new RelationshipPattern
			{
				Regex = MemberOfPattern(),
				Type = RelationshipType.MemberOf,
				Confidence = 0.9,
				IsDirectional = true
			},

			// Location relationships
			new RelationshipPattern
			{
				Regex = LocatedInPattern(),
				Type = RelationshipType.LocatedIn,
				Confidence = 0.85,
				IsDirectional = true
			},

			// Works for relationships
			new RelationshipPattern
			{
				Regex = WorksForPattern(),
				Type = RelationshipType.WorksFor,
				Confidence = 0.9,
				IsDirectional = true
			},

			// Author/Creator relationships
			new RelationshipPattern
			{
				Regex = AuthorOfPattern(),
				Type = RelationshipType.AuthorOf,
				Confidence = 0.9,
				IsDirectional = true
			},

			// Part of relationships
			new RelationshipPattern
			{
				Regex = PartOfPattern(),
				Type = RelationshipType.PartOf,
				Confidence = 0.85,
				IsDirectional = true
			},

			// Creates relationships
			new RelationshipPattern
			{
				Regex = CreatesPattern(),
				Type = RelationshipType.Creates,
				Confidence = 0.85,
				IsDirectional = true
			},

			// Uses relationships
			new RelationshipPattern
			{
				Regex = UsesPattern(),
				Type = RelationshipType.Uses,
				Confidence = 0.8,
				IsDirectional = true
			},

			// Collaboration relationships
			new RelationshipPattern
			{
				Regex = CollaboratesPattern(),
				Type = RelationshipType.CollaboratesWith,
				Confidence = 0.85,
				IsDirectional = false
			},

			// Ownership relationships
			new RelationshipPattern
			{
				Regex = OwnsPattern(),
				Type = RelationshipType.Owns,
				Confidence = 0.9,
				IsDirectional = true
			},

			// Management relationships
			new RelationshipPattern
			{
				Regex = ManagesPattern(),
				Type = RelationshipType.Manages,
				Confidence = 0.9,
				IsDirectional = true
			},

			// Influence relationships
			new RelationshipPattern
			{
				Regex = InfluencesPattern(),
				Type = RelationshipType.Influences,
				Confidence = 0.75,
				IsDirectional = true
			},

			// Support relationships
			new RelationshipPattern
			{
				Regex = SupportsPattern(),
				Type = RelationshipType.Supports,
				Confidence = 0.8,
				IsDirectional = true
			},

			// Generic related-to (catch-all for "and", "with", etc.)
			new RelationshipPattern
			{
				Regex = RelatedToPattern(),
				Type = RelationshipType.RelatedTo,
				Confidence = 0.6,
				IsDirectional = false
			}
		];

	// Pattern generators (using C# 11+ source generators for regex)
	[GeneratedRegex(@"\b(founded|established|created|started|formed)\b", RegexOptions.IgnoreCase)]
	private static partial Regex FoundedPattern();

	[GeneratedRegex(@"\b(member\s+of|belonged\s+to|part\s+of|joined|attended|participated\s+in)\b", RegexOptions.IgnoreCase)]
	private static partial Regex MemberOfPattern();

	[GeneratedRegex(@"\b(at|in|located\s+in|based\s+in|from)\b", RegexOptions.IgnoreCase)]
	private static partial Regex LocatedInPattern();

	[GeneratedRegex(@"\b(works?\s+for|worked\s+for|employed\s+by|works?\s+at)\b", RegexOptions.IgnoreCase)]
	private static partial Regex WorksForPattern();

	[GeneratedRegex(@"\b(wrote|authored|created|composed|published)\b", RegexOptions.IgnoreCase)]
	private static partial Regex AuthorOfPattern();

	[GeneratedRegex(@"\b(part\s+of|component\s+of|within|inside)\b", RegexOptions.IgnoreCase)]
	private static partial Regex PartOfPattern();

	[GeneratedRegex(@"\b(creates?|produces?|makes?|builds?|develops?)\b", RegexOptions.IgnoreCase)]
	private static partial Regex CreatesPattern();

	[GeneratedRegex(@"\b(uses?|utilized?|employs?|applies?)\b", RegexOptions.IgnoreCase)]
	private static partial Regex UsesPattern();

	[GeneratedRegex(@"\b(collaborates?\s+with|works?\s+with|partners?\s+with|teams?\s+with)\b", RegexOptions.IgnoreCase)]
	private static partial Regex CollaboratesPattern();

	[GeneratedRegex(@"\b(owns?|possesses?|has|holds?)\b", RegexOptions.IgnoreCase)]
	private static partial Regex OwnsPattern();

	[GeneratedRegex(@"\b(manages?|leads?|directs?|oversees?|heads?)\b", RegexOptions.IgnoreCase)]
	private static partial Regex ManagesPattern();

	[GeneratedRegex(@"\b(influences?|affects?|impacts?|shapes?)\b", RegexOptions.IgnoreCase)]
	private static partial Regex InfluencesPattern();

	[GeneratedRegex(@"\b(supports?|helps?|aids?|assists?|backs?)\b", RegexOptions.IgnoreCase)]
	private static partial Regex SupportsPattern();

	[GeneratedRegex(@"\b(and|with|alongside|together\s+with)\b", RegexOptions.IgnoreCase)]
	private static partial Regex RelatedToPattern();

	private static string GetChunkContent(ChunkerBase chunk)
	{
		if (chunk is ContentChunk contentChunk)
		{
			return contentChunk.Content;
		}

		return string.Empty;
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

	private double CalculateProximityConfidence(int distance)
	{
		// Very close entities (< 50 chars) get high confidence
		if (distance <= 50)
		{
			return 1.0;
		}

		// Far entities (near maxDistance) get low confidence
		if (distance >= maxDistance)
		{
			return minConfidence;
		}

		// Linear interpolation
		var ratio = (double)distance / maxDistance;
		return 1.0 - (ratio * (1.0 - minConfidence));
	}

	private static string GetContext(string text, int startPos, int endPos, int contextSize = 150)
	{
		// Ensure startPos <= endPos
		if (startPos > endPos)
		{
			(startPos, endPos) = (endPos, startPos);
		}

		var start = Math.Max(0, startPos - contextSize);
		var end = Math.Min(text.Length, endPos + contextSize);

		// Ensure valid range
		if (start >= text.Length || end <= 0 || start >= end)
		{
			return string.Empty;
		}

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

	private static string GetRelationshipKey(Guid entityId1, Guid entityId2, RelationshipType type) =>
		// For directional relationships, order matters
		$"{entityId1}_{entityId2}_{type}";

	private class RelationshipPattern
	{
		public required Regex Regex { get; init; }
		public required RelationshipType Type { get; init; }
		public required double Confidence { get; init; }
		public required bool IsDirectional { get; init; }
	}

	private class DetectedRelationship
	{
		public required RelationshipType Type { get; init; }
		public required Guid FromEntityId { get; init; }
		public required Guid ToEntityId { get; init; }
		public required double Confidence { get; init; }
		public required bool IsDirectional { get; init; }
		public required string Context { get; init; }
	}
}
