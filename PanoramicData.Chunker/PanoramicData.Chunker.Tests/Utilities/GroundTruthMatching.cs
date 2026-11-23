using PanoramicData.Chunker.Models.KnowledgeGraph;

namespace PanoramicData.Chunker.Tests.Utilities;

internal static class GroundTruthMatching
{
	public static readonly string[] DarwinEntities =
	[
		"Plinian Society", "Professor Jameson", "HMS Beagle", "Charles Darwin",
		"Edinburgh University", "Robert Grant", "Captain FitzRoy", "Galapagos Islands",
		"John Henslow", "Cambridge University", "Origin of Species"
	];

	public static bool IsFound(IEnumerable<Entity> entities, string groundTruth)
		=> entities.Any(e =>
			e.Name.Equals(groundTruth, StringComparison.OrdinalIgnoreCase) ||
			e.Name.Contains(groundTruth, StringComparison.OrdinalIgnoreCase) ||
			groundTruth.Contains(e.Name, StringComparison.OrdinalIgnoreCase) ||
			e.Aliases.Any(a =>
				a.Equals(groundTruth, StringComparison.OrdinalIgnoreCase) ||
				groundTruth.Contains(a, StringComparison.OrdinalIgnoreCase)));

	public static void WriteProperNouns(ITestOutputHelper output, IEnumerable<Entity> entities)
	{
		var properNouns = entities
			.Where(e => e.Type == EntityType.ProperNoun)
			.OrderByDescending(e => e.Confidence)
			.Take(50)
			.ToList();

		output.WriteLine($"Proper Nouns ({properNouns.Count}):");
		foreach (var entity in properNouns)
		{
			output.WriteLine($"  {entity.Name} (conf: {entity.Confidence:F2}, freq: {entity.Frequency})");
			if (entity.Aliases.Count > 0)
			{
				output.WriteLine($"    Aliases: {string.Join(", ", entity.Aliases)}");
			}
		}
	}
}