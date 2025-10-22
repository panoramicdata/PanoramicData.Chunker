# Phase 4: Plain Text Chunking - Preparation

**Phase**: 4 of 20  
**Status**: ?? **IN PROGRESS**  
**Date Started**: January 2025  
**Estimated Completion**: 2-3 sessions  

---

## Overview

Phase 4 implements intelligent plain text chunking with structure detection. This phase introduces heuristic-based document structure detection, making it possible to chunk unstructured plain text documents effectively.

### Objectives

1. ? **Implement PlainTextDocumentChunker** - Core chunking implementation
2. ? **Heading Detection** - ALL CAPS, underlines, numbering heuristics
3. ? **List Detection** - Bullets, numbered lists
4. ? **Code Block Detection** - Consistent indentation detection
5. ? **Paragraph Detection** - Double newline, spacing
6. ? **Token Counter Integration** - Use OpenAI token counting
7. ? **Comprehensive Testing** - Unit and integration tests
8. ? **Documentation** - Plain text chunking guide

---

## Why Plain Text at Phase 4?

- **Natural Progression**: Markdown and HTML are structured, plain text tests our heuristics
- **Real-World Need**: Many documents are plain text (logs, config files, code, transcripts)
- **Complexity**: More challenging than Markdown but simpler than binary formats
- **Foundation**: Establishes patterns for unstructured content detection

---

## Implementation Tasks

### Task 4.1: Plain Text Chunker Implementation

**Priority**: High  
**Estimated Effort**: 4-6 hours

#### Subtasks:
- [ ] Create `PlainTextDocumentChunker` class implementing `IDocumentChunker`
- [ ] Implement `CanHandleAsync()` for plain text detection
- [ ] Implement main `ChunkAsync()` method
- [ ] Create plain text-specific chunk types:
  - [ ] `PlainTextSectionChunk` (for detected headings)
  - [ ] `PlainTextParagraphChunk`
  - [ ] `PlainTextListItemChunk`
  - [ ] `PlainTextCodeBlockChunk`

#### Key Decisions:
- **Token Counter**: Use `ITokenCounter` from constructor (like Markdown and HTML)
- **Structure Detection**: Progressive detection (headers first, then paragraphs, etc.)
- **Hierarchy Building**: Use similar approach to Markdown (heading levels create parent-child)

---

### Task 4.2: Heading Detection Heuristics

**Priority**: High  
**Estimated Effort**: 3-4 hours

#### Heading Detection Strategies:

1. **ALL CAPS Heuristics**:
   ```plaintext
   INTRODUCTION          ? Heading (level 1)
   This is content...
   
   TECHNICAL DETAILS     ? Heading (level 1)
   More content...
   ```
   - Line is all uppercase
   - Line length > 3 characters (avoid "A", "I" false positives)
   - Followed by content (not another ALL CAPS line)
   - Optional: Max length check (e.g., < 100 chars for heading)

2. **Underline Detection**:
   ```plaintext
   Main Heading
   ============          ? Double underline = level 1
   
   Sub Heading
   -----------          ? Single underline = level 2
   ```
   - Line followed by line of `=` or `-` characters
   - Underline length ? heading length (±20%)
   - At least 3 underline characters

3. **Numbered Section Detection**:
   ```plaintext
   1. Introduction     ? Level 1
   Content...
   
   1.1 Background        ? Level 2
   Content...
   
   1.1.1 History         ? Level 3
   Content...
   ```
   - Pattern: `^\d+(\.\d+)*\.?\s+(.+)$`
   - Depth = number of dots in numbering
   - Followed by content

4. **Prefix Detection** (optional):
   ```plaintext
   # Section One         ? Level 1
   ## Subsection       ? Level 2
   ### Sub-subsection    ? Level 3
   ```
   - Markdown-style `#` prefix (even in plain text)
   - Count `#` for level

#### Implementation Notes:
- **Priority Order**: Try underlines ? numbered ? ALL CAPS ? prefixed
- **Confidence Scores**: Underlines (highest) > numbered > ALL CAPS > prefixed
- **False Positive Mitigation**: Check context, length, content patterns

