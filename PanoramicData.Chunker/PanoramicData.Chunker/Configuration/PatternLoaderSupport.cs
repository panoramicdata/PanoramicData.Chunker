using System.Text.Json;

namespace PanoramicData.Chunker.Configuration;

/// <summary>
/// File access and JSON handling shared by the entity and relationship pattern loaders.
/// </summary>
internal static class PatternLoaderSupport
{
	private static readonly JsonSerializerOptions JsonOptions = new()
	{
		PropertyNameCaseInsensitive = true,
		ReadCommentHandling = JsonCommentHandling.Skip,
		AllowTrailingCommas = true,
		WriteIndented = true
	};

	public static async Task<T> ReadAsync<T>(string filePath, string kind, CancellationToken cancellationToken)
		where T : class
	{
		if (!File.Exists(filePath))
		{
			throw new FileNotFoundException($"{kind} patterns file not found: {filePath}", filePath);
		}

		return await TryReadAsync<T>(filePath, cancellationToken)
			?? throw new InvalidOperationException($"Failed to deserialize {kind.ToLowerInvariant()} patterns configuration");
	}

	public static async Task<T?> TryReadAsync<T>(string filePath, CancellationToken cancellationToken)
		where T : class
	{
		var json = await File.ReadAllTextAsync(filePath, cancellationToken);
		return JsonSerializer.Deserialize<T>(json, JsonOptions);
	}

	public static async Task SaveAsync<T>(T config, string filePath, CancellationToken cancellationToken)
	{
		var json = JsonSerializer.Serialize(config, JsonOptions);
		await File.WriteAllTextAsync(filePath, json, cancellationToken);
	}

	public static string FindDefaultPath(string defaultPatternsPath, string kind)
	{
		// Try multiple locations in order of preference
		var searchPaths = new[]
		{
			defaultPatternsPath,
			Path.Combine(AppContext.BaseDirectory, defaultPatternsPath),
			Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly()?.Location ?? "") ?? "", defaultPatternsPath),
			Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) ?? "", defaultPatternsPath)
		};

		return searchPaths.FirstOrDefault(File.Exists)
			?? throw new FileNotFoundException(
				$"Default {kind.ToLowerInvariant()} patterns file not found. Searched locations:\n" +
				$"{string.Join("\n", searchPaths.Select(p => $"  - {Path.GetFullPath(p)}"))}\n\n" +
				$"Ensure {Path.GetFileName(defaultPatternsPath)} is copied to the output directory.");
	}
}
