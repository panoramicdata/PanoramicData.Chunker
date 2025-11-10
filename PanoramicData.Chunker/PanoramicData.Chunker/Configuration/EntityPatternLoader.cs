using System.Text.Json;

namespace PanoramicData.Chunker.Configuration;

/// <summary>
/// Service for loading and managing entity extraction patterns from JSON configuration.
/// </summary>
public class EntityPatternLoader
{
	private static readonly JsonSerializerOptions _jsonOptions = new()
	{
		PropertyNameCaseInsensitive = true,
		ReadCommentHandling = JsonCommentHandling.Skip,
		AllowTrailingCommas = true,
		WriteIndented = true
	};

	/// <summary>
	/// Default path to entity patterns file relative to application directory.
	/// </summary>
	public const string DefaultPatternsPath = "Configuration/EntityPatterns.json";

	/// <summary>
	/// Loads entity patterns from a JSON file.
	/// </summary>
	/// <param name="filePath">Path to the JSON configuration file.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>Entity patterns configuration.</returns>
	public static async Task<EntityPatternsConfiguration> LoadPatternsAsync(
		string filePath,
		CancellationToken cancellationToken = default)
	{
		if (!File.Exists(filePath))
		{
			throw new FileNotFoundException($"Entity patterns file not found: {filePath}", filePath);
		}

		var json = await File.ReadAllTextAsync(filePath, cancellationToken);
		var config = JsonSerializer.Deserialize<EntityPatternsConfiguration>(json, _jsonOptions)
			?? throw new InvalidOperationException("Failed to deserialize entity patterns configuration");

		return config;
	}

	/// <summary>
	/// Loads patterns from default location (Configuration/EntityPatterns.json in app directory).
	/// </summary>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>Entity patterns configuration.</returns>
	public static async Task<EntityPatternsConfiguration> LoadDefaultPatternsAsync(
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
			$"Default entity patterns file not found. Searched locations:\n" +
			$"{string.Join("\n", searchPaths.Select(p => $"  - {Path.GetFullPath(p)}"))}\n\n" +
			$"Ensure EntityPatterns.json is copied to the output directory.");
	}

	/// <summary>
	/// Saves patterns to a JSON file (for training tools).
	/// </summary>
	/// <param name="config">The configuration to save.</param>
	/// <param name="filePath">Path to save the JSON file.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	public static async Task SavePatternsAsync(
		EntityPatternsConfiguration config,
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
		var template = new EntityPatternsConfiguration
		{
			Version = "2.0",
			Description = "Custom entity extraction patterns - Add domain-specific proper nouns here",
			LastUpdated = DateTime.UtcNow.ToString("yyyy-MM-dd"),
			ProperNounDictionary = new ProperNounDictionary
			{
				People = ["CustomPerson"],
				Places = ["CustomPlace"],
				Organizations = ["CustomOrg"]
			},
			TitlePrefixes = new Dictionary<string, List<string>>
			{
				["custom"] = ["CustomTitle"]
			},
			OrganizationalSuffixes = ["CustomSuffix"],
			AllowedConnectors = ["of", "the", "and"],
			SentenceStarters = ["The", "In", "On"],
			ExtractionRules = new ExtractionRules
			{
				MinWordLength = 2,
				MinOccurrences = 1,
				BaseConfidence = 0.7,
				ConfidenceBoosts = new ConfidenceBoosts
				{
					InDictionary = 0.15,
					HasTitle = 0.10,
					MultiWord = 0.10,
					OrganizationalSuffix = 0.10,
					PerFrequency = 0.05,
					MaxFrequencyBoost = 0.20
				}
			},
			TrainingNotes = new EntityTrainingNotes
			{
				Instructions = "Add commonly occurring proper nouns to the dictionaries",
				DictionaryMaintenance = "Review extracted entities regularly and add high-frequency ones",
				CategoryClassification = "Classify entities by type for better accuracy",
				PerformanceMonitoring = "Track precision/recall and adjust thresholds"
			}
		};

		await SavePatternsAsync(template, filePath, cancellationToken);
	}
}
