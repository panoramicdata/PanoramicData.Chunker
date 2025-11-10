using System.Text.RegularExpressions;
using PanoramicData.Chunker.Models.KnowledgeGraph;

namespace PanoramicData.Chunker.Configuration;

/// <summary>
/// Configuration for relationship extraction patterns loaded from JSON.
/// </summary>
public class RelationshipPatternsConfiguration
{
	/// <summary>
	/// Version of the pattern configuration.
	/// </summary>
	public string Version { get; set; } = "1.0";

	/// <summary>
	/// Description of this pattern set.
	/// </summary>
	public string? Description { get; set; }

	/// <summary>
	/// Date when patterns were last updated.
	/// </summary>
	public string? LastUpdated { get; set; }

	/// <summary>
	/// Array of relationship extraction patterns.
	/// </summary>
	public List<RelationshipPatternDefinition> Patterns { get; set; } = [];

	/// <summary>
	/// Descriptions of pattern categories.
	/// </summary>
	public Dictionary<string, string>? Categories { get; set; }

	/// <summary>
	/// Training notes and instructions.
	/// </summary>
	public TrainingNotes? TrainingNotes { get; set; }
}

/// <summary>
/// Definition of a single relationship extraction pattern.
/// </summary>
public class RelationshipPatternDefinition
{
	/// <summary>
	/// Unique name for this pattern.
	/// </summary>
	public required string Name { get; set; }

	/// <summary>
	/// Regular expression pattern (raw string, not compiled).
	/// </summary>
	public required string Regex { get; set; }

	/// <summary>
	/// The RelationshipType enum value this pattern detects.
	/// </summary>
	public required string RelationshipType { get; set; }

	/// <summary>
	/// Confidence score for matches (0.0 to 1.0).
	/// </summary>
	public double Confidence { get; set; }

	/// <summary>
	/// Whether this relationship has a specific direction.
	/// </summary>
	public bool IsDirectional { get; set; }

	/// <summary>
	/// Human-readable description of what this pattern matches.
	/// </summary>
	public string? Description { get; set; }

	/// <summary>
	/// Example sentences that should match this pattern.
	/// </summary>
	public List<string>? Examples { get; set; }

	/// <summary>
	/// Category of this pattern (e.g., 'Organizational', 'Scientific').
	/// </summary>
	public string? Category { get; set; }

	/// <summary>
	/// Whether this pattern is active (default: true).
	/// </summary>
	public bool Enabled { get; set; } = true;

	/// <summary>
	/// Regex options (default: 'IgnoreCase').
	/// </summary>
	public string RegexOptions { get; set; } = "IgnoreCase";

	/// <summary>
	/// Converts this definition to a compiled Regex object.
	/// </summary>
	public Regex CompileRegex()
	{
		var options = ParseRegexOptions(RegexOptions);
		return new Regex(Regex, options, TimeSpan.FromSeconds(1)); // 1 second timeout
	}

	/// <summary>
	/// Parses regex options from string.
	/// </summary>
	private static RegexOptions ParseRegexOptions(string optionsString)
	{
		var result = System.Text.RegularExpressions.RegexOptions.None;

		if (string.IsNullOrWhiteSpace(optionsString))
		{
			return System.Text.RegularExpressions.RegexOptions.IgnoreCase;
		}

		var parts = optionsString.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

		foreach (var part in parts)
		{
			if (Enum.TryParse<RegexOptions>(part, ignoreCase: true, out var option))
			{
				result |= option;
			}
		}

		return result;
	}

	/// <summary>
	/// Converts string relationship type to enum.
	/// </summary>
	public RelationshipType GetRelationshipType()
	{
		if (Enum.TryParse<RelationshipType>(RelationshipType, ignoreCase: true, out var type))
		{
			return type;
		}

		throw new InvalidOperationException($"Invalid relationship type: {RelationshipType}");
	}
}

/// <summary>
/// Training notes and instructions for pattern maintenance.
/// </summary>
public class TrainingNotes
{
	/// <summary>
	/// General instructions for training.
	/// </summary>
	public string? Instructions { get; set; }

	/// <summary>
	/// Pattern design guidelines.
	/// </summary>
	public string? PatternDesign { get; set; }

	/// <summary>
	/// Confidence scoring guidelines.
	/// </summary>
	public string? ConfidenceScoring { get; set; }

	/// <summary>
	/// Testing strategy guidelines.
	/// </summary>
	public string? TestingStrategy { get; set; }
}
