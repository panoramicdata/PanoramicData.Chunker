# Phase 5: DOCX Chunking (Microsoft Word)

[? Back to Master Plan](../../MasterPlan.md)

---

## Phase Information

| Attribute | Value |
|-----------|-------|
| **Phase Number** | 5 |
| **Status** | ?? **PENDING** ? Ready to Start |
| **Date Started** | TBD |
| **Date Completed** | TBD |
| **Duration** | Estimated 2-3 weeks |
| **Test Count** | Target: 50+ tests |
| **Documentation** | Pending |
| **LOC Added** | Target: ~800-1,000 |

---

## Objective

Implement DOCX chunking using OpenXML SDK, introducing binary format parsing and establishing patterns for Office document formats (PPTX, XLSX).

---

## Why DOCX at Phase 5?

- **Most Common Office Format**: Word documents are ubiquitous in enterprises
- **Well-Documented SDK**: Microsoft's OpenXML SDK is mature and well-supported
- **Binary Format Introduction**: First binary format after text-based formats
- **Foundation for Office Suite**: Patterns established here apply to PPTX and XLSX
- **Real-World Need**: Essential for RAG systems processing business documents

---

## Tasks

### 5.1. OpenXML SDK Integration

- [ ] Add `DocumentFormat.OpenXml` NuGet package
- [ ] Research OpenXML document structure (w:document, w:body, w:p, w:r)
- [ ] Create utilities for OpenXML navigation
- [ ] Understand style hierarchy and inheritance

**Status**: Not started

### 5.2. DOCX Chunker Implementation

- [ ] Create `DocxDocumentChunker` class implementing `IDocumentChunker`
- [ ] Implement paragraph style detection (Heading1-Heading6, Normal)
- [ ] Implement paragraph chunking with text run consolidation
- [ ] Implement list detection and chunking (w:numPr)
- [ ] Implement table parsing and chunking (w:tbl)
- [ ] Implement image extraction (w:drawing, w:pict)
- [ ] Implement hyperlink extraction as annotations (w:hyperlink)
- [ ] Implement text formatting annotations (bold, italic, underline)
- [ ] Implement metadata population (hierarchy, sequence, depth)

**Status**: Not started

### 5.3. DOCX Table Handling

- [ ] Implement table structure detection (w:tbl, w:tr, w:tc)
- [ ] Parse table headers (w:tblHeader)
- [ ] Parse table rows with cell content
- [ ] Serialize tables to Markdown format
- [ ] Handle merged cells (w:gridSpan, w:vMerge)
- [ ] Handle nested tables

**Status**: Not started

### 5.4. DOCX Image Extraction

- [ ] Extract image binary data from parts
- [ ] Generate SHA256 hash for BinaryReference
- [ ] Extract image dimensions from w:extent
- [ ] Extract alt-text from docPr
- [ ] Determine MIME type from content type
- [ ] Store image metadata

**Status**: Not started

### 5.5. DOCX to Plain Text

- [ ] Implement clean text extraction from text runs
- [ ] Preserve paragraph breaks
- [ ] Handle special characters and symbols
- [ ] Process text boxes and shapes

**Status**: Not started

### 5.6. Factory Registration

- [ ] Register `DocxDocumentChunker` with `ChunkerFactory`
- [ ] Implement auto-detection (ZIP signature PK\\x03\\x04, _rels folder)
- [ ] Add DocumentType.Docx enum value support

**Status**: Not started

### 5.7. Testing - DOCX

- [ ] **Unit Tests** (Target: 40+ tests):
  - [ ] Test heading hierarchy detection (Heading1-6)
  - [ ] Test paragraph parsing with multiple runs
  - [ ] Test list parsing (numbered and bulleted)
  - [ ] Test table parsing (simple tables)
  - [ ] Test table parsing (complex with merged cells)
  - [ ] Test image extraction
  - [ ] Test formatting annotations (bold, italic, underline)
  - [ ] Test hyperlink extraction
  - [ ] Test metadata population
  - [ ] Test chunk hierarchy building
- [ ] **Integration Tests** (Target: 12+ tests):
  - [ ] Simple Word document (text and headings only)
  - [ ] Complex document (mixed content types)
  - [ ] Document with tables
  - [ ] Document with images
  - [ ] Document with nested lists
  - [ ] Large DOCX document (100+ pages)
  - [ ] Edge cases: empty document, only images, only tables
