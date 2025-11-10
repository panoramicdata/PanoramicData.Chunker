# GraphTrainer Tool - Design Specification

**Version**: 2.0  
**Phase**: 12.4 - Automated Training Tools  
**Status**: Design Document

---

## Overview

The GraphTrainer is a command-line tool for training and optimizing knowledge graph extraction models. It uses Ollama.Api for ML-powered pattern suggestions and provides commands for discovering patterns, optimizing confidence scores, and building entity dictionaries from document corpora.

---

## Project Structure

```
PanoramicData.Chunker.GraphTrainer/
??? PanoramicData.Chunker.GraphTrainer.csproj
??? Program.cs               # CLI entry point
??? Commands/
?   ??? DiscoverPatternsCommand.cs       # Discover relationship patterns
?   ??? OptimizeConfidenceCommand.cs     # Optimize confidence scores
?   ??? BuildDictionaryCommand.cs        # Build entity dictionaries
?   ??? ValidateCommand.cs    # Validate JSON configs
?   ??? AnalyzeCorpusCommand.cs     # Corpus analysis
?   ??? GenerateGroundTruthCommand.cs    # LLM-powered ground truth
??? Services/
?   ??? OllamaService.cs                 # Ollama API integration
?   ??? PatternDiscoveryService.cs       # Pattern extraction logic
?   ??? ConfidenceOptimizerService.cs    # Confidence tuning
?   ??? DictionaryBuilderService.cs      # Dictionary generation
?   ??? GroundTruthGenerator.cs # Ground truth creation
??? Models/
?   ??? CorpusAnalysis.cs   # Corpus statistics
?   ??? PatternCandidate.cs       # Discovered pattern
?   ??? ConfidenceOptimizationResult.cs  # Optimization results
?   ??? DictionaryEntry.cs       # Dictionary suggestions
??? Utilities/
    ??? TextAnalyzer.cs      # Text processing utilities
    ??? ProgressReporter.cs  # Progress reporting
    ??? ValidationHelper.cs           # Validation utilities

PanoramicData.Chunker.GraphTrainer.Tests/
??? Commands/
?   ??? *Tests.cs      # Command tests
??? Services/
?   ??? *Tests.cs          # Service tests
??? TestData/
    ??? sample-corpus/       # Test documents
    ??? sample-groundtruth.txt     # Test ground truth
    ??? sample-patterns.json# Test patterns
```

---

## Dependencies

```xml
<PackageReference Include="Ollama.Api" Version="1.0.7" />
<PackageReference Include="System.CommandLine" Version="2.0.0-rc.2.25502.107" />
<PackageReference Include="Spectre.Console" Version="0.49.1" />
<ProjectReference Include="..\PanoramicData.Chunker\PanoramicData.Chunker.csproj" />
```

---

## Commands

### 1. discover-patterns

**Purpose**: Discover relationship patterns from corpus using LLM analysis

**Usage**:
```bash
graphtrainer discover-patterns \
  --corpus ./documents/ \
  --groundtruth ./truth.txt \
  --output ./discovered-patterns.json \
  --model llama3.2 \
  --min-confidence 0.7
```

**Options**:
- `--corpus` (required): Directory containing documents to analyze
- `--groundtruth` (required): Ground truth file with known relationships
- `--output` (optional): Output file path (default: discovered-patterns.json)
- `--model` (optional): Ollama model to use (default: llama3.2)
- `--min-confidence` (optional): Minimum confidence threshold (default: 0.7)
- `--max-patterns` (optional): Maximum patterns to discover (default: 50)

**Algorithm**:
1. Load corpus and ground truth
2. Extract entity pairs with known relationships
3. For each relationship:
   - Get text between entities
   - Use Ollama to identify linguistic patterns
   - Generate regex from patterns
4. Test patterns against corpus
5. Calculate precision/recall for each pattern
6. Output patterns with confidence scores

**Output Format**:
```json
{
  "version": "2.0",
  "discoveredAt": "2025-01-15T10:30:00Z",
  "model": "llama3.2",
  "patterns": [
    {
      "name": "Discovered_Founded_1",
      "regex": "\\b(founded\\s+by)\\b",
      "relationshipType": "Founded",
      "confidence": 0.95,
      "precision": 0.92,
      "recall": 0.85,
    "examples": [
        "The Society was founded by Professor Jameson",
  "Darwin University founded by John Smith"
 ],
      "source": "llm_analysis"
    }
  ]
}
```

