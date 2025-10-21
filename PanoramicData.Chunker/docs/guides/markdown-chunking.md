# Markdown Chunking Guide

## Overview

The PanoramicData.Chunker library provides comprehensive support for parsing and chunking Markdown documents into semantically meaningful units. This guide explains how to use the Markdown chunker, the chunk types it produces, and best practices for working with Markdown content.

## Quick Start

### Basic Usage

```csharp
using PanoramicData.Chunker;
using PanoramicData.Chunker.Models;

// Create a factory
var factory = new ChunkerFactory();

// Get the Markdown chunker
var chunker = factory.GetChunker(DocumentType.Markdown);

// Chunk a document
using var stream = File.OpenRead("document.md");
var options = new ChunkingOptions
{
    MaxChunkSizeInTokens = 500,
    OverlapSizeInTokens = 50,
    Strategy = ChunkingStrategy.Structural
};

var result = await chunker.ChunkAsync(stream, options);

// Access the chunks
foreach (var chunk in result.Chunks)
{
    Console.WriteLine($"{chunk.SpecificType}: {chunk.GetPlainText()}");
}
```

### Auto-Detection

```csharp
// Let the factory auto-detect the document type
using var stream = File.OpenRead("document.md");
var chunker = await factory.GetChunkerForStreamAsync(stream, "document.md");

var result = await chunker.ChunkAsync(stream, options);
```

## Markdown Chunk Types

The Markdown chunker produces several specialized chunk types:

### 1. MarkdownSectionChunk

Represents headings (H1-H6) in the document.

```csharp
public class MarkdownSectionChunk : StructuralChunk
{
    public int HeadingLevel { get; set; }        // 1-6
    public string? HeadingText { get; set; }     // Text content
    public string? MarkdownSyntax { get; set; }  // e.g., "# Title"
}
```

**Example:**
```markdown
# Introduction
## Getting Started
### Installation
```

### 2. MarkdownParagraphChunk

Represents paragraphs of text.

```csharp
public class MarkdownParagraphChunk : ContentChunk
{
    public string? MarkdownContent { get; set; }  // Original Markdown
}
```

**Example:**
```markdown
This is a paragraph with **bold** and *italic* text.
```

### 3. MarkdownListItemChunk

Represents list items (ordered and unordered).

```csharp
public class MarkdownListItemChunk : ContentChunk
{
    public bool IsOrdered { get; set; }          // true for numbered lists
    public int? ItemNumber { get; set; }         // For ordered lists
    public int IndentLevel { get; set; }         // Nesting level
    public string? MarkdownContent { get; set; }
}
```

**Example:**
```markdown
- Unordered item
- Another item
  - Nested item

1. First item
2. Second item
```

### 4. MarkdownCodeBlockChunk

Represents code blocks (fenced and indented).

