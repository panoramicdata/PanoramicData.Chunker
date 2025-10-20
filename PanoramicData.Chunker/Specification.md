# Technical Specification: PanoramicData.Chunker

## 1. Introduction

This specification details the requirements for a .NET library, **PanoramicData.Chunker**, designed to perform hierarchical (Parent/Child) chunking on various document formats: Microsoft Office Open XML (DOCX, PPTX, XLSX), HTML, PDF, and other common formats. The primary goal is to produce semantically coherent and structured text chunks optimized for modern applications, particularly Retrieval-Augmented Generation (RAG) systems.

The library will utilize the official Microsoft DocumentFormat.OpenXml SDK for OpenXML formats, and appropriate third-party or integrated parsers for HTML, PDF, and other formats.

### 1.1. Design Principles

- **Async-First**: All I/O operations are asynchronous with proper cancellation support
- **Extensible**: Plugin architecture for custom document formats and processors
- **RAG-Optimized**: First-class support for embedding models, token counting, and vector databases
- **Type-Safe**: Strongly-typed models with rich metadata
- **Observable**: Comprehensive logging, telemetry, and quality metrics
- **Developer-Friendly**: Intuitive API with fluent builders and sensible defaults

## 2. Goals and Motivation

The Parent/Child chunking strategy aims to solve the problem of context loss in traditional fixed-size chunking:

- **Precision (Child)**: Generate small, highly specific chunks for precise vector embedding and retrieval matching.
- **Context (Parent)**: Link each small chunk back to a larger, semantically complete parent chunk to provide rich context to the Large Language Model (LLM) during generation.
- **Structural Awareness**: Leverage the inherent structural metadata (e.g., headings, slides, tables, DOM structure, PDF layout) and external location context (folder, website) to define chunk boundaries naturally, avoiding arbitrary text breaks.
- **Quality Assurance**: Provide metrics and validation to ensure chunks are semantically complete and optimally sized.
- **Format Agnostic**: Unified API across all document formats with format-specific optimizations.

## 3. Core Concepts and Output Data Structure

The library will return strongly-typed chunks that inherit from abstract base classes, ensuring semantic clarity.

### 3.1. C# Output Data Model Hierarchy

All returned units are concrete implementations of three abstract or specialized types:

| Type | Class | Purpose |
|------|-------|---------|
| Structural Chunk | `StructuralChunk` | Represents a grouping container (e.g., Document Section, Slide, Page Block, HTML `<article>`). These chunks provide high-level context. |
| Content Chunk | `ContentChunk` | Represents the actual retrievable text content (e.g., Paragraph, Table Row, Heading text). These are the small, vector-embeddable units. |
| Visual Chunk | `VisualChunk` | Represents non-textual objects (e.g., Images, Charts, Graphs). These chunks contain descriptive metadata and a reference to the binary asset. |

### 3.2. C# Class Definitions for Core Models