### 2. optimize-confidence

**Purpose**: Optimize confidence scores using ground truth and performance metrics

**Usage**:
```bash
graphtrainer optimize-confidence \
  --patterns ./RelationshipPatterns.json \
  --groundtruth ./truth.txt \
  --corpus ./documents/ \
  --target-recall 0.7 \
  --target-precision 0.8
```

**Options**:
- `--patterns` (required): Path to patterns JSON file
- `--groundtruth` (required): Ground truth file
- `--corpus` (required): Document corpus
- `--target-recall` (optional): Target recall (default: 0.7)
- `--target-precision` (optional): Target precision (default: 0.8)
- `--output` (optional): Output file (default: overwrites input)

**Algorithm**:
1. Load patterns and ground truth
2. Run extraction on corpus
3. Calculate current precision/recall for each pattern
4. Use gradient descent or grid search to find optimal confidence scores
5. Validate against constraints (recall ? target, precision ? target)
6. Output optimized configuration

**Output**:
- Updated patterns JSON with optimized confidence scores
- Metrics report showing improvements

### 3. build-dictionary

**Purpose**: Build entity dictionaries from high-frequency proper nouns in corpus

**Usage**:
```bash
graphtrainer build-dictionary \
  --corpus ./documents/ \
  --min-frequency 10 \
  --output ./custom-entities.json \
  --model llama3.2
```

**Options**:
- `--corpus` (required): Directory with documents
- `--min-frequency` (optional): Minimum occurrences (default: 5)
- `--output` (optional): Output file (default: entity-dictionary.json)
- `--model` (optional): Ollama model for classification (default: llama3.2)
- `--classify` (optional): Use LLM to classify entities (default: true)

**Algorithm**:
1. Extract all capitalized terms from corpus
2. Count frequency of each term
3. Filter by min-frequency threshold
4. Use Ollama to classify entities:
   - Person
   - Place
   - Organization
   - Other
5. Output organized dictionary

**Output Format**:
```json
{
  "version": "2.0",
  "generatedAt": "2025-01-15T10:30:00Z",
  "corpus": "./documents/",
  "minFrequency": 10,
  "properNounDictionary": {
    "people": ["Darwin", "Jameson", "Henslow"],
    "places": ["Edinburgh", "Cambridge", "Galapagos"],
    "organizations": ["Plinian", "Royal Society"]
  },
  "statistics": {
    "totalTerms": 5000,
    "uniqueTerms": 1500,
    "filtered": 150,
    "classified": {
"people": 50,
      "places": 40,
      "organizations": 30,
      "unclassified": 30
    }
  }
}
```

### 4. validate

**Purpose**: Validate configuration JSON files

**Usage**:
```bash
graphtrainer validate \
  --patterns ./RelationshipPatterns.json \
  --entities ./EntityPatterns.json
```

**Options**:
- `--patterns` (optional): Relationship patterns file
- `--entities` (optional): Entity patterns file
- `--strict` (optional): Enable strict validation (default: false)

**Validation Checks**:
- ? JSON schema compliance
- ? Regex compilation
- ? Confidence scores in range [0.0, 1.0]
- ? RelationshipType enum values
- ? No duplicate pattern names
- ? Examples match patterns (if strict)

### 5. analyze-corpus

**Purpose**: Analyze corpus for insights and statistics

**Usage**:
```bash
graphtrainer analyze-corpus \
  --corpus ./documents/ \
  --output ./analysis-report.json
```

**Options**:
- `--corpus` (required): Directory with documents
- `--output` (optional): Output file (default: corpus-analysis.json)
- `--detailed` (optional): Include detailed analysis (default: false)

