using PanoramicData.Chunker.Models.KnowledgeGraph;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace PanoramicData.Chunker.Configuration;

/// <summary>
/// Service for loading and managing relationship extraction patterns from JSON configuration.
/// </summary>
public class RelationshipPatternLoader
{
	private static readonly JsonSerializerOptions _jsonOptions = new()
	{
		PropertyNameCaseInsensitive = true,
		ReadCommentHandling = JsonCommentHandling.Skip,
		AllowTrailingCommas = true,
		WriteIndented = true // For when users edit the files
	};

	/// <summary>
	/// Default path to relationship patterns file relative to application directory.
	/// </summary>
	public const string DefaultPatternsPath = "Configuration/RelationshipPatterns.json";

	/// <summary>
	/// Loads relationship patterns from a JSON file.
	/// </summary>
	/// <param name="filePath">Path to the JSON configuration file.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>Compiled relationship patterns.</returns>
	public static async Task<List<CompiledRelationshipPattern>> LoadPatternsAsync(
		string filePath,
		CancellationToken cancellationToken = default)
	{
		if (!File.Exists(filePath))
		{
			throw new FileNotFoundException($"Relationship patterns file not found: {filePath}", filePath);
		}

		var json = await File.ReadAllTextAsync(filePath, cancellationToken);
		var config = JsonSerializer.Deserialize<RelationshipPatternsConfiguration>(json, _jsonOptions)
			?? throw new InvalidOperationException("Failed to deserialize relationship patterns configuration");

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
		CancellationToken cancellationToken = default)
	{
		// Try multiple locations in order of preference
		var searchPaths = new[]
		{
			// 1. Current directory
			DefaultPatternsPath,
			
			// 2. Application base directory
			Path.Combine(AppContext.BaseDirectory, DefaultPatternsPath),
			
			// 3. Entry assembly directory (for tests)
			Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly()?.Location ?? "") ?? "", DefaultPatternsPath),
			
			// 4. Executing assembly directory
			Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) ?? "", DefaultPatternsPath)
		};

		foreach (var path in searchPaths)
		{
			if (File.Exists(path))
			{
				return await LoadPatternsAsync(path, cancellationToken);
			}
		}

		// If not found anywhere, throw with helpful message
		throw new FileNotFoundException(
			$"Default relationship patterns file not found. Searched locations:\n" +
			$"{string.Join("\n", searchPaths.Select(p => $"  - {Path.GetFullPath(p)}"))}\n\n" +
			$"Ensure RelationshipPatterns.json is copied to the output directory.");
	}

	/// <summary>
	/// Validates pattern configuration without loading.
	/// </summary>
	/// <param name="filePath">Path to the JSON configuration file.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>Validation result.</returns>
	public static async Task<PatternValidationResult> ValidatePatternsAsync(
		string filePath,
		CancellationToken cancellationToken = default)
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

			var json = await File.ReadAllTextAsync(filePath, cancellationToken);
			var config = JsonSerializer.Deserialize<RelationshipPatternsConfiguration>(json, _jsonOptions);

			if (config == null)
			{
				result.IsValid = false;
				result.Errors.Add("Failed to deserialize configuration");
				return result;
			}

			// Validate each pattern
			foreach (var pattern in config.Patterns)
			{
				// Test regex compilation
				try
				{
					_ = pattern.CompileRegex();
				}
				catch (Exception ex)
				{
					result.IsValid = false;
					result.Errors.Add($"Pattern '{pattern.Name}': Invalid regex - {ex.Message}");
				}

				// Validate relationship type
				try
				{
					_ = pattern.GetRelationshipType();
				}
				catch (Exception ex)
				{
					result.IsValid = false;
					result.Errors.Add($"Pattern '{pattern.Name}': {ex.Message}");
				}

				// Validate confidence range
				if (pattern.Confidence is < 0.0 or > 1.0)
				{
					result.IsValid = false;
					result.Errors.Add($"Pattern '{pattern.Name}': Confidence must be between 0.0 and 1.0");
				}
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

	/// <summary>
	/// Saves patterns to a JSON file (for training tools).
	/// </summary>
	/// <param name="config">The configuration to save.</param>
	/// <param name="filePath">Path to save the JSON file.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	public static async Task SavePatternsAsync(
		RelationshipPatternsConfiguration config,
		string filePath,
		CancellationToken cancellationToken = default)
	{
		var json = JsonSerializer.Serialize(config, _jsonOptions);
		await File.WriteAllTextAsync(filePath, json, cancellationToken);
	}

	/// <summary>
	/// Creates a template configuration file for users to customize.
	/// </summary>
	/// <param name="filePath">Path to create the template file.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	public static async Task CreateTemplateAsync(
		string filePath,
		CancellationToken cancellationToken = default)
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
					Name = "CustomPattern",
					Regex = @"\b(custom|example)\b",
					RelationshipType = "RelatedTo",
					Confidence = 0.8,
					IsDirectional = false,
					Description = "Example pattern - replace with your own",
					Examples = ["Entity1 custom Entity2"],
					Category = "Custom",
					Enabled = false
				}
			],
			Categories = new Dictionary<string, string>
			{
				["Custom"] = "Custom patterns for your domain"
			},
			TrainingNotes = new TrainingNotes
			{
				Instructions = "Add new patterns based on observed relationships in your documents",
				PatternDesign = "Make patterns specific enough to avoid false positives",
				ConfidenceScoring = "Use 0.9-1.0 for very specific patterns, 0.5-0.7 for generic ones",
				TestingStrategy = "Test against ground truth and adjust confidence based on metrics"
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
