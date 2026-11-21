using System.Runtime.InteropServices;
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
public partial class PatternBasedRelationshipExtractor : IRelationshipExtractor
{
	private readonly int _maxDistance;
	private readonly double _minConfidence;
	private readonly bool _enablePatternMatching;
	private readonly bool _enableProximityRelationships;
	private const int ContextSize = 150;

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
		[Optional][DefaultParameterValue(500)] int maxDistance,
		[Optional][DefaultParameterValue(0.3)] double minConfidence,
		[Optional][DefaultParameterValue(true)] bool enablePatternMatching,
		[Optional][DefaultParameterValue(true)] bool enableProximityRelationships)
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
		[Optional][DefaultParameterValue(500)] int maxDistance,
		[Optional][DefaultParameterValue(0.3)] double minConfidence,
		[Optional][DefaultParameterValue(true)] bool enablePatternMatching,
		[Optional][DefaultParameterValue(true)] bool enableProximityRelationships,
		[Optional] CancellationToken cancellationToken)
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
		[Optional] CancellationToken cancellationToken)
	{
		var relationships = ExtractorHelpers.CollectRelationships(entities, chunks, _maxDistance, AddDetectedRelationships, cancellationToken);
		return await Task.FromResult(relationships);
	}

	private void AddDetectedRelationships(
		EntityPair pair,
		Guid chunkId,
		List<Relationship> relationships,
		Dictionary<string, Relationship> relationshipMap)
	{
		foreach (var detected in DetectRelationships(pair).Where(d => d.Confidence >= _minConfidence))
		{
			AddOrUpdateRelationship(relationshipMap, relationships, detected, chunkId);
		}
	}

	private List<DetectedRelationship> DetectRelationships(EntityPair pair)
	{
		var detected = _enablePatternMatching
			? DetectPatternBasedRelationships(pair.Content, pair.Entity1, pair.Entity2, pair.Positions1, pair.Positions2)
			: [];

		// Fall back to a co-occurrence relationship when no pattern matched
		return detected.Count == 0 && _enableProximityRelationships
			? CreateProximityRelationship(pair)
			: detected;
	}

	private List<DetectedRelationship> CreateProximityRelationship(EntityPair pair)
	{
		var proximityConfidence = ExtractorHelpers.InterpolateConfidence(pair.MinDistance, _maxDistance, _minConfidence);
		if (proximityConfidence < _minConfidence)
		{
			return [];
		}

		return
		[
			new DetectedRelationship
			{
				Type = pair.MinDistance < 100 ? RelationshipType.Mentions : RelationshipType.CooccursWith,
				FromEntityId = pair.Entity1.Id,
				ToEntityId = pair.Entity2.Id,
				Confidence = proximityConfidence,
				IsDirectional = false,
				Context = ExtractorHelpers.GetContext(pair.Content, pair.Positions1.Min(), pair.Positions2.Max(), ContextSize)
			}
		];
	}

	private List<DetectedRelationship> DetectPatternBasedRelationships(
		string content,
		Entity entity1,
		Entity entity2,
		List<int> positions1,
		List<int> positions2)
	{
		var detected = new List<DetectedRelationship>();

		var (firstEntity, secondEntity, firstPos, secondPos) = OrderByPosition(entity1, entity2, positions1.Min(), positions2.Min());

		var betweenText = GetTextBetween(content, firstPos, secondPos, firstEntity.Name.Length);
		if (string.IsNullOrWhiteSpace(betweenText))
		{
			return detected;
		}

		var context = ExtractorHelpers.GetContext(content, firstPos, secondPos, ContextSize);
		foreach (var pattern in _patterns.Where(p => p.Regex.IsMatch(betweenText)))
		{
			var fromEntity = pattern.IsDirectional ? firstEntity : entity1;
			var toEntity = pattern.IsDirectional ? secondEntity : entity2;

			detected.Add(new DetectedRelationship
			{
				Type = pattern.Type,
				FromEntityId = fromEntity.Id,
				ToEntityId = toEntity.Id,
				Confidence = pattern.Confidence,
				IsDirectional = pattern.IsDirectional,
				Context = context,
				PatternName = pattern.Name
			});

			// A high-confidence pattern ends the search
			if (pattern.Confidence >= 0.9)
			{
				break;
			}
		}

		return detected;
	}
}
