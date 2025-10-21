# Phase 2: HTML Chunking - Preparation Notes

**Status**: ?? Ready to Start  
**Date**: January 2025  
**Prerequisites**: Phase 1 Complete ?

---

## Technology Decision: HtmlAgilityPack

### Selected Library: **HtmlAgilityPack**

**Rationale**:
- ? **Mature and stable**: Well-established library with years of production use
- ? **MIT License**: Free for commercial use, no licensing concerns
- ? **Excellent performance**: Fast HTML parsing with low memory footprint
- ? **Robust HTML handling**: Handles malformed HTML gracefully (real-world scenarios)
- ? **XPath support**: Powerful querying capabilities
- ? **LINQ integration**: Works well with LINQ for querying
- ? **Active maintenance**: Regular updates and community support
- ? **.NET compatibility**: Works with .NET 9 and has excellent track record

**Alternative Considered**:
- **AngleSharp**: Modern, more feature-rich but heavier. Better suited if we need CSS selectors or browser-like behavior.
- **Decision**: HtmlAgilityPack is the right choice for document chunking - lighter, faster, and handles malformed HTML better.

### Package Information
```xml
<PackageReference Include="HtmlAgilityPack" Version="1.11.59" />
```

---

## Phase 2 Implementation Strategy

### Building on Phase 1 Patterns

Phase 1 (Markdown) established excellent patterns that we'll reuse:

1. **Chunk Type Pattern** ?
   - Create 7 HTML-specific chunk types (similar to Markdown)
   - `HtmlSectionChunk`, `HtmlParagraphChunk`, `HtmlListItemChunk`, etc.

2. **Chunker Implementation Pattern** ?
   - `HtmlDocumentChunker : IDocumentChunker`
   - Hierarchy building using `HierarchyBuilder` utility
   - Quality metrics calculation
   - Validation support

3. **Testing Pattern** ?
   - Unit tests for each HTML element type
   - Integration tests with real HTML documents
   - Test data organization in `TestData/Html/`

4. **Factory Registration** ?
   - Register with `ChunkerFactory`
   - Auto-detection via DOCTYPE and HTML tags

5. **Documentation Pattern** ?
   - XML docs for all public APIs
   - Comprehensive guide: `docs/guides/html-chunking.md`
   - Code examples and best practices

---

## HTML-Specific Considerations

### 1. Semantic HTML Elements
Unlike Markdown, HTML has semantic containers:
- `<article>` - Self-contained composition
- `<section>` - Thematic grouping
- `<main>` - Main content
- `<aside>` - Tangential content
- `<nav>` - Navigation links

**Strategy**: Treat these as `StructuralChunk` types with proper hierarchy.

### 2. DOM Hierarchy vs. Heading Hierarchy
HTML has two hierarchies:
- **DOM hierarchy**: Parent-child element relationships
- **Heading hierarchy**: H1-H6 logical structure

**Strategy**: Prioritize heading hierarchy (like Markdown) but preserve semantic element info in metadata.

### 3. Nested Elements
HTML allows more nesting than Markdown:
- `<div>` within `<div>`
- `<span>` within `<p>`
- Lists within lists within lists

**Strategy**: Use recursive processing, similar to Markdown list handling.

### 4. Text Extraction Challenges
- Inline formatting: `<strong>`, `<em>`, `<code>`
- Line breaks: `<br>` vs. block elements
- Whitespace handling: HTML collapses whitespace
- Special entities: `&nbsp;`, `&amp;`, etc.

**Strategy**: Use `InnerText` for plain text, preserve formatting as annotations.

### 5. Security Concerns
HTML can contain dangerous content:
- `<script>` tags
- `<iframe>` tags
- JavaScript event handlers
- Data URIs

**Strategy**: Strip dangerous tags by default, make configurable.

---

## Planned HTML Chunk Types

### 1. HtmlSectionChunk (StructuralChunk)
```csharp
public class HtmlSectionChunk : StructuralChunk
{
    public int HeadingLevel { get; set; }  // 1-6 for H1-H6
    public string? HeadingText { get; set; }
    public string? ElementType { get; set; }  // "article", "section", "main", etc.
    public string? HtmlContent { get; set; }
}
```

### 2. HtmlParagraphChunk (ContentChunk)
```csharp
public class HtmlParagraphChunk : ContentChunk
{
    public string? HtmlContent { get; set; }
    public bool ContainsFormatting { get; set; }
}
```

