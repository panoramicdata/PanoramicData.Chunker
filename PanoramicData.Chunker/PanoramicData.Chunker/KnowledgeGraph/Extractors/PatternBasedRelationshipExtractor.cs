using PanoramicData.Chunker.Configuration;
using PanoramicData.Chunker.Interfaces.KnowledgeGraph;
using PanoramicData.Chunker.Models;
using PanoramicData.Chunker.Models.KnowledgeGraph;

namespace PanoramicData.Chunker.KnowledgeGraph.Extractors;

/// <summary>
/// Extracts relationships between entities using pattern matching and linguistic analysis.
/// Uses regex patterns loaded from JSON configuration files for flexibility and trainability.
/// </summary>
/// <remarks>
/// Phase 12 Enhancements:
/// - Patterns now loaded from external JSON configuration
/// - Supports custom pattern files for domain-specific extraction
/// - Enables training and refinement without code changes
/// - Pre-compiles patterns at initialization for performance
/// 
/// Pattern Configuration:
/// - Default patterns: Configuration/RelationshipPatterns.json (embedded resource)
/// - Custom patterns: Can be loaded from any JSON file matching the schema
/// 
/// Supports 35+ relationship types including: Founded, MemberOf, LocatedIn, WorksFor,
/// AuthorOf, PartOf, Creates, Uses, Collaborates, StudiedAt, TraveledOn, MentorOf,
/// PresentedTo, Visited, Discovered, Observed, Studied, Collected, Wrote, Developed,
/// Proposed, InfluencedBy, LivedIn, Invited, and more.
/// </remarks>
public class PatternBasedRelationshipExtractor : IRelationshipExtractor
{
	private readonly int _maxDistance;
	private readonly double _minConfidence;
	private readonly bool _enablePatternMatching;
	private readonly bool _enableProximityRelationships;
	private readonly List<CompiledRelationshipPattern> _patterns;

	/// <inheritdoc/>
	public string Name => "PatternBasedRelationshipExtractor";

	/// <inheritdoc/>
	public string Version => "2.0"; // Phase 12: JSON-based patterns

	/// <inheritdoc/>
	public IReadOnlyList<RelationshipType> SupportedRelationshipTypes { get; }

	/// <summary>
	/// Initializes a new instance of the <see cref="PatternBasedRelationshipExtractor"/> class
	/// with default patterns from embedded resource.
	/// </summary>
	/// <param name="maxDistance">Maximum character distance between entities (default: 500).</param>
	/// <param name="minConfidence">Minimum confidence score for relationships (default: 0.3).</param>
	/// <param name="enablePatternMatching">Enable pattern-based relationship detection (default: true).</param>
	/// <param name="enableProximityRelationships">Enable distance-based co-occurrence relationships (default: true).</param>
	public PatternBasedRelationshipExtractor(
		int maxDistance = 500,
		double minConfidence = 0.3,
		bool enablePatternMatching = true,
		bool enableProximityRelationships = true)
	{
		_maxDistance = maxDistance;
		_minConfidence = minConfidence;
		_enablePatternMatching = enablePatternMatching;
		_enableProximityRelationships = enableProximityRelationships;

		// Load default patterns synchronously (for backward compatibility)
		// In production, consider using async factory pattern
		_patterns = LoadDefaultPatternsSync();

		SupportedRelationshipTypes = _patterns
			.Select(p => p.Type)
			.Distinct()
			.ToList()
			.AsReadOnly();
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="PatternBasedRelationshipExtractor"/> class
	/// with custom patterns from a JSON file.
	/// </summary>
	/// <param name="patternFilePath">Path to custom pattern JSON file.</param>
	/// <param name="maxDistance">Maximum character distance between entities (default: 500).</param>
	/// <param name="minConfidence">Minimum confidence score for relationships (default: 0.3).</param>
	/// <param name="enablePatternMatching">Enable pattern-based relationship detection (default: true).</param>
	/// <param name="enableProximityRelationships">Enable distance-based co-occurrence relationships (default: true).</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	public static async Task<PatternBasedRelationshipExtractor> CreateAsync(
		string patternFilePath,
		int maxDistance = 500,
		double minConfidence = 0.3,
		bool enablePatternMatching = true,
		bool enableProximityRelationships = true,
		CancellationToken cancellationToken = default)
	{
		var patterns = await RelationshipPatternLoader.LoadPatternsAsync(patternFilePath, cancellationToken);

		return new PatternBasedRelationshipExtractor(
			patterns,
			maxDistance,
			minConfidence,
			enablePatternMatching,
			enableProximityRelationships);
	}

	/// <summary>
	/// Internal constructor that accepts pre-loaded patterns.
	/// </summary>
	private PatternBasedRelationshipExtractor(
		List<CompiledRelationshipPattern> patterns,
		int maxDistance,
		double minConfidence,
		bool enablePatternMatching,
		bool enableProximityRelationships)
	{
		_patterns = patterns;
		_maxDistance = maxDistance;
		_minConfidence = minConfidence;
		_enablePatternMatching = enablePatternMatching;
		_enableProximityRelationships = enableProximityRelationships;

		SupportedRelationshipTypes = _patterns
			.Select(p => p.Type)
			.Distinct()
			.ToList()
			.AsReadOnly();
	}

	/// <summary>
	/// Loads default patterns synchronously (for backward compatibility).
	/// </summary>
	private static List<CompiledRelationshipPattern> LoadDefaultPatternsSync() =>
		// This is a workaround for constructor limitation
		// Ideally use async factory pattern in production
		RelationshipPatternLoader.LoadDefaultPatternsAsync().GetAwaiter().GetResult();

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

					if (minDistance > _maxDistance)
					{
						continue;
					}

					// Try pattern-based extraction first
					List<DetectedRelationship>? detectedRelationships = null;
					if (_enablePatternMatching)
					{
						detectedRelationships = DetectPatternBasedRelationships(
							content,
							entity1,
							entity2,
							positions1,
							positions2);
					}

					// If no patterns matched and proximity is enabled, create co-occurrence relationship
					if ((detectedRelationships == null || detectedRelationships.Count == 0) && _enableProximityRelationships)
					{
						var proximityConfidence = CalculateProximityConfidence(minDistance);
						if (proximityConfidence >= _minConfidence)
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
							if (detected.Confidence < _minConfidence)
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
					Context = GetContext(content, firstPos, secondPos),
					PatternName = pattern.Name
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

	// Helper methods

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
		if (distance >= _maxDistance)
		{
			return _minConfidence;
		}

		// Linear interpolation
		var ratio = (double)distance / _maxDistance;
		return 1.0 - (ratio * (1.0 - _minConfidence));
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

	// Helper classes

	private class DetectedRelationship
	{
		public required RelationshipType Type { get; init; }
		public required Guid FromEntityId { get; init; }
		public required Guid ToEntityId { get; init; }
		public required double Confidence { get; init; }
		public required bool IsDirectional { get; init; }
		public required string Context { get; init; }
		public string? PatternName { get; init; }
	}
}
