# Phase 12: Named Entity Recognition Integration

[? Back to Master Plan](../MasterPlan.md)

---

## Phase Information

| Attribute | Value |
|-----------|-------|
| **Phase Number** | 12 |
| **Status** | ?? **PENDING** |
| **Duration** | 3 weeks |
| **Prerequisites** | Phase 11 complete |
| **Test Count** | 50+ |
| **Documentation** | ?? Pending |
| **LOC Estimate** | ~2,500 |

---

## Objective

Integrate advanced Named Entity Recognition (NER) capabilities to extract Person, Organization, Location, Date/Time entities with high precision. Implement entity normalization and resolution.

---

## Why This Phase?

- **High-Value Entities**: NER extracts the most important entity types
- **80%+ Precision**: ML-based extraction vs. keyword heuristics
- **Cross-Document Linking**: Entity resolution enables multi-document graphs
- **Production Ready**: Use battle-tested NER libraries

---

## Tasks

### 12.1. NER Provider Selection ? PENDING

- [ ] Evaluate ML.NET NER models
- [ ] Evaluate Azure Cognitive Services
- [ ] Evaluate spaCy.NET integration
- [ ] Create comparison matrix (precision, recall, cost, latency)
- [ ] Select primary provider
- [ ] Document decision rationale

### 12.2. NER Provider Interface ? PENDING

- [ ] Define `INERProvider` interface
- [ ] Define `NERResult` class
- [ ] Define `NEREntity` class
- [ ] Support for confidence scores
- [ ] Support for entity spans (start, end positions)
- [ ] Support for batch processing

### 12.3. ML.NET NER Implementation ? PENDING

- [ ] Implement `MLNetNERProvider` class
- [ ] Load pre-trained NER model
- [ ] Batch text processing
- [ ] Entity type mapping
- [ ] Confidence score extraction
- [ ] Performance optimization

### 12.4. Azure Cognitive Services Implementation ? PENDING

- [ ] Implement `AzureCognitiveNERProvider` class
- [ ] API key management
- [ ] Rate limiting and retry logic
- [ ] Entity type mapping
- [ ] Cost tracking
- [ ] Fallback to offline provider

### 12.5. Named Entity Extractor ? PENDING

- [ ] Implement `NamedEntityExtractor` class
- [ ] Support for multiple NER providers
- [ ] Provider fallback logic
- [ ] Entity confidence thresholding
- [ ] Entity deduplication within chunks
- [ ] Source tracking (chunk IDs, positions)

### 12.6. Entity Normalization ? PENDING

- [ ] Implement `EntityNormalizer` class
- [ ] Name normalization (lowercase, trim, remove accents)
- [ ] Organization name normalization ("Microsoft Corp" ? "microsoft corporation")
- [ ] Location normalization (geocoding optional)
- [ ] Date normalization (various formats ? ISO 8601)
- [ ] Alias detection and grouping

### 12.7. Entity Resolution ? PENDING

- [ ] Implement `EntityResolver` class
- [ ] Fuzzy matching algorithm (Levenshtein distance)
- [ ] Alias-based matching
- [ ] Context-based disambiguation
- [ ] Confidence scoring for matches
- [ ] Entity merging strategy

### 12.8. Confidence Scoring ? PENDING

- [ ] NER provider confidence
- [ ] Frequency-based confidence
- [ ] Context-based confidence
- [ ] Combined confidence calculation
- [ ] Confidence calibration

### 12.9. Integration with Extraction Pipeline ? PENDING

- [ ] Add `NamedEntityExtractor` to pipeline
- [ ] Configure NER provider in options
- [ ] Entity normalization step
- [ ] Entity resolution step
- [ ] Merge with keyword entities

### 12.10. Testing ? PENDING

- [ ] Unit tests for `NamedEntityExtractor`
- [ ] Unit tests for `EntityNormalizer`
- [ ] Unit tests for `EntityResolver`
- [ ] Unit tests for each NER provider
- [ ] Integration tests with real documents
- [ ] Precision/recall benchmarks
- [ ] Performance benchmarks