```csharp
// Base class for shared properties
public abstract class ChunkerBase
{
    /// <summary>
    /// Unique identifier for this specific chunk.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Unique identifier for the immediate parent of this chunk.
    /// </summary>
    public Guid? ParentId { get; set; }

    /// <summary>
    /// The document-specific role (e.g., "Heading1", "TableRow", "Image", "Section").
    /// </summary>
    public string SpecificType { get; set; }

    /// <summary>
    /// Strongly-typed metadata about this chunk.
    /// </summary>
    public ChunkMetadata Metadata { get; set; }

    /// <summary>
    /// The hierarchical depth of this chunk (0 = root level).
    /// </summary>
    public int Depth { get; set; }

    /// <summary>
    /// Sequential order of this chunk in the document (0-based).
    /// </summary>
    public int SequenceNumber { get; set; }

    /// <summary>
    /// Array of ancestor chunk IDs from root to immediate parent.
    /// </summary>
    public Guid[] AncestorIds { get; set; } = Array.Empty<Guid>();

    /// <summary>
    /// Quality and size metrics for this chunk.
    /// </summary>
    public ChunkQualityMetrics? QualityMetrics { get; set; }
}

// Represents large, non-embeddable structural groupings (Sections, Pages, Slides)
public abstract class StructuralChunk : ChunkerBase
{
    /// <summary>
    /// Direct child chunks nested under this structural unit.
    /// Note: This is populated only when using hierarchical output mode.
    /// </summary>
    public List<ChunkerBase> Children { get; set; } = new List<ChunkerBase>();

    /// <summary>
    /// Optional summary of the content within this structural chunk.
    /// Generated when ChunkingOptions.GenerateSummaries is enabled.
    /// </summary>
    public string? Summary { get; set; }
}

// Represents small, vector-embeddable units of text (Paragraphs, List Items, Cells)
public abstract class ContentChunk : ChunkerBase
{
    /// <summary>
    /// The raw text content of the chunk (plain text).
    /// </summary>
    public string Content { get; set; }

    /// <summary>
    /// HTML representation of the content, preserving formatting.
    /// Populated when ChunkingOptions.PreserveFormatting is enabled.
    /// </summary>
    public string? HtmlContent { get; set; }

    /// <summary>
    /// Markdown representation of the content.
    /// Populated when ChunkingOptions.GenerateMarkdown is enabled.
    /// </summary>
    public string? MarkdownContent { get; set; }

    /// <summary>
    /// Rich text annotations (bold, italic, links, code, etc.).
    /// </summary>
    public List<ContentAnnotation>? Annotations { get; set; }

    /// <summary>
    /// Keywords extracted from this chunk.
    /// Generated when ChunkingOptions.ExtractKeywords is enabled.
    /// </summary>
    public List<string>? Keywords { get; set; }
}

// Represents visual elements like images, charts, and graphs
public class VisualChunk : ChunkerBase
{
    /// <summary>
    /// The caption or alt-text associated with the visual element (if present in the document).
    /// </summary>
    public string? Caption { get; set; }

    /// <summary>
    /// A description of the visual content, potentially generated by an LLM or OCR/VLM.
    /// This is the primary text for vector embedding.
    /// </summary>
    public string? GeneratedDescription { get; set; }

    /// <summary>
    /// The MIME type of the extracted binary data (e.g., "image/png", "image/jpeg").
    /// </summary>
    public string MimeType { get; set; }

    /// <summary>
    /// Unique hash or URL/identifier for the binary image data.
    /// Used to look up the asset in external storage (e.g., Azure Blob Storage).
    /// </summary>
    public string BinaryReference { get; set; }

    /// <summary>
    /// Width of the image in pixels (if available).
    /// </summary>
    public int? Width { get; set; }

    /// <summary>
    /// Height of the image in pixels (if available).
    /// </summary>
    public int? Height { get; set; }

    /// <summary>
    /// Size of the binary data in bytes.
    /// </summary>
    public long? SizeInBytes { get; set; }
}

// Specialized chunk type for tables with enhanced metadata
public class TableChunk : ContentChunk
{
    /// <summary>
    /// Detailed metadata about the table structure.
    /// </summary>
    public TableMetadata TableInfo { get; set; }

    /// <summary>
    /// Serialized representation of the table in the preferred format.
    /// </summary>
    public string SerializedTable { get; set; }

    /// <summary>
    /// Format of the serialized table (Markdown, CSV, JSON).
    /// </summary>
    public TableSerializationFormat SerializationFormat { get; set; }
}

// Table metadata
public class TableMetadata
{
    public int RowCount { get; set; }
    public int ColumnCount { get; set; }
    public string[] Headers { get; set; }
    public bool HasMergedCells { get; set; }
    public bool HasHeaderRow { get; set; }
    public TableSerializationFormat PreferredFormat { get; set; }
}

public enum TableSerializationFormat
{
    Markdown,
    Csv,
    Json,
    Html
}

// Content annotation for rich text
public class ContentAnnotation
{
    /// <summary>
    /// Starting character index in the Content string.
    /// </summary>
    public int StartIndex { get; set; }

    /// <summary>
    /// Length of the annotated text.
    /// </summary>
    public int Length { get; set; }

    /// <summary>
    /// Type of annotation.
    /// </summary>
    public AnnotationType Type { get; set; }

    /// <summary>
    /// Additional attributes (e.g., href for links, language for code).
    /// </summary>
    public Dictionary<string, string>? Attributes { get; set; }
}

public enum AnnotationType
{
    Bold,
    Italic,
    Underline,
    Strikethrough,
    Link,
    Code,
    Highlight,
    Subscript,
    Superscript
}

// Quality metrics for chunks
public class ChunkQualityMetrics
{
    /// <summary>
    /// Number of tokens (using the configured token counter).
    /// </summary>
    public int TokenCount { get; set; }

    /// <summary>
    /// Number of characters.
    /// </summary>
    public int CharacterCount { get; set; }

    /// <summary>
    /// Number of words.
    /// </summary>
    public int WordCount { get; set; }

    /// <summary>
    /// Semantic completeness score (0-1).
    /// 1.0 = chunk contains complete sentences/thoughts.
    /// </summary>
    public double SemanticCompleteness { get; set; }

    /// <summary>
    /// Indicates if this chunk contains an incomplete table (truncated due to size limits).
    /// </summary>
    public bool HasIncompleteTable { get; set; }

    /// <summary>
    /// Indicates if the chunk ends with a truncated sentence.
    /// </summary>
    public bool HasTruncatedSentence { get; set; }

    /// <summary>
    /// Indicates if this chunk was split from a larger parent due to size constraints.
    /// </summary>
    public bool WasSplit { get; set; }
}

// Strongly-typed Metadata class
public class ChunkMetadata
{
    /// <summary>
    /// Document type (DOCX, PPTX, XLSX, HTML, PDF, etc.).
    /// </summary>
    public string DocumentType { get; set; }

    /// <summary>
    /// Original path/name of the file.
    /// </summary>
    public string? FilePath { get; set; }

    /// <summary>
    /// Unique document identifier (e.g., SHA256 hash or internal ID).
    /// </summary>
    public string SourceId { get; set; }

    /// <summary>
    /// Internal structural path (e.g., "Section 1.2 > Introduction").
    /// </summary>
    public string Hierarchy { get; set; }

    /// <summary>
    /// External context path (e.g., "Folder/Subfolder/ProjectX" or "https://example.com/docs/api").
    /// </summary>
    public string? ExternalHierarchy { get; set; }

    /// <summary>
    /// Page number (if applicable).
    /// </summary>
    public int? PageNumber { get; set; }

    /// <summary>
    /// Sheet name (for XLSX documents).
    /// </summary>
    public string? SheetName { get; set; }

    /// <summary>
    /// Classification tags for the chunk.
    /// </summary>
    public List<string> Tags { get; set; } = new List<string>();

    /// <summary>
    /// Bounding box coordinates (PDF-specific): "x,y,width,height".
    /// </summary>
    public string? BoundingBox { get; set; }

    /// <summary>
    /// Indicates if this chunk's content block contains an image (PDF-specific).
    /// </summary>
    public bool HasImage { get; set; }

    /// <summary>
    /// Language code (ISO 639-1) detected or specified for this chunk.
    /// </summary>
    public string? Language { get; set; }

    /// <summary>
    /// Timestamp when the chunk was created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// Custom metadata key-value pairs.
    /// </summary>
    public Dictionary<string, object>? CustomMetadata { get; set; }
}
```

### 3.3. Chunking Result

The primary return type from chunking operations:

```csharp
/// <summary>
/// Result of a chunking operation.
/// </summary>
public class ChunkingResult
{
    /// <summary>
    /// The generated chunks.
    /// </summary>
    public IReadOnlyList<ChunkerBase> Chunks { get; set; }

    /// <summary>
    /// Statistical information about the chunking process.
    /// </summary>
    public ChunkingStatistics Statistics { get; set; }

    /// <summary>
    /// Warnings or issues encountered during chunking.
    /// </summary>
    public List<ChunkingWarning> Warnings { get; set; } = new List<ChunkingWarning>();

    /// <summary>
    /// Indicates if the chunking operation completed successfully.
    /// </summary>
    public bool Success { get; set; }
}

/// <summary>
/// Statistics about a chunking operation.
/// </summary>
public class ChunkingStatistics
{
    public int TotalChunks { get; set; }
    public int StructuralChunks { get; set; }
    public int ContentChunks { get; set; }
    public int VisualChunks { get; set; }
    public int TableChunks { get; set; }
    public int MaxDepth { get; set; }
    public TimeSpan ProcessingTime { get; set; }
    public Dictionary<string, int> ChunkTypeDistribution { get; set; } = new Dictionary<string, int>();
    public int TotalTokens { get; set; }
    public int AverageTokensPerChunk { get; set; }
    public int MaxTokensInChunk { get; set; }
    public int MinTokensInChunk { get; set; }
}

/// <summary>
/// Warning or issue encountered during chunking.
/// </summary>
public class ChunkingWarning
{
    public WarningLevel Level { get; set; }
    public string Message { get; set; }
    public Guid? ChunkId { get; set; }
    public string Code { get; set; }
    public Dictionary<string, object>? Context { get; set; }
}

public enum WarningLevel
{
    Info,
    Warning,
    Error
}
```

## 4. API Design

### 4.1. Primary API

The library exposes a static `Chunker` class for simple usage and a fluent `ChunkerBuilder` for advanced scenarios.

