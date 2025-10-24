# NuGet Package Publishing Guide for PanoramicData.Chunker

## Package Created ?

**Package File**: `nupkg/PanoramicData.Chunker.0.9.0.nupkg`  
**Size**: 120 KB  
**Version**: 0.9.0 (Beta)  
**Created**: January 24, 2025

---

## Pre-Publishing Checklist

### ? Completed
- [x] Package metadata configured (version, authors, description, tags)
- [x] README.md included in package
- [x] Documentation XML file generated
- [x] SourceLink configured for debugging support
- [x] Symbol package (.snupkg) will be created
- [x] All 409 tests passing (100%)
- [x] Build successful with Release configuration

### ?? Warnings to Address (Optional)
1. **Duplicate SourceLink Reference**: Remove one instance (cosmetic issue)
2. **HtmlImageChunk Width/Height**: Add `new` keyword to hide warnings
3. **Prerelease Dependency**: UglyToad.PdfPig 1.7.0-custom-5 is prerelease
   - Consider this is acceptable for 0.9.0 beta
   - Or wait for stable PdfPig release before 1.0.0

---

## Publishing Options

### Option 1: Publish to NuGet.org (Recommended)

#### Step 1: Get API Key
1. Go to https://www.nuget.org/
2. Sign in with your account
3. Go to Account Settings ? API Keys
4. Create a new API key with "Push" permissions
5. Select package ID pattern: `PanoramicData.*` or specific `PanoramicData.Chunker`

#### Step 2: Publish the Package

```powershell
# Set your API key (do this once)
$apiKey = "YOUR_NUGET_API_KEY_HERE"

# Push to NuGet.org
dotnet nuget push nupkg/PanoramicData.Chunker.0.9.0.nupkg `
    --api-key $apiKey `
    --source https://api.nuget.org/v3/index.json
```

#### Step 3: Verify

- Package will appear at: https://www.nuget.org/packages/PanoramicData.Chunker/
- It may take 10-15 minutes to index and appear in search
- Symbol package (.snupkg) will be automatically extracted and published

---

### Option 2: Publish to GitHub Packages

```powershell
# Authenticate with GitHub (do this once)
dotnet nuget add source --username YOUR_GITHUB_USERNAME `
    --password YOUR_GITHUB_PAT `
    --store-password-in-clear-text `
  --name github `
 "https://nuget.pkg.github.com/panoramicdata/index.json"

# Push to GitHub Packages
dotnet nuget push nupkg/PanoramicData.Chunker.0.9.0.nupkg `
    --source "github"
```

---

### Option 3: Publish to Azure Artifacts (Private Feed)

```powershell
# Add Azure Artifacts source
dotnet nuget add source https://pkgs.dev.azure.com/panoramicdata/_packaging/YOUR_FEED/nuget/v3/index.json `
    --name azure `
    --username az `
    --password YOUR_AZURE_PAT `
    --store-password-in-clear-text

# Push to Azure Artifacts
dotnet nuget push nupkg/PanoramicData.Chunker.0.9.0.nupkg `
    --source "azure"
```

---

## Post-Publishing Steps

### 1. Tag the Release in Git

```bash
git tag -a v0.9.0 -m "Version 0.9.0 - Beta Release"
git push origin v0.9.0
```

### 2. Create GitHub Release

1. Go to https://github.com/panoramicdata/PanoramicData.Chunker/releases
2. Click "Create a new release"
3. Select tag `v0.9.0`
4. Title: "v0.9.0 - Beta Release"
5. Description:
```markdown
## PanoramicData.Chunker 0.9.0 (Beta)

### ?? Features
- Support for 8 document formats: Markdown, HTML, Plain Text, DOCX, PPTX, XLSX, CSV, PDF
- Hierarchical chunking with parent-child relationships
- OpenAI token counting integration
- Quality metrics and validation framework
- 409 comprehensive tests (100% pass rate)

### ?? Installation

```bash
dotnet add package PanoramicData.Chunker --version 0.9.0
```

### ?? Document Formats Supported
- ? Markdown (`.md`)
- ? HTML (`.html`, `.htm`)
- ? Plain Text (`.txt`)
- ? Microsoft Word (`.docx`)
- ? Microsoft PowerPoint (`.pptx`)
- ? Microsoft Excel (`.xlsx`)
- ? CSV (`.csv`)
- ? PDF (`.pdf`) - Basic text extraction (OCR support planned for v1.0)