**Analysis Output**:
```json
{
  "summary": {
    "totalDocuments": 100,
    "totalChunks": 2500,
    "averageChunkSize": 450,
    "totalEntities": 5000,
    "uniqueEntities": 1200,
    "totalRelationships": 8000
  },
  "entityDistribution": {
 "ProperNoun": 3000,
    "Organization": 1500,
    "Location": 500
  },
  "relationshipDistribution": {
    "Founded": 50,
    "MemberOf": 120,
    "StudiedAt": 80
  },
  "topEntities": [
    {"name": "Darwin", "frequency": 150},
    {"name": "Edinburgh", "frequency": 95}
  ],
  "recommendations": [
    "Add 'Darwin' to people dictionary (high frequency)",
    "Consider adding 'GraduatedFrom' pattern (observed 45 times)"
  ]
}
```

### 6. generate-groundtruth

**Purpose**: Generate ground truth annotations using LLM analysis

**Usage**:
```bash
graphtrainer generate-groundtruth \
  --corpus ./documents/ \
  --output ./groundtruth.txt \
  --model llama3.2 \
  --confidence-threshold 0.8
```

**Options**:
- `--corpus` (required): Documents to annotate
- `--output` (optional): Output file (default: groundtruth.txt)
- `--model` (optional): Ollama model (default: llama3.2)
- `--confidence-threshold` (optional): Minimum confidence (default: 0.8)
- `--review` (optional): Enable human review mode (default: false)

**Algorithm**:
1. Chunk documents
2. For each chunk, use Ollama to:
   - Extract entities
   - Identify relationships
   - Assign confidence scores
3. Filter by confidence threshold
4. If review mode: Present to user for verification
5. Output in ground truth format

**Output Format** (TSV):
```
Entity1	RelationType	Entity2	Confidence	ChunkID	Context
Darwin	StudiedAt	Edinburgh	0.95	chunk-001	...Darwin studied at Edinburgh University...
Professor Jameson	Founded	Plinian Society	0.98	chunk-002	...the Society was founded by Professor Jameson...
```

---

## OllamaService

**Key Methods**:

```csharp
public class OllamaService
{
    private readonly IOllamaApiClient _client;
    
    public async Task<List<string>> DiscoverPatternsAsync(
        string text,
        string relationshipType,
        CancellationToken cancellationToken);
    
    public async Task<EntityClassification> ClassifyEntityAsync(
        string entityName,
        string context,
        CancellationToken cancellationToken);
    
    public async Task<List<GroundTruthAnnotation>> AnnotateTextAsync(
        string text,
        CancellationToken cancellationToken);
    
    public async Task<string> GenerateRegexAsync(
    List<string> examples,
        CancellationToken cancellationToken);
}
```

**Prompts**:

```csharp
// Pattern Discovery Prompt
var prompt = $@"
Analyze the following text and identify linguistic patterns that indicate a '{relationshipType}' relationship:

Text: {text}

Entity 1: {entity1}
Entity 2: {entity2}

Provide regex patterns that could match the text between these entities.
Focus on verbs, prepositions, and connective phrases.

Output format: One pattern per line, no explanations.
";

// Entity Classification Prompt
var prompt = $@"
Classify the following entity into one of these categories:
- Person
- Place
- Organization
- Other

Entity: {entityName}
Context: {context}

Output only the category name.
";

// Ground Truth Annotation Prompt
var prompt = $@"
Extract entities and relationships from this text:

{text}

For each relationship found, provide:
1. Entity 1 name
2. Relationship type (Founded, MemberOf, StudiedAt, etc.)
3. Entity 2 name
4. Confidence (0.0-1.0)
5. Brief context

Output format (one per line):
Entity1 | RelationType | Entity2 | Confidence | Context
";
```

---

## Example Workflows

### Workflow 1: Discover Patterns from New Domain

```bash
# 1. Analyze corpus to understand it
graphtrainer analyze-corpus --corpus ./biology-docs/ --output analysis.json

# 2. Generate ground truth using LLM
graphtrainer generate-groundtruth \
  --corpus ./biology-docs/ \
  --output biology-truth.txt \
  --model llama3.2 \
  --review

# 3. Discover patterns
graphtrainer discover-patterns \
  --corpus ./biology-docs/ \
  --groundtruth biology-truth.txt \
  --output biology-patterns.json

# 4. Build entity dictionary
graphtrainer build-dictionary \
  --corpus ./biology-docs/ \
  --min-frequency 5 \
  --output biology-entities.json

# 5. Validate configurations
graphtrainer validate \
  --patterns biology-patterns.json \
  --entities biology-entities.json
```

