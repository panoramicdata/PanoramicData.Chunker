using PanoramicData.Chunker.Models.KnowledgeGraph;

namespace PanoramicData.Chunker.Interfaces.KnowledgeGraph;

/// <summary>
/// Interface for normalizing entity names and values.
/// </summary>
public interface IEntityNormalizer
{
	/// <summary>
	/// Normalizes an entity name for comparison and deduplication.
	/// </summary>
	/// <param name="name">The raw entity name.</param>
	/// <param name="entityType">The type of entity.</param>
	/// <returns>The normalized name.</returns>
	string Normalize(string name, EntityType entityType);

	/// <summary>
	/// Determines if two entity names are equivalent after normalization.
	/// </summary>
	/// <param name="name1">The first name.</param>
	/// <param name="name2">The second name.</param>
	/// <param name="entityType">The type of entity.</param>
	/// <returns>True if the names are equivalent.</returns>
	bool AreEquivalent(string name1, string name2, EntityType entityType);

	/// <summary>
	/// Generates potential aliases for an entity name.
	/// </summary>
	/// <param name="name">The entity name.</param>
	/// <param name="entityType">The type of entity.</param>
	/// <returns>List of potential aliases.</returns>
	List<string> GenerateAliases(string name, EntityType entityType);
}