```csharp
/// <summary>
/// Main entry point for document chunking operations.
/// </summary>
public static class Chunker
{
    /// <summary>
    /// Chunk a document from a stream with default options.
    /// </summary>
    public static Task<ChunkingResult> ChunkAsync(
        Stream documentStream, 
        DocumentType type,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Chunk a document from a stream with custom options.
    /// </summary>
    public static Task<ChunkingResult> ChunkAsync(
        Stream documentStream, 
        DocumentType type, 
        ChunkingOptions options,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Chunk a document from a file path (auto-detects type from extension).
    /// </summary>
    public static Task<ChunkingResult> ChunkFileAsync(
        string filePath, 
        ChunkingOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Chunk a document with automatic format detection.
    /// </summary>
    public static Task<ChunkingResult> ChunkAutoDetectAsync(
        Stream documentStream, 
        string? fileNameHint = null,
        ChunkingOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Create a fluent builder for advanced chunking scenarios.
    /// </summary>
    public static ChunkerBuilder CreateBuilder();
}

/// <summary>
/// Fluent builder for configuring and executing chunking operations.
/// </summary>
public class ChunkerBuilder
{
    public ChunkerBuilder WithDocument(Stream stream, DocumentType type);
    public ChunkerBuilder WithDocument(string filePath);
    public ChunkerBuilder WithMaxTokens(int maxTokens);
    public ChunkerBuilder WithOverlap(int overlapTokens);
    public ChunkerBuilder WithTokenCounter(ITokenCounter tokenCounter);
    public ChunkerBuilder WithExternalHierarchy(string hierarchy);
    public ChunkerBuilder WithTags(params string[] tags);
    public ChunkerBuilder WithStrategy(ChunkingStrategy strategy);
    public ChunkerBuilder ExtractImages(bool extract = true);
    public ChunkerBuilder GenerateImageDescriptions(IImageDescriptionProvider provider);
    public ChunkerBuilder GenerateSummaries(ILlmProvider provider);
    public ChunkerBuilder ExtractKeywords(bool extract = true);
    public ChunkerBuilder PreserveFormatting(bool preserve = true);
    public ChunkerBuilder WithLogger(ILogger logger);
    public ChunkerBuilder WithOutputFormat(OutputFormat format);
    public ChunkerBuilder EnableStreaming(bool enable = true);
    public ChunkerBuilder WithCaching(ICacheProvider cache);
    
    public Task<ChunkingResult> ChunkAsync(CancellationToken cancellationToken = default);
}
```

### 4.2. Chunking Options

```csharp
/// <summary>
/// Configuration options for chunking operations.
/// </summary>
public class ChunkingOptions
{
    // Size and Token Control
    public int MaxTokens { get; set; } = 512;
    public int OverlapTokens { get; set; } = 50;
    public ITokenCounter? TokenCounter { get; set; }
    public TokenCountingMethod TokenCountingMethod { get; set; } = TokenCountingMethod.CharacterBased;

    // Chunking Strategy
    public ChunkingStrategy Strategy { get; set; } = ChunkingStrategy.Structural;

    // Content Processing
    public bool PreserveFormatting { get; set; } = false;
    public bool GenerateMarkdown { get; set; } = false;
    public bool ExtractKeywords { get; set; } = false;
    public bool GenerateSummaries { get; set; } = false;

    // Images
    public bool ExtractImages { get; set; } = true;
    public bool GenerateImageDescriptions { get; set; } = false;
    public IImageDescriptionProvider? ImageDescriptionProvider { get; set; }
    public int MaxImageSizeBytes { get; set; } = 10 * 1024 * 1024; // 10MB

    // Tables
    public TableSerializationFormat TableFormat { get; set; } = TableSerializationFormat.Markdown;
    public bool IncludeTableHeaders { get; set; } = true;

    // Metadata
    public string? ExternalHierarchy { get; set; }
    public List<string> Tags { get; set; } = new List<string>();
    public string? SourceId { get; set; }
    public Dictionary<string, object>? CustomMetadata { get; set; }

    // Output
    public OutputFormat OutputFormat { get; set; } = OutputFormat.Flat;
    public bool IncludeQualityMetrics { get; set; } = true;
    public bool ValidateChunks { get; set; } = true;

    // Performance
    public bool EnableStreaming { get; set; } = false;
    public int BatchSize { get; set; } = 100;
    public int MaxDegreeOfParallelism { get; set; } = 1;
    public bool EnableCaching { get; set; } = false;
    public ICacheProvider? CacheProvider { get; set; }

    // Logging
    public ILogger? Logger { get; set; }
    public bool EnableDetailedLogging { get; set; } = false;

    // LLM Provider (for summaries and keywords)
    public ILlmProvider? LlmProvider { get; set; }
}

public enum ChunkingStrategy
{
    /// <summary>
    /// Use document structure (headings, slides, etc.) to define boundaries.
    /// </summary>
    Structural,

    /// <summary>
    /// Use embeddings to find semantically coherent boundaries.
    /// </summary>
    Semantic,

    /// <summary>
    /// Combine structural and semantic approaches.
    /// </summary>
    Hybrid,

    /// <summary>
    /// Traditional fixed-size chunks with overlap.
    /// </summary>
    FixedSize,

    /// <summary>
    /// Sentence-boundary aware chunking.
    /// </summary>
    Sentence,

    /// <summary>
    /// Topic modeling based chunking.
    /// </summary>
    Topic
}

public enum OutputFormat
{
    /// <summary>
    /// Flat list of all chunks with ParentId references.
    /// </summary>
    Flat,

    /// <summary>
    /// Hierarchical tree structure with Children populated.
    /// </summary>
    Hierarchical,

    /// <summary>
    /// Only leaf chunks (ContentChunk and VisualChunk).
    /// </summary>
    LeavesOnly
}

public enum TokenCountingMethod
{
    CharacterBased,
    OpenAI_CL100K,      // GPT-4, GPT-3.5-turbo
    OpenAI_P50K,        // GPT-3
    OpenAI_R50K,        // GPT-2
    Claude,             // Anthropic
    Cohere,             // Cohere
    Custom              // User-provided ITokenCounter
}

public enum DocumentType
{
    Docx,
    Pptx,
    Xlsx,
    Html,
    Pdf,
    Markdown,
    PlainText,
    Rtf,
    Csv,
    Json,
    Xml,
    Email,
    Auto                // Auto-detect
}
```

### 4.3. Preset Configurations

