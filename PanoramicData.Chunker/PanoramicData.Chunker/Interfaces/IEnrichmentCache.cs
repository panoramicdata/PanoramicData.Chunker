using PanoramicData.Chunker.Models.LLM;

namespace PanoramicData.Chunker.Interfaces;

/// <summary>
/// Interface for caching enrichment results.
/// </summary>
public interface IEnrichmentCache
{
	/// <summary>
	/// Tries to get a cached enrichment result.
	/// </summary>
	/// <param name="cacheKey">The cache key.</param>
	/// <param name="result">The cached result if found.</param>
	/// <returns>True if found in cache, false otherwise.</returns>
	bool TryGet(string cacheKey, out EnrichedChunk? result);

	/// <summary>
	/// Sets a cache entry.
	/// </summary>
	/// <param name="cacheKey">The cache key.</param>
	/// <param name="value">The value to cache.</param>
	/// <param name="duration">How long to cache the value.</param>
	void Set(string cacheKey, EnrichedChunk value, TimeSpan duration);

	/// <summary>
	/// Clears all cached entries.
	/// </summary>
	void Clear();

	/// <summary>
	/// Gets cache statistics.
	/// </summary>
	/// <returns>Cache statistics (hits, misses, size).</returns>
	CacheStatistics GetStatistics();
}

/// <summary>
/// Statistics for enrichment cache.
/// </summary>
public record CacheStatistics
{
	/// <summary>
	/// Number of cache hits.
	/// </summary>
	public long Hits { get; init; }

	/// <summary>
	/// Number of cache misses.
	/// </summary>
	public long Misses { get; init; }

	/// <summary>
	/// Current number of entries in cache.
	/// </summary>
	public int Size { get; init; }

	/// <summary>
	/// Cache hit rate (0.0-1.0).
	/// </summary>
	public double HitRate => Hits + Misses > 0 ? (double)Hits / (Hits + Misses) : 0.0;
}
