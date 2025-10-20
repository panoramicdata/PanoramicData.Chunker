# Test Data Repository

This directory contains test documents used for integration and unit testing of the PanoramicData.Chunker library.

## Directory Structure

```
TestData/
├── Markdown/
│   ├── simple.md                 # Basic Markdown with headers and paragraphs
│   ├── complex.md                # Complex nested structure
│   ├── tables.md                 # Markdown with tables
│   ├── code-blocks.md            # Various code block formats
│   ├── lists.md                  # Nested lists (ordered and unordered)
│   └── images.md                 # Markdown with images
├── Html/
│   ├── simple.html               # Basic HTML page
│   ├── complex.html              # Complex HTML with semantic elements
│   ├── tables.html               # HTML with tables
│   └── semantic.html             # HTML5 semantic elements
├── PlainText/
│   ├── simple.txt                # Plain text with basic structure
│   ├── headers.txt               # Text with header detection heuristics
│   └── code.txt                  # Text with code blocks
├── Docx/
│   ├── simple.docx               # Basic Word document
│   ├── complex.docx              # Complex with mixed content
│   └── tables.docx               # Document with tables
├── Pptx/
│   ├── simple.pptx               # Basic presentation
│   └── complex.pptx              # Complex presentation
├── Xlsx/
│   ├── simple.xlsx               # Basic spreadsheet
│   └── complex.xlsx              # Complex spreadsheet with multiple sheets
├── Pdf/
│   ├── simple.pdf                # Single-column PDF
│   ├── multicolumn.pdf           # Multi-column layout
│   └── tables.pdf                # PDF with tables
├── Csv/
│   ├── simple.csv                # Basic CSV
│   └── complex.csv               # CSV with special characters
└── Images/
    ├── chart.png                 # Sample chart image
    ├── diagram.png               # Sample diagram
    └── photo.jpg                 # Sample photo
```

## Adding Test Documents

When adding new test documents:

1. **Choose Representative Samples**: Select documents that represent real-world use cases
2. **Include Edge Cases**: Add documents that test boundary conditions
3. **Anonymize Data**: Remove any sensitive or personal information
4. **Document Expected Behavior**: Create corresponding test cases that define expected chunking behavior
5. **Keep Files Small**: Use reasonably-sized files for fast test execution (except when testing large document handling)

## Test Document Guidelines

### Markdown Files
- Include all heading levels (H1-H6)
- Mix different content types (paragraphs, lists, code blocks, tables)
- Include inline formatting (bold, italic, links)

### HTML Files
- Use semantic HTML5 elements where appropriate
- Include both simple and complex nested structures
- Test with various encoding scenarios

### Office Documents
- Create documents in the latest Office format
- Include real-world formatting scenarios
- Test with images, tables, and charts

### PDF Files
- Include both text-based and scanned PDFs (for OCR testing)
- Test single and multi-column layouts
- Include documents with mixed content types

## License and Attribution

Test documents should be:
- Created specifically for this project, OR
- Sourced from public domain or Creative Commons licensed content with proper attribution

Any copyrighted content must have explicit permission for use in testing.

## Expected Output

For each test document, consider creating a corresponding JSON file with expected chunking output. This allows for regression testing:

```
TestData/
├── Markdown/
│   ├── simple.md
│   └── simple.expected.json      # Expected chunking output
```

This helps ensure that changes to the library don't inadvertently break existing functionality.
