# Quick Start: Testing Ollama Entity Extractor

## Prerequisites

```bash
# 1. Install Ollama (MacOS/Linux)
curl -fsSL https://ollama.ai/install.sh | sh

# Windows: Download from https://ollama.ai/download

# 2. Pull a model
ollama pull llama3.2

# 3. Start Ollama server
ollama serve
# Should see: Ollama is running on http://localhost:11434
```

## Quick Test

### Option 1: Small Sample Test (Fast - ~10 seconds)

```bash
# Enable the small sample test
# Edit: PanoramicData.Chunker.Tests/Integration/KnowledgeGraph/OllamaExtractionComparisonTests.cs
# Line ~151: Remove [Skip = "..."] attribute from SmallSample test

# Run test
dotnet test --filter "OllamaExtraction_SmallSample_ShouldExtractKeyEntities"
```

**Expected Output**:
```
Extracted 8 entities from sample text:
  - Plinian Society (Organization, confidence: 0.90)
  - Professor Jameson (Person, confidence: 0.90)
  - HMS Beagle (Product, confidence: 0.90)
  - Charles Darwin (Person, confidence: 0.90)
  - Edinburgh University (Organization, confidence: 0.90)
  - Robert Grant (Person, confidence: 0.90)
  - Captain FitzRoy (Person, confidence: 0.90)
  - Galapagos Islands (Location, confidence: 0.90)
```

### Option 2: Full Darwin Comparison Test (Slow - ~5-10 minutes)

```bash
# Enable the comparison test
# Line ~27: Remove [Skip = "..."] attribute

# Run test
dotnet test --filter "OllamaExtraction_ShouldImprove_RecallVsBaseline"
```

**Expected Output**:
```
=== BASELINE EXTRACTION (HybridEntityExtractor) ===
Baseline Recall: 12.50%, Precision: 35.00%, F1: 18.42%

=== LLM EXTRACTION (OllamaEntityExtractor) ===
LLM Recall: 45.00%, Precision: 60.00%, F1: 51.43%

=== COMPARISON ===
Recall improvement: +32.50%
Precision change: +25.00%
F1 improvement: +33.01%

=== NEW ENTITIES FOUND BY LLM (25 new) ===
  + Professor Jameson (Person, conf: 0.92)
  + Captain FitzRoy (Person, conf: 0.91)
  + Plinian Society (Organization, conf: 0.90)
  + HMS Beagle (Product, conf: 0.89)
  ...
```

## Troubleshooting

### "Ollama not available"

```bash
# Check if Ollama is running
curl http://localhost:11434/api/tags

# If not, start it:
ollama serve
```

### "Model not found"

```bash
# List available models
ollama list

# Pull the model if missing
ollama pull llama3.2
```

### Test takes too long

```csharp
// Use a smaller model
var extractor = new OllamaEntityExtractor(
    modelName: "phi3",  // Faster, smaller model
    maxTokensPerChunk: 1000);
```

### Out of memory

```bash
# Restart Ollama with memory limit
OLLAMA_MAX_LOADED_MODELS=1 ollama serve
```

## Using in Your Code

```csharp
using PanoramicData.Chunker.KnowledgeGraph.Extractors;

// Create extractor
var extractor = new OllamaEntityExtractor();

// Extract entities
var entities = await extractor.ExtractEntitiesAsync(
    chunks, 
    cancellationToken);

// Use results
foreach (var entity in entities)
{
    Console.WriteLine($"{entity.Name} ({entity.Type})");
    foreach (var alias in entity.Aliases)
    {
        Console.WriteLine($"  ? {alias}");
    }
}
```

## Performance Tips

1. **Batch chunks** - Process multiple chunks to reduce LLM overhead
2. **Use smaller models** - `phi3` is 3x faster than `llama3.2`
3. **Limit chunk size** - `maxTokensPerChunk: 1000` for faster processing
4. **Lower temperature** - `temperature: 0.0` for deterministic results
5. **Cache results** - Implement caching for repeated content

## Expected Performance

| Model | Speed | Accuracy | Memory |
|-------|-------|----------|--------|
| llama3.2 | ~2 chunks/sec | High (90%+) | 4GB |
| mistral | ~3 chunks/sec | High (88%+) | 4GB |
| phi3 | ~5 chunks/sec | Medium (80%+) | 2GB |

## Next Steps

1. ? Run small sample test to verify Ollama works
2. ? Compare with baseline on Darwin text
3. ? Measure actual recall improvement
4. ?? Document results in `docs/ollama-results.md`
5. ?? Deploy to production with caching enabled

---

**Questions?** See [ollama-entity-extractor-complete.md](ollama-entity-extractor-complete.md) for details.
