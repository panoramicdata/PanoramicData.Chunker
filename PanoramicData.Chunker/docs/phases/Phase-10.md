# Phase 10: LLM Integration (Ollama)

[← Back to Master Plan](../MasterPlan.md)

---

## Phase Information

| Attribute | Value |
|-----------|-------|
| **Phase Number** | 10 |
| **Status** | 📅 **PENDING** |
| **Duration** | 2-3 weeks |
| **Prerequisites** | Phase 9 (PDF Chunking) complete |
| **Test Count** | 25+ |
| **Documentation** | 📅 Pending |
| **LOC Estimate** | ~2,000 |

---

## Objective

Integrate Ollama LLM provider for chunk enrichment including summaries, keyword extraction, and preliminary named entity recognition (NER). This phase lays the foundation for advanced Knowledge Graph capabilities in Phases 11-16.

---

## Why This Phase?

- **Foundation for Knowledge Graph**: LLM-based enrichment is essential for high-quality entity extraction
- **Local-First**: Ollama enables privacy-preserving, cost-free LLM inference
- **Extensibility**: Architecture supports future providers (OpenAI, Azure, Anthropic)
- **Value Delivery**: Even basic summarization provides immediate value to users

---

## Tasks

### 10.1. Core LLM Abstractions ⬜ PENDING

- [ ] Define `ILLMProvider` interface
- [ ] Define `LLMOptions` configuration class
- [ ] Define `LLMResponse` model
- [ ] Define `LLMRequest` model
- [ ] Define `EnrichedChunk` model
- [ ] Define `IPromptTemplate` interface
- [ ] Define `IChunkEnricher` interface
- [ ] Add XML documentation for all interfaces

### 10.2. Ollama Provider Implementation ⬜ PENDING

- [ ] Install `Ollama.Api` NuGet package
- [ ] Implement `OllamaLLMProvider` class
- [ ] Connection management (configurable base URL)
- [ ] Model configuration (llama3, mistral, etc.)
- [ ] Streaming support (optional)
- [ ] Error handling and retry logic
- [ ] Timeout configuration
- [ ] Token usage tracking

### 10.3. Prompt Template Management ⬜ PENDING

- [ ] Implement `PromptTemplateManager` class
- [ ] Default templates for:
  - Chunk summarization
  - Keyword extraction
  - Preliminary NER (Person, Organization, Location)
- [ ] Template variable substitution (`{content}`, `{max_words}`)
- [ ] Template validation
- [ ] Support for custom templates
- [ ] Template versioning

### 10.4. Chunk Enrichment Pipeline ⬜ PENDING

- [ ] Implement `ChunkEnricher` class
- [ ] Batch processing support (multiple chunks)
- [ ] Parallel enrichment (configurable concurrency)
- [ ] Summary generation
- [ ] Keyword extraction (top N keywords)
- [ ] Preliminary NER extraction
- [ ] Confidence scoring
- [ ] Source tracking (chunk ID, position)

### 10.5. Integration with Chunking Pipeline ⬜ PENDING

- [ ] Add `EnableLLMEnrichment` flag to `ChunkingOptions`
- [ ] Add `LLMEnrichmentOptions` configuration class
- [ ] Extend `ChunkingResult` with `EnrichedChunks` property
- [ ] Optional enrichment (disabled by default)
- [ ] Backward compatibility (existing API unchanged)
- [ ] Performance logging (enrichment time per chunk)

### 10.6. Configuration Management ⬜ PENDING

- [ ] Add `OllamaOptions` configuration class
- [ ] Configurable properties:
  - Base URL (default: `http://localhost:11434`)
  - Model name (default: `llama3`)
  - Temperature (default: 0.7)
  - Max tokens (default: 500)
  - Request timeout (default: 30s)
  - Retry attempts (default: 3)
- [ ] Configuration validation
- [ ] Environment variable support

### 10.7. Caching Layer ⬜ PENDING

- [ ] Implement `IEnrichmentCache` interface
- [ ] In-memory cache implementation
- [ ] Cache key generation (hash of chunk content + model + template)
- [ ] TTL (time-to-live) configuration
- [ ] Cache hit/miss metrics
- [ ] Optional cache bypass