---

### Task 4.3: Paragraph Detection

**Priority**: High  
**Estimated Effort**: 2-3 hours

#### Paragraph Detection Rules:

1. **Double Newline**:
 ```plaintext
   First paragraph.
   
   Second paragraph.   ? Double newline = new paragraph
   ```
 - Two consecutive `\n` characters
   - Works for most formatted text

2. **Indentation Change**:
   ```plaintext
Indented paragraph.
   
   Back to normal.      ? Indentation decrease = new paragraph
   ```
   - Significant indentation change (±4 spaces)

3. **Sentence Completion**:
   ```plaintext
   Complete sentence.
   Next sentence starts here.  ? Period + newline = potential paragraph
   ```
   - Line ends with sentence-ending punctuation (`.`, `!`, `?`)
   - Next line starts with capital letter

#### Implementation:
- **Primary**: Double newline (most reliable)
- **Secondary**: Indentation changes
- **Fallback**: Sentence completion (less reliable)

---

### Task 4.4: List Detection

**Priority**: Medium  
**Estimated Effort**: 3-4 hours

#### List Detection Patterns:

1. **Bulleted Lists**:
   ```plaintext
   - First item
   - Second item
   - Third item
   
   * Alternative bullet
   * Another item
   
   • Unicode bullet
   • Another item
   ```
   - Patterns: `^[\s]*[-*•]\s+(.+)$`
   - Detect nesting by indentation

2. **Numbered Lists**:
   ```plaintext
   1. First item
   2. Second item
   3. Third item
   
   1) Alternative style
   2) Another item
   ```
   - Patterns: `^[\s]*\d+[.)]\s+(.+)$`
   - Detect sequential numbering

3. **Alphabetic Lists**:
   ```plaintext
   a. First item
   b. Second item
   c. Third item
   
   A) Alternative
   B) Another
   ```
   - Patterns: `^[\s]*[a-zA-Z][.)]\s+(.+)$`

#### Nesting Detection:
```plaintext
- Level 1
  - Level 2 (indented)
    - Level 3 (more indented)
- Back to Level 1
```
- Track indentation level
- Group by indentation changes

---

### Task 4.5: Code Block Detection

**Priority**: Medium  
**Estimated Effort**: 2-3 hours

#### Code Block Detection Heuristics:

1. **Consistent Indentation**:
   ```plaintext
   This is normal text.
   
    public class Example {
     public void Method() {
        Console.WriteLine("Code");
  }
       }
   
   Back to normal text.
   ```
   - Multiple consecutive lines with same indentation (?4 spaces or 1 tab)
- Ends when indentation returns to normal

2. **Code Indicators**:
   - Lines with common code patterns:
     - `public class`, `function `, `def `, `var `, `const `
     - Opening/closing braces `{`, `}`
     - Semicolons at line end (`;`)
   - Higher density of special characters
   - Lack of natural language patterns

3. **Fenced Code Blocks** (if present):
   ```plaintext
   ```
   code here
   ```
   ```
   - Markdown-style backticks (even in plain text)

#### Implementation:
- Detect consistent indentation first
- Validate with code indicators
- Track language if possible (by extension or content)

---

### Task 4.6: Text Normalization

**Priority**: High  
**Estimated Effort**: 2 hours

#### Normalization Tasks:

1. **Line Ending Normalization**:
 - Convert `\r\n` (Windows) ? `\n` (Unix)
   - Convert `\r` (old Mac) ? `\n`
   - Consistent line endings simplify parsing

2. **Whitespace Normalization**:
   - Preserve paragraph separation (double newlines)
   - Normalize indentation to spaces (tabs ? 4 spaces)
   - Trim trailing whitespace from lines
   - **Don't**: Remove all whitespace (semantic meaning)

3. **Encoding Detection**:
   - UTF-8 (most common)
   - UTF-16 (with BOM)
   - ASCII
   - Use `System.Text.Encoding.GetEncoding()` with BOM detection

---

### Task 4.7: Factory Registration

**Priority**: High  
**Estimated Effort**: 1 hour