```csharp
/// <summary>
/// Predefined chunking configurations for common scenarios.
/// </summary>
public static class ChunkingPresets
{
    /// <summary>
    /// Optimized for OpenAI embeddings (text-embedding-ada-002, text-embedding-3-small/large).
    /// </summary>
    public static ChunkingOptions ForOpenAIEmbeddings() => new()
    {
        MaxTokens = 512,
        OverlapTokens = 50,
        TokenCountingMethod = TokenCountingMethod.OpenAI_CL100K,
        Strategy = ChunkingStrategy.Structural,
        ExtractImages = true,
        IncludeQualityMetrics = true
    };

    /// <summary>
    /// Optimized for Cohere embeddings.
    /// </summary>
    public static ChunkingOptions ForCohereEmbeddings() => new()
    {
        MaxTokens = 256,
        OverlapTokens = 25,
        TokenCountingMethod = TokenCountingMethod.Cohere,
        Strategy = ChunkingStrategy.Structural
    };

    /// <summary>
    /// Optimized for Azure AI Search with semantic ranking.
    /// </summary>
    public static ChunkingOptions ForAzureAISearch() => new()
    {
        MaxTokens = 1024,
        OverlapTokens = 100,
        Strategy = ChunkingStrategy.Structural,
        PreserveFormatting = true,
        ExtractKeywords = true
    };

    /// <summary>
    /// Optimized for processing large documents with streaming.
    /// </summary>
    public static ChunkingOptions ForLargeDocuments() => new()
    {
        EnableStreaming = true,
        BatchSize = 50,
        MaxDegreeOfParallelism = 4,
        EnableCaching = true
    };

    /// <summary>
    /// Semantic chunking with embedding-based boundary detection.
    /// </summary>
    public static ChunkingOptions ForSemanticChunking() => new()
    {
        Strategy = ChunkingStrategy.Semantic,
        MaxTokens = 512,
        IncludeQualityMetrics = true
    };

    /// <summary>
    /// Maximum context preservation for RAG systems.
    /// </summary>
    public static ChunkingOptions ForRAG() => new()
    {
        MaxTokens = 512,
        OverlapTokens = 100,
        Strategy = ChunkingStrategy.Structural,
        ExtractImages = true,
        GenerateImageDescriptions = true,
        ExtractKeywords = true,
        PreserveFormatting = true,
        IncludeQualityMetrics = true
    };
}
```

## 5. Functional Requirements and Document Mapping Logic

### 5.1. DOCX Chunking (Word Documents)

The chunking logic for DOCX must primarily rely on paragraph styles to establish the hierarchy.

| Hierarchy Level | Definition Source (OpenXML Tag/Style) | Concrete Type | SpecificType |
|----------------|---------------------------------------|---------------|--------------|
| Parent Chunk (L1) | Heading 1 (`w:pStyle="Heading1"`) | `StructuralChunk` | Section |
| Parent Chunk (L2) | Heading 2 (`w:pStyle="Heading2"`) | `StructuralChunk` | Subsection |
| Parent Chunk (L3-L6) | Heading 3-6 (`w:pStyle="Heading3"` through `Heading6"`) | `StructuralChunk` | Subsection |
| Child Chunk | Standard Paragraph (`w:pStyle="Normal"`) | `ContentChunk` | Paragraph |
| Child Chunk | List Item (`w:numPr`) | `ContentChunk` | ListItem |
| Visual Content | Drawing/Picture (`w:drawing`, `w:pict`) | `VisualChunk` | Image |
| Visual Content | Chart (`w:chart`) | `VisualChunk` | Chart |
| Special Content | Table (`w:tbl`) | `TableChunk` | Table |
| Special Content | Code Block (`w:pStyle="Code"` or similar) | `ContentChunk` | Code |

**Additional Requirements**:
- Preserve hyperlinks as annotations
- Extract comments and track changes (optional)
- Handle footnotes and endnotes
- Detect and preserve text formatting (bold, italic, etc.)

### 5.2. PPTX Chunking (PowerPoint Presentations)

The chunking logic for PPTX is slide-centric.

| Hierarchy Level | Definition Source (OpenXML Tag/Style) | Concrete Type | SpecificType |
|----------------|---------------------------------------|---------------|--------------|
| Parent Chunk | An entire Slide (`p:sld`) | `StructuralChunk` | Slide |
| Child Chunk | Slide Title (`p:phType="title"`) | `ContentChunk` | SlideTitle |
| Child Chunk | Content Placeholder (`p:phType="body"`) | `ContentChunk` | SlideContent |
| Child Chunk | Text Box (`p:txBody` without placeholder type) | `ContentChunk` | TextBox |
| Child Chunk | Speaker Notes (`p:notes`) | `ContentChunk` | SpeakerNote |
| Visual Content | Picture (`p:pic`) | `VisualChunk` | Image |
| Visual Content | Chart (`p:graphicFrame` with chart) | `VisualChunk` | Chart |
| Visual Content | Shape (`p:sp` with no text) | `VisualChunk` | Shape |
| Special Content | Table (`p:graphicFrame` with table) | `TableChunk` | Table |

**Additional Requirements**:
- Extract slide numbers and include in metadata
- Preserve slide layout information
- Handle grouped objects
- Extract SmartArt as structured content

### 5.3. XLSX Chunking (Excel Spreadsheets)

XLSX focuses on structured data with support for visual elements.

| Hierarchy Level | Definition Source (OpenXML Tag/Style) | Concrete Type | SpecificType |
|----------------|---------------------------------------|---------------|--------------|
| Parent Chunk (L1) | Workbook | `StructuralChunk` | Workbook |
| Parent Chunk (L2) | Worksheet (`sheet.xml`) | `StructuralChunk` | Worksheet |
| Parent Chunk (L3) | Named Range | `StructuralChunk` | NamedRange |
| Parent Chunk (L3) | Table (`table.xml`) | `StructuralChunk` | Table |
| Child Chunk | Table Row (within defined table) | `TableChunk` | TableRow |
| Child Chunk | Data Row (non-table) | `ContentChunk` | DataRow |
| Child Chunk | Cell with formula | `ContentChunk` | FormulaCell |
| Visual Content | Chart | `VisualChunk` | Chart |
| Visual Content | Drawing/Image | `VisualChunk` | Drawing |
| Special Content | Pivot Table | `StructuralChunk` | PivotTable |

**Additional Requirements**:
- Prepend header row to each data row for context
- Detect and preserve cell formatting (dates, currency, etc.)
- Extract cell comments
- Handle merged cells appropriately
- Support multiple worksheets

### 5.4. HTML Chunking (Web Pages)

The chunking logic for HTML relies on the Document Object Model (DOM) structure.

| Hierarchy Level | Definition Source (DOM Element) | Concrete Type | SpecificType |
|----------------|--------------------------------|---------------|--------------|
| Parent Chunk (L1) | `<article>`, `<section>` with `<h1>` | `StructuralChunk` | Article |
| Parent Chunk (L2) | `<section>` with `<h2>` | `StructuralChunk` | Section |
| Parent Chunk (L3-L6) | `<section>` with `<h3>`-`<h6>` | `StructuralChunk` | Subsection |
| Child Chunk | `<p>` | `ContentChunk` | Paragraph |
| Child Chunk | `<li>` | `ContentChunk` | ListItem |
| Child Chunk | `<blockquote>` | `ContentChunk` | Quote |
| Child Chunk | `<pre>`, `<code>` | `ContentChunk` | Code |
| Visual Content | `<img>` | `VisualChunk` | Image |
| Visual Content | `<svg>` | `VisualChunk` | SVG |
| Visual Content | `<canvas>` | `VisualChunk` | Canvas |
| Visual Content | `<video>`, `<audio>` | `VisualChunk` | Media |
| Special Content | `<table>` | `TableChunk` | Table |

