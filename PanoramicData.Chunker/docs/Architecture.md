# PanoramicData.Chunker Architecture

## Overview

PanoramicData.Chunker is designed as a modular, extensible document chunking library for .NET. The architecture emphasizes clean separation of concerns, testability, and ease of extension for new document formats.

## Architectural Principles

1. **Single Responsibility**: Each component has a clear, focused purpose
2. **Open/Closed Principle**: Open for extension (new formats), closed for modification
3. **Dependency Inversion**: Depend on abstractions (interfaces), not concrete implementations
4. **Interface Segregation**: Small, focused interfaces rather than large, monolithic ones
5. **Composition over Inheritance**: Favor composition for flexibility

## High-Level Architecture

```
┌─────────────────────────────────────────────────────────┐
│                    Client Application                    │
└──────────────────────┬──────────────────────────────────┘
                       │
                       │ Uses
                       ▼
┌─────────────────────────────────────────────────────────┐
│                  DocumentChunker (API)                   │
│  Static entry point + ChunkerBuilder (fluent API)       │
└──────────────────────┬──────────────────────────────────┘
                       │
                       │ Delegates to
                       ▼
┌─────────────────────────────────────────────────────────┐
│                   ChunkerFactory                         │
│  Creates appropriate IDocumentChunker implementations    │
└──────────────────────┬──────────────────────────────────┘
                       │
                       │ Creates
                       ▼
        ┌──────────────┴──────────────┐
        │                             │
        ▼                             ▼
┌──────────────────┐      ┌──────────────────────┐
│MarkdownChunker  │      │  HtmlChunker         │  ... (More format-specific chunkers)
│(IDocumentChunker)│      │  (IDocumentChunker)  │
└──────────────────┘      └──────────────────────┘
        │                             │
        └──────────────┬──────────────┘
                       │ Uses
                       ▼
┌─────────────────────────────────────────────────────────┐
│                   Core Services                          │
│  • ITokenCounter                                         │
│  • IChunkValidator                                       │
│  • IChunkSerializer                                      │
│  • IImageDescriptionProvider (optional)                 │
│  • ILlmProvider (optional)                              │
│  • ICacheProvider (optional)                            │
└──────────────────────┬──────────────────────────────────┘
                       │
                       │ Produces
                       ▼
┌─────────────────────────────────────────────────────────┐
│                  Core Data Models                        │
│  • ChunkerBase (abstract)                               │
│  • StructuralChunk                                      │
│  • ContentChunk                                         │
│  • VisualChunk                                          │
│  • TableChunk                                           │
│  • ChunkingResult                                       │
└─────────────────────────────────────────────────────────┘
```

## Layer Architecture

### 1. API Layer
**Purpose**: Provide a simple, intuitive API for consumers

**Components**:
- `DocumentChunker`: Static API for common scenarios
- `ChunkerBuilder`: Fluent API for advanced configuration

**Responsibilities**:
- Validate input parameters
- Provide sensible defaults
- Route requests to the appropriate chunker via factory

### 2. Factory Layer
**Purpose**: Abstract chunker instantiation and selection

**Components**:
- `ChunkerFactory`: Creates appropriate chunker based on document type
- `TokenCounterFactory`: Creates appropriate token counter
- `SerializerFactory`: Creates appropriate serializer

**Responsibilities**:
- Document type auto-detection
- Chunker instantiation
- Dependency injection

### 3. Chunker Layer
**Purpose**: Format-specific document parsing and chunking

**Components**:
- `MarkdownDocumentChunker`
- `HtmlDocumentChunker`
- `DocxDocumentChunker`
- `PdfDocumentChunker`
- ... (one per document format)

**Responsibilities**:
- Parse format-specific structures
- Extract content and metadata
- Build chunk hierarchy
- Apply chunking strategy
- Populate quality metrics

### 4. Service Layer
**Purpose**: Provide reusable services across all chunkers