### 3. HtmlListItemChunk (ContentChunk)
```csharp
public class HtmlListItemChunk : ContentChunk
{
    public bool IsOrdered { get; set; }
    public int? ItemNumber { get; set; }
    public int NestingLevel { get; set; }
    public string? HtmlContent { get; set; }
}
```

### 4. HtmlCodeBlockChunk (ContentChunk)
```csharp
public class HtmlCodeBlockChunk : ContentChunk
{
    public string? Language { get; set; }  // From class attribute
    public bool IsInline { get; set; }     // <code> vs <pre><code>
    public string? HtmlContent { get; set; }
}
```

### 5. HtmlQuoteChunk (ContentChunk)
```csharp
public class HtmlQuoteChunk : ContentChunk
{
    public int NestingLevel { get; set; }
    public string? Citation { get; set; }  // From cite attribute
    public string? HtmlContent { get; set; }
}
```

### 6. HtmlTableChunk (TableChunk)
```csharp
public class HtmlTableChunk : TableChunk
{
    public string? HtmlContent { get; set; }
    public string? Caption { get; set; }  // From <caption> tag
    public bool HasHeaderRow { get; set; }
    public bool HasFooterRow { get; set; }
}
```

### 7. HtmlImageChunk (VisualChunk)
```csharp
public class HtmlImageChunk : VisualChunk
{
    public string? Source { get; set; }      // src attribute
    public string? AltText { get; set; }     // alt attribute
    public string? Title { get; set; }       // title attribute
    public int? Width { get; set; }          // width attribute
    public int? Height { get; set; }         // height attribute
    public bool IsResponsive { get; set; }
}
```

---

## Implementation Checklist (Phase 2.1 - 2.2)

### Phase 2.1: HTML Parser Integration
- [ ] Add HtmlAgilityPack NuGet package (v1.11.59)
- [ ] Create `HtmlParserAdapter` wrapper class
- [ ] Implement HTML loading and validation
- [ ] Test with well-formed and malformed HTML
- [ ] Document HtmlAgilityPack patterns

### Phase 2.2: HTML Chunker Core Implementation
- [ ] Create 7 HTML chunk type classes
- [ ] Implement `HtmlDocumentChunker`
- [ ] Implement semantic element detection
- [ ] Implement heading hierarchy parsing (H1-H6)
- [ ] Implement paragraph extraction
- [ ] Implement list parsing (ul/ol/li)
- [ ] Implement code block detection (pre/code)
- [ ] Implement blockquote handling
- [ ] Implement table parsing
- [ ] Implement image extraction
- [ ] Script/style tag filtering
- [ ] Quality metrics calculation
- [ ] Hierarchy building

---

## HtmlAgilityPack Quick Reference

### Basic Usage
```csharp
var htmlDoc = new HtmlDocument();
htmlDoc.LoadHtml(htmlContent);

// Get all H1 elements
var h1Elements = htmlDoc.DocumentNode.SelectNodes("//h1");

// Get paragraphs
var paragraphs = htmlDoc.DocumentNode.SelectNodes("//p");

// Navigate hierarchy
foreach (var node in htmlDoc.DocumentNode.ChildNodes)
{
    if (node.NodeType == HtmlNodeType.Element)
    {
        // Process element
    }
}
```

### XPath Queries
```csharp
// All headers
var headers = doc.DocumentNode.SelectNodes("//h1 | //h2 | //h3 | //h4 | //h5 | //h6");

// Tables
var tables = doc.DocumentNode.SelectNodes("//table");

// Semantic elements
var articles = doc.DocumentNode.SelectNodes("//article | //section | //main | //aside");

// Lists
var lists = doc.DocumentNode.SelectNodes("//ul | //ol");
```

### Text Extraction
```csharp
// Plain text (no formatting)
var plainText = node.InnerText;

// HTML content (with tags)
var htmlContent = node.InnerHtml;

// Attributes
var className = node.GetAttributeValue("class", "");
var id = node.GetAttributeValue("id", "");
```

### Handling Malformed HTML
```csharp
// HtmlAgilityPack automatically fixes:
// - Missing closing tags
// - Improperly nested elements
// - Invalid attributes
// - Encoding issues

// It creates a valid DOM tree even from broken HTML!
```

---

