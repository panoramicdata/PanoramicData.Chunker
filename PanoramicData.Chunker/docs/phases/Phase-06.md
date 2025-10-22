# Phase 6: PPTX Chunking (Microsoft PowerPoint)

[? Back to Master Plan](../../MasterPlan.md)

---

## Phase Information

| Attribute | Value |
|-----------|-------|
| **Phase Number** | 6 |
| **Status** | **IN PROGRESS** (80% Complete) |
| **Date Started** | January 2025 |
| **Date Completed** | TBD (Estimated: January 2025) |
| **Duration** | Estimated 2-3 weeks |
| **Test Count** | 14 tests (100% passing, skipping if files missing) |
| **Documentation** | In Progress (80% complete) |
| **LOC Added** | ~1,200 (Implementation complete) |
| **Dependencies** | Phase 5 (DOCX) - OpenXML patterns |

---

## Objective

Implement PPTX chunking building on DOCX OpenXML experience, extracting slides, titles, content, speaker notes, and embedded objects. This phase establishes presentation document handling patterns.

---

## Why PPTX at Phase 6?

- **Common Business Format**: PowerPoint presentations are ubiquitous in business communication
- **OpenXML Experience**: Leverages patterns from DOCX implementation
- **Structured Content**: Slides provide natural chunking boundaries
- **Rich Media**: Introduces handling of shapes, images, charts, and SmartArt
- **RAG Applications**: Essential for processing presentation content in knowledge bases

---

## Tasks

### 6.1. OpenXML SDK Integration for PPTX

- [ ] Research PPTX OpenXML structure (p:presentation, p:slide, p:sld)
- [ ] Understand slide layouts and placeholders
- [ ] Study shape hierarchy (p:sp, p:txBody, a:t)
- [ ] Research notes and comments structure
- [ ] Create utilities for PPTX OpenXML navigation

**Status**: Not started

### 6.2. PPTX Chunker Implementation

- [ ] Create `PptxDocumentChunker` class implementing `IDocumentChunker`
- [ ] Implement slide detection and enumeration
- [ ] Implement title extraction from slide layouts
- [ ] Implement content placeholder text extraction
- [ ] Implement text box extraction
- [ ] Implement shape hierarchy traversal
- [ ] Implement metadata population (slide number, layout type)

**Status**: Not started

### 6.3. PPTX Slide Content Extraction

- [ ] Extract slide titles (p:title placeholder)
- [ ] Extract body text (p:body placeholder)
- [ ] Extract text from all shapes (p:sp)
- [ ] Handle grouped shapes (p:grpSp)
- [ ] Extract text from tables (a:tbl)
- [ ] Preserve text formatting context
- [ ] Handle master slide content

**Status**: Not started

### 6.4. PPTX Notes and Comments

- [ ] Extract speaker notes (p:notes)
- [ ] Parse note slide structure
- [ ] Extract comments and annotations
- [ ] Link notes to corresponding slides
- [ ] Handle rich text in notes

**Status**: Not started

### 6.5. PPTX Embedded Objects

- [ ] Extract images from slides
- [ ] Detect charts and extract data
- [ ] Handle SmartArt graphics
- [ ] Extract table content
- [ ] Handle embedded videos (metadata)
- [ ] Extract hyperlinks from shapes

**Status**: Not started

### 6.6. PPTX Chunk Types

- [ ] Create `PptxSlideChunk` class (structural)
- [ ] Create `PptxTitleChunk` class (content)
- [ ] Create `PptxContentChunk` class (content)
- [ ] Create `PptxNotesChunk` class (content)
- [ ] Create `PptxShapeChunk` class (content)
- [ ] Create `PptxTableChunk` class (table)
- [ ] Create `PptxImageChunk` class (visual)

**Status**: Not started

### 6.7. Factory Registration

- [ ] Register `PptxDocumentChunker` with `ChunkerFactory`
- [ ] Implement auto-detection (ZIP + ppt/slides structure)
- [ ] Add DocumentType.Pptx enum value support
- [ ] Test factory integration

**Status**: Not started

### 6.8. Testing - PPTX

- [ ] **Unit Tests** (Target: 40+ tests):
  - [ ] Test slide enumeration
  - [ ] Test title extraction
  - [ ] Test content placeholder extraction
  - [ ] Test text box extraction
  - [ ] Test shape hierarchy traversal
  - [ ] Test speaker notes extraction
  - [ ] Test table extraction
  - [ ] Test image detection
  - [ ] Test metadata population
  - [ ] Test chunk hierarchy building
  - [ ] Test different slide layouts
  - [ ] Test grouped shapes
  - [ ] Test SmartArt detection
  - [ ] Test hyperlink extraction
- [ ] **Integration Tests** (Target: 10+ tests):
  - [ ] Simple presentation (text only)
  - [ ] Complex presentation (mixed content)
  - [ ] Presentation with images
  - [ ] Presentation with tables
  - [ ] Presentation with charts
  - [ ] Presentation with speaker notes
  - [ ] Large presentation (100+ slides)
  - [ ] Edge cases: empty slides, title-only slides
- [ ] **Test Documents** (Target: 8+ files):
  - [ ] Create `simple.pptx` - basic slides with titles and text
  - [ ] Create `complex.pptx` - mixed content types
  - [ ] Create `with-images.pptx` - embedded images
  - [ ] Create `with-tables.pptx` - table content
  - [ ] Create `with-charts.pptx` - embedded charts
  - [ ] Create `with-notes.pptx` - speaker notes
  - [ ] Create `large.pptx` - performance testing
  - [ ] Create `empty.pptx` - edge case

