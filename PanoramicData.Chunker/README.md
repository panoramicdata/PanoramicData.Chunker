# PanoramicData.Chunker

A powerful, flexible .NET library for hierarchical document chunking, optimized for Retrieval-Augmented Generation (RAG) systems.

## ?? Overview

PanoramicData.Chunker transforms documents into semantically coherent chunks while preserving their hierarchical structure. It supports multiple document formats and provides rich metadata for each chunk, making it ideal for RAG systems, vector databases, and document analysis pipelines.

## ? Features

- **Multi-Format Support**: DOCX, PPTX, XLSX, HTML, PDF, Markdown, Plain Text, CSV, and more
- **Hierarchical Chunking**: Maintains parent-child relationships for context preservation
- **RAG-Optimized**: Built-in support for token counting, embedding models, and vector databases
- **Type-Safe**: Strongly-typed chunk models with rich metadata
- **Async-First**: Modern async/await patterns throughout
- **Extensible**: Plugin architecture for custom document formats and processors
- **Quality Metrics**: Automatic calculation of chunk quality and completeness scores

## ?? Installation

```bash
dotnet add package PanoramicData.Chunker
```

## ?? Quick Start

### Basic Usage

```csharp
using PanoramicData.Chunker;
using PanoramicData.Chunker.Configuration;

// Chunk a Markdown file
var result = await DocumentChunker.ChunkFileAsync(
    "document.md",
    ChunkingPresets.ForOpenAIEmbeddings()
);

// Access chunks
foreach (var chunk in result.Chunks)
{
    if (chunk is ContentChunk contentChunk)
    {
        Console.WriteLine($"{contentChunk.SpecificType}: {contentChunk.Content}");
    }
}
```

### Fluent API

```csharp
var result = await DocumentChunker.CreateBuilder()
    .WithDocument("document.md")
    .WithMaxTokens(512)
    .WithOverlap(50)
    .ExtractImages()
    .IncludeQualityMetrics()
    .ChunkAsync();
```

### Custom Options

```csharp
var options = new ChunkingOptions
{
    MaxTokens = 1024,
    OverlapTokens = 100,
    Strategy = ChunkingStrategy.Structural,
    ExtractImages = true,
    IncludeQualityMetrics = true,
    Tags = new List<string> { "documentation", "v1.0" },
    ExternalHierarchy = "Docs/UserGuide"
};

await using var stream = File.OpenRead("document.docx");
var result = await DocumentChunker.ChunkAsync(stream, DocumentType.Docx, options);
```

## ?? Chunk Types

### StructuralChunk
Represents grouping containers (sections, pages, slides) that provide high-level context.

### ContentChunk
Represents embeddable text content (paragraphs, list items, table rows).

### VisualChunk
Represents non-textual objects (images, charts) with metadata and binary references.

### TableChunk
Specialized chunk for tables with enhanced metadata and serialization options.

## ?? Supported Formats

| Format | Status | Features |
|--------|--------|----------|
| Markdown | ? Planned (Phase 1) | Headers, paragraphs, lists, code blocks, tables, images |
| HTML | ? Planned (Phase 2) | Semantic elements, headers, tables, images, formatting |
| DOCX | ? Planned (Phase 5) | Styles, tables, images, formatting, comments |
| PPTX | ? Planned (Phase 6) | Slides, speaker notes, tables, images, charts |
| XLSX | ? Planned (Phase 7) | Worksheets, tables, formulas, charts |
| PDF | ? Planned (Phase 9) | Text extraction, layout analysis, images, tables |
| Plain Text | ? Planned (Phase 4) | Smart paragraph detection, heading inference |
| CSV | ? Planned (Phase 8) | Auto-delimiter detection, header prepending |

## ?? Configuration Presets

```csharp
// Optimized for OpenAI embeddings
ChunkingPresets.ForOpenAIEmbeddings()

// Optimized for Cohere embeddings
ChunkingPresets.ForCohereEmbeddings()

// Optimized for Azure AI Search
ChunkingPresets.ForAzureAISearch()

// For large documents with streaming
ChunkingPresets.ForLargeDocuments()

// Maximum context preservation for RAG
ChunkingPresets.ForRAG()
```

## ?? Roadmap

- **Phase 0** (Current): Foundation and core infrastructure ?
- **Phase 1**: Markdown chunking (MVP)
- **Phase 2**: HTML chunking
- **Phase 3**: Advanced token counting (OpenAI, Claude, Cohere)
- **Phase 4**: Plain text chunking
- **Phase 5-7**: Office formats (DOCX, PPTX, XLSX)
- **Phase 8**: CSV chunking
- **Phase 9**: PDF chunking
- **Phase 10-12**: AI features (image descriptions, LLM integration, semantic chunking)
- **Phase 13**: Performance optimization and streaming
- **Phase 14**: Serialization and vector database integration
- **Phase 15**: Validation and quality assurance

See [MasterPlan.md](MasterPlan.md) for complete roadmap.

## ?? Documentation

- [Technical Specification](Specification.md) - Complete technical specification
- [Master Plan](MasterPlan.md) - Implementation roadmap
- API Reference (Coming soon)
- Cookbook (Coming soon)

## ?? Contributing

Contributions are welcome! Please see [CONTRIBUTING.md](CONTRIBUTING.md) for guidelines.

## ?? License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## ?? Acknowledgments

Built with:
- [DocumentFormat.OpenXml](https://github.com/OfficeDev/Open-XML-SDK) for Office formats
- [Markdig](https://github.com/xoofx/markdig) for Markdown parsing (planned)
- [AngleSharp](https://anglesharp.github.io/) for HTML parsing (planned)

## ?? Support

- ?? Issues: [GitHub Issues](https://github.com/panoramicdata/PanoramicData.Chunker/issues)
- ?? Discussions: [GitHub Discussions](https://github.com/panoramicdata/PanoramicData.Chunker/discussions)

---

**Status**: ?? Active Development (Phase 0 Complete)

Made with ?? for the .NET and RAG communities