## Testing Strategy for Phase 2

### Unit Test Coverage
1. **Element Detection Tests**
   - Test H1-H6 detection
   - Test semantic element detection
   - Test paragraph vs. div detection
   - Test list type detection

2. **Hierarchy Tests**
   - Test heading hierarchy building
   - Test nested semantic elements
   - Test mixed heading/semantic hierarchy

3. **Content Extraction Tests**
   - Test text extraction with formatting
   - Test entity decoding
   - Test whitespace handling
   - Test nested element handling

4. **Security Tests**
   - Test script tag filtering
   - Test iframe filtering
   - Test event handler removal
   - Test data URI handling

### Integration Test Documents
1. **Simple HTML** (like Markdown simple.md)
   ```html
   <!DOCTYPE html>
   <html>
   <body>
     <h1>Title</h1>
     <p>Paragraph</p>
   </body>
   </html>
   ```

2. **Semantic HTML**
   ```html
   <article>
     <header><h1>Article Title</h1></header>
     <section><h2>Section</h2><p>Content</p></section>
   </article>
   ```

3. **Complex HTML** (Wikipedia-style)
4. **Malformed HTML** (missing tags, broken nesting)
5. **Large HTML** (performance testing)

---

## Performance Considerations

### HtmlAgilityPack Performance
- **Parsing speed**: ~1-2ms for small documents, ~100ms for large (100KB+)
- **Memory usage**: Efficient, creates DOM tree in memory
- **Thread safety**: Not thread-safe, create new instances per thread

### Optimization Strategies
1. **Reuse parsers**: Pool HtmlDocument instances if processing many documents
2. **XPath efficiency**: Use specific queries instead of "//" (descendant axis)
3. **Selective parsing**: Skip unnecessary elements early
4. **Streaming**: For very large HTML, consider streaming approaches

---

## Differences from Markdown

| Aspect | Markdown | HTML |
|--------|----------|------|
| **Structure** | Linear with headers | Nested DOM + headers |
| **Parsing** | Markdig (AST) | HtmlAgilityPack (DOM) |
| **Validation** | Lenient | Very lenient (fixes errors) |
| **Semantics** | Implicit | Explicit (semantic tags) |
| **Complexity** | Simple | More complex |
| **Security** | Safe | Must sanitize |
| **Whitespace** | Preserved | Collapsed |
| **Nesting** | Limited | Deeply nested |

---

## Phase 2 Success Criteria

- [ ] All HTML elements supported (h1-h6, p, ul, ol, table, blockquote, pre, img)
- [ ] Semantic elements recognized (article, section, main, aside)
- [ ] Hierarchy built correctly (heading-based + semantic)
- [ ] Malformed HTML handled gracefully
- [ ] Security: dangerous tags filtered
- [ ] Test coverage >80%
- [ ] All tests passing
- [ ] Documentation complete
- [ ] Performance acceptable (similar to Markdown)

---

## Next Steps

1. **Review and approval of HtmlAgilityPack decision** ? (User suggested it!)
2. **Create HTML chunk type classes** (Phase 2.2)
3. **Implement HtmlDocumentChunker** (Phase 2.2)
4. **Write comprehensive tests** (Phase 2.7)
5. **Create HTML chunking guide** (Phase 2.8)
6. **Add benchmarks** (Phase 2.9)

---

## Estimated Timeline

- **Phase 2.1** (Parser Integration): 2-4 hours
- **Phase 2.2** (Core Implementation): 8-12 hours
- **Phase 2.3-2.5** (Sanitization, Conversion): 4-6 hours
- **Phase 2.6** (Factory Registration): 1-2 hours
- **Phase 2.7** (Testing): 6-8 hours
- **Phase 2.8** (Documentation): 3-4 hours
- **Phase 2.9** (Benchmarking): 2-3 hours

**Total Estimated Time**: 26-39 hours

Based on Phase 1 experience, we can likely complete Phase 2 faster due to established patterns.

---

**Status**: ? Ready to Begin  
**Technology**: HtmlAgilityPack (Approved)  
**Pattern**: Reuse Phase 1 patterns  
**Next Action**: Start Phase 2.1 (Add HtmlAgilityPack package)

---

**Document Version**: 1.0  
**Last Updated**: January 2025  
**Prepared For**: Phase 2 Implementation

**Let's continue the momentum! ??**