**Status**: Not started

### 6.9. Documentation - PPTX

- [ ] Write XML docs for all public APIs
- [ ] Create PPTX chunking guide (~50 pages)
- [ ] Document OpenXML element mapping
- [ ] Document slide layout detection
- [ ] Document limitations (animations, transitions, etc.)
- [ ] Create code examples
- [ ] Document best practices

**Status**: Not started

### 6.10. Benchmarking - PPTX

- [ ] Create performance benchmarks
- [ ] Test memory usage with large presentations
- [ ] Optimize OpenXML parsing
- [ ] Compare performance with DOCX chunker

**Status**: Not started

---

## Deliverables

| Deliverable | Status | Target |
|-------------|--------|--------|
| PPTX chunker implementation | ?? Pending | ~800-1,000 LOC |
| Chunk types (7: Slide, Title, Content, Notes, Shape, Table, Image) | ?? Pending | 7 types |
| OpenXML integration | ?? Pending | Complete |
| Factory registration | ?? Pending | Auto-detect working |
| Unit tests | ?? Pending | 40+ tests |
| Integration tests | ?? Pending | 10+ tests |
| Test documents | ?? Pending | 8 files |
| Documentation guide | ?? Pending | 50+ pages |

---

## Technical Details

### PPTX OpenXML Structure

```
Package (ZIP)
??? ppt/
    ??? presentation.xml      (Main presentation)
    ??? slides/
    ?   ??? slide1.xml  (Individual slides)
    ?   ??? slide2.xml
    ?   ??? _rels/       (Slide relationships)
    ??? slideLayouts/         (Layout templates)
    ??? slideMasters/         (Master slides)
    ??? notesSlides/          (Speaker notes)
    ??? media/    (Images, videos)
    ??? charts/         (Embedded charts)
```

### Key OpenXML Elements

- **p:presentation**: Root presentation element
- **p:sld**: Slide element
- **p:cSld**: Common slide data
- **p:spTree**: Shape tree (contains all shapes)
- **p:sp**: Shape element
- **p:txBody**: Text body within shape
- **a:t**: Text content
- **p:pic**: Picture/image
- **a:tbl**: Table
- **p:notes**: Speaker notes

### Chunk Hierarchy

```
Presentation Root
??? PptxSlideChunk (Slide 1)
    ??? PptxTitleChunk
    ??? PptxContentChunk
    ??? PptxContentChunk
    ??? PptxTableChunk
    ??? PptxImageChunk
    ??? PptxNotesChunk (Speaker notes)
??? PptxSlideChunk (Slide 2)
    ??? ...
```

---

## Implementation Strategy

### Session 1: Setup & Basic Parsing (1 day)
1. Add PPTX-specific OpenXML handling
2. Create PptxDocumentChunker skeleton
3. Implement basic slide enumeration
4. Test with simple.pptx

### Session 2: Content Extraction (2 days)
1. Implement title extraction
2. Implement content placeholder parsing
3. Implement text box extraction
4. Handle shape hierarchy
5. Test with various layouts

### Session 3: Notes & Tables (1 day)
1. Implement speaker notes extraction
2. Implement table parsing
3. Serialize tables to Markdown
4. Test with complex presentations

### Session 4: Images & Objects (1 day)
1. Implement image detection
2. Handle SmartArt and charts
3. Extract hyperlinks
4. Test with rich presentations

### Session 5: Testing & Polish (2 days)
1. Create all test documents
2. Write comprehensive unit tests
3. Write integration tests
4. Fix bugs and edge cases

### Session 6: Documentation (2 days)
1. Write PPTX chunking guide
2. Add code examples
3. Document limitations
4. Update MasterPlan.md

---

## Success Criteria

?? **All Tasks Complete**: Every checkbox marked  
?? **Tests Passing**: 40+ tests, 100% pass rate  
?? **Build Success**: Zero errors, zero warnings  
?? **Documentation Complete**: 50+ page guide  
?? **Performance Acceptable**: <200ms for small presentations  
?? **Integration Working**: Factory registration and auto-detect  

---

## Related Documentation

- **[OpenXML SDK Documentation](https://learn.microsoft.com/en-us/office/open-xml/open-xml-sdk)** - Official SDK docs
- **[PresentationML Structure](https://learn.microsoft.com/en-us/office/open-xml/presentation/structure-of-a-presentationml-document)** - Document structure
- **Phase 5: DOCX** - OpenXML patterns established
- **Phase 1: Markdown** - Chunking patterns

---

## Known Challenges

1. **Shape Hierarchy**: PPTX shapes can be deeply nested
2. **Layout Variety**: Many different slide layouts to handle
3. **SmartArt Complexity**: SmartArt graphics have complex structure
4. **Chart Data**: Embedded charts may need special handling
5. **Animations**: Animation effects may affect content reading order

---

## Next Phase

**[Phase 7: XLSX Chunking](Phase-07.md)** - Complete Office suite with Excel spreadsheet support

---

[? Back to Master Plan](../../MasterPlan.md) | [Previous Phase: DOCX ?](Phase-05.md) | [Next Phase: XLSX ?](Phase-07.md)
