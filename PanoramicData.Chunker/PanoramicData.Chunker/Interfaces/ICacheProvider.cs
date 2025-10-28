using PanoramicData.Chunker.Models;

namespace PanoramicData.Chunker.Interfaces;

/// <summary>
/// Interface for caching chunking results.
/// </summary>
public interface ICacheProvider
{
	/// <summary>
	/// Get cached chunking result.
	/// </summary>
	/// <param name="key">The cache key.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>The cached result, or null if not found.</returns>
	Task<ChunkingResult?> GetAsync(string key, CancellationToken cancellationToken);

	/// <summary>
	/// Set chunking result in cache.
	/// </summary>
	/// <param name="key">The cache key.</param>
	/// <param name="result">The result to cache.</param>
	/// <param name="expiration">Optional expiration time.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	Task SetAsync(
		string key,
		ChunkingResult result,
		TimeSpan? expiration,
		CancellationToken cancellationToken);

	/// <summary>
	/// Remove cached result.
	/// </summary>
	/// <param name="key">The cache key.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	Task RemoveAsync(string key, CancellationToken cancellationToken);
}