### 10.8. Error Handling & Resilience ⬜ PENDING

- [ ] Ollama connection errors (service not running)
- [ ] Model not found errors
- [ ] Timeout handling
- [ ] Retry logic with exponential backoff
- [ ] Fallback behavior (return unenriched chunks)
- [ ] Error logging with context
- [ ] Circuit breaker pattern (optional)

### 10.9. Testing ⬜ PENDING

- [ ] Unit tests for `OllamaLLMProvider`
- [ ] Unit tests for `PromptTemplateManager`
- [ ] Unit tests for `ChunkEnricher`
- [ ] Unit tests for `EnrichmentCache`
- [ ] Integration tests with real Ollama instance
- [ ] Integration tests with sample documents
- [ ] Performance benchmarks (enrichment time)
- [ ] Skip tests gracefully when Ollama not available

### 10.10. Documentation ⬜ PENDING

- [ ] Update `MasterPlan.md` with Phase 10 completion
- [ ] Complete `Phase-10.md` documentation
- [ ] XML documentation for all public APIs
- [ ] Ollama setup guide (installation, model pulling)
- [ ] Configuration examples
- [ ] Usage examples
- [ ] Troubleshooting guide

---

## Deliverables

| Deliverable | Status | Location |
|-------------|--------|----------|
| `ILLMProvider` interface | ⬜ Pending | `Interfaces/ILLMProvider.cs` |
| `IChunkEnricher` interface | ⬜ Pending | `Interfaces/IChunkEnricher.cs` |
| `OllamaLLMProvider` | ⬜ Pending | `LLM/Providers/OllamaLLMProvider.cs` |
| `PromptTemplateManager` | ⬜ Pending | `LLM/PromptTemplateManager.cs` |
| `ChunkEnricher` | ⬜ Pending | `LLM/ChunkEnricher.cs` |
| `EnrichedChunk` model | ⬜ Pending | `Models/EnrichedChunk.cs` |
| `LLMOptions` model | ⬜ Pending | `Models/LLMOptions.cs` |
| `EnrichmentCache` | ⬜ Pending | `LLM/Caching/EnrichmentCache.cs` |
| 25+ unit tests | ⬜ Pending | `Tests/Unit/LLM/` |
| Integration tests | ⬜ Pending | `Tests/Integration/LLM/` |
| Documentation | ⬜ Pending | `docs/` |

---

## Technical Details

### ILLMProvider Interface

```csharp
namespace PanoramicData.Chunker.LLM
{
    /// <summary>
    /// Abstraction for LLM providers (Ollama, OpenAI, Azure, etc.)
    /// </summary>
    public interface ILLMProvider
    {
        /// <summary>
        /// Generates text completion from a prompt.
        /// </summary>
     Task<LLMResponse> GenerateAsync(
      LLMRequest request,
            CancellationToken cancellationToken = default);

        /// <summary>
    /// Generates text completions for multiple prompts (batch).
        /// </summary>
        Task<IEnumerable<LLMResponse>> GenerateBatchAsync(
 IEnumerable<LLMRequest> requests,
            CancellationToken cancellationToken = default);

        /// <summary>
    /// Checks if the LLM provider is available/reachable.
        /// </summary>
        Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the name of the provider (e.g., "Ollama", "OpenAI").
        /// </summary>
        string ProviderName { get; }
    }

    public record LLMRequest
    {
 public required string Prompt { get; init; }
        public required string Model { get; init; }
        public double Temperature { get; init; } = 0.7;
        public int MaxTokens { get; init; } = 500;
   public string? SystemPrompt { get; init; }
    }

    public record LLMResponse
    {
        public required string Text { get; init; }
        public required string Model { get; init; }
        public int TokensUsed { get; init; }
  public TimeSpan Duration { get; init; }
        public bool Success { get; init; }
        public string? ErrorMessage { get; init; }
    }
}
```

### OllamaLLMProvider Implementation

