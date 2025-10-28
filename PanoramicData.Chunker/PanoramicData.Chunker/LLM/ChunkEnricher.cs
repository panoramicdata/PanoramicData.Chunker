using Microsoft.Extensions.Logging;
using PanoramicData.Chunker.Configuration;
using PanoramicData.Chunker.Interfaces;
using PanoramicData.Chunker.Models;
using PanoramicData.Chunker.Models.Llm;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace PanoramicData.Chunker.LLM;

/// <summary>
/// Enriches chunks with LLM-generated metadata.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="ChunkEnricher"/> class.
/// </remarks>
/// <param name="llmProvider">LLM provider.</param>
/// <param name="promptManager">Prompt template manager.</param>
/// <param name="cache">Enrichment cache.</param>
/// <param name="options">Enrichment options.</param>
/// <param name="logger">Logger instance.</param>
public class ChunkEnricher(
	ILlmProvider llmProvider,
	IPromptTemplate promptManager,
	IEnrichmentCache cache,
	LLMEnrichmentOptions options,
	ILogger<ChunkEnricher> logger) : IChunkEnricher
{
	private readonly ILlmProvider _llmProvider = llmProvider ?? throw new ArgumentNullException(nameof(llmProvider));
	private readonly IPromptTemplate _promptManager = promptManager ?? throw new ArgumentNullException(nameof(promptManager));
	private readonly IEnrichmentCache _cache = cache ?? throw new ArgumentNullException(nameof(cache));
	private readonly LLMEnrichmentOptions _options = options ?? throw new ArgumentNullException(nameof(options));
	private readonly ILogger<ChunkEnricher> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

	/// <inheritdoc/>
	public async Task<EnrichedChunk> EnrichAsync(
		ChunkerBase chunk,
		CancellationToken cancellationToken)
	{
		var stopwatch = Stopwatch.StartNew();

		// Get content from chunk (handle different chunk types)
		var content = GetChunkContent(chunk);
		if (string.IsNullOrWhiteSpace(content))
		{
			_logger.LogWarning("Chunk {ChunkId} has no content, skipping enrichment", chunk.Id);
			return new EnrichedChunk
			{
				ChunkId = chunk.Id,
				OriginalContent = string.Empty,
				EnrichmentDuration = stopwatch.Elapsed
			};
		}

		var cacheKey = GenerateCacheKey(content);

		// Check cache first
		if (_options.EnableCaching && _cache.TryGet(cacheKey, out var cachedResult))
		{
			_logger.LogDebug("Cache hit for chunk {ChunkId}", chunk.Id);
			// Update ChunkId to match current chunk
			return cachedResult! with { ChunkId = chunk.Id };
		}

		var enriched = new EnrichedChunk
		{
			ChunkId = chunk.Id,
			OriginalContent = content
		};

		var totalTokens = 0;

		// Generate summary
		if (_options.EnableSummarization)
		{
			var (summary, tokens) = await GenerateSummaryAsync(content, cancellationToken);
			enriched = enriched with { Summary = summary };
			totalTokens += tokens;
		}

		// Extract keywords
		if (_options.EnableKeywordExtraction)
		{
			var (keywords, tokens) = await ExtractKeywordsAsync(content, cancellationToken);
			enriched = enriched with { Keywords = keywords };
			totalTokens += tokens;
		}

		// Extract preliminary entities
		if (_options.EnablePreliminaryNER)
		{
			var (entities, tokens) = await ExtractEntitiesAsync(content, cancellationToken);
			enriched = enriched with { PreliminaryEntities = entities };
			totalTokens += tokens;
		}

		stopwatch.Stop();

		enriched = enriched with
		{
			TokensUsed = totalTokens,
			EnrichmentDuration = stopwatch.Elapsed
		};

		// Cache result
		if (_options.EnableCaching)
		{
			_cache.Set(cacheKey, enriched, _options.CacheDuration);
		}

		_logger.LogDebug(
			"Enriched chunk {ChunkId} in {Duration}ms with {Tokens} tokens",
			chunk.Id,
			stopwatch.ElapsedMilliseconds,
			totalTokens);

		return enriched;
	}

	/// <inheritdoc/>
	public async Task<IEnumerable<EnrichedChunk>> EnrichBatchAsync(
		IEnumerable<ChunkerBase> chunks,
		CancellationToken cancellationToken)
	{
		var semaphore = new SemaphoreSlim(_options.MaxConcurrency);
		var tasks = chunks.Select(async chunk =>
		{
			await semaphore.WaitAsync(cancellationToken);
			try
			{
				return await EnrichAsync(chunk, cancellationToken);
			}
			finally
			{
				_ = semaphore.Release();
			}
		});

		return await Task.WhenAll(tasks);
	}

	private async Task<(string Summary, int Tokens)> GenerateSummaryAsync(
		string content,
		CancellationToken cancellationToken)
	{
		try
		{
			var prompt = _promptManager.RenderTemplate("summarize", new Dictionary<string, string>
			{
				["content"] = content,
				["max_words"] = _options.SummaryMaxWords.ToString()
			});

			var request = new LlmRequest
			{
				Prompt = prompt,
				Model = _options.Model,
				Temperature = 0.7,
				MaxTokens = _options.SummaryMaxTokens
			};

			var response = await _llmProvider.GenerateAsync(request, cancellationToken);

			if (!response.Success)
			{
				_logger.LogWarning("Summary generation failed: {Error}", response.ErrorMessage);
				return (string.Empty, 0);
			}

			return (response.Text.Trim(), response.TokensUsed);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error generating summary");
			return (string.Empty, 0);
		}
	}

	private async Task<(List<string> Keywords, int Tokens)> ExtractKeywordsAsync(
		string content,
		CancellationToken cancellationToken)
	{
		try
		{
			var prompt = _promptManager.RenderTemplate("extract_keywords", new Dictionary<string, string>
			{
				["content"] = content,
				["max_keywords"] = _options.MaxKeywords.ToString()
			});

			var request = new LlmRequest
			{
				Prompt = prompt,
				Model = _options.Model,
				Temperature = 0.3, // Lower temperature for keyword extraction
				MaxTokens = 200
			};

			var response = await _llmProvider.GenerateAsync(request, cancellationToken);

			if (!response.Success)
			{
				_logger.LogWarning("Keyword extraction failed: {Error}", response.ErrorMessage);
				return ([], 0);
			}

			// Parse comma-separated keywords
			var keywords = response.Text
				.Split(',')
				.Select(k => k.Trim())
				.Where(k => !string.IsNullOrWhiteSpace(k))
				.Take(_options.MaxKeywords)
				.ToList();

			return (keywords, response.TokensUsed);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error extracting keywords");
			return ([], 0);
		}
	}

	private async Task<(List<PreliminaryEntity> Entities, int Tokens)> ExtractEntitiesAsync(
		string content,
		CancellationToken cancellationToken)
	{
		try
		{
			var prompt = _promptManager.RenderTemplate("extract_entities", new Dictionary<string, string>
			{
				["content"] = content
			});

			var request = new LlmRequest
			{
				Prompt = prompt,
				Model = _options.Model,
				Temperature = 0.3,
				MaxTokens = 500
			};

			var response = await _llmProvider.GenerateAsync(request, cancellationToken);

			if (!response.Success)
			{
				_logger.LogWarning("Entity extraction failed: {Error}", response.ErrorMessage);
				return ([], 0);
			}

			// Parse JSON response (basic parsing, will be enhanced in Phase 12)
			try
			{
				var entities = JsonSerializer.Deserialize<List<PreliminaryEntity>>(response.Text)
					?? [];

				return (entities, response.TokensUsed);
			}
			catch (JsonException ex)
			{
				_logger.LogWarning(ex, "Failed to parse entity extraction JSON");
				return ([], response.TokensUsed);
			}
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error extracting entities");
			return ([], 0);
		}
	}

	private string GenerateCacheKey(string content)
	{
		// Cache key based on content hash + model + enabled features
		var contentHash = GetHashString(content);
		var features = $"{_options.EnableSummarization}|{_options.EnableKeywordExtraction}|{_options.EnablePreliminaryNER}";
		return $"{_options.Model}:{features}:{contentHash}";
	}

	private static string GetHashString(string input)
	{
		var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
		return Convert.ToHexString(bytes)[..16].ToLowerInvariant();
	}

	private static string GetChunkContent(ChunkerBase chunk)
	{
		// Handle ContentChunk types
		if (chunk is ContentChunk contentChunk)
		{
			return contentChunk.Content;
		}

		// Handle StructuralChunk and VisualChunk types
		// For now, return empty string for non-content chunks
		// Future: could aggregate child content or use MarkdownContent if available
		return string.Empty;
	}
}