#### Registration Steps:
1. Register `PlainTextDocumentChunker` with `ChunkerFactory`
2. Implement `CanHandleAsync()`:
   - Check for absence of HTML tags
   - Check for absence of Markdown-specific syntax
   - Check for primarily printable ASCII/UTF-8 characters
3. Auto-detection via `.txt` extension

---

### Task 4.8: Testing - Plain Text

**Priority**: Critical  
**Estimated Effort**: 6-8 hours

#### Unit Tests (30-40 tests):

**Heading Detection Tests** (10 tests):
- [ ] Test ALL CAPS heading detection
- [ ] Test underlined heading detection (=== and ---)
- [ ] Test numbered section detection (1., 1.1, 1.1.1)
- [ ] Test prefix heading detection (#, ##, ###)
- [ ] Test false positives (short words, URLs)
- [ ] Test hierarchy building from detected headings

**Paragraph Detection Tests** (8 tests):
- [ ] Test double newline paragraph separation
- [ ] Test indentation-based paragraphs
- [ ] Test sentence-based paragraphs
- [ ] Test mixed paragraph detection

**List Detection Tests** (10 tests):
- [ ] Test bullet list detection (-, *, •)
- [ ] Test numbered list detection (1., 2., 3.)
- [ ] Test alphabetic list detection (a., b., c.)
- [ ] Test nested list detection (indentation)
- [ ] Test list type identification
- [ ] Test nesting level calculation

**Code Block Detection Tests** (6 tests):
- [ ] Test indented code block detection
- [ ] Test fenced code block detection
- [ ] Test code indicators (keywords, symbols)
- [ ] Test language detection

**Normalization Tests** (4 tests):
- [ ] Test line ending normalization
- [ ] Test whitespace normalization
- [ ] Test encoding detection

**Integration Tests** (6 tests):
- [ ] Test simple plain text (no structure)
- [ ] Test structured plain text (headings, lists)
- [ ] Test code-heavy text
- [ ] Test mixed content (text + code + lists)
- [ ] Test large plain text document
- [ ] Test edge cases (empty, single line)

#### Test Documents:
```
TestData/PlainText/
??? simple.txt # Basic paragraphs
??? structured.txt        # Headers, lists, paragraphs
??? code-heavy.txt    # Code blocks with comments
??? all-caps-headers.txt    # ALL CAPS headings
??? underlined-headers.txt  # Underlined headings
??? numbered-sections.txt   # 1., 1.1, 1.1.1 structure
??? lists.txt     # Various list formats
??? mixed.txt         # All structure types
??? large.txt               # Large document (splitting test)
```

---

### Task 4.9: Documentation

**Priority**: High  
**Estimated Effort**: 3-4 hours

#### Documentation Deliverables:

1. **XML Documentation**:
   - All public APIs documented
   - Heuristic algorithms explained
   - Examples for each chunk type

2. **Plain Text Chunking Guide** (`docs/guides/plain-text-chunking.md`):
   - Why plain text chunking is challenging
   - Heuristic detection algorithms
   - Examples of each detection type
   - Best practices
   - Limitations and edge cases
   - Comparison with structured formats

3. **Code Examples**:
   - Basic plain text chunking
   - Custom heading detection
   - Handling code-heavy text
   - Processing logs and transcripts

---

## Architecture Decisions

### Chunk Type Hierarchy

```csharp
PlainTextSectionChunk : StructuralChunk
{
    HeadingType: HeadingHeuristic  // AllCaps, Underlined, Numbered, Prefixed
    Confidence: double   // 0.0-1.0
    OriginalHeading: string
}

PlainTextParagraphChunk : ContentChunk
{
    DetectionMethod: ParagraphDetection  // DoubleNewline, Indentation, Sentence
}

PlainTextListItemChunk : ContentChunk
{
 ListType: string     // "bullet", "numbered", "alphabetic"
    NestingLevel: int
    Marker: string                  // "-", "1.", "a.", etc.
}

PlainTextCodeBlockChunk : ContentChunk
{
 Language: string?    // Detected or null
    IndentationLevel: int
    CodeIndicators: List<string>    // Keywords found
}
```

### Detection Pipeline

```
1. Text Normalization
   ?
2. Line-by-Line Parsing
   ?
3. Heading Detection (creates structure)
   ?
4. List Detection
   ?
5. Code Block Detection
   ?
6. Paragraph Detection (fallback)
   ?
7. Hierarchy Building
   ?
8. Token Counting
   ?
9. Quality Metrics
```

---

## Performance Considerations

### Optimization Strategies:

1. **Single-Pass Parsing** (when possible):
   - Detect headings, lists, code blocks in one pass
   - Avoid multiple full-document scans

2. **Regex Compilation**:
   - Compile frequently-used regexes once
   - Use `RegexOptions.Compiled` for performance

3. **Line-Based Processing**:
   - Process line-by-line (memory efficient)
   - Build chunks incrementally

4. **Early Exit**:
   - If heading detection fails, don't try again on every line
   - Cache detection results

---

## Success Criteria

| Criteria | Target | Notes |
|----------|--------|-------|
| **Heading Detection Accuracy** | >85% | On structured plain text |
| **Paragraph Detection** | >95% | Double newline most reliable |
| **List Detection** | >90% | Standard bullet/number formats |
| **Code Block Detection** | >80% | Consistent indentation |
| **Test Coverage** | >80% | All heuristics tested |
| **Build Success** | 100% | Zero errors, zero warnings |
| **Documentation** | Complete | Guide + XML docs |

---

## Risks and Mitigations

### Risk 1: False Positives in Heading Detection
**Impact**: Medium  
**Mitigation**:
- Use confidence scores
- Validate with context (e.g., followed by content)
- Provide configuration to tune sensitivity

### Risk 2: Inconsistent Plain Text Formats
**Impact**: High  
**Mitigation**:
- Support multiple detection heuristics
- Make detection configurable
- Document limitations clearly
- Provide examples of "good" plain text

### Risk 3: Performance with Large Documents
**Impact**: Medium  
**Mitigation**:
- Line-based processing (memory efficient)
- Compiled regexes
- Early exit strategies
- Streaming support (future)

---

## Dependencies

### Required:
- `PanoramicData.Chunker` (existing core)
- `ITokenCounter` interface (Phase 3)
- `ChunkerFactory` (Phase 0)

### Optional:
- None (plain text requires no external libraries)

---

## Deliverables Checklist

- [ ] `PlainTextDocumentChunker.cs` implementation
- [ ] Plain text-specific chunk types (4 classes)
- [ ] Heading detection algorithms (4 types)
- [ ] List detection algorithms (3 types)
- [ ] Code block detection
- [ ] Text normalization utilities
- [ ] Factory registration
- [ ] Unit tests (30-40 tests)
- [ ] Integration tests (6 tests)
- [ ] Test documents (9 files)
- [ ] XML documentation (complete)
- [ ] Plain Text Chunking Guide (20+ pages)
- [ ] Code examples

---

## Timeline Estimate

| Task | Estimated Hours |
|------|----------------|
| 4.1 Plain Text Chunker | 4-6 |
| 4.2 Heading Detection | 3-4 |
| 4.3 Paragraph Detection | 2-3 |
| 4.4 List Detection | 3-4 |
| 4.5 Code Block Detection | 2-3 |
| 4.6 Text Normalization | 2 |
| 4.7 Factory Registration | 1 |
| 4.8 Testing | 6-8 |
| 4.9 Documentation | 3-4 |
| **Total** | **26-35 hours** |
| **Sessions** | **2-3 (at 8-12 hrs/session)** |

---

## Next Steps

**Session 1** (This session):
1. Create plain text chunk types
2. Implement PlainTextDocumentChunker (basic structure)
3. Implement heading detection (all 4 types)
4. Implement basic tests

**Session 2**:
1. Implement paragraph, list, code detection
2. Implement text normalization
3. Complete unit tests
4. Create integration tests

**Session 3**:
1. Create test documents
2. Write documentation guide
3. Final testing and validation
4. Mark Phase 4 complete

---

**Phase 4 Status**: ?? **STARTING NOW**  
**Date**: January 2025  
**Ready to implement!** ??