### Workflow 2: Optimize Existing Patterns

```bash
# 1. Optimize confidence scores
graphtrainer optimize-confidence \
  --patterns RelationshipPatterns.json \
  --groundtruth darwin-truth.txt \
  --corpus ./darwin-autobiography/ \
  --target-recall 0.7 \
  --target-precision 0.8

# 2. Validate optimized patterns
graphtrainer validate --patterns RelationshipPatterns.json --strict
```

### Workflow 3: Incremental Dictionary Building

```bash
# 1. Build dictionary from new documents
graphtrainer build-dictionary \
  --corpus ./new-documents/ \
  --min-frequency 3 \
  --output new-entities.json

# 2. Merge with existing dictionary (manual step)
# User edits EntityPatterns.json to add new entries

# 3. Validate merged dictionary
graphtrainer validate --entities EntityPatterns.json
```

---

## Implementation Notes

### Ollama Integration

```csharp
// Initialize Ollama client
var client = new OllamaApiClient(new HttpClient
{
  BaseAddress = new Uri("http://localhost:11434")
});

// Check model availability
var models = await client.ListLocalModelsAsync();
if (!models.Any(m => m.Name == "llama3.2"))
{
    await client.PullModelAsync("llama3.2");
}

// Generate completion
var response = await client.GenerateCompletionAsync(new GenerateCompletionRequest
{
    Model = "llama3.2",
    Prompt = prompt,
  Stream = false,
    Options = new ModelOptions
    {
 Temperature = 0.7,
      NumPredict = 500
    }
});
```

### Progress Reporting (Spectre.Console)

```csharp
await AnsiConsole.Progress()
    .StartAsync(async ctx =>
    {
        var task = ctx.AddTask("[green]Discovering patterns[/]");
        
     foreach (var doc in documents)
        {
      await ProcessDocument(doc);
     task.Increment(100.0 / documents.Count);
        }
    });
```

### Pattern Testing

```csharp
// Test pattern against corpus
var pattern = new Regex(patternRegex);
var matches = 0;
var falsePositives = 0;

foreach (var (entity1, entity2, expectedType) in groundTruth)
{
    var text = GetTextBetween(entity1, entity2);
    var match = pattern.Match(text);
    
    if (match.Success && expectedType == relationshipType)
    {
        matches++;
    }
    else if (match.Success)
    {
falsePositives++;
    }
}

var precision = matches / (double)(matches + falsePositives);
var recall = matches / (double)groundTruth.Count;
```

---

## Testing Strategy

### Unit Tests

- Command parsing and validation
- Service methods with mocked Ollama
- Pattern generation and testing
- Dictionary building logic

### Integration Tests

- End-to-end command execution
- Actual Ollama integration (when available)
- File I/O and JSON serialization

### Test Data

- Sample corpus (Darwin autobiography excerpts)
- Sample ground truth
- Known-good patterns and entities

---

## Deployment

### As Global Tool

```xml
<PropertyGroup>
  <PackAsTool>true</PackAsTool>
  <ToolCommandName>graphtrainer</ToolCommandName>
  <PackageOutputPath>./nupkg</PackageOutputPath>
</PropertyGroup>
```

```bash
# Install globally
dotnet tool install --global PanoramicData.Chunker.GraphTrainer

# Use from anywhere
graphtrainer discover-patterns --corpus ./docs/
```

### As Project Tool

```bash
# Add to project
dotnet tool install PanoramicData.Chunker.GraphTrainer

# Use via dotnet
dotnet graphtrainer discover-patterns --corpus ./docs/
```

---

## Future Enhancements

1. **Interactive Mode**: TUI for guided training
2. **Batch Processing**: Process multiple corpora in parallel
3. **Cloud Integration**: Use Azure OpenAI instead of Ollama
4. **Active Learning**: Suggest which documents to annotate next
5. **Pattern Evolution**: Track pattern performance over time
6. **A/B Testing**: Compare pattern sets automatically

---

**Design Complete** ?  
**Ready for Implementation**: Phase 12.4

---

*Last Updated: January 2025*  
*Version: 2.0*