```csharp
public class MarkdownCodeBlockChunk : ContentChunk
{
    public string? Language { get; set; }        // e.g., "csharp", "python"
    public bool IsFenced { get; set; }           // true for ```
    public string? MarkdownContent { get; set; }
}
```

**Example:**
````markdown
```csharp
public void Hello()
{
    Console.WriteLine("Hello, World!");
}
```
````

### 5. MarkdownQuoteChunk

Represents blockquotes.

```csharp
public class MarkdownQuoteChunk : ContentChunk
{
    public int QuoteLevel { get; set; }          // Nesting level
    public string? MarkdownContent { get; set; }
}
```

**Example:**
```markdown
> This is a quote
> > Nested quote
```

### 6. MarkdownTableChunk

Represents Markdown tables with full structure preservation.

```csharp
public class MarkdownTableChunk : TableChunk
{
    public string? MarkdownContent { get; set; }
    public TableMetadata? TableInfo { get; set; }
}
```

**Example:**
```markdown
| Name | Age | City |
|------|-----|------|
| John | 30  | NYC  |
| Jane | 25  | LA   |
```

### 7. MarkdownImageChunk

Represents images (both inline and reference-style).

```csharp
public class MarkdownImageChunk : VisualChunk
{
    public string? AltText { get; set; }
    public string? Url { get; set; }
    public string? Title { get; set; }
    public bool IsReferenceStyle { get; set; }
}
```

**Example:**
```markdown
![Alt text](https://example.com/image.png "Title")
![Alt text][ref]

[ref]: https://example.com/image.png "Title"
```

## Hierarchical Structure

The Markdown chunker builds a hierarchical structure based on heading levels:

```markdown
# Chapter 1 (depth 0)
This is a paragraph (depth 1)

## Section 1.1 (depth 1)
Another paragraph (depth 2)

### Subsection 1.1.1 (depth 2)
More content (depth 3)
```

Each chunk knows its:
- **Depth**: How deep in the hierarchy (0 = root)
- **ParentId**: Reference to parent chunk
- **AncestorIds**: List of all ancestors
- **Children**: Direct child chunks (for structural chunks)

## Working with Chunks

### Filtering by Type

```csharp
using PanoramicData.Chunker.Extensions;

// Get only content chunks
var contentChunks = result.Chunks.ContentChunks();

// Get only sections
var sections = result.Chunks.OfType<MarkdownSectionChunk>();

// Get code blocks
var codeBlocks = result.Chunks.OfType<MarkdownCodeBlockChunk>();

// Get tables
var tables = result.Chunks.TableChunks();
```

### Navigating Hierarchy

```csharp
// Get all H1 sections (root level)
var topLevel = result.Chunks.RootChunks();

// Get sections at specific depth
var secondLevel = result.Chunks.AtDepth(1);

// Get children of a section
var children = section.GetChildren(result.Chunks);

// Get path from root to chunk
var path = chunk.GetPathFromRoot(result.Chunks);

// Get hierarchy as string
var hierarchyPath = chunk.GetHierarchyPath(result.Chunks);
// Output: "Chapter 1 > Section 1.1 > Subsection 1.1.1"
```

### Filtering by Content

```csharp
// Find chunks containing specific text
var searchResults = result.Chunks
    .ContainingText("API", StringComparison.OrdinalIgnoreCase);

// Filter by token count
var largeChunks = result.Chunks
    .WithMinTokens(100);

// Filter by quality metrics
var highQuality = result.Chunks
    .WithMinSemanticCompleteness(0.8);
```

### Converting Formats

```csharp
// Convert to plain text
string plainText = chunk.ToPlainText(includeChildren: true);

// Convert to Markdown
string markdown = chunk.ToMarkdown(includeMetadata: false);

// Convert to HTML
string html = chunk.ToHtml(includeMetadata: false);

// Get tree visualization
string tree = result.Chunks.ToTreeString();
/*
?? # Introduction
?  ?? Paragraph
?  ?? ## Getting Started
?     ?? Paragraph
?? # Conclusion
*/
```

## Chunking Options

### Size-Based Chunking

```csharp
var options = new ChunkingOptions
{
    MaxChunkSizeInTokens = 500,        // Maximum tokens per chunk
    OverlapSizeInTokens = 50,          // Overlap for context
    Strategy = ChunkingStrategy.Structural
};
```

When a chunk exceeds `MaxChunkSizeInTokens`, the chunker will:
1. Try to split at paragraph boundaries
2. Fall back to sentence boundaries
3. Fall back to phrase boundaries (punctuation)
4. Last resort: word-level splitting

### Overlap

Overlap helps maintain context between chunks:

```csharp
var options = new ChunkingOptions
{
    MaxChunkSizeInTokens = 200,
    OverlapSizeInTokens = 50  // Last 50 tokens repeated in next chunk
};
```

### Token Counting

```csharp
// Use default character-based counter
var options = new ChunkingOptions
{
    TokenCountingMethod = TokenCountingMethod.CharacterBased
};

// Or provide custom counter
var tokenCounter = new CharacterBasedTokenCounter(tokensPerCharacter: 0.25);
var chunker = new MarkdownDocumentChunker(tokenCounter);
```

## Quality Metrics

Every chunk includes quality metrics:

```csharp
var chunk = result.Chunks.First();
var metrics = chunk.QualityMetrics;

Console.WriteLine($"Tokens: {metrics.TokenCount}");
Console.WriteLine($"Words: {metrics.WordCount}");
Console.WriteLine($"Characters: {metrics.CharacterCount}");
Console.WriteLine($"Completeness: {metrics.SemanticCompleteness:P}");
Console.WriteLine($"Has complete sentences: {metrics.HasCompleteSentences}");
```

**SemanticCompleteness** indicates how "complete" a chunk is:
- **1.0**: Complete semantic unit (full paragraph, section, etc.)
- **0.8**: Mostly complete but may be truncated
- **0.5**: Partial chunk, split from larger content
- **< 0.5**: Fragment, missing significant context

## Metadata

Chunks include rich metadata:

```csharp
var metadata = chunk.Metadata;

Console.WriteLine($"Type: {metadata.DocumentType}");
Console.WriteLine($"Hierarchy: {metadata.Hierarchy}");
Console.WriteLine($"Source: {metadata.SourcePath}");
Console.WriteLine($"Created: {metadata.CreatedAt}");
Console.WriteLine($"Tags: {string.Join(", ", metadata.Tags)}");
```

## Serialization

### JSON Serialization

```csharp
using PanoramicData.Chunker.Serialization;

var serializer = new JsonChunkSerializer();

// Serialize to string
string json = serializer.SerializeToString(result.Chunks);

// Serialize to file
using var stream = File.Create("chunks.json");
await serializer.SerializeAsync(result.Chunks, stream);

// Serialize full result (includes statistics)
using var resultStream = File.Create("result.json");
await serializer.SerializeResultAsync(result, resultStream);

// Deserialize
using var inputStream = File.OpenRead("chunks.json");
var chunks = await serializer.DeserializeAsync(inputStream);
```

The JSON output includes type discriminators for proper deserialization:

```json
[
  {
    "$type": "MarkdownSection",
    "headingLevel": 1,
    "headingText": "Introduction",
    "id": "a1b2c3d4-...",
    "depth": 0,
    "sequenceNumber": 0,
    "qualityMetrics": {
      "tokenCount": 1,
      "wordCount": 1,
      "semanticCompleteness": 1.0
    }
  },
  {
    "$type": "MarkdownParagraph",
    "content": "This is an introduction.",
    "markdownContent": "This is an introduction.",
    "parentId": "a1b2c3d4-...",
    "depth": 1,
    "sequenceNumber": 1
  }
]
```

## Statistics

Every chunking operation produces statistics:

```csharp
var stats = result.Statistics;

Console.WriteLine($"Total chunks: {stats.TotalChunks}");
Console.WriteLine($"Content chunks: {stats.ContentChunkCount}");
Console.WriteLine($"Structural chunks: {stats.StructuralChunkCount}");
Console.WriteLine($"Visual chunks: {stats.VisualChunkCount}");
Console.WriteLine($"Max depth: {stats.MaxDepth}");
Console.WriteLine($"Avg tokens: {stats.AverageTokensPerChunk:F2}");
Console.WriteLine($"Duration: {stats.ProcessingDuration}");
```

## Validation

The chunker automatically validates chunks:

```csharp
var options = new ChunkingOptions
{
    ValidateChunks = true  // Default
};

var result = await chunker.ChunkAsync(stream, options);

// Check validation results
if (!result.Validation.IsValid)
{
    foreach (var issue in result.Validation.Issues)
    {
        Console.WriteLine($"{issue.Severity}: {issue.Message}");
    }
}

// Check warnings
foreach (var warning in result.Warnings)
{
    Console.WriteLine($"{warning.Level}: {warning.Message}");
}
```

## Advanced Features

### Building Trees

```csharp
// Build hierarchical tree with Children populated
var tree = result.Chunks.BuildTree();

foreach (var root in tree)
{
    TraverseTree(root);
}

void TraverseTree(ChunkerBase chunk)
{
    Console.WriteLine($"{new string(' ', chunk.Depth * 2)}{chunk.SpecificType}");
    
    if (chunk is StructuralChunk structural)
    {
        foreach (var child in structural.Children)
        {
            TraverseTree(child);
        }
    }
}
```

### Tree Traversal

```csharp
// Depth-first traversal
var root = result.Chunks.RootChunks().First();
root.TraverseDepthFirst(chunk => {
    Console.WriteLine($"Visiting: {chunk.SpecificType}");
});

// Breadth-first traversal
root.TraverseBreadthFirst(chunk => {
    Console.WriteLine($"Visiting: {chunk.SpecificType}");
});
```

### Filtering Trees

```csharp
// Filter tree to only include branches with code blocks
var codeOnlyTree = result.Chunks.FilterTreeByPredicate(
    chunk => chunk is MarkdownCodeBlockChunk
);

// Prune tree at specific depth
var shallowTree = result.Chunks.PruneAtDepth(2);
```

### Grouping

```csharp
// Group by type
var byType = result.Chunks.GroupByType();
foreach (var (type, chunks) in byType)
{
    Console.WriteLine($"{type}: {chunks.Count} chunks");
}

// Group by depth
var byDepth = result.Chunks.GroupByDepth();

// Group by parent
var byParent = result.Chunks.GroupByParent();

// Group into subtrees
var subtrees = result.Chunks.GroupBySubtree();
```

### Statistics

```csharp
// Get detailed statistics
var chunkStats = result.Chunks.GetStatistics();

Console.WriteLine($"Total: {chunkStats.TotalCount}");
Console.WriteLine($"Content: {chunkStats.ContentCount}");
Console.WriteLine($"Structural: {chunkStats.StructuralCount}");
Console.WriteLine($"Average tokens: {chunkStats.AverageTokens:F2}");
Console.WriteLine($"Max depth: {chunkStats.MaxDepth}");
```

## Best Practices

### 1. Choose Appropriate Chunk Sizes

```csharp
// For embeddings (OpenAI ada-002)
var options = new ChunkingOptions
{
    MaxChunkSizeInTokens = 500,   // Well below 8191 limit
    OverlapSizeInTokens = 50      // 10% overlap
};

// For LLM context (GPT-4)
var options = new ChunkingOptions
{
    MaxChunkSizeInTokens = 2000,  // Larger chunks for more context
    OverlapSizeInTokens = 200
};
```

### 2. Preserve Hierarchy for RAG

```csharp
// Include parent context when retrieving
var matchingChunk = FindRelevantChunk(query, embeddings);
var context = matchingChunk.GetContext(result.Chunks, before: 1, after: 1);

// Include full hierarchy path
var path = matchingChunk.GetPathFromRoot(result.Chunks);
var hierarchyContext = string.Join(" > ", path.Select(c => c.GetPlainText()));
```

### 3. Use Quality Metrics for Filtering

```csharp
// Only use high-quality chunks for embeddings
var qualityChunks = result.Chunks
    .WithMinSemanticCompleteness(0.8)
    .WithCompleteSentences()
    .ExcludeSplitChunks();
```

### 4. Handle Special Content Types

```csharp
// Extract and process code blocks separately
var codeBlocks = result.Chunks
    .OfType<MarkdownCodeBlockChunk>()
    .Where(cb => cb.Language == "csharp");

// Extract tables for structured processing
var tables = result.Chunks.TableChunks();
foreach (var table in tables)
{
    var csv = table.SerializeToString(TableSerializationFormat.Csv);
    // Process structured data
}
```

### 5. Leverage Metadata

```csharp
// Tag chunks for categorization
foreach (var chunk in result.Chunks)
{
    if (chunk.Content?.Contains("API") == true)
    {
        chunk.Metadata.Tags.Add("api-documentation");
    }
}

// Filter by tags
var apiChunks = result.Chunks.WithTag("api-documentation");
```

## Common Patterns

### Pattern 1: RAG System Integration

```csharp
// Chunk document
var result = await chunker.ChunkAsync(stream, options);

// Generate embeddings (pseudo-code)
foreach (var chunk in result.Chunks.ContentChunks())
{
    var text = chunk.ToPlainText();
    var embedding = await GenerateEmbedding(text);
    
    await vectorDb.UpsertAsync(new VectorRecord
    {
        Id = chunk.Id.ToString(),
        Vector = embedding,
        Metadata = new
        {
            chunk.SpecificType,
            HierarchyPath = chunk.GetHierarchyPath(result.Chunks),
            chunk.QualityMetrics.SemanticCompleteness
        }
    });
}
```

### Pattern 2: Documentation Processing

```csharp
// Extract structured content
var sections = result.Chunks
    .OfType<MarkdownSectionChunk>()
    .Where(s => s.HeadingLevel == 1);

foreach (var section in sections)
{
    var content = section.GetChildren(result.Chunks)
        .ContentChunks()
        .Select(c => c.ToPlainText());
    
    ProcessSection(section.HeadingText, string.Join("\n\n", content));
}
```

### Pattern 3: Content Analysis

```csharp
// Analyze document structure
var stats = result.Chunks.GetStatistics();
var codeToTextRatio = 
    stats.ChunkCountsByType.GetValueOrDefault("MarkdownCodeBlock", 0) / 
    (double)stats.TotalCount;

Console.WriteLine($"Code coverage: {codeToTextRatio:P}");

// Find longest sections
var longSections = result.Chunks
    .OfType<MarkdownSectionChunk>()
    .OrderByDescending(s => s.CountDescendants(result.Chunks))
    .Take(5);
```

## Markdown-Specific Features

### Link Preservation

Links are preserved as annotations:

```csharp
var chunk = result.Chunks
    .First(c => c.Annotations?.Any() == true);

foreach (var annotation in chunk.Annotations)
{
    if (annotation.Type == AnnotationType.Link)
    {
        Console.WriteLine($"Link: {annotation.Value}");
        Console.WriteLine($"Text: {annotation.DisplayText}");
    }
}
```

### Emphasis Detection

Text formatting is preserved:

```csharp
foreach (var annotation in chunk.Annotations)
{
    switch (annotation.Type)
    {
        case AnnotationType.Bold:
            Console.WriteLine($"Bold text: {annotation.DisplayText}");
            break;
        case AnnotationType.Italic:
            Console.WriteLine($"Italic text: {annotation.DisplayText}");
            break;
        case AnnotationType.Code:
            Console.WriteLine($"Inline code: {annotation.DisplayText}");
            break;
    }
}
```

### Table Structure

Tables preserve full structure:

```csharp
var table = result.Chunks
    .OfType<MarkdownTableChunk>()
    .First();

// Get table info
var info = table.TableInfo;
Console.WriteLine($"Rows: {info.RowCount}");
Console.WriteLine($"Columns: {info.ColumnCount}");
Console.WriteLine($"Headers: {string.Join(", ", info.Headers)}");

// Serialize in different formats
var markdown = table.SerializeToString(TableSerializationFormat.Markdown);
var csv = table.SerializeToString(TableSerializationFormat.Csv);
var json = table.SerializeToString(TableSerializationFormat.Json);
```

## Troubleshooting

### Issue: Chunks Too Large

**Solution**: Reduce `MaxChunkSizeInTokens` or check if content has extremely long paragraphs.

```csharp
// More aggressive splitting
var options = new ChunkingOptions
{
    MaxChunkSizeInTokens = 300,  // Smaller chunks
    OverlapSizeInTokens = 30
};
```

### Issue: Poor Semantic Completeness

**Solution**: Increase chunk size or adjust content structure.

```csharp
// Filter out low-quality splits
var quality Chunks = result.Chunks
    .Where(c => c.QualityMetrics.SemanticCompleteness > 0.7);
```

### Issue: Missing Hierarchy

**Solution**: Ensure document has proper heading structure.

```csharp
// Check hierarchy integrity
var validation = result.Validation;
if (!validation.IsValid)
{
    var hierarchyIssues = validation.Issues
        .Where(i => i.Message.Contains("hierarchy"));
}
```

### Issue: Incorrect Character Encoding

**Solution**: Specify encoding when opening file.

```csharp
using var stream = new FileStream(
    "document.md",
    FileMode.Open,
    FileAccess.Read);
using var reader = new StreamReader(stream, Encoding.UTF8);
var content = await reader.ReadToEndAsync();
```

## Performance Considerations

### Memory Usage

For large documents, consider:

```csharp
// Process in chunks to reduce memory footprint
var options = new ChunkingOptions
{
    MaxChunkSizeInTokens = 500,
    // Validation can be expensive for large documents
    ValidateChunks = false  
};
```

### Processing Speed

The Markdown chunker is highly optimized:
- **Small documents (<10KB)**: < 10ms
- **Medium documents (10KB-1MB)**: 10-100ms
- **Large documents (1MB-10MB)**: 100ms-1s

### Async Processing

Always use async methods for I/O operations:

```csharp
// Good
var result = await chunker.ChunkAsync(stream, options, cancellationToken);

// Avoid blocking
var result = chunker.ChunkAsync(stream, options).Result; // ?
```

## Limitations

1. **Markdown Variants**: Supports CommonMark and GitHub Flavored Markdown (GFM). Some extended syntaxes may not be fully supported.

2. **Complex Tables**: Very complex tables with merged cells may not preserve exact structure.

3. **Custom Extensions**: Custom Markdown extensions (e.g., custom directives) are not parsed.

4. **Math Equations**: LaTeX/MathJax equations are treated as code blocks.

5. **HTML in Markdown**: Embedded HTML is preserved but not parsed into separate chunks.

## Next Steps

- Explore [HTML Chunking](./html-chunking.md) for web content
- Learn about [Token Counting](./token-counting.md) for accurate sizing
- See [Serialization](./serialization.md) for export options
- Check [Performance Optimization](./performance.md) for large-scale processing

## API Reference

For complete API documentation, see:
- [MarkdownDocumentChunker API](../api/MarkdownDocumentChunker.md)
- [Chunk Types API](../api/chunk-types.md)
- [Extension Methods API](../api/extensions.md)
