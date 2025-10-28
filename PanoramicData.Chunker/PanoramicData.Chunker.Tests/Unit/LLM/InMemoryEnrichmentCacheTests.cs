using AwesomeAssertions;
using PanoramicData.Chunker.LLM.Caching;
using PanoramicData.Chunker.Models.Llm;

namespace PanoramicData.Chunker.Tests.Unit.LLM;

public class InMemoryEnrichmentCacheTests(ITestOutputHelper output) : BaseTest(output)
{
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
		var enrichedChunk = new EnrichedChunk
		{
			ChunkId = Guid.NewGuid(),
			OriginalContent = "Test content",
			Summary = "Test summary",
			Keywords = ["test", "content"],
			TokensUsed = 50
		};

		// Act
		cache.Set("test-key", enrichedChunk, TimeSpan.FromHours(1));
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
		var enrichedChunk = new EnrichedChunk
		{
			ChunkId = Guid.NewGuid(),
			OriginalContent = "Test",
			Summary = "Summary"
		};

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
		var cache = new InMemoryEnrichmentCache();
		var enrichedChunk = new EnrichedChunk
		{
			ChunkId = Guid.NewGuid(),
			OriginalContent = "Test"
		};

		cache.Set("key", enrichedChunk, TimeSpan.FromHours(1));

		// Act
		_ = cache.TryGet("key", out _);
		_ = cache.TryGet("key", out _);
		var stats = cache.GetStatistics();

		// Assert
		_ = stats.Hits.Should().Be(2);
		_ = stats.Misses.Should().Be(0);
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
		_ = stats.Hits.Should().Be(0);
		_ = stats.Misses.Should().Be(2);
		_ = stats.Size.Should().Be(0);

		_output.WriteLine($"Cache statistics - Hits: {stats.Hits}, Misses: {stats.Misses}");
	}

	[Fact]
	public void GetStatistics_HitRate_ShouldBeCalculatedCorrectly()
	{
		// Arrange
		var cache = new InMemoryEnrichmentCache();
		var enrichedChunk = new EnrichedChunk
		{
			ChunkId = Guid.NewGuid(),
			OriginalContent = "Test"
		};

		cache.Set("key", enrichedChunk, TimeSpan.FromHours(1));

		// Act
		_ = cache.TryGet("key", out _); // Hit
		_ = cache.TryGet("key", out _); // Hit
		_ = cache.TryGet("nonexistent", out _); // Miss
		var stats = cache.GetStatistics();

		// Assert
		_ = stats.Hits.Should().Be(2);
		_ = stats.Misses.Should().Be(1);
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
			cache.Set($"key{i}", new EnrichedChunk
			{
				ChunkId = Guid.NewGuid(),
				OriginalContent = $"Content {i}"
			}, TimeSpan.FromHours(1));
		}

		// Act
		cache.Clear();
		var stats = cache.GetStatistics();

		// Assert
		_ = stats.Size.Should().Be(0);
		_ = stats.Hits.Should().Be(0);
		_ = stats.Misses.Should().Be(0);

		_output.WriteLine("Cache cleared successfully");
	}

	[Fact]
	public void Set_WithSameKey_ShouldOverwritePreviousValue()
	{
		// Arrange
		var cache = new InMemoryEnrichmentCache();
		var chunk1 = new EnrichedChunk
		{
			ChunkId = Guid.NewGuid(),
			OriginalContent = "Original",
			Summary = "First summary"
		};
		var chunk2 = new EnrichedChunk
		{
			ChunkId = Guid.NewGuid(),
			OriginalContent = "Updated",
			Summary = "Second summary"
		};

		// Act
		cache.Set("key", chunk1, TimeSpan.FromHours(1));
		cache.Set("key", chunk2, TimeSpan.FromHours(1));
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
		_ = stats.Hits.Should().Be(0);
		_ = stats.Misses.Should().Be(0);

		_output.WriteLine($"Initial hit rate: {stats.HitRate}");
	}
}
