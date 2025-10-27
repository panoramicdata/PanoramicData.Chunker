using PanoramicData.Chunker.Interfaces;
using PanoramicData.Chunker.Models.Llm;
using System.Collections.Concurrent;

namespace PanoramicData.Chunker.LLM.Caching;

/// <summary>
/// In-memory cache implementation for enrichment results.
/// </summary>
public class InMemoryEnrichmentCache : IEnrichmentCache
{
	private readonly ConcurrentDictionary<string, CacheEntry> _cache = new();
	private long _hits;
	private long _misses;

	/// <inheritdoc/>
	public bool TryGet(string cacheKey, out EnrichedChunk? result)
	{
		if (_cache.TryGetValue(cacheKey, out var entry))
		{
			// Check if expired
			if (entry.ExpiresAt > DateTimeOffset.UtcNow)
			{
				_ = Interlocked.Increment(ref _hits);
				result = entry.Value;
				return true;
			}
			else
			{
				// Remove expired entry
				_ = _cache.TryRemove(cacheKey, out _);
			}
		}

		_ = Interlocked.Increment(ref _misses);
		result = null;
		return false;
	}

	/// <inheritdoc/>
	public void Set(string cacheKey, EnrichedChunk value, TimeSpan duration)
	{
		var entry = new CacheEntry
		{
			Value = value,
			ExpiresAt = DateTimeOffset.UtcNow.Add(duration)
		};

		_cache[cacheKey] = entry;
	}

	/// <inheritdoc/>
	public void Clear()
	{
		_cache.Clear();
		_hits = 0;
		_misses = 0;
	}

	/// <inheritdoc/>
	public CacheStatistics GetStatistics() => new()
	{
		Hits = _hits,
		Misses = _misses,
		Size = _cache.Count
	};

	private record CacheEntry
	{
		public required EnrichedChunk Value { get; init; }
		public required DateTimeOffset ExpiresAt { get; init; }
	}
}