**Components**:
- Token counters (character-based, OpenAI, Claude, etc.)
- Chunk validators
- Chunk serializers
- Image description providers
- LLM providers
- Cache providers

**Responsibilities**:
- Token counting
- Validation logic
- Serialization/deserialization
- External service integration

### 5. Model Layer
**Purpose**: Define core data structures

**Components**:
- Chunk types (`ChunkerBase`, `StructuralChunk`, etc.)
- Result types (`ChunkingResult`, `ValidationResult`)
- Configuration types (`ChunkingOptions`, `ChunkingPresets`)
- Metadata types (`ChunkMetadata`, `TableMetadata`)

**Responsibilities**:
- Data structure definitions
- Immutability where appropriate
- Validation attributes

### 6. Utilities Layer
**Purpose**: Provide cross-cutting concerns and helpers

**Components**:
- `HierarchyBuilder`: Build parent-child relationships
- Text normalization utilities
- Stream helpers
- Extension methods

## Design Patterns

### Factory Pattern
Used for creating format-specific chunkers:
```csharp
IDocumentChunker chunker = factory.CreateChunker(DocumentType.Markdown);
```

### Strategy Pattern
Different chunking strategies (structural, semantic, sliding window):
```csharp
options.ChunkingStrategy = ChunkingStrategy.Structural;
```

### Builder Pattern
Fluent API for configuration:
```csharp
var result = await DocumentChunker.CreateBuilder()
    .WithOptions(options)
    .WithTokenCounter(tokenCounter)
    .WithImageDescriptionProvider(imageProvider)
    .ChunkAsync(stream, DocumentType.Markdown);
```

### Template Method Pattern
`ChunkerBase` provides abstract methods for format-specific implementations

### Adapter Pattern
External libraries are wrapped in adapters implementing our interfaces

### Decorator Pattern
Optional services (image description, LLM) wrap core chunkers

## Data Flow

### Typical Chunking Flow

```
1. Client calls DocumentChunker.ChunkAsync()
   │
   ├─> 2. Factory creates appropriate IDocumentChunker
   │      based on DocumentType or auto-detection
   │
   └─> 3. Chunker parses document structure
          │
          ├─> 4. Extract content elements
          │      (headers, paragraphs, tables, etc.)
          │
          ├─> 5. Build chunk hierarchy
          │      (parent-child relationships)
          │
          ├─> 6. Apply chunking strategy
          │      (split, merge, semantic grouping)
          │
          ├─> 7. Calculate quality metrics
          │      (token counts, completeness)
          │
          ├─> 8. Optional: Generate image descriptions
          │
          ├─> 9. Optional: Generate summaries/keywords
          │
          └─> 10. Validate chunks
                 │
                 └─> 11. Return ChunkingResult
```

## Extension Points

### Adding a New Document Format

1. **Create Chunker Class**:
   ```csharp
   public class MyFormatChunker : IDocumentChunker
   {
       public async Task<ChunkingResult> ChunkAsync(
           Stream documentStream,
           ChunkingOptions options,
           CancellationToken cancellationToken)
       {
           // Implementation
       }
   }
   ```

2. **Register with Factory**:
   ```csharp
   factory.RegisterChunker(DocumentType.MyFormat, () => new MyFormatChunker());
   ```

3. **Add Auto-Detection Logic** (optional):
   ```csharp
   factory.RegisterDetector(DocumentType.MyFormat, stream => 
   {
       // Return true if stream contains MyFormat
   });
   ```

### Adding a New Token Counter

1. **Implement Interface**:
   ```csharp
   public class MyTokenCounter : ITokenCounter
   {
       public int CountTokens(string text) { /* ... */ }
   }
   ```

2. **Use in Configuration**:
   ```csharp
   var options = new ChunkingOptions
   {
       TokenCounter = new MyTokenCounter()
   };
   ```

### Adding a Custom Validator

1. **Implement Interface**:
   ```csharp
   public class MyValidator : IChunkValidator
   {
       public ValidationResult Validate(IEnumerable<ChunkerBase> chunks)
       {
           // Custom validation logic
       }
   }
   ```

