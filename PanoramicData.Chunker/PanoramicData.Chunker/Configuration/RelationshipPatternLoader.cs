using System.Runtime.InteropServices;
using PanoramicData.Chunker.Models.KnowledgeGraph;
using System.Text.RegularExpressions;

namespace PanoramicData.Chunker.Configuration;

/// <summary>
/// Service for loading and managing relationship extraction patterns from JSON configuration.
/// </summary>
public class RelationshipPatternLoader
{
	protected RelationshipPatternLoader()
	{
	}

	/// <summary>
	/// Default path to relationship patterns file relative to application directory.
	/// </summary>
	public static string DefaultPatternsPath { get; } = "Configuration/RelationshipPatterns.json";

	/// <summary>
	/// Loads relationship patterns from a JSON file.
	/// </summary>
	/// <param name="filePath">Path to the JSON configuration file.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>Compiled relationship patterns.</returns>
	public static async Task<List<CompiledRelationshipPattern>> LoadPatternsAsync(
		string filePath,
		[Optional] CancellationToken cancellationToken)
	{
		var config = await PatternLoaderSupport.ReadAsync<RelationshipPatternsConfiguration>(filePath, "Relationship", cancellationToken);

		// Validate version
		if (string.IsNullOrWhiteSpace(config.Version))
		{
			throw new InvalidOperationException("Configuration must have a version");
		}

		// Compile patterns
		var compiledPatterns = new List<CompiledRelationshipPattern>();

		foreach (var pattern in config.Patterns)
		{
			// Skip disabled patterns
			if (!pattern.Enabled)
			{
				continue;
			}

			try
			{
				var compiled = new CompiledRelationshipPattern
				{
					Name = pattern.Name,
					Regex = pattern.CompileRegex(),
					Type = pattern.GetRelationshipType(),
					Confidence = pattern.Confidence,
					IsDirectional = pattern.IsDirectional,
					Description = pattern.Description,
					Category = pattern.Category
				};

				compiledPatterns.Add(compiled);
			}
			catch (Exception ex)
			{
				throw new InvalidOperationException(
					$"Failed to compile pattern '{pattern.Name}': {ex.Message}", ex);
			}
		}

		if (compiledPatterns.Count == 0)
		{
			throw new InvalidOperationException("No valid patterns found in configuration");
		}

		return compiledPatterns;
	}

	/// <summary>
	/// Loads patterns from default location (Configuration/RelationshipPatterns.json in app directory).
	/// </summary>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>Compiled relationship patterns.</returns>
	public static async Task<List<CompiledRelationshipPattern>> LoadDefaultPatternsAsync(
		[Optional] CancellationToken cancellationToken)
		=> await LoadPatternsAsync(PatternLoaderSupport.FindDefaultPath(DefaultPatternsPath, "Relationship"), cancellationToken);

	/// <summary>
	/// Validates pattern configuration without loading.
	/// </summary>
	/// <param name="filePath">Path to the JSON configuration file.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>Validation result.</returns>
	public static async Task<PatternValidationResult> ValidatePatternsAsync(
		string filePath,
		[Optional] CancellationToken cancellationToken)
	{
		var result = new PatternValidationResult { IsValid = true };

		try
		{
			if (!File.Exists(filePath))
			{
				result.IsValid = false;
				result.Errors.Add($"File not found: {filePath}");
				return result;
			}

			var config = await PatternLoaderSupport.TryReadAsync<RelationshipPatternsConfiguration>(filePath, cancellationToken);

			if (config == null)
			{
				result.IsValid = false;
				result.Errors.Add("Failed to deserialize configuration");
				return result;
			}

			foreach (var pattern in config.Patterns)
			{
				ValidatePattern(pattern, result);
			}

			result.PatternCount = config.Patterns.Count;
			result.EnabledPatternCount = config.Patterns.Count(p => p.Enabled);
		}
		catch (Exception ex)
		{
			result.IsValid = false;
			result.Errors.Add($"Validation error: {ex.Message}");
		}

		return result;
	}

