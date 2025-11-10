# Knowledge Graph Training Guide

**Version**: 2.0  
**Phase**: 12 - Named Entity Recognition Enhancement  
**Last Updated**: January 2025

---

## Overview

Phase 12 introduces **trainable, configuration-driven** entity and relationship extraction. Instead of hardcoded patterns, all extraction rules are defined in JSON configuration files that can be:

- ? **Edited by users** without recompiling
- ? **Version controlled** alongside your application
- ? **Domain-customized** for specific document types
- ? **Trained** using automated tools (future enhancement)

---

## Architecture

### Configuration Files

| File | Purpose | Location |
|------|---------|----------|
| `RelationshipPatterns.json` | Defines regex patterns for detecting relationships | `Configuration/` |
| `RelationshipPatternsSchema.json` | JSON schema for validation | `Configuration/` |
| `EntityPatterns.json` | Defines proper noun dictionaries and rules | `Configuration/` |
| `EntityPatternsSchema.json` | JSON schema for validation | `Configuration/` |

These files are:
- **Copied to output directory** on build (via `CopyToOutputDirectory="PreserveNewest"`)
- **Included in NuGet package** (via `Pack="true"`)
- **User-editable** at runtime

### Loading Behavior

```csharp
// Default: Automatically loads from Configuration/ directory
var extractor = new PatternBasedRelationshipExtractor();

// Custom: Load from specific file
var extractor = await PatternBasedRelationshipExtractor.CreateAsync(
    "CustomPatterns/MyDomain.json");
```

**Search Order**:
1. Current directory (`Configuration/RelationshipPatterns.json`)
2. Application base directory
3. Entry assembly directory (for tests)
4. Executing assembly directory

---

## Training Workflow

### Phase 1: Baseline Extraction

1. **Run extraction** on your document corpus with default patterns
2. **Collect metrics** (precision, recall, F1)
3. **Identify gaps** in detection

### Phase 2: Pattern Analysis

1. **Review missed relationships** (false negatives)
2. **Extract text samples** where ground truth relationships exist
3. **Identify linguistic patterns** used in your domain

### Phase 3: Pattern Addition

1. **Edit `RelationshipPatterns.json`**
2. **Add new patterns** based on observed text
3. **Set confidence scores** based on pattern specificity
4. **Test against ground truth**

### Phase 4: Iteration

1. **Re-run extraction** with updated patterns
2. **Measure improvement** in recall/precision
3. **Adjust confidence scores** to balance metrics
4. **Repeat** until targets met

---

## Relationship Pattern Training

### Pattern Structure

```json
{
  "name": "PatternName",
  "regex": "\\b(pattern|words)\\b",
  "relationshipType": "RelationshipTypeName",
  "confidence": 0.9,
  "isDirectional": true,
  "description": "What this pattern matches",
  "examples": [
    "Example sentence 1",
    "Example sentence 2"
  ],
  "category": "CategoryName",
  "enabled": true
}
```

### Adding a New Pattern

**Example**: Adding a "GraduatedFrom" relationship pattern

1. **Identify the linguistic pattern** in your documents:
   ```
   "John graduated from MIT in 2020"
   "She received her degree from Harvard"
   "Darwin earned his BA from Cambridge University"
   ```

2. **Extract the pattern**: `graduated from|received.*degree from|earned.*from`

3. **Add to `RelationshipPatterns.json`**:
   ```json
   {
     "name": "GraduatedFrom",
     "regex": "\\b(graduated\\s+from|received.*?degree\\s+from|earned.*?from)\\b",
     "relationshipType": "StudiedAt",
     "confidence": 0.95,
     "isDirectional": true,
     "description": "Educational degree completion",
     "examples": [
       "John graduated from MIT",
"She received her PhD from Harvard"
     ],
     "category": "Educational",
     "enabled": true
   }
   ```

4. **Test the pattern**:
 ```bash
   dotnet test --filter "GroundTruthComparisonTests"
   ```

5. **Adjust confidence** based on precision/recall metrics