## Threading and Concurrency

- **Immutable Models**: Chunk models are immutable after creation
- **Thread-Safe Factories**: Factories can be used concurrently
- **Stateless Chunkers**: Chunker instances can be reused
- **Async/Await**: All I/O operations are asynchronous
- **Cancellation Support**: All long-running operations support `CancellationToken`

## Memory Management

- **Stream-Based Processing**: Documents processed from streams, not loaded entirely into memory
- **Lazy Evaluation**: Where possible, use lazy evaluation (e.g., `IAsyncEnumerable`)
- **Disposable Resources**: Properly dispose of streams and external resources
- **Object Pooling**: Consider pooling for frequently allocated objects (future optimization)

## Error Handling Strategy

### Error Categories

1. **User Errors**: Invalid input, misconfiguration
   - Throw `ArgumentException`, `ArgumentNullException`
   - Validate early, fail fast

2. **Document Errors**: Malformed documents, unsupported features
   - Add warnings to `ChunkingResult.Warnings`
   - Continue processing where possible

3. **External Service Errors**: API failures, network issues
   - Implement retry logic with exponential backoff
   - Degrade gracefully (fallback to defaults)

4. **System Errors**: Out of memory, disk full
   - Allow exceptions to bubble up
   - Ensure resources are cleaned up

### Exception Hierarchy (Future)

```
ChunkerException (base)
├── DocumentFormatException
├── ChunkingException
├── ValidationException
└── ExternalServiceException
```

## Performance Considerations

### Optimization Strategies

1. **Lazy Parsing**: Don't parse more than necessary
2. **Caching**: Cache expensive operations (embeddings, token counts)
3. **Parallel Processing**: Process independent chunks in parallel
4. **Memory Efficiency**: Use `Span<T>` and `Memory<T>` for string operations
5. **Streaming**: Process documents in a streaming fashion for large files

### Performance Targets

- **Small Documents** (<100KB): <100ms
- **Medium Documents** (100KB-10MB): <1s
- **Large Documents** (>10MB): Streaming, no time limit
- **Memory Usage**: <2x document size for binary formats

## Testing Strategy

### Test Pyramid

```
           ┌─────────────┐
           │  End-to-End │
           │    Tests    │  (Few, slow, comprehensive)
           └─────────────┘
         ┌─────────────────┐
         │   Integration   │
         │      Tests      │  (Some, medium speed)
         └─────────────────┘
    ┌──────────────────────────┐
    │       Unit Tests         │  (Many, fast, focused)
    └──────────────────────────┘
```

### Test Coverage Goals

- **Unit Tests**: >80% code coverage
- **Integration Tests**: All chunker implementations
- **Performance Tests**: All chunker implementations
- **Edge Cases**: Malformed input, empty documents, etc.

## Security Considerations

1. **Input Validation**: Validate all user input
2. **Resource Limits**: Impose limits on document size, processing time
3. **Sandboxing**: Consider sandboxing for untrusted documents
4. **Dependency Scanning**: Regularly scan dependencies for vulnerabilities
5. **Secrets Management**: Never log or expose API keys
6. **HTML Sanitization**: Sanitize HTML content to prevent XSS

## Future Architecture Enhancements

### Planned

- **Plugin System**: Dynamic loading of format-specific chunkers
- **Distributed Processing**: Support for distributed chunking (Azure Functions, AWS Lambda)
- **Event Sourcing**: Emit events for chunking progress
- **Metrics and Telemetry**: Built-in OpenTelemetry support
- **Configuration Validation**: Enhanced validation at startup

### Under Consideration

- **gRPC Service**: Expose chunking as a gRPC service
- **Docker Image**: Official Docker image for containerized deployments
- **Cloud-Native**: Optimizations for cloud environments (Azure, AWS, GCP)

---

**Last Updated**: 2024 (Phase 0)  
**Version**: 0.1.0-alpha
