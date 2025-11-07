using PanoramicData.Chunker.Models.KnowledgeGraph;
using System.Text;

namespace PanoramicData.Chunker.Tests.Helpers;

/// <summary>
/// Result of comparing extracted knowledge graph against ground truth.
/// </summary>
public class GroundTruthComparisonResult
{
	public int TotalGroundTruthRelationships { get; set; }
	public int TotalExtractedRelationships { get; set; }

	public int TruePositives { get; set; }
	public int FalsePositives { get; set; }
	public int FalseNegatives { get; set; }

	// Quality Metrics
	public double Precision => TruePositives + FalsePositives > 0
		? (double)TruePositives / (TruePositives + FalsePositives)
		: 0.0;

	public double RecallRate => TruePositives + FalseNegatives > 0
		? (double)TruePositives / (TruePositives + FalseNegatives)
		: 0.0;

	public double F1Score => Precision + RecallRate > 0
		? 2 * (Precision * RecallRate) / (Precision + RecallRate)
		: 0.0;

	// Detailed results
	public List<GroundTruthMatch> Matches { get; set; } = [];
	public List<GroundTruthMiss> Misses { get; set; } = [];

	/// <summary>
	/// Generates a detailed text report of the comparison.
	/// </summary>
	public string GenerateReport()
	{
		var sb = new StringBuilder();
		sb.AppendLine("=== Ground Truth Comparison Report ===");
		sb.AppendLine();
		sb.AppendLine("Overall Metrics:");
		sb.AppendLine($"  Ground Truth Relationships: {TotalGroundTruthRelationships}");
		sb.AppendLine($"  Extracted Relationships: {TotalExtractedRelationships}");
		sb.AppendLine();
		sb.AppendLine($"True Positives:  {TruePositives} ({TruePositives * 100.0 / TotalGroundTruthRelationships:F1}%)");
		sb.AppendLine($"  False Negatives: {FalseNegatives} ({FalseNegatives * 100.0 / TotalGroundTruthRelationships:F1}%)");
		sb.AppendLine($"  False Positives: {FalsePositives}");
		sb.AppendLine();
		sb.AppendLine("Quality Metrics:");
		sb.AppendLine($"  Precision: {Precision:P1}");
		sb.AppendLine($"  Recall:    {RecallRate:P1}");
		sb.AppendLine($"  F1 Score:  {F1Score:P1}");
		sb.AppendLine();

		// Top misses
		sb.AppendLine("Top 10 Misses:");
		foreach (var miss in Misses.Take(10))
		{
			sb.AppendLine($"  {miss.GroundTruth.Entity1} -> {miss.GroundTruth.RelationType} -> {miss.GroundTruth.Entity2}");
			sb.AppendLine($"    Reason: {miss.Reason}");
		}

		return sb.ToString();
	}
}

/// <summary>
/// Represents a match between ground truth and extracted relationship.
/// </summary>
public class GroundTruthMatch
{
	public required GroundTruthRelationship GroundTruth { get; set; }
	public Relationship? ExtractedRelationship { get; set; }
	public MatchQuality Quality { get; set; }
}

/// <summary>
/// Represents a ground truth relationship that was not found.
/// </summary>
public class GroundTruthMiss
{
	public required GroundTruthRelationship GroundTruth { get; set; }
	public required string Reason { get; set; }
	public MissCategory Category { get; set; }
}

/// <summary>
/// Quality of match between ground truth and extracted relationship.
/// </summary>
public enum MatchQuality
{
	Exact,        // Perfect match
	EntityAliasMatch,   // Entity names differ but aliases match
	TypeMismatch,       // Entities match but relationship type wrong
	NoMatch       // Not found
}

/// <summary>
/// Category of relationship miss for failure analysis.
/// </summary>
public enum MissCategory
{
	EntityNotExtracted,      // One or both entities missing
	RelationshipNotDetected, // Entities exist but no relationship
	WrongRelationshipType,   // Relationship exists but wrong type
	ChunkingBoundary,    // Entities in separate chunks
	LowConfidence            // Extracted but below threshold
}

/// <summary>
/// Compares extracted knowledge graph against ground truth relationships.
/// </summary>
public class GroundTruthComparison
{
	/// <summary>
	/// Compares the extracted graph against ground truth.
	/// </summary>
	public static GroundTruthComparisonResult Compare(
		Graph extractedGraph,
		List<GroundTruthRelationship> groundTruth)
	{
		var result = new GroundTruthComparisonResult
		{
			TotalGroundTruthRelationships = groundTruth.Count,
			TotalExtractedRelationships = extractedGraph.Relationships.Count
		};

		foreach (var gt in groundTruth)
		{
			var match = FindMatchingRelationship(extractedGraph, gt);

			if (match.Quality is MatchQuality.Exact or
				MatchQuality.EntityAliasMatch)
			{
				result.TruePositives++;
				result.Matches.Add(match);
			}
			else
			{
				result.FalseNegatives++;
				result.Misses.Add(new GroundTruthMiss
				{
					GroundTruth = gt,
					Reason = DetermineMissReason(extractedGraph, gt),
					Category = CategorizeMiss(extractedGraph, gt)
				});
			}
		}

		result.FalsePositives = result.TotalExtractedRelationships - result.TruePositives;

		return result;
	}

