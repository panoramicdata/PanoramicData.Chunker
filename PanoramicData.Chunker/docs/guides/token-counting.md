# Token Counting Guide

## Overview

**PanoramicData.Chunker** provides accurate token counting for popular embedding models, ensuring your chunks are optimally sized for RAG (Retrieval-Augmented Generation) systems. This guide explains how to use token counting effectively.

## Why Token Counting Matters

When working with embedding models and LLMs, **tokens** (not characters) determine:
- Input size limits
- API costs
- Processing speed
- Embedding quality

**Example**: The phrase "Hello, world!" might be:
- **13 characters**
- **4 tokens** (using OpenAI's CL100K encoding: `["Hello", ",", " world", "!"]`)

Using character-based estimation (`chars / 4`) gives ~3 tokens - close, but not exact. For production RAG systems, accuracy matters.

---

## Supported Token Counting Methods

### 1. OpenAI CL100K (GPT-4, GPT-3.5-turbo) ? **Recommended**

**Best for**:
- GPT-4 and GPT-3.5-turbo models
- text-embedding-ada-002
- text-embedding-3-small
- text-embedding-3-large

**Encoding**: CL100K (100,000 token vocabulary)

**Usage**:
```csharp
using PanoramicData.Chunker.Infrastructure.TokenCounters;

var counter = OpenAITokenCounter.ForGpt4();
var tokenCount = counter.CountTokens("Your text here");

// Or use in chunking options
var options = ChunkingPresets.ForOpenAIEmbeddings();
```

**Accuracy**: Exact match with OpenAI's official tokenization

### 2. OpenAI P50K (GPT-3)

**Best for**:
- GPT-3 (davinci, curie, babbage, ada)
- Legacy OpenAI models

**Encoding**: P50K (50,000 token vocabulary)

**Usage**:
```csharp
var counter = OpenAITokenCounter.ForGpt3();
var options = ChunkingPresets.ForGPT3();
```

### 3. OpenAI R50K (GPT-2)

**Best for**:
- GPT-2 models
- Older research projects

**Encoding**: R50K (50,000 token vocabulary)

**Usage**:
```csharp
var counter = OpenAITokenCounter.ForGpt2();
```

### 4. Character-Based (Default Fallback)

**Best for**:
- Quick prototyping
- When exact accuracy isn't critical
- Performance-critical scenarios (fastest)

**Algorithm**: `tokens ? characters / 4`

**Usage**:
```csharp
var counter = new CharacterBasedTokenCounter();
var options = ChunkingPresets.ForFastProcessing();
```

**Pros**: Fast, no external dependencies  
**Cons**: Approximate, less accurate for complex text

---

## Quick Start

### Using Presets (Easiest)

```csharp
using PanoramicData.Chunker;
using PanoramicData.Chunker.Configuration;

// For OpenAI embeddings (uses OpenAI CL100K)
var options = ChunkingPresets.ForOpenAIEmbeddings();

var result = await DocumentChunker.ChunkFileAsync("document.md", options);

Console.WriteLine($"Total tokens: {result.Statistics.TotalTokens}");
Console.WriteLine($"Average tokens per chunk: {result.Statistics.AverageTokensPerChunk}");
```

### Custom Configuration

```csharp
using PanoramicData.Chunker.Infrastructure.TokenCounters;

var options = new ChunkingOptions
{
    MaxTokens = 512,
    OverlapTokens = 50,
    TokenCounter = OpenAITokenCounter.ForGpt4(),
    TokenCountingMethod = TokenCountingMethod.OpenAI_CL100K
};

var result = await DocumentChunker.ChunkFileAsync("document.pdf", options);
```

### Using the Factory

```csharp
using PanoramicData.Chunker.Infrastructure;

// Create counter from method
var counter = TokenCounterFactory.Create(TokenCountingMethod.OpenAI_CL100K);

// Or from options
var options = new ChunkingOptions 
{ 
    TokenCountingMethod = TokenCountingMethod.OpenAI_CL100K 
};
var counter = TokenCounterFactory.GetOrCreate(options);
```

---

## Advanced Usage

### Token-Based Splitting

The OpenAI token counter can split text at **exact token boundaries**:

```csharp
var counter = OpenAITokenCounter.ForGpt4();
var longText = "...very long document...";

// Split into batches of max 500 tokens with 50 token overlap
var batches = counter.SplitIntoTokenBatches(longText, maxTokens: 500, overlap: 50);

foreach (var batch in batches)
{
    Console.WriteLine($"Batch: {batch.Length} chars");
 // Process each batch
}
```

**Benefits**:
- No mid-word splits
- Preserves semantic boundaries
- Accurate for embedding models
- Overlap maintains context

### Async Token Counting

For CPU-intensive operations:

```csharp
var counter = OpenAITokenCounter.ForGpt4();
var tokenCount = await counter.CountTokensAsync(largeText, cancellationToken);
```

### Comparing Token Counters

```csharp
var text = "The quick brown fox jumps over the lazy dog.";

var charBased = new CharacterBasedTokenCounter();
var openAI = OpenAITokenCounter.ForGpt4();

Console.WriteLine($"Character-based: {charBased.CountTokens(text)} tokens");
Console.WriteLine($"OpenAI CL100K: {openAI.CountTokens(text)} tokens");

// Output:
// Character-based: 11 tokens (45 chars / 4)
// OpenAI CL100K: 10 tokens (actual tokenization)
```

---

## Best Practices

### 1. Choose the Right Method

| Use Case | Recommended Method |
|----------|-------------------|
| Production RAG with OpenAI | `OpenAI_CL100K` |
| GPT-3 models | `OpenAI_P50K` |
| Prototyping/testing | `CharacterBased` |
| Performance-critical | `CharacterBased` |
| Legacy systems | `OpenAI_R50K` |

### 2. Set Appropriate Max Tokens

**Embedding Model Limits**:
- text-embedding-ada-002: **8,191 tokens**
- text-embedding-3-small: **8,191 tokens**
- text-embedding-3-large: **8,191 tokens**
- GPT-4: **8,192 tokens** (some variants: 32K, 128K)
- GPT-3.5-turbo: **4,096 tokens** (some variants: 16K)

**Recommended chunk sizes**:
```csharp
// Conservative (leaves room for prompts)
var options = new ChunkingOptions { MaxTokens = 512 };

// Medium (good for most RAG systems)
var options = new ChunkingOptions { MaxTokens = 1024 };

// Large (for document summarization)
var options = new ChunkingOptions { MaxTokens = 2048 };
```

### 3. Use Overlap for Context

```csharp
var options = new ChunkingOptions
{
    MaxTokens = 512,
    OverlapTokens = 100  // ~20% overlap maintains context
};
```

**Why overlap matters**:
- Prevents context loss at chunk boundaries
- Improves retrieval quality
- Maintains semantic continuity

### 4. Monitor Token Usage

```csharp
var result = await DocumentChunker.ChunkFileAsync("large-document.pdf", options);

Console.WriteLine($"Document Statistics:");
Console.WriteLine($"  Total chunks: {result.Statistics.TotalChunks}");
Console.WriteLine($"  Total tokens: {result.Statistics.TotalTokens}");
Console.WriteLine($"  Average per chunk: {result.Statistics.AverageTokensPerChunk}");
Console.WriteLine($"  Max tokens: {result.Statistics.MaxTokensInChunk}");
Console.WriteLine($"  Min tokens: {result.Statistics.MinTokensInChunk}");

// Estimate embedding API costs (example for OpenAI)
var estimatedCost = (result.Statistics.TotalTokens / 1000.0) * 0.0001; // $0.0001 per 1K tokens
Console.WriteLine($"  Estimated cost: ${estimatedCost:F4}");
```

---

## Performance Considerations

### Speed Comparison

| Method | Speed | Accuracy |
|--------|-------|----------|
| Character-Based | ??? Fastest | ?? Approximate |
| OpenAI CL100K | ?? Fast | ??? Exact |
| OpenAI P50K | ?? Fast | ??? Exact |
| OpenAI R50K | ?? Fast | ??? Exact |

### Optimization Tips

1. **Reuse token counters**:
```csharp
// ? Good: Create once, use many times
var counter = OpenAITokenCounter.ForGpt4();
foreach (var doc in documents)
{
    var tokens = counter.CountTokens(doc);
}

// ? Bad: Creating new counter each time
foreach (var doc in documents)
{
    var counter = OpenAITokenCounter.ForGpt4();
    var tokens = counter.CountTokens(doc);
}
```

2. **Use character-based for non-critical paths**:
```csharp
// Quick validation check
if (new CharacterBasedTokenCounter().CountTokens(text) > 10000)
{
    // Document is large, process with accurate counter
    var accurate = OpenAITokenCounter.ForGpt4().CountTokens(text);
}
```

3. **Batch processing**:
```csharp
var counter = OpenAITokenCounter.ForGpt4();
var tasks = documents.Select(async doc => 
    await counter.CountTokensAsync(doc, cancellationToken));
var tokenCounts = await Task.WhenAll(tasks);
```

---

## Common Scenarios

### Scenario 1: RAG System with OpenAI Embeddings

```csharp
using PanoramicData.Chunker;
using PanoramicData.Chunker.Configuration;

// Use preset optimized for OpenAI
var options = ChunkingPresets.ForRAG();

// Chunk your knowledge base
var documents = Directory.GetFiles("knowledge-base", "*.pdf");
foreach (var doc in documents)
{
    var result = await DocumentChunker.ChunkFileAsync(doc, options);
    
    // Store chunks with accurate token counts
    foreach (var chunk in result.Chunks.ContentChunks())
    {
    await vectorDb.UpsertAsync(new
        {
     Id = chunk.Id,
     Content = chunk.ToPlainText(),
  Tokens = chunk.QualityMetrics.TokenCount,
     Embedding = await embeddingService.GetEmbeddingAsync(chunk.ToPlainText())
        });
    }
}
```

### Scenario 2: Cost Estimation

```csharp
// Estimate costs before processing
var options = ChunkingPresets.ForOpenAIEmbeddings();
var results = new List<ChunkingResult>();

foreach (var file in documentFiles)
{
    var result = await DocumentChunker.ChunkFileAsync(file, options);
    results.Add(result);
}

var totalTokens = results.Sum(r => r.Statistics.TotalTokens);
var embeddingCost = (totalTokens / 1000.0) * 0.0001; // text-embedding-ada-002
var storageMB = results.Sum(r => r.Chunks.Count) * 0.001; // Estimate

Console.WriteLine($"Processing Summary:");
Console.WriteLine($"  Documents: {results.Count}");
Console.WriteLine($"  Total chunks: {results.Sum(r => r.Statistics.TotalChunks)}");
Console.WriteLine($"  Total tokens: {totalTokens:N0}");
Console.WriteLine($"  Estimated embedding cost: ${embeddingCost:F2}");
Console.WriteLine($"  Estimated storage: {storageMB:F1} MB");
```

### Scenario 3: Custom Token Limits

```csharp
// Different limits for different document types
var options = new ChunkingOptions
{
    MaxTokens = documentType switch
    {
        "code" => 1024,      // Code can be longer
        "chat" => 256,       // Chat messages are short
        "article" => 512,    // Articles are medium
        _ => 512
    },
    TokenCounter = OpenAITokenCounter.ForGpt4()
};
```

---

## Troubleshooting

### Issue: Token counts seem inaccurate

**Solution**: Ensure you're using the correct token counter for your model:

```csharp
// ? Correct for GPT-4
var counter = OpenAITokenCounter.ForGpt4();

// ? Wrong for GPT-4 (uses GPT-3 encoding)
var counter = OpenAITokenCounter.ForGpt3();
```

### Issue: Chunks exceed max tokens

**Solution**: Check quality metrics and adjust settings:

```csharp
var oversizedChunks = result.Chunks
    .Where(c => c.QualityMetrics?.TokenCount > options.MaxTokens)
    .ToList();

if (oversizedChunks.Any())
{
    Console.WriteLine($"Found {oversizedChunks.Count} oversized chunks");
    // Reduce MaxTokens or enable splitting
    options.MaxTokens = 400; // Reduce
}
```

### Issue: Claude/Cohere not supported

The library currently supports OpenAI tokenization. For other models:

```csharp
// Workaround: Use character-based estimation
var options = new ChunkingOptions
{
    TokenCountingMethod = TokenCountingMethod.CharacterBased
};

// Or implement custom counter
public class ClaudeTokenCounter : ITokenCounter
{
    // Your implementation
}

options.TokenCounter = new ClaudeTokenCounter();
options.TokenCountingMethod = TokenCountingMethod.Custom;
```

---

## API Reference

### ITokenCounter Interface

```csharp
public interface ITokenCounter
{
    // Count tokens synchronously
    int CountTokens(string text);
    
  // Count tokens asynchronously
    Task<int> CountTokensAsync(string text, CancellationToken cancellationToken = default);
    
    // Split text into token-sized batches
    IEnumerable<string> SplitIntoTokenBatches(string text, int maxTokens, int overlap = 0);
}
```

### OpenAITokenCounter Class

```csharp
public class OpenAITokenCounter : ITokenCounter
{
    // Factory methods
    public static OpenAITokenCounter ForGpt4();  // CL100K
    public static OpenAITokenCounter ForGpt3();  // P50K
    public static OpenAITokenCounter ForGpt2();  // R50K
    
    // Constructor with custom encoding
    public OpenAITokenCounter(string encodingName);
  
    // Properties
    public string EncodingName { get; }
}
```

### TokenCounterFactory Class

```csharp
public static class TokenCounterFactory
{
    // Create counter by method
    public static ITokenCounter Create(
        TokenCountingMethod method, 
        ITokenCounter? customCounter = null);
    
    // Get or create from options
    public static ITokenCounter GetOrCreate(ChunkingOptions options);
}
```

---

## What's Next?

- **Phase 4**: Plain Text Chunking
- **Phase 5**: DOCX Chunking
- **Future**: Claude and Cohere token counting support

## Related Guides

- [Markdown Chunking Guide](markdown-chunking.md)
- [ChunkingOptions Reference](chunking-options.md)
- [RAG Best Practices](rag-best-practices.md)

---

**Need help?** Open an issue on [GitHub](https://github.com/panoramicdata/PanoramicData.Chunker/issues)
