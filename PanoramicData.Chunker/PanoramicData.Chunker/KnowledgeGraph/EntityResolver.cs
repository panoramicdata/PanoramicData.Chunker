using PanoramicData.Chunker.Interfaces.KnowledgeGraph;
using PanoramicData.Chunker.Models.KnowledgeGraph;

namespace PanoramicData.Chunker.KnowledgeGraph;

/// <summary>
/// Resolves and deduplicates entities based on normalized names and aliases.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="EntityResolver"/> class.
/// </remarks>
/// <param name="normalizer">The entity normalizer to use.</param>
/// <param name="similarityThreshold">Similarity threshold for considering entities as duplicates (0.0 to 1.0).</param>
public class EntityResolver(IEntityNormalizer? normalizer = null, double similarityThreshold = 0.85) : IEntityResolver
{
	private readonly IEntityNormalizer _normalizer = normalizer ?? new BasicEntityNormalizer();

	/// <inheritdoc/>
	public List<Entity> Resolve(IEnumerable<Entity> entities)
	{
		var entityList = entities.ToList();
		if (entityList.Count == 0)
		{
			return [];
		}

		// Group entities by type for more efficient processing
		var groupedByType = entityList.GroupBy(e => e.Type);
		var resolvedEntities = new List<Entity>();

		foreach (var group in groupedByType)
		{
			var typeEntities = group.ToList();
			var resolved = ResolveGroup(typeEntities);
			resolvedEntities.AddRange(resolved);
		}

		return resolvedEntities;
	}

	/// <inheritdoc/>
	public Entity MergeEntities(IEnumerable<Entity> entities)
	{
		var entityList = entities.ToList();
		if (entityList.Count == 0)
		{
			throw new ArgumentException("Cannot merge empty entity list.", nameof(entities));
		}

		if (entityList.Count == 1)
		{
			return entityList[0];
		}

		// Use the entity with the highest confidence as the base
		var baseEntity = entityList.OrderByDescending(e => e.Confidence).First();

		// Merge all others into the base
		foreach (var entity in entityList.Where(e => e.Id != baseEntity.Id))
		{
			baseEntity.Merge(entity);
		}

		return baseEntity;
	}

	/// <inheritdoc/>
	public bool AreDuplicates(Entity entity1, Entity entity2)
	{
		if (entity1.Type != entity2.Type)
		{
			return false;
		}

		// Check normalized names
		var normalized1 = _normalizer.Normalize(entity1.Name, entity1.Type);
		var normalized2 = _normalizer.Normalize(entity2.Name, entity2.Type);

		if (normalized1.Equals(normalized2, StringComparison.OrdinalIgnoreCase))
		{
			return true;
		}

		// Check if either entity's name matches the other's aliases
		if (entity1.Aliases.Any(alias => _normalizer.AreEquivalent(alias, entity2.Name, entity1.Type)))
		{
			return true;
		}

		if (entity2.Aliases.Any(alias => _normalizer.AreEquivalent(alias, entity1.Name, entity2.Type)))
		{
			return true;
		}

		// Check string similarity for potential typos or variations
		var similarity = CalculateSimilarity(normalized1, normalized2);
		return similarity >= similarityThreshold;
	}

	private List<Entity> ResolveGroup(List<Entity> entities)
	{
		if (entities.Count == 0)
		{
			return [];
		}

		var resolved = new List<Entity>();
		var processed = new HashSet<Guid>();

		foreach (var entity in entities)
		{
			if (processed.Contains(entity.Id))
			{
				continue;
			}

			// Find all duplicates of this entity
			var duplicates = new List<Entity> { entity };
			processed.Add(entity.Id);

			foreach (var other in entities)
			{
				if (processed.Contains(other.Id))
				{
					continue;
				}

				if (AreDuplicates(entity, other))
				{
					duplicates.Add(other);
					processed.Add(other.Id);
				}
			}

			// Merge all duplicates
			var merged = MergeEntities(duplicates);
			resolved.Add(merged);
		}

		return resolved;
	}

	private static double CalculateSimilarity(string str1, string str2)
	{
		if (string.IsNullOrEmpty(str1) || string.IsNullOrEmpty(str2))
		{
			return 0.0;
		}

		if (str1.Equals(str2, StringComparison.OrdinalIgnoreCase))
		{
			return 1.0;
		}

		// Use Levenshtein distance for similarity
		var distance = LevenshteinDistance(str1, str2);
		var maxLength = Math.Max(str1.Length, str2.Length);

		return 1.0 - ((double)distance / maxLength);
	}

	private static int LevenshteinDistance(string str1, string str2)
	{
		var len1 = str1.Length;
		var len2 = str2.Length;

		var matrix = new int[len1 + 1, len2 + 1];

		// Initialize first column and row
		for (var i = 0; i <= len1; i++)
		{
			matrix[i, 0] = i;
		}

		for (var j = 0; j <= len2; j++)
		{
			matrix[0, j] = j;
		}

		// Calculate distances
		for (var i = 1; i <= len1; i++)
		{
			for (var j = 1; j <= len2; j++)
			{
				var cost = str1[i - 1] == str2[j - 1] ? 0 : 1;

				matrix[i, j] = Math.Min(
					Math.Min(
						matrix[i - 1, j] + 1,      // Deletion
						matrix[i, j - 1] + 1),     // Insertion
					matrix[i - 1, j - 1] + cost);  // Substitution
			}
		}

		return matrix[len1, len2];
	}
}