	private static void ValidatePattern(RelationshipPatternDefinition pattern, PatternValidationResult result)
	{
		TryValidate(() => pattern.CompileRegex(), $"Pattern '{pattern.Name}': Invalid regex - ", result);
		TryValidate(() => pattern.GetRelationshipType(), $"Pattern '{pattern.Name}': ", result);

		if (pattern.Confidence is < 0.0 or > 1.0)
		{
			result.IsValid = false;
			result.Errors.Add($"Pattern '{pattern.Name}': Confidence must be between 0.0 and 1.0");
		}
	}

	private static void TryValidate(Action validation, string errorPrefix, PatternValidationResult result)
	{
		try
		{
			validation();
		}
		catch (Exception ex)
		{
			result.IsValid = false;
			result.Errors.Add($"{errorPrefix}{ex.Message}");
		}
	}

	/// <summary>
	/// Saves patterns to a JSON file (for training tools).
	/// </summary>
	/// <param name="config">The configuration to save.</param>
	/// <param name="filePath">Path to save the JSON file.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	public static async Task SavePatternsAsync(
		RelationshipPatternsConfiguration config,
		string filePath,
		[Optional] CancellationToken cancellationToken)
		=> await PatternLoaderSupport.SaveAsync(config, filePath, cancellationToken);

	/// <summary>
	/// Creates a template configuration file for users to customize.
	/// </summary>
	/// <param name="filePath">Path to create the template file.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	public static async Task CreateTemplateAsync(
		string filePath,
		[Optional] CancellationToken cancellationToken)
	{
		var template = new RelationshipPatternsConfiguration
		{
			Version = "2.0",
			Description = "Custom relationship extraction patterns - Edit this file to add domain-specific patterns",
			LastUpdated = DateTime.UtcNow.ToString("yyyy-MM-dd"),
			Patterns =
			[
				new RelationshipPatternDefinition
				{
					Enabled = false,
					Category = "Custom",
					Examples = ["Entity1 custom Entity2"],
					Description = "Example pattern - replace with your own",
					IsDirectional = false,
					Confidence = 0.8,
					RelationshipType = "RelatedTo",
					Regex = @"\b(custom|example)\b",
					Name = "CustomPattern"
				}
			],
			TrainingNotes = new TrainingNotes
			{
				TestingStrategy = "Test against ground truth and adjust confidence based on metrics",
				ConfidenceScoring = "Use 0.9-1.0 for very specific patterns, 0.5-0.7 for generic ones",
				PatternDesign = "Make patterns specific enough to avoid false positives",
				Instructions = "Add new patterns based on observed relationships in your documents"
			},
			Categories = new Dictionary<string, string>
			{
				["Custom"] = "Custom patterns for your domain"
			}
		};

		await SavePatternsAsync(template, filePath, cancellationToken);
	}
}

/// <summary>
/// Compiled relationship pattern ready for use in extraction.
/// </summary>
public class CompiledRelationshipPattern
{
	/// <summary>
	/// Pattern name.
	/// </summary>
	public required string Name { get; init; }

	/// <summary>
	/// Compiled regex.
	/// </summary>
	public required Regex Regex { get; init; }

	/// <summary>
	/// Relationship type.
	/// </summary>
	public required RelationshipType Type { get; init; }

	/// <summary>
	/// Confidence score.
	/// </summary>
	public required double Confidence { get; init; }

	/// <summary>
	/// Whether directional.
	/// </summary>
	public required bool IsDirectional { get; init; }

	/// <summary>
	/// Description.
	/// </summary>
	public string? Description { get; init; }

	/// <summary>
	/// Category.
	/// </summary>
	public string? Category { get; init; }
}

/// <summary>
/// Result of pattern validation.
/// </summary>
public class PatternValidationResult
{
	/// <summary>
	/// Whether validation passed.
	/// </summary>
	public bool IsValid { get; set; }

	/// <summary>
	/// Total number of patterns.
	/// </summary>
	public int PatternCount { get; set; }

	/// <summary>
	/// Number of enabled patterns.
	/// </summary>
	public int EnabledPatternCount { get; set; }

	/// <summary>
	/// Validation errors.
	/// </summary>
	public List<string> Errors { get; set; } = [];

	/// <summary>
	/// Validation warnings.
	/// </summary>
	public List<string> Warnings { get; set; } = [];
}