### Confidence Scoring Guidelines

| Range | Description | Use Case |
|-------|-------------|----------|
| 0.9-1.0 | Very high | Unambiguous patterns with few false positives |
| 0.7-0.9 | High | Specific patterns with clear intent |
| 0.5-0.7 | Medium | Moderately specific patterns |
| 0.3-0.5 | Low | Generic patterns, catch-all |
| 0.0-0.3 | Very low | Experimental, high false positive risk |

**Example**:
- `"founded by"` ? 0.95 (very specific, unambiguous)
- `"worked with"` ? 0.75 (specific but could mean many things)
- `"and"` ? 0.50 (very generic, used for fallback)

### Pattern Categories

Organize patterns by domain for maintainability:

- **Organizational**: Founding, membership, management
- **Educational**: Study, mentorship, teaching
- **Academic**: Presentation, publication, research
- **Employment**: Work, employment, service
- **Geographic**: Location, residence
- **Scientific**: Discovery, observation, experimentation
- **Social**: Collaboration, support, relationships

---

## Entity Pattern Training

### Dictionary Structure

```json
{
  "properNounDictionary": {
    "people": ["CommonFirstName", "CommonLastName"],
    "places": ["CityName", "CountryName"],
    "organizations": ["CompanyName", "UniversityName"]
  },
  "titlePrefixes": {
    "academic": ["Professor", "Dr"],
    "corporate": ["CEO", "CTO"]
  },
  "organizationalSuffixes": [
    "University", "Corporation", "Institute"
  ]
}
```

### Adding Domain-Specific Entities

**Example**: Biotechnology domain

1. **Identify common entities** in your corpus:
   - Companies: "Genentech", "Amgen", "Biogen"
   - People: "Rosalind", "Watson", "Crick"
   - Places: "Cambridge", "Basel", "BioValley"

2. **Add to `EntityPatterns.json`**:
   ```json
   {
     "properNounDictionary": {
       "people": ["Rosalind", "Watson", "Crick", "Franklin"],
  "places": ["Basel", "BioValley", "ResearchPark"],
       "organizations": ["Genentech", "Amgen", "Biogen", "Biotech"]
     }
   }
   ```

3. **Add domain-specific titles**:
   ```json
   {
     "titlePrefixes": {
   "scientific": ["Dr", "Professor", "Researcher"],
       "corporate": ["CEO", "CSO", "VP"]
     }
 }
   ```

### Extraction Rules

Tune extraction parameters in `extractionRules`:

```json
{
  "extractionRules": {
    "minWordLength": 2,
    "minOccurrences": 1,
    "baseConfidence": 0.7,
    "confidenceBoosts": {
      "inDictionary": 0.15,
      "hasTitle": 0.10,
    "multiWord": 0.10,
      "organizationalSuffix": 0.10,
      "perFrequency": 0.05,
      "maxFrequencyBoost": 0.20
    }
  }
}
```

**Tuning Guide**:
- **Increase `minOccurrences`** to reduce rare entity noise (e.g., 2-3 for cleaner output)
- **Increase `baseConfidence`** for stricter extraction (e.g., 0.8 instead of 0.7)
- **Increase dictionary boost** to prioritize known entities
- **Decrease frequency boost** if rare entities are important

### Entity Extraction Training Workflow

#### Step 1: Run Baseline Entity Extraction

```csharp
// Use default configuration
var extractor = new CapitalizationEntityExtractor();
var entities = await extractor.ExtractEntitiesAsync(chunks, cancellationToken);

Console.WriteLine($"Extracted {entities.Count} entities");
Console.WriteLine($"False positives: {IdentifyFalsePositives(entities).Count}");
Console.WriteLine($"Missed entities: {IdentifyMissedEntities(entities, groundTruth).Count}");
```

#### Step 2: Analyze Results

Review extraction quality:

```bash
# False Positives (extracted but shouldn't be)
The, In, During, However...  # Sentence starters
ABSTRACT, INTRODUCTION...     # Section headers

# False Negatives (missed but should be extracted)
specialized-terms, domain-jargon
hyphenated-names, accented-names
```

