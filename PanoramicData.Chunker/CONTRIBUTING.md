# Contributing to PanoramicData.Chunker

Thank you for your interest in contributing to PanoramicData.Chunker! This document provides guidelines and instructions for contributing.

## ?? How Can I Contribute?

### Reporting Bugs

Before creating bug reports, please check existing issues. When creating a bug report, include:

- **Clear title and description**
- **Steps to reproduce**
- **Expected vs actual behavior**
- **Code samples** if applicable
- **Environment details** (.NET version, OS, etc.)

### Suggesting Enhancements

Enhancement suggestions are tracked as GitHub issues. When suggesting an enhancement:

- **Use a clear and descriptive title**
- **Provide detailed description** of the proposed functionality
- **Explain why this enhancement would be useful**
- **Provide examples** if possible

### Pull Requests

1. **Fork the repository** and create your branch from `main`
2. **Follow the coding standards** (see below)
3. **Add tests** for your changes
4. **Ensure all tests pass** (`dotnet test`)
5. **Update documentation** if needed
6. **Write clear commit messages**
7. **Submit a pull request**

## ?? Coding Standards

### General Guidelines

- Use **C# 12** language features
- Enable **nullable reference types**
- Follow **Microsoft .NET coding conventions**
- Write **XML documentation** for all public APIs
- Use **async/await** for all I/O operations

### Code Style

```csharp
// ? Good
public async Task<ChunkingResult> ChunkAsync(
    Stream documentStream,
    ChunkingOptions options,
    CancellationToken cancellationToken = default)
{
    // Implementation
}

// ? Bad
public Task<ChunkingResult> Chunk(Stream stream, ChunkingOptions opts)
{
    // Missing async, poor naming, no cancellation support
}
```

### Testing

- **80%+ code coverage** required
- Write **unit tests** for all new functionality
- Include **integration tests** for document processing
- Use **descriptive test names**

```csharp
[Fact]
public async Task ChunkAsync_WithValidMarkdown_ShouldReturnChunks()
{
    // Arrange
    var markdown = "# Header\n\nParagraph";
    
    // Act
    var result = await ChunkAsync(markdown);
    
    // Assert
    Assert.True(result.Success);
    Assert.NotEmpty(result.Chunks);
}
```

## ??? Development Setup

### Prerequisites

- .NET 9 SDK
- Git
- IDE (Visual Studio 2022, VS Code, or Rider)

### Building

```bash
git clone https://github.com/panoramicdata/PanoramicData.Chunker.git
cd PanoramicData.Chunker
dotnet restore
dotnet build
```

### Running Tests

```bash
dotnet test
```

### Running Benchmarks

```bash
cd PanoramicData.Chunker.Benchmarks
dotnet run -c Release
```

## ?? Project Structure

```
PanoramicData.Chunker/
??? Models/              # Core data models
??? Configuration/       # Options and presets
??? Interfaces/         # Public interfaces
??? Infrastructure/     # Core implementations
??? Utilities/          # Helper utilities
??? Chunkers/           # Format-specific chunkers

PanoramicData.Chunker.Tests/
??? Unit/               # Unit tests
??? Integration/        # Integration tests
??? TestData/           # Test documents
```

## ?? Adding a New Document Format

1. **Create chunker class** implementing `IDocumentChunker`
2. **Add unit tests** in `Tests/Unit/Chunkers/`
3. **Add integration tests** with sample documents
4. **Update README** with format support
5. **Update MasterPlan** to check off tasks

Example:

```csharp
public class MyFormatChunker : IDocumentChunker
{
    public DocumentType SupportedType => DocumentType.MyFormat;
    
    public async Task<ChunkingResult> ChunkAsync(
        Stream documentStream,
        ChunkingOptions options,
        CancellationToken cancellationToken = default)
    {
        // Implementation
    }
    
    public async Task<bool> CanHandleAsync(
        Stream documentStream,
        CancellationToken cancellationToken = default)
    {
        // Detection logic
    }
}
```

## ?? Commit Message Format

```
<type>(<scope>): <subject>

<body>

<footer>
```

**Types:**
- `feat`: New feature
- `fix`: Bug fix
- `docs`: Documentation changes
- `test`: Adding or updating tests
- `refactor`: Code refactoring
- `perf`: Performance improvements
- `chore`: Maintenance tasks

**Example:**
```
feat(markdown): add support for code blocks with syntax highlighting

- Implemented code block detection
- Added syntax highlighting metadata
- Included tests for various languages

Closes #123
```

## ? Pull Request Checklist

- [ ] Code follows project conventions
- [ ] All tests pass
- [ ] Added tests for new functionality
- [ ] Updated documentation
- [ ] No compiler warnings
- [ ] XML docs for public APIs
- [ ] CHANGELOG.md updated (if applicable)

## ?? Issue Labels

- `bug`: Something isn't working
- `enhancement`: New feature or request
- `documentation`: Documentation improvements
- `good first issue`: Good for newcomers
- `help wanted`: Community help needed
- `question`: Further information requested

## ?? Resources

- [Technical Specification](Specification.md)
- [Master Plan](MasterPlan.md)
- [.NET Coding Conventions](https://docs.microsoft.com/en-us/dotnet/csharp/fundamentals/coding-style/coding-conventions)

## ?? Questions?

Feel free to open an issue or start a discussion on GitHub Discussions.

## ?? Code of Conduct

Please note that this project is released with a Contributor Code of Conduct. By participating in this project you agree to abide by its terms.

---

Thank you for contributing to PanoramicData.Chunker! ??