### 12.11. Documentation ? PENDING

- [ ] Update `Phase-12.md` documentation
- [ ] NER provider comparison guide
- [ ] Configuration guide
- [ ] Entity type reference
- [ ] Benchmark results documentation

---

## Deliverables

| Deliverable | Status | Location |
|-------------|--------|----------|
| `INERProvider` interface | ? Pending | `Interfaces/INERProvider.cs` |
| `MLNetNERProvider` | ? Pending | `KnowledgeGraph/NER/` |
| `AzureCognitiveNERProvider` | ? Pending | `KnowledgeGraph/NER/` |
| `NamedEntityExtractor` | ? Pending | `KnowledgeGraph/Extractors/` |
| `EntityNormalizer` | ? Pending | `KnowledgeGraph/` |
| `EntityResolver` | ? Pending | `KnowledgeGraph/` |
| 50+ unit tests | ? Pending | `Tests/Unit/KnowledgeGraph/` |
| Integration tests | ? Pending | `Tests/Integration/KnowledgeGraph/` |
| Benchmark suite | ? Pending | `Benchmarks/KnowledgeGraph/` |
| Documentation | ? Pending | `docs/` |

---

## Technical Details

### NER Pipeline

```
1. Input: List<ChunkerBase> chunks
2. Batch chunks for NER processing
3. Call NER provider (ML.NET or Azure)
4. For each detected entity:
   a. Map NER type to EntityType enum
   b. Extract confidence score
   c. Track source chunk and position
   d. Normalize entity name
5. Resolve duplicate entities
6. Merge aliases
7. Output: List<Entity>
```

### Entity Normalization Rules

**Person Names**:
- "John Doe" ? "john doe"
- "Dr. Jane Smith, PhD" ? "jane smith"
- Remove titles and suffixes

**Organizations**:
- "Microsoft Corporation" ? "microsoft corporation"
- "Amazon.com, Inc." ? "amazon"
- "Apple, Inc." ? "apple"

**Locations**:
- "New York City, NY" ? "new york city"
- "United States of America" ? "united states"

**Dates**:
- "January 1st, 2024" ? "2024-01-01"
- "1/1/24" ? "2024-01-01"

### Entity Resolution Algorithm

```csharp
public async Task<List<Entity>> ResolveAsync(List<Entity> entities)
{
    var groups = new Dictionary<string, List<Entity>>();
    
    foreach (var entity in entities)
    {
     // Find potential matches
      var matches = FindPotentialMatches(entity, groups);
  
     if (matches.Any())
        {
            // Merge with best match
      var bestMatch = SelectBestMatch(entity, matches);
            MergeEntities(bestMatch, entity);
        }
  else
        {
  // Create new entity group
            groups[entity.NormalizedName] = new List<Entity> { entity };
        }
    }
    
    return groups.Values.Select(g => g.First()).ToList();
}

private List<Entity> FindPotentialMatches(Entity entity, Dictionary<string, List<Entity>> groups)
{
    var matches = new List<Entity>();
    
    foreach (var group in groups.Values)
    {
        var representative = group.First();
    
        // Exact match on normalized name
        if (entity.NormalizedName == representative.NormalizedName)
  {
        matches.AddRange(group);
          continue;
}
     
        // Fuzzy match (Levenshtein distance < 3)
        if (LevenshteinDistance(entity.NormalizedName, representative.NormalizedName) < 3)
        {
     matches.AddRange(group);
 continue;
     }
        
     // Alias match
        if (entity.Aliases.Any(a => representative.Aliases.Contains(a)))
        {
      matches.AddRange(group);
        }
 }
    
    return matches;
}
```

---

## Success Criteria

? **NER Integration**:
- ML.NET provider working
- Azure Cognitive Services provider working
- Provider fallback logic
- Batch processing support