#### Step 3: Update Dictionary

Add missed entities to `EntityPatterns.json`:

```json
{
  "properNounDictionary": {
  "people": [
    // Add names frequently appearing in your domain
      "Faraday", "Maxwell", "Curie", "Einstein"
    ],
    "places": [
      // Add locations specific to your documents
   "Galapagos", "Patagonia", "TierraDelFuego"
    ],
    "organizations": [
      // Add organizations mentioned
      "RoyalSociety", "Smithsonian", "UNESCO"
    ]
  }
}
```

#### Step 4: Tune Confidence Thresholds

Adjust confidence boosts based on precision/recall:

```json
{
  "extractionRules": {
    "baseConfidence": 0.75,  // Raised from 0.7
    "confidenceBoosts": {
"inDictionary": 0.20,  // Increased from 0.15
  "hasTitle": 0.15,      // Increased from 0.10
      "multiWord": 0.10,
      "organizationalSuffix": 0.10,
      "perFrequency": 0.03,  // Decreased from 0.05
      "maxFrequencyBoost": 0.15  // Decreased from 0.20
 }
  }
}
```

**Impact**:
- **Higher `baseConfidence`**: Fewer low-quality entities
- **Higher `inDictionary` boost**: Prioritize known entities
- **Higher `hasTitle` boost**: Better extraction of titled entities
- **Lower frequency boosts**: Reduce bias toward common words

#### Step 5: Add Domain Titles

If your documents use domain-specific titles:

```json
{
  "titlePrefixes": {
    "academic": ["Professor", "Dr", "PhD"],
    "scientific": ["Researcher", "Scientist", "Investigator"],
    "medical": ["Physician", "Surgeon", "Clinician"],
    "corporate": ["CEO", "CTO", "VP", "Director"],
    "government": ["Minister", "Senator", "Ambassador"],
    "ships": ["HMS", "USS", "RMS"],  // For maritime documents
    "geographic": ["Mount", "Mt", "Lake", "River"]
  }
}
```

#### Step 6: Test Custom Configuration

```csharp
// Load custom configuration
var extractor = await CapitalizationEntityExtractor.CreateAsync(
    "Configuration/Custom/BiologyEntities.json");

var entities = await extractor.ExtractEntitiesAsync(chunks, cancellationToken);

// Compare to baseline
var improvement = CalculateImprovement(baselineEntities, entities, groundTruth);
Console.WriteLine($"Recall improved by: {improvement.RecallDelta:P2}");
Console.WriteLine($"Precision improved by: {improvement.PrecisionDelta:P2}");
```

### Common Entity Extraction Issues

#### Issue 1: Too Many Sentence Starters

**Problem**: "The", "In", "However" extracted as entities

**Solution**: Add to `sentenceStarters` list:

```json
{
  "sentenceStarters": [
    "The", "In", "On", "At", "For", "With", "From", "To", "By",
    "However", "Therefore", "Thus", "Meanwhile", "Furthermore"
  ]
}
```

#### Issue 2: Missing Compound Names

**Problem**: "University of Edinburgh" ? only "Edinburgh" extracted

**Solution**: Add connectors:

```json
{
  "allowedConnectors": ["of", "the", "and", "in", "at", "on", "de", "del", "van", "von"]
}
```

#### Issue 3: False Positive Acronyms

**Problem**: "HTTP", "URL", "API" extracted as people

**Solution**: Extractor already filters 2-5 letter all-caps. For longer ones:

```json
{
  "sentenceStarters": ["HTTP", "HTTPS", "API", "REST", "JSON", "XML"]
}
```

#### Issue 4: Domain Jargon Not Recognized

**Problem**: Technical terms like "QuantumEntanglement", "NeuralNetwork" missed

**Solution**: Add to dictionary:

```json
{
  "properNounDictionary": {
    "organizations": ["QuantumComputing", "ArtificialIntelligence"],
    "places": ["SiliconValley", "ResearchTriangle"]
  }
}
```