**Additional Requirements**:
- Preserve hyperlinks as annotations
- Extract `alt` text from images
- Handle nested lists properly
- Respect `<main>`, `<aside>`, `<nav>` semantic elements
- Filter out script and style tags
- Preserve inline formatting (strong, em, etc.)

### 5.5. PDF Chunking (Portable Document Format)

PDF chunking requires a layout-aware parser to infer structure and extract embedded images.

| Hierarchy Level | Definition Source (Layout Inference) | Concrete Type | SpecificType |
|----------------|-------------------------------------|---------------|--------------|
| Parent Chunk (L1) | Page | `StructuralChunk` | Page |
| Parent Chunk (L2) | Content Block (spatial grouping) | `StructuralChunk` | PageBlock |
| Child Chunk | Heading (inferred from font size/weight) | `ContentChunk` | InferredHeading |
| Child Chunk | Paragraph (text runs) | `ContentChunk` | TextRun |
| Child Chunk | List (inferred from bullets/numbers) | `ContentChunk` | List |
| Visual Content | Embedded image | `VisualChunk` | Image |
| Special Content | Table (spatial/grid analysis) | `TableChunk` | Table |

**Additional Requirements**:
- Use font size and weight to infer headings
- Detect multi-column layouts
- Extract text in reading order
- Preserve bounding box coordinates
- Handle rotated text
- Extract embedded fonts information
- Support OCR for scanned PDFs (optional)

### 5.6. Markdown Chunking

| Hierarchy Level | Definition Source | Concrete Type | SpecificType |
|----------------|------------------|---------------|--------------|
| Parent Chunk (L1-L6) | `#` through `######` headers | `StructuralChunk` | Section |
| Child Chunk | Paragraph (blank line separated) | `ContentChunk` | Paragraph |
| Child Chunk | List item | `ContentChunk` | ListItem |
| Child Chunk | Code block (fenced or indented) | `ContentChunk` | Code |
| Child Chunk | Blockquote | `ContentChunk` | Quote |
| Visual Content | Image `![alt](url)` | `VisualChunk` | Image |
| Special Content | Table | `TableChunk` | Table |

### 5.7. Plain Text Chunking

| Hierarchy Level | Definition Source | Concrete Type | SpecificType |
|----------------|------------------|---------------|--------------|
| Parent Chunk | Document | `StructuralChunk` | Document |
| Child Chunk | Paragraph (double newline separated) | `ContentChunk` | Paragraph |
| Child Chunk | List item (detected by bullets/numbers) | `ContentChunk` | ListItem |

**Additional Requirements**:
- Intelligent paragraph detection
- Detect and preserve indentation patterns
- Identify potential headers (ALL CAPS, underlined, etc.)

### 5.8. CSV Chunking

| Hierarchy Level | Definition Source | Concrete Type | SpecificType |
|----------------|------------------|---------------|--------------|
| Parent Chunk | Entire CSV | `StructuralChunk` | CsvDocument |
| Child Chunk | Data row | `TableChunk` | TableRow |

**Additional Requirements**:
- Auto-detect delimiter
- Prepend header row to each data row
- Handle quoted fields with delimiters

## 6. Implementation Details

### 6.1. Architecture

The library follows a plugin-based architecture with clear separation of concerns:

```csharp
/// <summary>
/// Core interface for document chunkers.
/// </summary>
public interface IDocumentChunker
{
    /// <summary>
    /// Supported document type.
    /// </summary>
    DocumentType SupportedType { get; }

    /// <summary>
    /// Chunk a document.
    /// </summary>
    Task<ChunkingResult> ChunkAsync(
        Stream documentStream, 
        ChunkingOptions options, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Validate if the stream contains a valid document of this type.
    /// </summary>
    Task<bool> CanHandleAsync(Stream documentStream, CancellationToken cancellationToken = default);
}

/// <summary>
/// Factory for creating appropriate chunkers.
/// </summary>
public interface IChunkerFactory
{
    IDocumentChunker GetChunker(DocumentType type);
    IDocumentChunker GetChunkerForStream(Stream stream, string? fileNameHint = null);
    void RegisterChunker(IDocumentChunker chunker);
}
```

### 6.2. Token Counting

```csharp
/// <summary>
/// Interface for token counting implementations.
/// </summary>
public interface ITokenCounter
{
    /// <summary>
    /// Count tokens in the given text.
    /// </summary>
    int CountTokens(string text);

    /// <summary>
    /// Async version for potentially expensive counting operations.
    /// </summary>
    Task<int> CountTokensAsync(string text, CancellationToken cancellationToken = default);

    /// <summary>
    /// Split text at token boundaries to fit within maxTokens.
    /// </summary>
    IEnumerable<string> SplitIntoTokenBatches(string text, int maxTokens, int overlap = 0);
}

// Built-in implementations will be provided for:
// - CharacterBasedTokenCounter
// - OpenAITokenCounter (using SharpToken or similar)
// - ClaudeTokenCounter
// - CohereTokenCounter
```

### 6.3. Image Description Generation

```csharp
/// <summary>
/// Interface for generating descriptions of visual content.
/// </summary>
public interface IImageDescriptionProvider
{
    /// <summary>
    /// Generate a description of the image.
    /// </summary>
    Task<ImageDescription> GenerateDescriptionAsync(
        byte[] imageData, 
        string mimeType, 
        string? existingCaption = null,
        CancellationToken cancellationToken = default);
}

public class ImageDescription
{
    public string Description { get; set; }
    public double Confidence { get; set; }
    public List<string>? DetectedObjects { get; set; }
    public List<string>? DetectedText { get; set; } // OCR results
}

// Suggested implementations (optional, via separate packages):
// - AzureComputerVisionProvider
// - OpenAIVisionProvider
// - LocalOcrProvider (using Tesseract)
```

### 6.4. LLM Provider Integration

```csharp
/// <summary>
/// Interface for LLM operations (summaries, keywords).
/// </summary>
public interface ILlmProvider
{
    /// <summary>
    /// Generate a summary of the content.
    /// </summary>
    Task<string> GenerateSummaryAsync(
        string content, 
        int maxTokens = 100,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Extract keywords from the content.
    /// </summary>
    Task<List<string>> ExtractKeywordsAsync(
        string content, 
        int maxKeywords = 5,
        CancellationToken cancellationToken = default);
}

// Suggested implementations (optional, via separate packages):
// - OpenAILlmProvider
// - AzureOpenAILlmProvider
// - AnthropicLlmProvider
```