```csharp
using Ollama.Api;
using Ollama.Api.Models;

namespace PanoramicData.Chunker.LLM.Providers
{
    public class OllamaLLMProvider : ILLMProvider
    {
        private readonly OllamaClient _client;
        private readonly OllamaOptions _options;
        private readonly ILogger<OllamaLLMProvider> _logger;

        public OllamaLLMProvider(
            OllamaOptions options,
            ILogger<OllamaLLMProvider> logger)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

   _client = new OllamaClient(new OllamaClientOptions
            {
      BaseUrl = options.BaseUrl
            });
 }

 public string ProviderName => "Ollama";

        public async Task<LLMResponse> GenerateAsync(
 LLMRequest request,
CancellationToken cancellationToken = default)
   {
  var stopwatch = Stopwatch.StartNew();

  try
            {
   var ollamaRequest = new GenerateRequest
     {
              Model = request.Model,
  Prompt = request.Prompt,
            Options = new ModelOptions
                {
      Temperature = (float)request.Temperature,
        NumPredict = request.MaxTokens
        },
     System = request.SystemPrompt
    };

     var response = await _client.Generate.GenerateAsync(
           ollamaRequest,
       cancellationToken);

       stopwatch.Stop();

        return new LLMResponse
                {
         Text = response.Response ?? string.Empty,
              Model = request.Model,
   TokensUsed = response.PromptEvalCount + response.EvalCount,
       Duration = stopwatch.Elapsed,
    Success = true
    };
            }
            catch (Exception ex)
         {
        stopwatch.Stop();
      _logger.LogError(ex, "Ollama generation failed for model {Model}", request.Model);

        return new LLMResponse
      {
      Text = string.Empty,
     Model = request.Model,
    TokensUsed = 0,
         Duration = stopwatch.Elapsed,
         Success = false,
   ErrorMessage = ex.Message
    };
      }
        }

 public async Task<IEnumerable<LLMResponse>> GenerateBatchAsync(
 IEnumerable<LLMRequest> requests,
       CancellationToken cancellationToken = default)
        {
            // Process sequentially for now (Ollama doesn't have native batch API)
  var responses = new List<LLMResponse>();

            foreach (var request in requests)
  {
      var response = await GenerateAsync(request, cancellationToken);
    responses.Add(response);
            }

 return responses;
        }

        public async Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default)
  {
         try
            {
          var models = await _client.Models.ListAsync(cancellationToken);
     return models?.Models?.Any() ?? false;
    }
   catch
   {
      return false;
     }
        }
    }

 public record OllamaOptions
    {
        public string BaseUrl { get; init; } = "http://localhost:11434";
      public string DefaultModel { get; init; } = "llama3";
        public double Temperature { get; init; } = 0.7;
        public int MaxTokens { get; init; } = 500;
        public int TimeoutSeconds { get; init; } = 30;
    public int RetryAttempts { get; init; } = 3;
    }
}
```

### Prompt Templates

```csharp
namespace PanoramicData.Chunker.LLM
{
    public class PromptTemplateManager
    {
      private static readonly Dictionary<string, string> _defaultTemplates = new()
        {
            ["summarize"] = """
                Summarize the following text in {max_words} words or less. Be concise and capture the main points:

         {content}

          Summary:
    """,

            ["extract_keywords"] = """
   Extract the top {max_keywords} most important keywords from the following text. Return only the keywords, comma-separated:

         {content}

    Keywords:
  """,

      ["extract_entities"] = """
       Extract named entities from the following text. Identify persons, organizations, and locations. Return as JSON:

          {content}

     Entities (JSON):
   """
        };

        public string GetTemplate(string templateName)
        {
          if (_defaultTemplates.TryGetValue(templateName, out var template))
   {
       return template;
            }

      throw new ArgumentException($"Unknown template: {templateName}");
}

        public string RenderTemplate(string templateName, Dictionary<string, string> variables)
        {
            var template = GetTemplate(templateName);

   foreach (var (key, value) in variables)
     {
  template = template.Replace($"{{{key}}}", value);
     }

            return template;
        }
    }
}
```

### ChunkEnricher Implementation

