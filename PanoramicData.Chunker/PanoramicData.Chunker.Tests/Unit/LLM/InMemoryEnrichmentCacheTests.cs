using AwesomeAssertions;
using PanoramicData.Chunker.Interfaces;
using PanoramicData.Chunker.LLM.Caching;
using PanoramicData.Chunker.Models.Llm;

namespace PanoramicData.Chunker.Tests.Unit.LLM;

public class InMemoryEnrichmentCacheTests(ITestOutputHelper output) : BaseTest(output)
{
	private static readonly TimeSpan _oneHour = TimeSpan.FromHours(1);

	[Fact]
	public void TryGet_WithNonExistentKey_ShouldReturnFalse()
	{
		// Arrange
		var cache = new InMemoryEnrichmentCache();

		// Act
		var result = cache.TryGet("nonexistent", out var value);

		// Assert
		_ = result.Should().BeFalse();
		_ = value.Should().BeNull();
	}

	[Fact]
	public void Set_AndTryGet_ShouldReturnCachedValue()
	{
		// Arrange
		var cache = new InMemoryEnrichmentCache();
		var enrichedChunk = CreateChunk("Test content", "Test summary") with
		{
			Keywords = ["test", "content"],
			TokensUsed = 50
		};

		// Act
		cache.Set("test-key", enrichedChunk, _oneHour);
		var result = cache.TryGet("test-key", out var value);

		// Assert
		_ = result.Should().BeTrue();
		_ = value.Should().NotBeNull();
		_ = value!.Summary.Should().Be("Test summary");
		_ = value.Keywords.Should().Contain("test");

		_output.WriteLine($"Cached value retrieved: {value.Summary}");
	}

	[Fact]
	public async Task TryGet_WithExpiredEntry_ShouldReturnFalse()
	{
		// Arrange
		var cache = new InMemoryEnrichmentCache();
		var enrichedChunk = CreateChunk("Test", "Summary");

		// Act
		cache.Set("test-key", enrichedChunk, TimeSpan.FromMilliseconds(50));
		await Task.Delay(100, CancellationToken); // Wait for expiration
		var result = cache.TryGet("test-key", out var value);

		// Assert
		_ = result.Should().BeFalse();
		_ = value.Should().BeNull();

		_output.WriteLine("Cache entry expired as expected");
	}

	[Fact]
	public void GetStatistics_AfterCacheHit_ShouldIncrementHits()
	{
		// Arrange
		var cache = CreateCacheWithKey();

		// Act
		_ = cache.TryGet("key", out _);
		_ = cache.TryGet("key", out _);
		var stats = cache.GetStatistics();

		// Assert
		AssertCounts(stats, 2, 0);
		_ = stats.Size.Should().Be(1);

		_output.WriteLine($"Cache statistics - Hits: {stats.Hits}, Misses: {stats.Misses}, Size: {stats.Size}");
	}

	[Fact]
	public void GetStatistics_AfterCacheMiss_ShouldIncrementMisses()
	{
		// Arrange
		var cache = new InMemoryEnrichmentCache();

		// Act
		_ = cache.TryGet("nonexistent1", out _);
		_ = cache.TryGet("nonexistent2", out _);
		var stats = cache.GetStatistics();

		// Assert
		AssertCounts(stats, 0, 2);
		_ = stats.Size.Should().Be(0);

		_output.WriteLine($"Cache statistics - Hits: {stats.Hits}, Misses: {stats.Misses}");
	}

	[Fact]
	public void GetStatistics_HitRate_ShouldBeCalculatedCorrectly()
	{
		// Arrange
		var cache = CreateCacheWithKey();

		// Act
		_ = cache.TryGet("key", out _); // Hit
		_ = cache.TryGet("key", out _); // Hit
		_ = cache.TryGet("nonexistent", out _); // Miss
		var stats = cache.GetStatistics();

		// Assert
		AssertCounts(stats, 2, 1);
		_ = stats.HitRate.Should().BeApproximately(0.666, 0.01); // 2/3

		_output.WriteLine($"Hit rate: {stats.HitRate:P}");
	}

	[Fact]
	public void Clear_ShouldRemoveAllEntries()
	{
		// Arrange
		var cache = new InMemoryEnrichmentCache();
		for (var i = 0; i < 5; i++)
		{
			cache.Set($"key{i}", CreateChunk($"Content {i}"), _oneHour);
		}

		// Act
		cache.Clear();
		var stats = cache.GetStatistics();

		// Assert
		_ = stats.Size.Should().Be(0);
		AssertCounts(stats, 0, 0);

		_output.WriteLine("Cache cleared successfully");
	}

	[Fact]
	public void Set_WithSameKey_ShouldOverwritePreviousValue()
	{
		// Arrange
		var cache = new InMemoryEnrichmentCache();
		var chunk1 = CreateChunk("Original", "First summary");
		var chunk2 = CreateChunk("Updated", "Second summary");

		// Act
		cache.Set("key", chunk1, _oneHour);
		cache.Set("key", chunk2, _oneHour);
		_ = cache.TryGet("key", out var value);

		// Assert
		_ = value.Should().NotBeNull();
		_ = value!.Summary.Should().Be("Second summary");

		_output.WriteLine($"Updated cache value: {value.Summary}");
	}

	[Fact]
	public void GetStatistics_WithZeroAccess_ShouldReturnZeroHitRate()
	{
		// Arrange
		var cache = new InMemoryEnrichmentCache();

		// Act
		var stats = cache.GetStatistics();

		// Assert
		_ = stats.HitRate.Should().Be(0.0);
		AssertCounts(stats, 0, 0);

		_output.WriteLine($"Initial hit rate: {stats.HitRate}");
	}

	private static EnrichedChunk CreateChunk(string content, string? summary = null) => new()
	{
		ChunkId = Guid.NewGuid(),
		OriginalContent = content,
		Summary = summary
	};

	private static InMemoryEnrichmentCache CreateCacheWithKey()
	{
		var cache = new InMemoryEnrichmentCache();
		cache.Set("key", CreateChunk("Test"), _oneHour);
		return cache;
	}

	private static void AssertCounts(CacheStatistics stats, int hits, int misses)
	{
		_ = stats.Hits.Should().Be(hits);
		_ = stats.Misses.Should().Be(misses);
	}
}