### Evaluation Metrics

Track entity extraction quality:

```csharp
var metrics = EvaluateEntityExtraction(extractedEntities, groundTruth);

Console.WriteLine($"Total Entities: {metrics.TotalExtracted}");
Console.WriteLine($"True Positives: {metrics.TruePositives}");
Console.WriteLine($"False Positives: {metrics.FalsePositives}");
Console.WriteLine($"False Negatives: {metrics.FalseNegatives}");
Console.WriteLine($"Precision: {metrics.Precision:P2}");
Console.WriteLine($"Recall: {metrics.Recall:P2}");
Console.WriteLine($"F1 Score: {metrics.F1Score:P2}");
```

**Target Metrics**:
- **Precision**: >80% (few false positives)
- **Recall**: >70% (most entities found)
- **F1 Score**: >75% (balanced)

---

## Automated Training Tools (Future)

### Planned Tools

#### 1. Pattern Discovery Tool
```bash
dotnet run --project TrainingTools -- discover-patterns \
    --corpus ./documents/ \
    --ground-truth ./ground-truth.txt \
    --output ./discovered-patterns.json
```

Analyzes text between known entity pairs to suggest new patterns.

#### 2. Confidence Optimizer
```bash
dotnet run --project TrainingTools -- optimize-confidence \
    --patterns ./RelationshipPatterns.json \
    --ground-truth ./ground-truth.txt \
    --target-recall 0.7 \
    --target-precision 0.8
```

Automatically adjusts confidence scores to meet target metrics.

#### 3. Dictionary Builder
```bash
dotnet run --project TrainingTools -- build-dictionary \
    --corpus ./documents/ \
    --min-frequency 10 \
    --output ./custom-entities.json
```

Extracts high-frequency proper nouns for dictionary inclusion.

#### 4. Validation Runner
```bash
dotnet run --project TrainingTools -- validate \
    --patterns ./RelationshipPatterns.json \
    --entities ./EntityPatterns.json
```

Validates JSON schema and tests regex compilation.

---

## Best Practices

### 1. Version Control Your Patterns

```bash
git add Configuration/RelationshipPatterns.json
git add Configuration/EntityPatterns.json
git commit -m "Add biotech domain patterns (v2.1)"
```

### 2. Document Pattern Changes

Add comments to JSON (supported via `ReadCommentHandling.Skip`):

```json
{
  "name": "FoundedByPassive",
  // Added 2025-01-15: Improved recall for passive voice
  // See: Issue #42
  "regex": "\\b(founded\\s+by|established\\s+by)\\b",
  "confidence": 0.95
}
```

### 3. Test Against Ground Truth

Always validate changes:

```csharp
[Fact]
public async Task CustomPatterns_ShouldImproveRecall()
{
    var extractor = await PatternBasedRelationshipExtractor.CreateAsync(
"Configuration/Custom/BiologyPatterns.json");
    
    var results = await ExtractAndCompareAsync(extractor);
    results.RecallRate.Should().BeGreaterThan(0.7);
}
```

### 4. Start Conservative, Then Expand

1. **Start** with high-confidence patterns (0.9+)
2. **Measure** precision (should be >80%)
3. **Add** medium-confidence patterns (0.7-0.9)
4. **Balance** recall vs precision
5. **Avoid** very low confidence patterns (<0.5) unless necessary

### 5. Domain-Specific Configurations

Create separate pattern files per domain:

```
Configuration/
??? RelationshipPatterns.json     # General patterns
??? BiologyPatterns.json            # Biology domain
??? LegalPatterns.json              # Legal domain
??? FinancePatterns.json            # Finance domain
??? EntityPatterns.json             # General entities
```

Load domain-specific patterns:

```csharp
var extractor = await PatternBasedRelationshipExtractor.CreateAsync(
    "Configuration/BiologyPatterns.json");
```

---

## Metrics and Evaluation

### Ground Truth Format

