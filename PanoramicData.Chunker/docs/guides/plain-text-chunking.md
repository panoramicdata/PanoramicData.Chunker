# Plain Text Chunking Guide

**PanoramicData.Chunker** - Intelligent Structure Detection for Plain Text Documents

---

## Table of Contents

1. [Overview](#overview)
2. [Why Plain Text Chunking is Challenging](#why-plain-text-chunking-is-challenging)
3. [Quick Start](#quick-start)
4. [Detection Heuristics](#detection-heuristics)
5. [Heading Detection](#heading-detection)
6. [List Detection](#list-detection)
7. [Code Block Detection](#code-block-detection)
8. [Paragraph Detection](#paragraph-detection)
9. [Configuration Options](#configuration-options)
10. [Best Practices](#best-practices)
11. [Common Scenarios](#common-scenarios)
12. [Limitations and Edge Cases](#limitations-and-edge-cases)
13. [Performance Considerations](#performance-considerations)
14. [API Reference](#api-reference)

---

## Overview

Plain text chunking is the most challenging document format to process because it lacks explicit structure markup. Unlike Markdown (with `#` headers) or HTML (with `<h1>` tags), plain text relies on **formatting conventions** and **visual cues** that must be detected heuristically.

**PanoramicData.Chunker** uses sophisticated heuristic algorithms to detect structure in plain text documents, including:
- **4 heading detection methods** with confidence scoring
- **3 list types** (bullets, numbered, alphabetic) with nesting
- **2 code block types** (fenced and indented)
- **Paragraph detection** with multi-line support

### Supported Formats

```plaintext
? Plain text files (.txt)
? Log files
? Transcripts
? README files
? Configuration files (with comments)
? Code files (as plain text)
? Technical documentation
```

---

## Why Plain Text Chunking is Challenging

### The Problem

Unlike structured formats, plain text has **no explicit markup**:

```plaintext
INTRODUCTION   ? Is this a heading?

This is a paragraph.    ? Or just emphasis?
It continues here.

- Item 1       ? Clear list marker
- Item 2

    code block?         ? Indented = code?
    or just indented text?

1. Section          ? Numbered heading?
Content here.    ? Or numbered list?
```

### The Solution

**Heuristic-based detection** with confidence scoring:

1. **Try multiple detection methods** in priority order
2. **Assign confidence scores** (0.0 - 1.0)
3. **Validate with context** (what comes before/after)
4. **Prefer explicit over implicit** (underlines > ALL CAPS)

---

## Quick Start

### Basic Usage

```csharp
using PanoramicData.Chunker;
using PanoramicData.Chunker.Configuration;

// Use preset for OpenAI embeddings
var options = ChunkingPresets.ForOpenAIEmbeddings();

// Chunk a plain text file
var result = await DocumentChunker.ChunkFileAsync("document.txt", options);

// Process chunks
foreach (var chunk in result.Chunks)
{
    if (chunk is PlainTextSectionChunk section)
    {
     Console.WriteLine($"Heading: {section.HeadingText}");
        Console.WriteLine($"  Level: {section.HeadingLevel}");
        Console.WriteLine($"  Type: {section.HeadingType}");
     Console.WriteLine($"  Confidence: {section.Confidence:F2}");
    }
    else if (chunk is PlainTextParagraphChunk paragraph)
    {
     Console.WriteLine($"Paragraph: {paragraph.Content}");
    }
}

// View statistics
Console.WriteLine($"Total chunks: {result.Statistics.TotalChunks}");
Console.WriteLine($"Total tokens: {result.Statistics.TotalTokens}");
```

### From Stream

```csharp
using var stream = File.OpenRead("document.txt");
var result = await DocumentChunker.ChunkAsync(stream, DocumentType.PlainText, options);
```

### Auto-Detection

```csharp
using var stream = File.OpenRead("document.txt");
var result = await DocumentChunker.ChunkAutoDetectAsync(stream, "document.txt", options);
// Automatically detects plain text from .txt extension or content
```

---

## Detection Heuristics

### Heuristic Priority Order

The chunker tries detection methods in order of **confidence**:

```
1. Underlined Headings (95% confidence)    ? Highest
2. Numbered Sections (85% confidence)
3. Prefixed Headers (75% confidence)
4. ALL CAPS (70% confidence)              ? Lowest
```

**Why this order?**
- Underlines are **explicit** formatting
- Numbered sections follow **clear patterns**
- Prefixed headers are **intentional** (Markdown-style)
- ALL CAPS can be **ambiguous** (acronyms, emphasis)

### Confidence Scoring

Each heading detection returns a **confidence score**:

```csharp
var sections = result.Chunks.OfType<PlainTextSectionChunk>();

foreach (var section in sections)
{
    Console.WriteLine($"{section.HeadingText}");
    Console.WriteLine($"  Type: {section.HeadingType}");
 Console.WriteLine($"  Confidence: {section.Confidence:F2}");
    
    // Filter by confidence if needed
    if (section.Confidence > 0.8)
    {
        // High confidence heading
    }
}
```

**Use confidence scores to**:
- Filter low-confidence detections
- Prioritize high-confidence headings
- Make informed decisions in your application

---

## Heading Detection

### 1. Underlined Headings (Highest Confidence)

**Pattern**: Text followed by `===` (level 1) or `---` (level 2)

```plaintext
Main Heading
============       ? Level 1 (confidence: 0.95)

Sub Heading
-----------        ? Level 2 (confidence: 0.90)
```

**Detection Rules**:
- Underline must be ?3 characters
- Underline length should be ? heading length (±20%)
- Only `=` or `-` characters

**Example**:

```csharp
var text = @"
Introduction
============

Content here.
";

var result = await DocumentChunker.ChunkAsync(stream, DocumentType.PlainText, options);
var section = result.Chunks.OfType<PlainTextSectionChunk>().First();

// section.HeadingText = "Introduction"
// section.HeadingLevel = 1
// section.HeadingType = HeadingHeuristic.Underlined
// section.Confidence = 0.95
```

**Best Practice**: Use underlined headings for maximum reliability.

### 2. Numbered Sections

**Pattern**: Hierarchical numbering like `1.`, `1.1`, `1.1.1`

```plaintext
1. Introduction               ? Level 1

Content under introduction.

1.1 Background             ? Level 2

More detailed content.

1.1.1 History ? Level 3

Even more detail.

1.2 Scope        ? Level 2 (back up)

2. Methods      ? Level 1 (new top level)
```

**Detection Rules**:
- Pattern: `^\d+(\.\d+)*\.?\s+(.+)$`
- Level = number of dots + 1
- Must be followed by text

**Example**:

```csharp
var text = @"
1. Section One
Content here.

1.1 Subsection
More content.
";

var sections = result.Chunks.OfType<PlainTextSectionChunk>().ToList();
// sections[0].HeadingLevel = 1  (1.)
// sections[1].HeadingLevel = 2  (1.1)
```

**Hierarchy Building**:

```csharp
// Parent-child relationships are built automatically
var level1 = sections.First(s => s.HeadingLevel == 1);
var level2 = sections.First(s => s.HeadingLevel == 2);

// level1.ParentId = null
// level2.ParentId = level1.Id
```

### 3. Prefixed Headers

**Pattern**: Markdown-style `#`, `##`, `###` prefixes

```plaintext
# Main Heading? Level 1

Content here.

## Sub Heading        ? Level 2

More content.

### Sub-sub Heading   ? Level 3
```

**Detection Rules**:
- Pattern: `^#{1,6}\s+(.+)$`
- Level = number of `#` characters (max 6)

**Example**:

```csharp
var text = @"
# Documentation

## Getting Started

Content here.
";

var sections = result.Chunks.OfType<PlainTextSectionChunk>().ToList();
// sections[0].HeadingLevel = 1
// sections[1].HeadingLevel = 2
// Both have HeadingType = HeadingHeuristic.Prefixed
```

**Note**: Even in plain text, Markdown-style headers work!

### 4. ALL CAPS Headings

**Pattern**: Lines with all uppercase letters

```plaintext
INTRODUCTION          ? Detected as heading

This is regular text.

TECHNICAL DETAILS     ? Another heading
```

**Detection Rules**:
- All letters must be uppercase
- Length: 4-100 characters
- Must be >50% letters
- Short words like "USA", "API" are **not** detected

**False Positive Prevention**:

```plaintext
This mentions USA in text.     ? "USA" NOT detected (too short)

THIS IS A VERY LONG LINE THAT EXCEEDS ONE HUNDRED CHARACTERS...
   ? NOT detected (too long)

SECTION ONE            ? Detected ?
```

**Example**:

```csharp
var text = @"
INTRODUCTION

This is the introduction content. It mentions USA.

METHODS

The methods section.
";

var sections = result.Chunks.OfType<PlainTextSectionChunk>().ToList();
// Only "INTRODUCTION" and "METHODS" detected
// "USA" is ignored (too short)
```

**Confidence**: 0.70 (lowest of heading types)

---

## List Detection

### Bullet Lists

**Supported Markers**: `-`, `*`, `•` (Unicode bullet)

```plaintext
Shopping List:
- Apples ? Bullet list
- Bananas
- Oranges

Alternative style:
* First item   ? Asterisk bullets
* Second item
* Third item

Unicode bullets:
• Option A        ? Unicode bullet point
• Option B
```

**Example**:

```csharp
var text = @"
Items:
- First
- Second
- Third
";

var listItems = result.Chunks.OfType<PlainTextListItemChunk>().ToList();

foreach (var item in listItems)
{
    Console.WriteLine($"{item.Marker} {item.Content}");
    Console.WriteLine($"  Type: {item.ListType}");    // "bullet"
    Console.WriteLine($"  Ordered: {item.IsOrdered}");    // false
}
```

### Numbered Lists

**Supported Formats**: `1.`, `2)`, etc.

```plaintext
Steps:
1. First step     ? Numbered list
2. Second step
3. Third step

Alternative:
1) First          ? Parenthesis style
2) Another
```

**Example**:

```csharp
var text = @"
Steps:
1. Do this
2. Then this
3. Finally this
";

var listItems = result.Chunks.OfType<PlainTextListItemChunk>().ToList();
// All have ListType = "numbered", IsOrdered = true
```

### Nested Lists

**Detection**: Based on **indentation** (2 spaces = 1 level)

```plaintext
- Level 0 item
  - Level 1 item      ? 2 spaces indent
    - Level 2 item    ? 4 spaces indent
  - Back to level 1
- Back to level 0
```

**Example**:

```csharp
var text = @"
- Parent item
  - Child item
    - Grandchild item
";

var listItems = result.Chunks.OfType<PlainTextListItemChunk>().ToList();

// listItems[0].NestingLevel = 0
// listItems[1].NestingLevel = 1  (2 spaces)
// listItems[2].NestingLevel = 2  (4 spaces)
```

**Nesting Calculation**: `NestingLevel = indentation / 2`

---

## Code Block Detection

### Fenced Code Blocks

**Pattern**: Triple backticks with optional language

````plaintext
Text before code.

```csharp
public class Example
{
    public void Method() { }
}
```

Text after code.
````

**Language Detection**:

````csharp
var text = @"
```python
def hello():
    print('Hello')
```
";

var codeBlock = result.Chunks.OfType<PlainTextCodeBlockChunk>().First();
// codeBlock.Language = "python"
// codeBlock.IsFenced = true
````

**Without Language**:

````plaintext
```
some code
```
````

```csharp
// codeBlock.Language = null
// codeBlock.IsFenced = true
```

### Indented Code Blocks

**Pattern**: Consistent indentation of 4+ spaces or 1 tab

```plaintext
Regular text.

    public class Test
    {
        // code here
    }

Back to regular text.
```

**Detection Rules**:
- 4+ spaces or 1 tab
- At least 2 consecutive lines
- Maintains consistent indentation

**Example**:

```csharp
var text = @"
Normal text.

    public void Method()
    {
        Console.WriteLine(""Hello"");
    }

More text.
";

var codeBlock = result.Chunks.OfType<PlainTextCodeBlockChunk>().First();
// codeBlock.IsFenced = false
// codeBlock.IndentationLevel = 4
```

### Code Indicators

For indented blocks, the chunker detects **code indicators**:

```csharp
var codeBlock = result.Chunks.OfType<PlainTextCodeBlockChunk>().First();

// Keywords found in the code
Console.WriteLine(string.Join(", ", codeBlock.CodeIndicators));
// Output: "public, class, return"
```

**Detected Keywords**:
- `public`, `private`, `class`, `function`, `def`
- `var`, `const`, `return`, `if`, `for`, `while`

**Purpose**: Validation that indented text is actually code

---

## Paragraph Detection

### Double Newline (Primary Method)

**Pattern**: Paragraphs separated by blank lines

```plaintext
First paragraph.

Second paragraph.

Third paragraph.
```

**Multi-line Paragraphs**:

```plaintext
This is a paragraph
that spans multiple
lines without blank lines.

Next paragraph.
```

**Example**:

```csharp
var text = @"
This is the first paragraph.

This is the second paragraph.
It spans multiple lines.

Third paragraph.
";

var paragraphs = result.Chunks.OfType<PlainTextParagraphChunk>().ToList();
// 3 paragraphs detected
// Lines within paragraphs are joined with spaces
```

### Detection Priority

1. **Skip empty lines**
2. **Try heading detection** (highest priority)
3. **Try list detection**
4. **Try code block detection**
5. **Default: paragraph** (fallback)

This ensures headings and lists aren't treated as paragraphs.

---

## Configuration Options

### Basic Configuration

```csharp
var options = new ChunkingOptions
{
    MaxTokens = 512,
    OverlapTokens = 50,
    TokenCounter = OpenAITokenCounter.ForGpt4(),
  Strategy = ChunkingStrategy.Structural
};

var result = await DocumentChunker.ChunkFileAsync("document.txt", options);
```

### Using Presets

```csharp
// For OpenAI embeddings (recommended)
var options = ChunkingPresets.ForOpenAIEmbeddings();

// For fast processing (character-based tokens)
var options = ChunkingPresets.ForFastProcessing();

// For RAG systems
var options = ChunkingPresets.ForRAG();
```

### Token Counting

```csharp
using PanoramicData.Chunker.Infrastructure.TokenCounters;

// OpenAI token counting (accurate)
var options = new ChunkingOptions
{
  TokenCounter = OpenAITokenCounter.ForGpt4(),
    TokenCountingMethod = TokenCountingMethod.OpenAI_CL100K
};

// Character-based (fast approximation)
var options = new ChunkingOptions
{
    TokenCounter = new CharacterBasedTokenCounter(),
    TokenCountingMethod = TokenCountingMethod.CharacterBased
};
```

---

## Best Practices

### 1. Create "Good" Plain Text

**Use explicit formatting** for best results:

? **Good**:
```plaintext
Introduction
============

This is content under the introduction.

1. First Section
Content here.

1.1 Subsection
More content.
```

? **Difficult**:
```plaintext
introduction

content

section one
content
```

**Why?** Explicit formatting (underlines, numbering) has higher confidence.

### 2. Choose Appropriate Heading Style

| Style | Confidence | When to Use |
|-------|-----------|-------------|
| Underlined | 0.95/0.90 | Technical docs, formal documents |
| Numbered | 0.85 | Hierarchical content, reports |
| Prefixed | 0.75 | Markdown-friendly plain text |
| ALL CAPS | 0.70 | Simple documents, headers only |

**Recommendation**: Use **underlined** or **numbered** for reliability.

### 3. Consistent List Formatting

? **Consistent**:
```plaintext
- Item 1
- Item 2
- Item 3
```

? **Inconsistent**:
```plaintext
- Item 1
* Item 2
- Item 3
```

**Why?** Consistency helps detection and looks professional.

### 4. Code Block Indentation

? **Clear Code Block**:
```plaintext
Text before.

    public class Example
    {
        // 4 spaces indent
    }

Text after.
```

? **Ambiguous**:
```plaintext
Text before.
  public class Example
    {
  // inconsistent indent
    }
Text after.
```

**Why?** Consistent 4-space indentation is universally recognized.

### 5. Monitor Confidence Scores

```csharp
var sections = result.Chunks.OfType<PlainTextSectionChunk>();

// Filter by confidence
var highConfidence = sections.Where(s => s.Confidence > 0.8);
var lowConfidence = sections.Where(s => s.Confidence <= 0.8);

Console.WriteLine($"High confidence: {highConfidence.Count()}");
Console.WriteLine($"Low confidence: {lowConfidence.Count()}");

// Log low confidence for review
foreach (var section in lowConfidence)
{
  Console.WriteLine($"?? Low confidence: {section.HeadingText} ({section.Confidence:F2})");
}
```

---

## Common Scenarios

### Scenario 1: Processing Log Files

```csharp
using PanoramicData.Chunker;
using PanoramicData.Chunker.Configuration;

var options = new ChunkingOptions
{
    MaxTokens = 256,  // Logs are usually short entries
    TokenCounter = new CharacterBasedTokenCounter()  // Speed over accuracy
};

var result = await DocumentChunker.ChunkFileAsync("application.log", options);

// Group by timestamp pattern (custom logic)
var logEntries = result.Chunks.OfType<PlainTextParagraphChunk>()
    .Where(p => p.Content.Contains("[ERROR]") || p.Content.Contains("[WARN]"));

foreach (var entry in logEntries)
{
    Console.WriteLine(entry.Content);
}
```

### Scenario 2: Processing Transcripts

```csharp
var options = new ChunkingOptions
{
    MaxTokens = 512,
    OverlapTokens = 50,  // Maintain context between speakers
    TokenCounter = OpenAITokenCounter.ForGpt4()
};

var result = await DocumentChunker.ChunkFileAsync("transcript.txt", options);

// Transcripts often use patterns like:
// Speaker A: Text here
// Speaker B: Response here

var paragraphs = result.Chunks.OfType<PlainTextParagraphChunk>();
```

### Scenario 3: Technical Documentation

```csharp
var options = ChunkingPresets.ForOpenAIEmbeddings();

var result = await DocumentChunker.ChunkFileAsync("README.txt", options);

// Get all headings
var sections = result.Chunks.OfType<PlainTextSectionChunk>();

// Get all code blocks
var codeBlocks = result.Chunks.OfType<PlainTextCodeBlockChunk>();

// Get list items
var listItems = result.Chunks.OfType<PlainTextListItemChunk>();

// Build table of contents
foreach (var section in sections.OrderBy(s => s.SequenceNumber))
{
    var indent = new string(' ', section.HeadingLevel * 2);
    Console.WriteLine($"{indent}{section.HeadingText}");
}
```

### Scenario 4: Code Files as Plain Text

```csharp
var options = new ChunkingOptions
{
    MaxTokens = 1024,  // Code can be longer
    TokenCounter = OpenAITokenCounter.ForGpt4()
};

var result = await DocumentChunker.ChunkFileAsync("Program.cs", options);

// Most content will be detected as code blocks
var codeBlocks = result.Chunks.OfType<PlainTextCodeBlockChunk>();

foreach (var block in codeBlocks)
{
    Console.WriteLine($"Code block ({block.QualityMetrics.TokenCount} tokens):");
    Console.WriteLine($"  Indicators: {string.Join(", ", block.CodeIndicators)}");
}
```

### Scenario 5: Mixed Content Documents

```csharp
var options = ChunkingPresets.ForRAG();

var result = await DocumentChunker.ChunkFileAsync("mixed-document.txt", options);

// Process different chunk types appropriately
foreach (var chunk in result.Chunks)
{
    switch (chunk)
  {
        case PlainTextSectionChunk section:
     // Index heading for navigation
          await vectorDb.IndexHeadingAsync(section.HeadingText);
          break;

        case PlainTextCodeBlockChunk code:
         // Special handling for code
            await vectorDb.IndexCodeAsync(code.Content, code.Language);
break;
 
        case PlainTextListItemChunk listItem:
   // Group list items together
            await vectorDb.IndexListItemAsync(listItem.Content);
     break;
   
        case PlainTextParagraphChunk paragraph:
     // Standard paragraph indexing
      await vectorDb.IndexParagraphAsync(paragraph.Content);
            break;
    }
}
```

---

## Limitations and Edge Cases

### Known Limitations

#### 1. Ambiguous ALL CAPS

**Problem**: Can't distinguish between headings and emphasis

```plaintext
This text mentions NASA and the USA.  ? Acronyms, not headings

IMPORTANT NOTE     ? Could be heading or emphasis
```

**Solution**: Use confidence scores and context

```csharp
var sections = result.Chunks.OfType<PlainTextSectionChunk>()
    .Where(s => s.HeadingType == HeadingHeuristic.AllCaps);

// Review low-confidence ALL CAPS detections
var ambiguous = sections.Where(s => s.Confidence < 0.75);
```

#### 2. Numbered Lists vs Numbered Sections

**Problem**: Similar patterns

```plaintext
1. Introduction      ? Section heading?
Content here.

1. First item        ? Or list item?
2. Second item
```

**Detection Logic**:
- If followed by more content: **Section**
- If followed by sequential numbers: **List**

**Workaround**: Use different styles

```plaintext
1. Introduction      ? Section (with content)
This is content.

Items:
- First          ? Bullet list (unambiguous)
- Second
```

#### 3. Inconsistent Formatting

**Problem**: Mixed styles confuse detection

```plaintext
SECTION ONE          ? ALL CAPS

Section Two     ? Title Case (not detected)
-----------          ? Underline doesn't match

# Section Three? Prefixed
```

**Solution**: Stick to one heading style per document

#### 4. Short Paragraphs

**Problem**: May be mistaken for headings or list items

```plaintext
Short.               ? Could be heading, paragraph, or list

More text here.
```

**Solution**: Add context or use blank lines

```plaintext
Short paragraph here.  ? Clear paragraph

More text here.
```

### Edge Cases

#### Empty Documents

```csharp
var result = await DocumentChunker.ChunkFileAsync("empty.txt", options);
// result.Success = true
// result.Chunks = empty list
```

#### Only Whitespace

```csharp
// Document with only spaces and newlines
// result.Success = true
// result.Chunks = empty list
```

#### Very Long Lines

```plaintext
THIS IS A VERY LONG LINE EXCEEDING ONE HUNDRED CHARACTERS USED FOR SOME REASON...
```

**Detection**: Not detected as heading (> 100 chars)

#### Nested Lists with Inconsistent Indentation

```plaintext
- Level 0
   - Level 1 (3 spaces - odd)
   - Level 2 (5 spaces - still works)
```

**Detection**: Works, but nesting level may be approximate

---

## Performance Considerations

### Speed

**Plain text chunking is fast**:

| Document Size | Processing Time |
|--------------|----------------|
| Small (1 KB) | < 10 ms |
| Medium (100 KB) | < 100 ms |
| Large (1 MB) | < 1 second |
| Very Large (10 MB) | < 5 seconds |

### Memory Usage

**Line-based processing** keeps memory usage low:

```csharp
// Processes line-by-line (memory efficient)
var result = await DocumentChunker.ChunkFileAsync("large-file.txt", options);
```

### Optimization Tips

#### 1. Choose Fast Token Counting

```csharp
// Fast (character-based)
var options = ChunkingPresets.ForFastProcessing();

// Accurate (OpenAI tokenization - slightly slower)
var options = ChunkingPresets.ForOpenAIEmbeddings();
```

**Trade-off**: Character-based is ~20% faster but less accurate for token counts.

#### 2. Disable Validation (if not needed)

```csharp
var options = new ChunkingOptions
{
    ValidateChunks = false  // Skip validation for speed
};
```

#### 3. Batch Processing

```csharp
var files = Directory.GetFiles("documents", "*.txt");

var results = new ConcurrentBag<ChunkingResult>();

await Parallel.ForEachAsync(files, async (file, ct) =>
{
    var result = await DocumentChunker.ChunkFileAsync(file, options, ct);
    results.Add(result);
});
```

### Regex Performance

**All regex patterns are compiled** for performance:

```csharp
[GeneratedRegex(@"^(?<numbering>\d+(\.\d+)+)\.?\s+(?<text>.+)$")]
private static partial Regex NumberedSectionRegex();
```

**Benefit**: ~10x faster than non-compiled regex

---

## API Reference

### PlainTextDocumentChunker

```csharp
public class PlainTextDocumentChunker : IDocumentChunker
{
    public PlainTextDocumentChunker(
        ITokenCounter tokenCounter, 
        ILogger<PlainTextDocumentChunker>? logger = null);
    
    public DocumentType SupportedType { get; }  // DocumentType.PlainText
    
    public Task<bool> CanHandleAsync(
        Stream documentStream, 
        CancellationToken cancellationToken = default);
    
    public Task<ChunkingResult> ChunkAsync(
        Stream documentStream,
        ChunkingOptions options,
        CancellationToken cancellationToken = default);
}
```

### PlainTextSectionChunk

```csharp
public class PlainTextSectionChunk : StructuralChunk
{
    public HeadingHeuristic HeadingType { get; set; }  // Detection method
    public double Confidence { get; set; }    // 0.0 - 1.0
    public string HeadingText { get; set; }             // Heading text
    public int HeadingLevel { get; set; }               // 1-6
}

public enum HeadingHeuristic
{
AllCaps,      // HEADING
    Underlined,   // Heading\n======
    Numbered,  // 1., 1.1, 1.1.1
    Prefixed      // #, ##, ###
}
```

### PlainTextParagraphChunk

```csharp
public class PlainTextParagraphChunk : ContentChunk
{
    public ParagraphDetection DetectionMethod { get; set; }
}

public enum ParagraphDetection
{
    DoubleNewline,      // Most common
    Indentation,          // Indentation change
    SentenceCompletion    // Sentence-based
}
```

### PlainTextListItemChunk

```csharp
public class PlainTextListItemChunk : ContentChunk
{
    public string ListType { get; set; }     // "bullet", "numbered"
    public int NestingLevel { get; set; }      // 0-based
    public string Marker { get; set; }   // "-", "1.", etc.
    public bool IsOrdered { get; set; }     // true for numbered
}
```

### PlainTextCodeBlockChunk

```csharp
public class PlainTextCodeBlockChunk : ContentChunk
{
    public string? Language { get; set; }    // Detected language
    public int IndentationLevel { get; set; }           // Spaces
    public List<string> CodeIndicators { get; set; }    // Keywords found
    public bool IsFenced { get; set; }   // ``` style
}
```

---

## When to Use Plain Text vs Other Formats

### Use Plain Text Chunking When:

? You have **pure text files** (.txt)  
? You have **log files** or **transcripts**  
? You have **README files** without Markdown markers  
? You need to process **legacy documents**  
? You have **configuration files** with comments  

### Use Markdown Chunking Instead When:

? Document uses Markdown syntax (`# headers`, `**bold**`, `[links]()`)  
? More accurate structure detection needed  
? Document has tables, images, or complex formatting  

### Use HTML Chunking Instead When:

? Document is HTML/web content  
? You need DOM-based parsing  
? Document has semantic HTML5 elements  

---

## Troubleshooting

### Issue: Headings Not Detected

**Problem**: Expected headings not being detected

**Solutions**:

1. **Check formatting**:
```plaintext
INTRODUCTION   ? 4+ chars ?
================  ? 3+ underline chars ?
```

2. **Check line length** (ALL CAPS must be 4-100 chars)

3. **Review confidence scores**:
```csharp
var sections = result.Chunks.OfType<PlainTextSectionChunk>();
foreach (var s in sections)
{
    Console.WriteLine($"{s.HeadingText}: {s.Confidence:F2}");
}
```

### Issue: False Positive Headings

**Problem**: Regular text detected as headings

**Solutions**:

1. **Filter by confidence**:
```csharp
var highConfidence = sections.Where(s => s.Confidence > 0.8);
```

2. **Avoid ALL CAPS in body text**

3. **Use explicit underlines** for important headings

### Issue: Lists Not Detected

**Problem**: List items not being recognized

**Solutions**:

1. **Check markers** (must be `-`, `*`, `•`, `1.`, etc.)

2. **Check spacing** (marker + space + text):
```plaintext
? - Item text
? -Item text  (no space)
```

3. **Check indentation** for nesting (2 spaces per level)

### Issue: Code Not Detected as Code Block

**Problem**: Code treated as paragraphs

**Solutions**:

1. **Use fenced blocks**:
````plaintext
```
code here
```
````

2. **Ensure 4+ space indent**:
```plaintext
    code here
    more code
```

3. **At least 2 consecutive lines** for indented blocks

---

## Related Guides

- [Markdown Chunking Guide](markdown-chunking.md) - Structured Markdown processing
- [Token Counting Guide](token-counting.md) - Accurate token counting
- [RAG Best Practices](rag-best-practices.md) - Building RAG systems

---

## Need Help?

- **Issues**: [GitHub Issues](https://github.com/panoramicdata/PanoramicData.Chunker/issues)
- **Discussions**: [GitHub Discussions](https://github.com/panoramicdata/PanoramicData.Chunker/discussions)
- **Documentation**: [docs/](docs/)

---

**Made with ?? by [Panoramic Data](https://panoramicdata.com)**