? **Entity Extraction**:
- 80%+ precision on Person entities
- 80%+ precision on Organization entities
- 75%+ precision on Location entities
- 70%+ precision on Date entities

? **Normalization & Resolution**:
- Handle common variations
- Fuzzy matching working
- Alias detection working
- Entity merging accurate

? **Performance**:
- Process 100-page document in < 10 seconds
- Batch processing reduces API calls
- Acceptable cost for Azure provider

? **Testing**:
- 50+ unit tests passing
- Integration tests with real documents
- Benchmark suite complete
- Precision/recall documented

---

## Performance Targets

| Operation | Target | Notes |
|-----------|--------|-------|
| NER extraction (100 chunks) | < 5 seconds | ML.NET local processing |
| NER extraction (100 chunks) | < 10 seconds | Azure API with rate limiting |
| Entity normalization | < 100ms | 100 entities |
| Entity resolution | < 500ms | 100 entities |
| End-to-end (100 chunks) | < 10 seconds | Including NER, normalize, resolve |

---

## Dependencies

**External Libraries**:
- `Microsoft.ML` (ML.NET)
- `Azure.AI.TextAnalytics` (Azure Cognitive Services)
- Optional: `Fastenshtein` (fast Levenshtein distance)

**Configuration**:
- Azure Cognitive Services API key (for Azure provider)
- NER model files (for ML.NET)

**Internal**:
- Phase 11 complete (core models and interfaces)
- Entity extraction pipeline

---

## Example Usage

```csharp
// Configure NER with ML.NET
var options = new ChunkingOptions
{
    EnableKnowledgeGraph = true,
    KnowledgeGraphOptions = new KnowledgeGraphOptions
    {
 EnableNER = true,
        NERProvider = new MLNetNERProvider("models/ner-model.zip"),
        MinEntityConfidence = 0.7
    }
};

// Or configure with Azure Cognitive Services
var azureOptions = new ChunkingOptions
{
    EnableKnowledgeGraph = true,
    KnowledgeGraphOptions = new KnowledgeGraphOptions
    {
     EnableNER = true,
        NERProvider = new AzureCognitiveNERProvider(
    endpoint: "https://....cognitiveservices.azure.com/",
      apiKey: Configuration["Azure:CognitiveServices:Key"]
        ),
        MinEntityConfidence = 0.8
    }
};

// Extract entities
var result = await DocumentChunker.ChunkFileAsync("document.pdf", options);

// Access named entities
var people = result.KnowledgeGraph.Graph.Entities
    .Where(e => e.Type == EntityType.Person)
    .OrderByDescending(e => e.Frequency)
    .ToList();

var organizations = result.KnowledgeGraph.Graph.Entities
    .Where(e => e.Type == EntityType.Organization)
    .ToList();

// Check entity resolution
foreach (var person in people)
{
    Console.WriteLine($"{person.Name} (aliases: {string.Join(", ", person.Aliases)})");
  Console.WriteLine($"  Appears in {person.ChunkIds.Count} chunks");
    Console.WriteLine($"  Confidence: {person.Confidence:F2}");
}
```

---

## Benchmark Results (Target)

| Document Type | Entities Extracted | Precision | Recall | Time |
|---------------|-------------------|-----------|--------|------|
| News article (50 paragraphs) | 120 | 85% | 78% | 3.2s |
| Business document (100 pages) | 450 | 82% | 75% | 8.7s |
| Research paper (30 pages) | 180 | 88% | 82% | 4.1s |
| Legal contract (80 pages) | 220 | 80% | 72% | 7.3s |

---

## Status: **?? PENDING**

**Ready to Start**: After Phase 11 complete

**Estimated Start Date**: Q1 2025

---

[? Back to Master Plan](../MasterPlan.md) | [Previous Phase: Knowledge Graph Foundation ?](Phase-11.md) | [Next Phase: Advanced Relationships ?](Phase-13.md)