### 6.5. Extraction Phase (Parsing)

**Open Package/Document**: Utilizes format-specific libraries.
- DOCX/PPTX/XLSX: `DocumentFormat.OpenXml`
- HTML: `AngleSharp` or `HtmlAgilityPack`
- PDF: `iText7`, `PdfPig`, or `UglyToad.PdfPig`
- Markdown: `Markdig`

**Text, Visual, and Metadata Retrieval**:
- Iterate through the relevant document parts or structural nodes
- For visual elements:
  - Extract binary data
  - Generate SHA256 hash for `BinaryReference`
  - Extract alt-text, captions, or surrounding context
  - Optionally generate descriptions using `IImageDescriptionProvider`
  - Store binary data externally (not in chunk objects)
- Extract dimensions, file size, and MIME type for images

**Structural Identification**: 
- Use appropriate querying methods to identify structural markers
- Assign correct `SpecificType` and `ParentId`
- Build hierarchy with `Depth` and `AncestorIds`
- Populate `ExternalHierarchy` and `Tags` from options
- Assign sequential `SequenceNumber` for document order

### 6.6. Splitting Phase (Token/Length Control)

For chunks exceeding `MaxTokens`, implement secondary splitting:

**Text Normalization**: 
- Convert to clean, normalized text
- Remove excessive whitespace
- Normalize line endings
- Preserve sentence boundaries

**Recursive Splitting**: 
- Use `ITokenCounter` to measure chunk size
- Split on natural boundaries (sentences > phrases > words > characters)
- Apply only to `ContentChunk` instances
- `VisualChunk` and `StructuralChunk` are not split

**Overlap**: 
- Support configurable `OverlapTokens`
- Ensure overlap occurs at sentence boundaries when possible
- Maintain semantic continuity

**Smart Splitting Algorithm**:
```
1. If chunk <= MaxTokens, keep as-is
2. Try splitting at paragraph boundaries
3. Try splitting at sentence boundaries
4. Try splitting at phrase boundaries (commas, semicolons)
5. As last resort, split at word boundaries
6. Apply overlap between resulting chunks
7. Update quality metrics to flag split chunks
```

### 6.7. Relationship Management

- Generate unique `Guid` for each chunk's `Id`
- Assign `ParentId` to link child chunks to parents
- Maintain in-memory `Dictionary<Guid, ChunkerBase>` during processing
- Build `AncestorIds` array by traversing parent chain
- Calculate `Depth` based on position in hierarchy
- Assign sequential `SequenceNumber` for flat list ordering

### 6.8. Quality Metrics Calculation

For each chunk, calculate:
- `TokenCount` using configured `ITokenCounter`
- `CharacterCount` and `WordCount`
- `SemanticCompleteness`:
  - 1.0 if chunk contains complete sentences
  - Lower scores for truncated sentences
  - Consider paragraph/section completeness
- `HasIncompleteTable`: Check if table was truncated
- `HasTruncatedSentence`: Check if last sentence is incomplete
- `WasSplit`: Flag if chunk resulted from splitting operation

### 6.9. Validation

```csharp
/// <summary>
/// Interface for chunk validation.
/// </summary>
public interface IChunkValidator
{
    Task<ValidationResult> ValidateAsync(
        IEnumerable<ChunkerBase> chunks,
        CancellationToken cancellationToken = default);
}

public class ValidationResult
{
    public bool IsValid { get; set; }
    public List<ValidationIssue> Issues { get; set; } = new List<ValidationIssue>();
    
    // Specific checks
    public bool HasOrphanedChunks { get; set; }
    public bool HasCircularReferences { get; set; }
    public List<ChunkerBase> OversizedChunks { get; set; } = new List<ChunkerBase>();
    public List<ChunkerBase> UndersizedChunks { get; set; } = new List<ChunkerBase>();
    public bool HasInvalidHierarchy { get; set; }
}

public class ValidationIssue
{
    public ValidationSeverity Severity { get; set; }
    public string Message { get; set; }
    public Guid? ChunkId { get; set; }
    public string Code { get; set; }
}

public enum ValidationSeverity
{
    Info,
    Warning,
    Error,
    Critical
}
```

### 6.10. Serialization and Export

```csharp
/// <summary>
/// Interface for chunk serialization.
/// </summary>
public interface IChunkSerializer
{
    Task SerializeAsync(
        IEnumerable<ChunkerBase> chunks, 
        Stream output,
        CancellationToken cancellationToken = default);
    
    Task<T> DeserializeAsync<T>(
        Stream input,
        CancellationToken cancellationToken = default) where T : IEnumerable<ChunkerBase>;
}

// Built-in serializers:
// - JsonChunkSerializer (using System.Text.Json)
// - ParquetChunkSerializer (for large-scale data processing)
// - MarkdownChunkSerializer (human-readable output)
// - CsvChunkSerializer (simple tabular format)

// Optional vector database serializers (separate packages):
// - PineconeSerializer
// - QdrantSerializer
// - WeaviateSerializer
// - AzureAISearchSerializer
```

### 6.11. Streaming Support

For large documents, implement streaming to avoid loading entire document in memory:

```csharp
/// <summary>
/// Streaming interface for processing large documents.
/// </summary>
public interface IChunkStream : IAsyncEnumerable<ChunkerBase>
{
    ChunkingStatistics Statistics { get; }
    IAsyncEnumerable<ChunkingWarning> Warnings { get; }
}

// Usage:
await foreach (var chunk in chunker.ChunkStreamAsync(largeStream, options))
{
    // Process chunk immediately
    await ProcessChunkAsync(chunk);
}
```

### 6.12. Caching

```csharp
/// <summary>
/// Interface for caching chunking results.
/// </summary>
public interface ICacheProvider
{
    Task<ChunkingResult?> GetAsync(string key, CancellationToken cancellationToken = default);
    Task SetAsync(string key, ChunkingResult result, TimeSpan? expiration = null, CancellationToken cancellationToken = default);
    Task RemoveAsync(string key, CancellationToken cancellationToken = default);
}

// Built-in implementations:
// - MemoryCacheProvider (using IMemoryCache)
// - DistributedCacheProvider (using IDistributedCache)
```

### 6.13. Logging and Observability

```csharp
/// <summary>
/// Logging integration using Microsoft.Extensions.Logging.
/// </summary>
public interface IChunkingLogger
{
    void LogChunkingStarted(DocumentType type, long fileSize);
    void LogChunkCreated(ChunkerBase chunk);
    void LogChunkingCompleted(ChunkingStatistics stats);
    void LogWarning(ChunkingWarning warning);
    void LogError(Exception ex, string context);
    void LogValidationResult(ValidationResult result);
}

// Built-in support for structured logging with log levels:
// - Trace: Individual chunk creation
// - Debug: Detailed processing steps
// - Information: Chunking start/complete
// - Warning: Non-critical issues
// - Error: Processing errors
// - Critical: Fatal errors
```