```csharp
namespace PanoramicData.Chunker.LLM
{
    public class ChunkEnricher : IChunkEnricher
 {
        private readonly ILLMProvider _llmProvider;
     private readonly PromptTemplateManager _promptManager;
        private readonly IEnrichmentCache _cache;
        private readonly LLMEnrichmentOptions _options;
        private readonly ILogger<ChunkEnricher> _logger;

    public async Task<EnrichedChunk> EnrichAsync(
            ChunkerBase chunk,
   CancellationToken cancellationToken = default)
  {
        var cacheKey = GenerateCacheKey(chunk);

  // Check cache first
          if (_cache.TryGet(cacheKey, out var cachedResult))
      {
    _logger.LogDebug("Cache hit for chunk {ChunkId}", chunk.Id);
     return cachedResult;
          }

   var enriched = new EnrichedChunk
            {
    ChunkId = chunk.Id,
      OriginalContent = chunk.Content
    };

            // Generate summary
            if (_options.EnableSummarization)
            {
         enriched.Summary = await GenerateSummaryAsync(chunk, cancellationToken);
            }

 // Extract keywords
            if (_options.EnableKeywordExtraction)
    {
    enriched.Keywords = await ExtractKeywordsAsync(chunk, cancellationToken);
  }

            // Extract preliminary entities
if (_options.EnablePreliminaryNER)
     {
     enriched.PreliminaryEntities = await ExtractEntitiesAsync(chunk, cancellationToken);
            }

        // Cache result
            _cache.Set(cacheKey, enriched, _options.CacheDuration);

  return enriched;
        }

 private async Task<string> GenerateSummaryAsync(
          ChunkerBase chunk,
  CancellationToken cancellationToken)
        {
        var prompt = _promptManager.RenderTemplate("summarize", new Dictionary<string, string>
          {
                ["content"] = chunk.Content,
                ["max_words"] = _options.SummaryMaxWords.ToString()
      });

   var request = new LLMRequest
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
        return string.Empty;
 }

        return response.Text.Trim();
        }

        private async Task<List<string>> ExtractKeywordsAsync(
     ChunkerBase chunk,
            CancellationToken cancellationToken)
        {
   var prompt = _promptManager.RenderTemplate("extract_keywords", new Dictionary<string, string>
       {
        ["content"] = chunk.Content,
    ["max_keywords"] = _options.MaxKeywords.ToString()
 });

        var request = new LLMRequest
            {
       Prompt = prompt,
           Model = _options.Model,
        Temperature = 0.3, // Lower temperature for keyword extraction
  MaxTokens = 200
       };

    var response = await _llmProvider.GenerateAsync(request, cancellationToken);

  if (!response.Success)
        {
           return new List<string>();
   }

            // Parse comma-separated keywords
        return response.Text
       .Split(',')
    .Select(k => k.Trim())
          .Where(k => !string.IsNullOrWhiteSpace(k))
   .Take(_options.MaxKeywords)
    .ToList();
        }

        private async Task<List<PreliminaryEntity>> ExtractEntitiesAsync(
            ChunkerBase chunk,
 CancellationToken cancellationToken)
        {
 var prompt = _promptManager.RenderTemplate("extract_entities", new Dictionary<string, string>
      {
         ["content"] = chunk.Content
          });

            var request = new LLMRequest
            {
    Prompt = prompt,
   Model = _options.Model,
    Temperature = 0.3,
        MaxTokens = 500
 };

      var response = await _llmProvider.GenerateAsync(request, cancellationToken);

  if (!response.Success)
        {
       return new List<PreliminaryEntity>();
            }

 // Parse JSON response (basic parsing, will be enhanced in Phase 12)
       try
            {
          return JsonSerializer.Deserialize<List<PreliminaryEntity>>(response.Text)
    ?? new List<PreliminaryEntity>();
}
            catch (JsonException ex)
      {
  _logger.LogWarning(ex, "Failed to parse entity extraction JSON");
   return new List<PreliminaryEntity>();
         }
        }

        private string GenerateCacheKey(ChunkerBase chunk)
  {
       // Simple cache key based on content hash + model
    var contentHash = GetHashString(chunk.Content);
      return $"{_options.Model}:{contentHash}";
        }

    private static string GetHashString(string input)
  {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
            return Convert.ToHexString(bytes).ToLowerInvariant();
    }
    }
}
```

### EnrichedChunk Model