Create `ground-truth.txt` with known relationships:

```
Entity1	RelationType	Entity2	Confidence	Section	Notes
Darwin	StudiedAt	Edinburgh	1.0	Education	Medical studies
Professor Jameson	Founded	Plinian Society	1.0	Organizations	Society founder
```

### Running Evaluation

```csharp
var groundTruth = GroundTruthLoader.Load("ground-truth.txt");
var results = GroundTruthComparison.Compare(extractedGraph, groundTruth);

Console.WriteLine($"Recall: {results.RecallRate:P2}");
Console.WriteLine($"Precision: {results.Precision:P2}");
Console.WriteLine($"F1 Score: {results.F1Score:P2}");
```

### Target Metrics (Phase 12 Goals)

| Metric | Baseline (Phase 11) | Target (Phase 12) | Actual |
|--------|---------------------|-------------------|--------|
| Recall | 2.0% | **50-60%** | TBD |
| Precision | 0.01% | **30-40%** | TBD |
| F1 Score | 0.04% | **40-50%** | TBD |

---

## Troubleshooting

### Pattern Not Matching

1. **Test regex** in online tool (regex101.com)
2. **Check escaping**: JSON requires `\\b` for word boundaries
3. **Verify entity names**: Ensure exact match with extracted entities
4. **Check distance**: Entities must be within `maxDistance` (default: 500 chars)

### Low Recall

1. **Add more patterns** covering different phrasings
2. **Lower confidence thresholds** (but watch precision!)
3. **Expand entity dictionaries** to catch more proper nouns
4. **Increase `maxDistance`** for longer documents

### Low Precision (Too Many False Positives)

1. **Increase confidence thresholds**
2. **Make patterns more specific** (add context words)
3. **Remove generic patterns** (e.g., "and", "with")
4. **Add negative lookaheads** to exclude common false positives

### File Not Found Errors

```
Entity patterns file not found: Configuration/EntityPatterns.json
```

**Solution**:
1. Check `CopyToOutputDirectory` in `.csproj`
2. Verify files exist in output directory: `bin/Debug/net9.0/Configuration/`
3. For tests, ensure files are in test project output too

---

## Migration from Phase 11

### Before (Hardcoded Patterns)

```csharp
// Phase 11: Patterns embedded in code
var extractor = new PatternBasedRelationshipExtractor();
// Cannot change patterns without recompiling
```

### After (Configuration-Driven)

```csharp
// Phase 12: Patterns in JSON
var extractor = new PatternBasedRelationshipExtractor();
// Edit Configuration/RelationshipPatterns.json to update patterns!
```

### Breaking Changes

**None!** Default constructor still works with built-in patterns.

**New Features**:
- `CreateAsync(string filePath)` - Load custom patterns
- `RelationshipPatternLoader.LoadPatternsAsync()` - Direct file loading
- `RelationshipPatternLoader.ValidatePatternsAsync()` - Validation
- `RelationshipPatternLoader.CreateTemplateAsync()` - Generate custom template

---

## Next Steps

1. ? **Phase 12.1**: Enhanced entity extraction (Complete)
2. ? **Phase 12.2**: Enhanced relationship patterns (Complete)
3. ? **Phase 12.3**: JSON configuration architecture (Complete)
4. ?? **Phase 12.4**: Training tools development
5. ?? **Phase 12.5**: Automated pattern discovery
6. ?? **Phase 12.6**: Confidence optimization

---

## Support and Contribution

### Reporting Issues

Found a pattern that doesn't work? [Open an issue](https://github.com/panoramicdata/PanoramicData.Chunker/issues) with:
- Sample text
- Expected relationship
- Pattern that should match
- Actual behavior

### Contributing Patterns

Have patterns for a specific domain? Submit a PR with:
- Domain-specific pattern file (`Configuration/YourDomain/RelationshipPatterns.json`)
- Ground truth test cases
- Documentation of pattern rationale

---

**Last Updated**: January 2025  
**Version**: 2.0 (Phase 12)

