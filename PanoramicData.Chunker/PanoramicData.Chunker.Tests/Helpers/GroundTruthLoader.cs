namespace PanoramicData.Chunker.Tests.Helpers;

/// <summary>
/// Represents a single ground truth relationship from the Darwin autobiography dataset.
/// </summary>
public class GroundTruthRelationship
{
	/// <summary>
	/// First entity in the relationship (source).
	/// </summary>
	public required string Entity1 { get; set; }

	/// <summary>
	/// Type of relationship between entities.
	/// </summary>
	public required string RelationType { get; set; }

	/// <summary>
	/// Second entity in the relationship (target).
	/// </summary>
	public required string Entity2 { get; set; }

	/// <summary>
	/// Confidence score for this relationship (0.0-1.0).
	/// 1.0 = explicitly stated, 0.9 = strongly implied, 0.8 = reasonable inference.
	/// </summary>
	public double Confidence { get; set; }

	/// <summary>
	/// Document section where this relationship appears.
	/// </summary>
	public required string Section { get; set; }

	/// <summary>
	/// Additional notes or justification for this relationship.
	/// </summary>
	public required string Notes { get; set; }

	/// <summary>
	/// Returns a string representation of this ground truth relationship.
	/// </summary>
	public override string ToString()
		=> $"{Entity1} --[{RelationType}]--> {Entity2} (confidence: {Confidence:F1})";
}

/// <summary>
/// Loads ground truth relationships from TSV files.
/// </summary>
public static class GroundTruthLoader
{
	/// <summary>
	/// Loads ground truth relationships from a TSV file.
	/// </summary>
	/// <param name="filePath">Path to the TSV file (relative to test project root).</param>
	/// <returns>List of ground truth relationships.</returns>
	/// <exception cref="FileNotFoundException">If the file doesn't exist.</exception>
	/// <exception cref="InvalidDataException">If the file format is invalid.</exception>
	public static List<GroundTruthRelationship> Load(string filePath)
	{
		// Resolve path relative to test project
		var fullPath = Path.Combine(AppContext.BaseDirectory, filePath);

		if (!File.Exists(fullPath))
		{
			throw new FileNotFoundException($"Ground truth file not found: {fullPath}");
		}

		var relationships = new List<GroundTruthRelationship>();
		var lines = File.ReadAllLines(fullPath);

		if (lines.Length == 0)
		{
			throw new InvalidDataException($"Ground truth file is empty: {fullPath}");
		}

		// Skip header line
		var dataLines = lines.Skip(1).Where(line => !string.IsNullOrWhiteSpace(line));

		var lineNumber = 1; // After header
		foreach (var line in dataLines)
		{
			lineNumber++;

			try
			{
				var parts = line.Split('\t');

				if (parts.Length < 6)
				{
					throw new InvalidDataException(
						$"Line {lineNumber}: Expected 6 columns, got {parts.Length}");
				}

				// Parse confidence score
				if (!double.TryParse(parts[3], out var confidence))
				{
					throw new InvalidDataException(
						$"Line {lineNumber}: Invalid confidence score '{parts[3]}'");
				}

				// Validate confidence range
				if (confidence < 0.0 || confidence > 1.0)
				{
					throw new InvalidDataException(
						$"Line {lineNumber}: Confidence {confidence} out of range [0.0, 1.0]");
				}

				relationships.Add(new GroundTruthRelationship
				{
					Entity1 = parts[0].Trim(),
					RelationType = parts[1].Trim(),
					Entity2 = parts[2].Trim(),
					Confidence = confidence,
					Section = parts[4].Trim(),
					Notes = parts[5].Trim()
				});
			}
			catch (Exception ex) when (ex is not InvalidDataException)
			{
				throw new InvalidDataException(
					$"Line {lineNumber}: Error parsing ground truth: {ex.Message}", ex);
			}
		}

		if (relationships.Count == 0)
		{
			throw new InvalidDataException($"No valid relationships found in file: {fullPath}");
		}

		return relationships;
	}

	/// <summary>
	/// Gets statistics about the loaded ground truth dataset.
	/// </summary>
	public static GroundTruthStatistics GetStatistics(List<GroundTruthRelationship> groundTruth)
	{
		return new GroundTruthStatistics
		{
			TotalRelationships = groundTruth.Count,
			UniqueEntity1Count = groundTruth.Select(r => r.Entity1).Distinct().Count(),
			UniqueEntity2Count = groundTruth.Select(r => r.Entity2).Distinct().Count(),
			UniqueRelationshipTypes = groundTruth.Select(r => r.RelationType).Distinct().Count(),
			AverageConfidence = groundTruth.Average(r => r.Confidence),
			ConfidenceDistribution = groundTruth
				.GroupBy(r => r.Confidence)
				.OrderByDescending(g => g.Key)
				.ToDictionary(g => g.Key, g => g.Count()),
			RelationshipTypeDistribution = groundTruth
				.GroupBy(r => r.RelationType)
				.OrderByDescending(g => g.Count())
				.ToDictionary(g => g.Key, g => g.Count()),
			SectionDistribution = groundTruth
				.GroupBy(r => r.Section)
				.OrderByDescending(g => g.Count())
				.ToDictionary(g => g.Key, g => g.Count())
		};
	}
}

/// <summary>
/// Statistics about a ground truth dataset.
/// </summary>
public class GroundTruthStatistics
{
	public int TotalRelationships { get; set; }
	public int UniqueEntity1Count { get; set; }
	public int UniqueEntity2Count { get; set; }
	public int UniqueRelationshipTypes { get; set; }
	public double AverageConfidence { get; set; }
	public Dictionary<double, int> ConfidenceDistribution { get; set; } = new();
	public Dictionary<string, int> RelationshipTypeDistribution { get; set; } = new();
	public Dictionary<string, int> SectionDistribution { get; set; } = new();

	public override string ToString()
	{
		var sb = new System.Text.StringBuilder();
		sb.AppendLine("Ground Truth Statistics:");
		sb.AppendLine($"  Total Relationships: {TotalRelationships}");
		sb.AppendLine($"  Unique Entities (Entity1): {UniqueEntity1Count}");
		sb.AppendLine($"  Unique Entities (Entity2): {UniqueEntity2Count}");
		sb.AppendLine($"  Unique Relationship Types: {UniqueRelationshipTypes}");
		sb.AppendLine($"  Average Confidence: {AverageConfidence:F2}");
		sb.AppendLine();
		sb.AppendLine("Confidence Distribution:");
		foreach (var kvp in ConfidenceDistribution.OrderByDescending(k => k.Key))
		{
			sb.AppendLine($"  {kvp.Key:F1}: {kvp.Value} relationships");
		}
		sb.AppendLine();
		sb.AppendLine("Top 10 Relationship Types:");
		foreach (var kvp in RelationshipTypeDistribution.Take(10))
		{
			sb.AppendLine($"  {kvp.Key}: {kvp.Value} occurrences");
		}
		sb.AppendLine();
		sb.AppendLine("Section Distribution:");
		foreach (var kvp in SectionDistribution.OrderByDescending(k => k.Value))
		{
			sb.AppendLine($"  {kvp.Key}: {kvp.Value} relationships");
		}
		return sb.ToString();
	}
}