```csharp
namespace PanoramicData.Chunker.Models
{
    public record EnrichedChunk
{
 public required Guid ChunkId { get; init; }
    public required string OriginalContent { get; init; }
        public string? Summary { get; init; }
        public List<string> Keywords { get; init; } = new();
        public List<PreliminaryEntity> PreliminaryEntities { get; init; } = new();
        public int TokensUsed { get; init; }
        public TimeSpan EnrichmentDuration { get; init; }
    }

public record PreliminaryEntity
    {
   public required string Text { get; init; }
    public required string Type { get; init; } // "Person", "Organization", "Location"
        public double Confidence { get; init; } = 1.0;
    }
}
```

### Configuration

```csharp
namespace PanoramicData.Chunker.Configuration
{
    public record LLMEnrichmentOptions
    {
        public bool EnableSummarization { get; init; } = true;
        public bool EnableKeywordExtraction { get; init; } = true;
        public bool EnablePreliminaryNER { get; init; } = false;

        public string Model { get; init; } = "llama3";
 public int SummaryMaxWords { get; init; } = 50;
        public int SummaryMaxTokens { get; init; } = 200;
        public int MaxKeywords { get; init; } = 10;

        public bool EnableCaching { get; init; } = true;
        public TimeSpan CacheDuration { get; init; } = TimeSpan.FromHours(24);

        public int MaxConcurrency { get; init; } = 5;
    }
}
```

---

## Integration Example

```csharp
using PanoramicData.Chunker;
using PanoramicData.Chunker.LLM;
using PanoramicData.Chunker.LLM.Providers;

// Configure Ollama
var ollamaOptions = new OllamaOptions
{
    BaseUrl = "http://localhost:11434",
    DefaultModel = "llama3",
    Temperature = 0.7
};

// Create LLM provider
var llmProvider = new OllamaLLMProvider(ollamaOptions, loggerFactory.CreateLogger<OllamaLLMProvider>());

// Configure enrichment
var enrichmentOptions = new LLMEnrichmentOptions
{
    EnableSummarization = true,
    EnableKeywordExtraction = true,
    EnablePreliminaryNER = true,
    Model = "llama3",
    MaxKeywords = 10
};

// Create chunking options with LLM enrichment
var chunkingOptions = new ChunkingOptions
{
    MaxTokens = 512,
    EnableLLMEnrichment = true,
    LLMEnrichmentOptions = enrichmentOptions
};

// Chunk and enrich document
var result = await DocumentChunker.ChunkFileAsync("document.pdf", chunkingOptions);

// Access enriched chunks
foreach (var chunk in result.Chunks)
{
    if (chunk is EnrichedChunk enriched)
    {
        Console.WriteLine($"Summary: {enriched.Summary}");
        Console.WriteLine($"Keywords: {string.Join(", ", enriched.Keywords)}");
      Console.WriteLine($"Entities: {enriched.PreliminaryEntities.Count}");
    }
}
```

---

## Success Criteria

✅ **Core Abstractions**:
- `ILLMProvider` interface defined
- `IChunkEnricher` interface defined
- All models defined with XML docs

✅ **Ollama Integration**:
- `OllamaLLMProvider` implemented
- Connection to local Ollama working
- Support for llama3, mistral, phi models
- Error handling robust

✅ **Enrichment Pipeline**:
- Summary generation working
- Keyword extraction working
- Preliminary NER working
- Batch processing supported

✅ **Configuration**:
- Configurable via `LLMEnrichmentOptions`
- Disabled by default (backward compatible)
- Environment variable support

✅ **Caching**:
- In-memory cache working
- Cache hit rate >50% for repeated content
- TTL configuration working

✅ **Testing**:
- 25+ unit tests passing
- Integration tests with Ollama
- Tests skip gracefully when Ollama unavailable
- Performance benchmarks documented

✅ **Documentation**:
- Phase documentation complete
- API documentation (XML)
- Ollama setup guide
- Usage examples

---

## Performance Targets

| Operation | Target | Notes |
|-----------|--------|-------|
| Summary generation | < 3 seconds | Per chunk (llama3) |
| Keyword extraction | < 2 seconds | Per chunk |
| Preliminary NER | < 5 seconds | Per chunk |
| Batch processing (10 chunks) | < 15 seconds | Parallel, max 5 concurrent |
| Cache lookup | < 1ms | In-memory |
| Ollama availability check | < 100ms | Connection test |

---

