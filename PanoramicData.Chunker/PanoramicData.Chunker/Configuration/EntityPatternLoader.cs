using System.Runtime.InteropServices;
using System.Text.Json;

namespace PanoramicData.Chunker.Configuration;

/// <summary>
/// Service for loading and managing entity extraction patterns from JSON configuration.
/// </summary>
public class EntityPatternLoader
{
	/// <summary>
	/// Default path to entity patterns file relative to application directory.
	/// </summary>
	public static string DefaultPatternsPath { get; } = "Configuration/EntityPatterns.json";

	protected EntityPatternLoader()
	{
	}

	/// <summary>
	/// Loads entity patterns from a JSON file.
	/// </summary>
	/// <param name="filePath">Path to the JSON configuration file.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>Entity patterns configuration.</returns>
	public static async Task<EntityPatternsConfiguration> LoadPatternsAsync(
		string filePath,
		[Optional] CancellationToken cancellationToken)
		=> await PatternLoaderSupport.ReadAsync<EntityPatternsConfiguration>(filePath, "Entity", cancellationToken);

	/// <summary>
	/// Loads patterns from default location (Configuration/EntityPatterns.json in app directory).
	/// </summary>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>Entity patterns configuration.</returns>
	public static async Task<EntityPatternsConfiguration> LoadDefaultPatternsAsync(
		[Optional] CancellationToken cancellationToken)
	{
		var path = PatternLoaderSupport.FindDefaultPath(DefaultPatternsPath, "Entity");
		return await LoadPatternsAsync(path, cancellationToken);
	}

	/// <summary>
	/// Creates a template configuration file for users to customize.
	/// </summary>
	/// <param name="filePath">Path to create the template file.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	public static async Task CreateTemplateAsync(
		string filePath,
		[Optional] CancellationToken cancellationToken)
		=> await SavePatternsAsync(BuildTemplate(), filePath, cancellationToken);

	private const string TemplateJson = """
		{
			"Version": "2.0",
			"Description": "Custom entity extraction patterns - Add domain-specific proper nouns here",
			"ProperNounDictionary": { "People": ["CustomPerson"], "Places": ["CustomPlace"], "Organizations": ["CustomOrg"] },
			"TitlePrefixes": { "custom": ["CustomTitle"] },
			"OrganizationalSuffixes": ["CustomSuffix"],
			"AllowedConnectors": ["of", "the", "and"],
			"SentenceStarters": ["The", "In", "On"],
			"ExtractionRules": {
				"MinWordLength": 2,
				"MinOccurrences": 1,
				"BaseConfidence": 0.7,
				"ConfidenceBoosts": {
					"InDictionary": 0.15,
					"HasTitle": 0.1,
					"MultiWord": 0.1,
					"OrganizationalSuffix": 0.1,
					"PerFrequency": 0.05,
					"MaxFrequencyBoost": 0.2
				}
			},
			"TrainingNotes": {
				"Instructions": "Add commonly occurring proper nouns to the dictionaries",
				"DictionaryMaintenance": "Review extracted entities regularly and add high-frequency ones",
				"CategoryClassification": "Classify entities by type for better accuracy",
				"PerformanceMonitoring": "Track precision/recall and adjust thresholds"
			}
		}
		""";

	private static EntityPatternsConfiguration BuildTemplate()
	{
		var template = JsonSerializer.Deserialize<EntityPatternsConfiguration>(TemplateJson)!;
		template.LastUpdated = DateTime.UtcNow.ToString("yyyy-MM-dd");
		return template;
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
		[Optional] CancellationToken cancellationToken)
	{
		await PatternLoaderSupport.SaveAsync(config, filePath, cancellationToken);
	}
}