## 7. Extension Points and Extensibility

### 7.1. Custom Document Format Support

```csharp
// Implement IDocumentChunker for custom formats
public class CustomDocumentChunker : IDocumentChunker
{
    public DocumentType SupportedType => DocumentType.Custom;
    
    public async Task<ChunkingResult> ChunkAsync(
        Stream documentStream, 
        ChunkingOptions options, 
        CancellationToken cancellationToken = default)
    {
        // Custom implementation
    }
    
    public async Task<bool> CanHandleAsync(Stream documentStream, CancellationToken cancellationToken = default)
    {
        // Detection logic
    }
}

// Register with factory
chunkerFactory.RegisterChunker(new CustomDocumentChunker());
```

### 7.2. Custom Token Counters

```csharp
public class CustomTokenCounter : ITokenCounter
{
    public int CountTokens(string text)
    {
        // Custom implementation
    }
    
    public Task<int> CountTokensAsync(string text, CancellationToken cancellationToken = default)
    {
        // Async implementation
    }
    
    public IEnumerable<string> SplitIntoTokenBatches(string text, int maxTokens, int overlap = 0)
    {
        // Custom splitting logic
    }
}
```

### 7.3. Custom Chunk Types

```csharp
// Extend base classes for domain-specific chunks
public class CodeChunk : ContentChunk
{
    public string Language { get; set; }
    public string? SyntaxHighlightedHtml { get; set; }
    public List<string> ImportStatements { get; set; }
}

public class EmailChunk : StructuralChunk
{
    public string From { get; set; }
    public string To { get; set; }
    public string Subject { get; set; }
    public DateTime SentDate { get; set; }
    public List<AttachmentReference> Attachments { get; set; }
}
```

## 8. Helper Extensions and Utilities

### 8.1. LINQ-style Query Extensions

```csharp
public static class ChunkExtensions
{
    // Filtering
    public static IEnumerable<ChunkerBase> OfSpecificType(this IEnumerable<ChunkerBase> chunks, string specificType);
    public static IEnumerable<ContentChunk> ContentChunks(this IEnumerable<ChunkerBase> chunks);
    public static IEnumerable<StructuralChunk> StructuralChunks(this IEnumerable<ChunkerBase> chunks);
    public static IEnumerable<VisualChunk> VisualChunks(this IEnumerable<ChunkerBase> chunks);
    public static IEnumerable<TableChunk> TableChunks this IEnumerable<ChunkerBase> chunks);
    
    // Hierarchy navigation
    public static IEnumerable<ChunkerBase> WithinDepthRange(this IEnumerable<ChunkerBase> chunks, int minDepth, int maxDepth);
    public static IEnumerable<ChunkerBase> AtDepth(this IEnumerable<ChunkerBase> chunks, int depth);
    public static IEnumerable<ChunkerBase> GetChildren(this ChunkerBase chunk, IEnumerable<ChunkerBase> allChunks);
    public static ChunkerBase? GetParent(this ChunkerBase chunk, IEnumerable<ChunkerBase> allChunks);
    public static IEnumerable<ChunkerBase> GetAncestors(this ChunkerBase chunk, IEnumerable<ChunkerBase> allChunks);
    public static IEnumerable<ChunkerBase> GetDescendants(this ChunkerBase chunk, IEnumerable<ChunkerBase> allChunks);
    
    // Metadata filtering
    public static IEnumerable<ChunkerBase> WithTag(this IEnumerable<ChunkerBase> chunks, string tag);
    public static IEnumerable<ChunkerBase> WithAnyTag(this IEnumerable<ChunkerBase> chunks, params string[] tags);
    public static IEnumerable<ChunkerBase> OnPage(this IEnumerable<ChunkerBase> chunks, int pageNumber);
    public static IEnumerable<ChunkerBase> InHierarchy(this IEnumerable<ChunkerBase> chunks, string hierarchyPrefix);
    
    // Quality filtering
    public static IEnumerable<ChunkerBase> WithMinTokens(this IEnumerable<ChunkerBase> chunks, int minTokens);
    public static IEnumerable<ChunkerBase> WithMaxTokens(this IEnumerable<ChunkerBase> chunks, int maxTokens);
    public static IEnumerable<ChunkerBase> WithMinSemanticCompleteness(this IEnumerable<ChunkerBase> chunks, double minScore);
    public static IEnumerable<ChunkerBase> ExcludeSplitChunks(this IEnumerable<ChunkerBase> chunks);
    
    // Conversion
    public static string ToPlainText(this ChunkerBase chunk);
    public static string ToMarkdown(this ChunkerBase chunk);
    public static string ToHtml(this ChunkerBase chunk);
    
    // Tree building
    public static ChunkerBase BuildTree(this IEnumerable<ChunkerBase> chunks);
    public static IEnumerable<ChunkerBase> FlattenTree this ChunkerBase root);
}
```

### 8.2. Batch Processing Utilities

```csharp
public static class BatchChunker
{
    /// <summary>
    /// Process multiple documents in parallel.
    /// </summary>
    public static async Task<Dictionary<string, ChunkingResult>> ChunkBatchAsync(
        Dictionary<string, Stream> documents,
        DocumentType type,
        ChunkingOptions options,
        int maxParallelism = 4,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Process all files in a directory.
    /// </summary>
    public static async Task<Dictionary<string, ChunkingResult>> ChunkDirectoryAsync(
        string directoryPath,
        string searchPattern = "*.*",
        ChunkingOptions? options = null,
        bool recursive = false,
        CancellationToken cancellationToken = default);
}
```

## 9. Performance Requirements

### 9.1. Performance Targets

- **Small Documents** (<1MB): Process in <1 second
- **Medium Documents** (1-10MB): Process in <10 seconds
- **Large Documents** (10-100MB): Process in <60 seconds with streaming
- **Memory Usage**: Peak memory should not exceed 2x document size
- **Concurrent Processing**: Support at least 10 concurrent documents without degradation

### 9.2. Optimization Strategies

- Lazy evaluation where possible
- Streaming for large documents
- Parallel processing of independent chunks
- Efficient string handling (StringBuilder, Span<char>)
- Connection pooling for external services
- Result caching with configurable TTL

## 10. Testing Strategy

### 10.1. Test Coverage Requirements

- **Unit Tests**: 80%+ code coverage
- **Integration Tests**: All document formats with real-world samples
- **Performance Tests**: Benchmark suite for all document types
- **Fuzz Testing**: Handle corrupted/malformed documents gracefully

### 10.2. Test Document Suite