## Dependencies

### External Libraries

```xml
<PackageReference Include="Ollama.Api" Version="*" />
```

### System Requirements

- **Ollama**: Installed and running locally (`http://localhost:11434`)
- **Models**: At least one model pulled (e.g., `ollama pull llama3`)
- **RAM**: 8GB+ recommended (4GB minimum for small models)
- **Disk**: 5GB+ for llama3 model

### Ollama Setup

```bash
# Install Ollama (see https://ollama.ai)
# Windows: Download installer from ollama.ai
# macOS: brew install ollama
# Linux: curl -fsSL https://ollama.ai/install.sh | sh

# Pull a model
ollama pull llama3

# Verify Ollama is running
curl http://localhost:11434/api/tags
```

---

## Testing Strategy

### Unit Tests (15 tests)

1. `OllamaLLMProvider_GenerateAsync_Success`
2. `OllamaLLMProvider_GenerateAsync_Timeout`
3. `OllamaLLMProvider_IsAvailableAsync_WhenRunning`
4. `OllamaLLMProvider_IsAvailableAsync_WhenNotRunning`
5. `PromptTemplateManager_GetTemplate_Success`
6. `PromptTemplateManager_RenderTemplate_VariableSubstitution`
7. `ChunkEnricher_EnrichAsync_WithSummary`
8. `ChunkEnricher_EnrichAsync_WithKeywords`
9. `ChunkEnricher_EnrichAsync_WithPreliminaryNER`
10. `ChunkEnricher_EnrichAsync_CacheHit`
11. `ChunkEnricher_EnrichAsync_CacheMiss`
12. `EnrichmentCache_SetAndGet_Success`
13. `EnrichmentCache_Expiration_AfterTTL`
14. `LLMEnrichmentOptions_Validation`
15. `OllamaOptions_Validation`

### Integration Tests (10 tests)

1. `Integration_Ollama_SummarizeChunk`
2. `Integration_Ollama_ExtractKeywords`
3. `Integration_Ollama_ExtractEntities`
4. `Integration_Ollama_BatchProcessing`
5. `Integration_Ollama_ModelNotFound`
6. `Integration_Ollama_ServiceUnavailable`
7. `Integration_EndToEnd_PdfWithEnrichment`
8. `Integration_EndToEnd_MarkdownWithEnrichment`
9. `Integration_Performance_100Chunks`
10. `Integration_CacheEfficiency_RepeatedContent`

---

## Known Limitations

1. **Ollama Only**: Phase 10 implements only Ollama. OpenAI/Azure support deferred to future phases.
2. **Sequential Batch Processing**: Ollama API doesn't have native batch support, so requests are processed sequentially (but can be parallelized with semaphore).
3. **Basic NER**: Preliminary NER in Phase 10 is prompt-based. Phase 12 will add ML-based NER with spaCy/ML.NET.
4. **No Streaming**: Phase 10 uses completion API, not streaming. Streaming support deferred.
5. **Local Only**: Assumes Ollama running locally. Remote Ollama support possible but not prioritized.

---

## Future Enhancements (Post-Phase 10)

- **Phase 11-16**: Use enriched chunks for Knowledge Graph construction
- **Future Phases**: Add OpenAI/Azure/Anthropic providers
- **Future Phases**: Streaming support for real-time enrichment
- **Future Phases**: Fine-tuned models for domain-specific extraction
- **Future Phases**: Multi-language support

---

## Risks & Mitigation

| Risk | Mitigation |
|------|------------|
| Ollama not installed | Graceful degradation: skip enrichment, log warning |
| Model not available | Fallback to default model or skip enrichment |
| Slow LLM response | Timeout + retry logic, configurable concurrency |
| High memory usage | Process in batches, limit concurrent requests |
| Cache memory growth | TTL expiration, max cache size limit |
| Inconsistent LLM output | Prompt engineering, validation, error handling |

---

## Status: **📅 PENDING**

**Ready to Start**: After Phase 9 complete

**Estimated Start Date**: January 2025

---

[← Back to Master Plan](../MasterPlan.md) | [Previous Phase: PDF Chunking (Basic) ←](Phase-09.md) | [Next Phase: Knowledge Graph Foundation →](Phase-11.md)