### ?? Documentation
- [Getting Started Guide](https://github.com/panoramicdata/PanoramicData.Chunker#readme)
- [API Documentation](https://github.com/panoramicdata/PanoramicData.Chunker/tree/main/docs)
- [Sample Code](https://github.com/panoramicdata/PanoramicData.Chunker/tree/main/samples)

### ?? Beta Release Notes
This is a beta release. APIs may change before the 1.0 stable release. Feedback and contributions are welcome!

### ?? Known Limitations
- PDF OCR not yet supported (planned for Phase 18)
- Image descriptions not yet implemented (planned for Phase 10)
- LLM integration not yet available (planned for Phase 11)

### ?? Acknowledgments
Built using:
- DocumentFormat.OpenXml for Office file parsing
- UglyToad.PdfPig for PDF processing
- Markdig for Markdown parsing
- AngleSharp for HTML parsing
- SharpToken for OpenAI token counting
```

6. Attach the .nupkg file
7. Mark as "pre-release" since it's 0.9.0
8. Publish release

### 3. Update Documentation

Update README.md with installation instructions:

```markdown
## Installation

```bash
dotnet add package PanoramicData.Chunker
```

Or via Package Manager Console:

```powershell
Install-Package PanoramicData.Chunker
```
```

### 4. Announce the Release

Consider announcing on:
- GitHub Discussions
- Company blog
- Twitter/LinkedIn
- Relevant .NET communities

---

## Versioning Strategy

Current: **0.9.0 (Beta)**

### Roadmap to 1.0:
- 0.9.0 ? Current beta release (Phases 0-9 complete)
- 0.9.1 ? Bug fixes and minor improvements
- 0.9.2 ? Additional format support (RTF, JSON, XML, Email - Phase 16)
- 0.10.0 ? Performance optimizations (Phase 13)
- 0.11.0 ? Semantic chunking (Phase 12)
- 1.0.0 ? Stable release with all core features

### Future Versions:
- 1.1.0 ? Image descriptions (Phase 10)
- 1.2.0 ? LLM integration (Phase 11)
- 2.0.0 ? PDF OCR support (Phase 18)

---

## Package Contents

The NuGet package includes:
- ? `PanoramicData.Chunker.dll` - Main library assembly
- ? `PanoramicData.Chunker.xml` - XML documentation
- ? README.md - Package documentation
- ? Dependencies:
  - AngleSharp 1.3.0
  - DocumentFormat.OpenXml 3.2.0
  - Markdig 0.42.0
  - Microsoft.Extensions.Logging.Abstractions 9.0.0
  - SharpToken 2.0.4
  - UglyToad.PdfPig 1.7.0-custom-5

---

## Testing the Package Locally

Before publishing, you can test the package locally:

```powershell
# Create a test project
dotnet new console -n ChunkerTest
cd ChunkerTest

# Add local package source
dotnet nuget add source C:\Users\david\Projects\PanoramicData.Chunker\PanoramicData.Chunker\nupkg --name local

# Install the package
dotnet add package PanoramicData.Chunker --version 0.9.0 --source local

# Create a test file
# Program.cs content:
```csharp
using PanoramicData.Chunker;
using PanoramicData.Chunker.Configuration;

var markdown = "# Test\nThis is a test document.";
using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(markdown));

var result = await DocumentChunker.ChunkAsync(stream, DocumentType.Markdown);

Console.WriteLine($"Success: {result.Success}");
Console.WriteLine($"Chunks: {result.Chunks.Count}");
```

```powershell
# Run the test
dotnet run
```

---

## Troubleshooting

### Package Push Fails with 409 Conflict
- Version 0.9.0 already exists on the feed
- Increment version number and repack

### Package Not Appearing in Search
- Wait 10-15 minutes for indexing
- Check package status at NuGet.org

### Dependency Resolution Issues
- Ensure target framework compatibility (.NET 9.0)
- Check for prerelease dependency warnings

---

## Security Considerations

- ? SourceLink enabled for secure debugging
- ? Symbols published separately (.snupkg)
- ?? Consider signing the assembly (future enhancement)
- ?? Consider enabling package signing (future enhancement)

---

## Next Steps

1. **Immediate**: Test the package locally
2. **Short-term**: Publish to NuGet.org as 0.9.0 beta
3. **Medium-term**: Gather feedback and iterate
4. **Long-term**: Complete remaining phases and release 1.0.0

---

**Package Status**: ? **Ready to Publish**  
**Recommendation**: Publish as 0.9.0 beta to NuGet.org  
**Target Date**: January 2025

---

*Generated: January 24, 2025*  
*Package: PanoramicData.Chunker.0.9.0.nupkg*