	private static GroundTruthMatch FindMatchingRelationship(
		Graph graph,
		GroundTruthRelationship groundTruth)
	{
		// Find entities
		var entity1 = FindEntity(graph, groundTruth.Entity1);
		var entity2 = FindEntity(graph, groundTruth.Entity2);

		if (entity1 == null || entity2 == null)
		{
			return new GroundTruthMatch
			{
				GroundTruth = groundTruth,
				Quality = MatchQuality.NoMatch
			};
		}

		// Find relationship
		var relationships = graph.GetRelationships(entity1.Id);
		var match = relationships.FirstOrDefault(r =>
			r.ToEntityId == entity2.Id &&
			r.Type.ToString().Equals(groundTruth.RelationType,
				StringComparison.OrdinalIgnoreCase));

		if (match != null)
		{
			return new GroundTruthMatch
			{
				GroundTruth = groundTruth,
				ExtractedRelationship = match,
				Quality = MatchQuality.Exact
			};
		}

		// Check for type mismatch
		var anyRelationship = relationships.FirstOrDefault(r => r.ToEntityId == entity2.Id);
		if (anyRelationship != null)
		{
			return new GroundTruthMatch
			{
				GroundTruth = groundTruth,
				ExtractedRelationship = anyRelationship,
				Quality = MatchQuality.TypeMismatch
			};
		}

		return new GroundTruthMatch
		{
			GroundTruth = groundTruth,
			Quality = MatchQuality.NoMatch
		};
	}

	private static Entity? FindEntity(Graph graph, string name)
	{
		// Step 1: Exact match
		var entity = graph.GetEntitiesByName(name).FirstOrDefault();
		if (entity != null) return entity;

		// Step 2: Normalized match
		var normalized = name.ToLowerInvariant().Trim();
		entity = graph.Entities.FirstOrDefault(e =>
			e.NormalizedName == normalized);
		if (entity != null) return entity;

		// Step 3: Alias match
		entity = graph.Entities.FirstOrDefault(e =>
			e.Aliases.Contains(name, StringComparer.OrdinalIgnoreCase));
		if (entity != null) return entity;

		// Step 4: Partial match (e.g., "Charles Darwin" -> "Darwin")
		entity = graph.Entities.FirstOrDefault(e =>
			e.Name.Contains(name, StringComparison.OrdinalIgnoreCase) ||
			name.Contains(e.Name, StringComparison.OrdinalIgnoreCase));
		if (entity != null) return entity;

		// Step 5: NEW - Fuzzy word-by-word match for multi-word entities
		// Handles cases like "HMS Beagle" matching "Beagle" or "'Beagle'"
		var nameWords = SplitIntoSignificantWords(name);
		if (nameWords.Count > 0)
		{
			entity = graph.Entities.FirstOrDefault(e =>
			{
				var entityWords = SplitIntoSignificantWords(e.Name);
				if (entityWords.Count == 0) return false;

				// Calculate overlap: if 80%+ of words match, consider it a match
				var matchCount = nameWords.Intersect(entityWords, StringComparer.OrdinalIgnoreCase).Count();
				var minWords = Math.Min(nameWords.Count, entityWords.Count);
				var maxWords = Math.Max(nameWords.Count, entityWords.Count);

				// If one is a subset of the other (e.g., "Beagle" and "HMS Beagle")
				if (matchCount == minWords && maxWords - minWords <= 1)
				{
					return true;
				}

				// If 80%+ overlap
				return (double)matchCount / maxWords >= 0.8;
			});
			if (entity != null) return entity;
		}

		// Step 6: NEW - Check if ground truth name appears in entity aliases
		entity = graph.Entities.FirstOrDefault(e =>
		{
			// Check if any alias contains the search name
			return e.Aliases.Any(alias =>
				alias.Contains(name, StringComparison.OrdinalIgnoreCase) ||
				name.Contains(alias, StringComparison.OrdinalIgnoreCase));
		});

		return entity;
	}

	/// <summary>
	/// Splits a name into significant words, filtering out common particles.
	/// </summary>
	private static HashSet<string> SplitIntoSignificantWords(string name)
	{
		var words = name.Split([' ', '\'', '"', ','], StringSplitOptions.RemoveEmptyEntries)
			.Select(w => w.ToLowerInvariant().Trim())
			.Where(w => w.Length > 2) // Skip very short words
			.Where(w => w is not "the" and not "of" and not "and" and not "for") // Skip common particles
			.ToHashSet(StringComparer.OrdinalIgnoreCase);

		return words;
	}

	private static string DetermineMissReason(Graph graph, GroundTruthRelationship gt)
	{
		var entity1 = FindEntity(graph, gt.Entity1);
		var entity2 = FindEntity(graph, gt.Entity2);

		if (entity1 == null)
			return $"Entity '{gt.Entity1}' not extracted";
		if (entity2 == null)
			return $"Entity '{gt.Entity2}' not extracted";

		var relationships = graph.GetRelationships(entity1.Id);
		if (!relationships.Any(r => r.ToEntityId == entity2.Id))
			return "No relationship detected between entities";

		return "Relationship type mismatch";
	}

	private static MissCategory CategorizeMiss(Graph graph, GroundTruthRelationship gt)
	{
		var entity1 = FindEntity(graph, gt.Entity1);
		var entity2 = FindEntity(graph, gt.Entity2);

		if (entity1 == null || entity2 == null)
			return MissCategory.EntityNotExtracted;

		var relationships = graph.GetRelationships(entity1.Id);
		if (!relationships.Any(r => r.ToEntityId == entity2.Id))
			return MissCategory.RelationshipNotDetected;

		return MissCategory.WrongRelationshipType;
	}
}