Maintain a comprehensive test document library:
- Simple documents (basic structure, minimal content)
- Complex documents (deep hierarchies, mixed content types)
- Edge cases (empty documents, extremely large documents)
- Malformed documents (corrupted files, invalid structures)
- Real-world samples (anonymized production documents)

### 10.3. Quality Metrics

Track and report:
- Processing time per document type
- Memory consumption patterns
- Chunk quality scores
- Edge case handling success rate
- API usability (developer experience)

## 11. Documentation Requirements

### 11.1. Code Documentation

- XML documentation comments on all public APIs
- IntelliSense-friendly descriptions
- Code examples in XML comments
- Parameter validation documentation

### 11.2. User Documentation

- **Quick Start Guide**: Get started in 5 minutes
- **Comprehensive Guide**: All features and options
- **Cookbook**: Common scenarios and solutions
- **API Reference**: Complete API documentation
- **Performance Guide**: Best practices and optimization tips
- **Migration Guide**: From other libraries (LangChain, LlamaIndex, etc.)

### 11.3. Sample Projects

- Basic console application
- ASP.NET Core web API integration
- RAG system with vector database
- Batch processing pipeline
- Custom chunker implementation
- Azure Functions deployment

## 12. Distribution and Packaging

### 12.1. NuGet Packages

**Core Package**: `PanoramicData.Chunker`
- Core interfaces and models
- Basic implementations
- DOCX, PPTX, XLSX support
- Character-based token counting

**Optional Packages**:
- `PanoramicData.Chunker.Html` - HTML chunking
- `PanoramicData.Chunker.Pdf` - PDF chunking
- `PanoramicData.Chunker.Markdown` - Markdown chunking
- `PanoramicData.Chunker.TokenCounters` - Advanced token counters (OpenAI, Claude, etc.)
- `PanoramicData.Chunker.ImageDescription` - Image description providers
- `PanoramicData.Chunker.VectorDb` - Vector database serializers
- `PanoramicData.Chunker.LlmProviders` - LLM integrations

### 12.2. Versioning

Follow Semantic Versioning (SemVer):
- Major: Breaking API changes
- Minor: New features, backward compatible
- Patch: Bug fixes, no API changes

### 12.3. Compatibility

- Target .NET 9.0 initially
- Consider multi-targeting for broader compatibility (.NET 8.0, .NET Standard 2.1)
- Document minimum version requirements for dependencies

## 13. Security Considerations

### 13.1. Input Validation

- Validate document streams before processing
- Set maximum file size limits
- Implement timeout mechanisms
- Sanitize extracted content (especially HTML)

### 13.2. External Service Integration

- Secure API key management
- Rate limiting for external APIs
- Circuit breaker pattern for resilience
- No logging of sensitive content

### 13.3. Data Privacy

- No telemetry collection without explicit opt-in
- Support for on-premises/offline operation
- Configurable data retention policies
- GDPR compliance considerations

## 14. Future Enhancements (Post-MVP)

### 14.1. Advanced Features

- **Semantic Chunking**: Embedding-based boundary detection
- **Multi-language Support**: Language detection and language-specific processing
- **OCR Integration**: Extract text from images and scanned PDFs
- **Audio Transcription**: Support for audio/video files with transcription
- **Diff Chunking**: Efficient re-chunking of modified documents
- **Version Control**: Track chunk changes across document versions

### 14.2. Additional Formats

- Email formats (.eml, .msg, .pst)
- Archive formats (extract and chunk contents)
- E-book formats (EPUB, MOBI)
- CAD/Design files (metadata extraction)
- Code repositories (syntax-aware chunking)

### 14.3. Cloud Integration

- Azure Blob Storage direct integration
- AWS S3 direct integration
- Google Cloud Storage direct integration
- Built-in vector database connectors
- Managed chunking service (Azure Functions/AWS Lambda)

### 14.4. Developer Experience

- Visual Studio Code extension for chunk visualization
- Interactive chunk explorer
- Performance profiler
- Chunk quality analyzer
- Configuration wizard

## 15. Success Metrics

### 15.1. Technical Metrics

- **Adoption**: NuGet download count
- **Performance**: Processing speed benchmarks
- **Reliability**: Error rate < 0.1%
- **Quality**: Chunk semantic completeness > 0.95

### 15.2. Developer Experience Metrics

- **Time to First Chunk**: < 5 minutes from install to first result
- **Documentation Quality**: Community-reported issues < 5% related to unclear docs
- **API Satisfaction**: Developer survey scores > 4.5/5
- **Community Engagement**: Active GitHub discussions, contributions

## 16. Implementation Roadmap

### Phase 1: Foundation (Weeks 1-4)
- Core data models
- Basic API design
- DOCX chunker implementation
- Character-based token counting
- JSON serialization
- Unit test framework

### Phase 2: Office Formats (Weeks 5-8)
- PPTX chunker
- XLSX chunker
- Image extraction (no descriptions)
- Table handling
- Quality metrics
- Integration tests

### Phase 3: Web and PDF (Weeks 9-12)
- HTML chunker
- PDF chunker (basic)
- Markdown chunker
- Fluent API builder
- Validation framework
- Performance optimization

### Phase 4: Advanced Features (Weeks 13-16)
- Advanced token counters (OpenAI, Claude)
- Image description integration
- LLM provider support
- Streaming support
- Caching layer
- Comprehensive documentation

### Phase 5: Polish and Release (Weeks 17-20)
- Performance benchmarking
- Security audit
- Documentation review
- Sample applications
- NuGet packaging
- Public beta release

### Phase 6: Community and Growth (Ongoing)
- Community feedback integration
- Additional format support
- Plugin ecosystem
- Performance improvements
- Feature requests
- Stable 1.0 release

## 17. Appendix

### 17.1. Glossary

- **Chunk**: A discrete unit of text or content extracted from a document
- **Structural Chunk**: A container chunk that groups related content
- **Content Chunk**: A leaf chunk containing actual text for embedding
- **Visual Chunk**: A chunk representing non-textual content (images, charts)
- **Token**: A unit of text used for embedding model input (varies by model)
- **RAG**: Retrieval-Augmented Generation
- **Semantic Completeness**: Measure of how complete/coherent a chunk's content is

### 17.2. References

- DocumentFormat.OpenXml: https://github.com/OfficeDev/Open-XML-SDK
- SharpToken (OpenAI tokenizer): https://github.com/dmitry-brazhenko/SharpToken
- AngleSharp (HTML parser): https://anglesharp.github.io/
- Markdig (Markdown parser): https://github.com/xoofx/markdig
- UglyToad.PdfPig (PDF parser): https://github.com/UglyToad/PdfPig

### 17.3. License

**Recommended**: MIT License for maximum adoption and flexibility

### 17.4. Contributing Guidelines

To be established:
- Code style guide
- Pull request process
- Issue templates
- Code of conduct
- Security disclosure policy