- [ ] **Test Documents** (Target: 8+ files):
  - [ ] Create `simple.docx` - basic text with headings
  - [ ] Create `complex.docx` - mixed content
  - [ ] Create `with-tables.docx` - various table formats
  - [ ] Create `with-images.docx` - embedded images
  - [ ] Create `with-lists.docx` - nested lists
  - [ ] Create `large.docx` - performance testing
  - [ ] Create `empty.docx` - edge case
  - [ ] Create `formatting.docx` - rich formatting

**Status**: Not started

### 5.8. Documentation - DOCX

- [ ] Write XML docs for all public APIs
- [ ] Create DOCX chunking guide (~50 pages)
- [ ] Document OpenXML element mapping
- [ ] Document style detection logic
- [ ] Document limitations (comments, track changes, etc.)
- [ ] Create code examples
- [ ] Document best practices

**Status**: Not started

### 5.9. Benchmarking - DOCX

- [ ] Create performance benchmarks
- [ ] Test memory usage with large documents (100+ pages)
- [ ] Optimize OpenXML parsing (lazy loading, streaming)
- [ ] Compare performance with other libraries

**Status**: Not started

---

## Deliverables

| Deliverable | Status | Target |
|-------------|--------|--------|
| DOCX chunker implementation | ?? Pending | ~800 LOC |
| Chunk types (5: Section, Paragraph, ListItem, Table, Image) | ?? Pending | 5 types |
| OpenXML integration | ?? Pending | Complete |
| Factory registration | ?? Pending | Auto-detect working |
| Unit tests | ?? Pending | 40+ tests |
| Integration tests | ?? Pending | 12+ tests |
| Test documents | ?? Pending | 8 files |
| Documentation guide | ?? Pending | 50+ pages |

---

## Technical Details

### OpenXML Structure

```
Package (ZIP)
??? word/
    ??? document.xml        (Main document)
    ??? styles.xml          (Style definitions)
    ??? numbering.xml       (List numbering)
    ??? media/     (Images)
    ??? _rels/     (Relationships)
```

### Key OpenXML Elements

- **w:document**: Root element
- **w:body**: Document body
- **w:p**: Paragraph
- **w:pPr**: Paragraph properties (including style)
- **w:r**: Text run
- **w:t**: Text content
- **w:tbl**: Table
- **w:drawing**: Image
- **w:hyperlink**: Hyperlink

### Chunk Hierarchy

```
Document Root
??? DocxSection (Heading 1)
    ??? DocxParagraph
    ??? DocxSection (Heading 2)
    ??? DocxParagraph
        ??? DocxListItem
        ??? DocxListItem
        ??? DocxTable
     ??? DocxImage
    ??? DocxParagraph
```

---

## Implementation Strategy

### Session 1: Setup & Basic Parsing (1 day)
1. Add OpenXML SDK package
2. Create DocxDocumentChunker skeleton
3. Implement basic paragraph parsing
4. Test with simple.docx

### Session 2: Heading Detection (1 day)
1. Implement style-based heading detection
2. Build chunk hierarchy
3. Test with nested headings
4. Create unit tests

### Session 3: Lists & Tables (2 days)
1. Implement list detection and chunking
2. Implement table parsing
3. Serialize tables to Markdown
4. Test with complex documents

### Session 4: Images & Formatting (1 day)
1. Implement image extraction
2. Implement formatting annotations
3. Implement hyperlinks
4. Test with rich documents

### Session 5: Testing & Polish (2 days)
1. Create all test documents
2. Write comprehensive unit tests
3. Write integration tests
4. Fix bugs and edge cases

### Session 6: Documentation (2 days)
1. Write DOCX chunking guide
2. Add code examples
3. Document limitations
4. Update MasterPlan.md

---

## Success Criteria

? **All Tasks Complete**: Every checkbox marked  
? **Tests Passing**: 50+ tests, 100% pass rate  
? **Build Success**: Zero errors, zero warnings  
? **Documentation Complete**: 50+ page guide  
? **Performance Acceptable**: <100ms for small docs  
? **Integration Working**: Factory registration and auto-detect  

---

## Related Documentation

- **[OpenXML SDK Documentation](https://learn.microsoft.com/en-us/office/open-xml/open-xml-sdk)** - Official SDK docs
- **[OpenXML Structure](https://learn.microsoft.com/en-us/office/open-xml/structure-of-a-wordprocessingml-document)** - Document structure
- **Phase 4: Plain Text** - Similar heuristic approach
- **Phase 1: Markdown** - Chunking patterns established

---

## Next Phase

**[Phase 6: PPTX Chunking](Phase-06.md)** - Apply OpenXML patterns to PowerPoint presentations

---

[? Back to Master Plan](../../MasterPlan.md) | [Previous Phase: Plain Text ?](Phase-04.md) | [Next Phase: PPTX ?](Phase-06.md)
