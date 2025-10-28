# GitHub Copilot Instructions for PanoramicData.Chunker

## Documentation Guidelines

### DO ?

1. **Update Existing Documentation**
   - Update `docs/MasterPlan.md` when phase status changes
   - Update individual phase files in `docs/phases/Phase-XX.md` as work progresses
   - Keep phase documentation in sync with implementation

2. **Maintain Project Status**
   - Update project statistics (test counts, LOC, phase completion %)
   - Update phase status dashboard in MasterPlan
   - Update milestone timelines

3. **Document in Code**
   - Add XML documentation comments to all public APIs
   - Include inline comments for complex logic
   - Document OpenXML patterns and element mappings

### DON'T ?

1. **Don't Create Extraneous .md Files**
   - ? Don't create `PHASE_X_SUMMARY.md` files
   - ? Don't create `PHASE_X_VALIDATION_REPORT.md` files
   - ? Don't create `PHASE_X_EXECUTION_COMPLETE.md` files
   - ? Don't create `PHASE_X_READY_FOR_TEST_FILES.md` files
   - ? Don't create temporary status files

2. **Exception**: Test file specification documents like `PPTX_TEST_FILES.md` that provide detailed specifications for creating test files are acceptable when needed.

3. **Use Existing Structure**
   - All phase documentation goes in `docs/phases/Phase-XX.md`
   - All project-level documentation goes in `docs/MasterPlan.md`
   - Test results and status are documented within phase files

## Documentation Structure

```
docs/
??? MasterPlan.md       ? Project-level status, all phases summary
??? phases/
? ??? Phase-00.md   ? Foundation
?   ??? Phase-01.md  ? Markdown (include implementation, tests, results)
?   ??? Phase-02.md  ? HTML
?   ??? ...
?   ??? Phase-20.md     ? Release
??? other specialized docs only when necessary
```

## Phase Documentation Template

Each phase file (`Phase-XX.md`) should contain:

1. **Phase Information** - Status, dates, metrics
2. **Implementation Progress** - What's complete, what remains
3. **Objective** - Why this phase exists
4. **Tasks** - Detailed task breakdown with checkboxes
5. **Deliverables** - What was delivered
6. **Technical Details** - Architecture, patterns used
7. **Implementation Summary** - Features completed
8. **Test Results** - Include in the phase file when tests pass
9. **Success Criteria** - Met or pending
10. **Related Documentation** - Links to other resources

## Workflow

### When Completing a Phase

1. Update the phase file (`docs/phases/Phase-XX.md`):
   - Mark all tasks complete
   - Add test results section
   - Update deliverables table
   - Add performance metrics
   - Mark status as "Complete"

2. Update MasterPlan.md:
   - Update project status dashboard
   - Move phase from "In Progress" to "Complete"
   - Update quick stats (test counts, LOC, formats supported)
   - Update recent updates section
   - Update milestone timeline

3. **DO NOT** create separate completion summary files

### When Starting a Phase

1. Create/update the phase file with:
   - Status: "In Progress"
   - Tasks section with checkboxes
   - Implementation progress tracker

2. Update MasterPlan.md:
   - Update "Current Phase"
   - Note the phase as "In Progress" in the summary table

## Testing Standards

### Test Assertion Style

**ALWAYS use FluentAssertions, NEVER use `Assert.XXX`**

? **WRONG** - Do not use xUnit Assert syntax:
```csharp
Assert.Equal(expected, actual);
Assert.Single(collection);
Assert.NotEmpty(collection);
Assert.True(condition);
Assert.NotNull(value);
```

? **CORRECT** - Use FluentAssertions:
```csharp
actual.Should().Be(expected);
collection.Should().ContainSingle();
collection.Should().NotBeEmpty();
condition.Should().BeTrue();
value.Should().NotBeNull();
```

### Common Conversions

| xUnit Assert | FluentAssertions |
|--------------|------------------|
| `Assert.Equal(expected, actual)` | `actual.Should().Be(expected)` |
| `Assert.NotEqual(expected, actual)` | `actual.Should().NotBe(expected)` |
| `Assert.True(condition)` | `condition.Should().BeTrue()` |
| `Assert.False(condition)` | `condition.Should().BeFalse()` |
| `Assert.Null(value)` | `value.Should().BeNull()` |
| `Assert.NotNull(value)` | `value.Should().NotBeNull()` |
| `Assert.Empty(collection)` | `collection.Should().BeEmpty()` |
| `Assert.NotEmpty(collection)` | `collection.Should().NotBeEmpty()` |
| `Assert.Single(collection)` | `collection.Should().ContainSingle()` |
| `Assert.Contains(item, collection)` | `collection.Should().Contain(item)` |
| `Assert.DoesNotContain(item, collection)` | `collection.Should().NotContain(item)` |
| `Assert.All(collection, predicate)` | `collection.Should().AllSatisfy(predicate)` |
| `Assert.Throws<T>()` | `act.Should().Throw<T>()` |
| `Assert.ThrowsAsync<T>()` | `await act.Should().ThrowAsync<T>()` |

### Why FluentAssertions?

- More readable and expressive assertions
- Better error messages
- Consistent fluent API
- Eliminates FAA0002 analyzer warnings
- Industry best practice for .NET testing

## Code Quality Standards

- Zero compilation errors
- Zero warnings
- All tests passing
- >80% code coverage
- XML docs on all public APIs
- Follow established patterns from prior phases
- **Use FluentAssertions for all test assertions**
- **All async methods MUST have mandatory CancellationToken parameters**

## Async/Await Standards

### Mandatory CancellationToken

**ALL async methods MUST have a mandatory CancellationToken parameter (no default value).**

? **WRONG** - Optional CancellationToken:
```csharp
public async Task<Result> ProcessAsync(string input, CancellationToken cancellationToken = default)
{
    // ...
}
```

? **CORRECT** - Mandatory CancellationToken:
```csharp
public async Task<Result> ProcessAsync(string input, CancellationToken cancellationToken)
{
    // ...
}
```

### Rationale

1. **Explicit Cancellation**: Forces callers to think about cancellation
2. **Better Testing**: Tests must explicitly pass cancellation tokens
3. **Production Ready**: Prevents forgotten cancellation support
4. **API Clarity**: Makes cancellation a first-class concern

### Exceptions

The ONLY acceptable use of `CancellationToken cancellationToken = default` is in:
- **Public API surface** where backward compatibility is critical
- **Interface definitions** that must remain compatible

For all **implementation methods**, use **mandatory** cancellation tokens.

### Pass CancellationToken Down

Always pass the cancellation token to all async calls:

```csharp
public async Task ProcessAsync(string input, CancellationToken cancellationToken)
{
    // ? Pass it down
    await _repository.SaveAsync(entity, cancellationToken);
    await _cache.SetAsync(key, value, cancellationToken);
    
    // ? Don't ignore it
    await _repository.SaveAsync(entity); // WRONG
}
```

## Naming Conventions

- Chunker classes: `{Type}DocumentChunker` (e.g., `PptxDocumentChunker`)
- Chunk types: `{Type}{Purpose}Chunk` (e.g., `PptxSlideChunk`, `PptxTitleChunk`)
- Test classes: `{ClassName}Tests` (e.g., `PptxDocumentChunkerTests`)
- Test methods: `{MethodName}_Should{ExpectedBehavior}_When{Condition}`
- Database objects: **PascalCase** (tables, columns, indexes)

## Database Standards

### Naming Convention

**ALL database objects MUST use PascalCase:**

? **CORRECT**:
```sql
CREATE TABLE KgEntities (
    Id UUID PRIMARY KEY,
    Name VARCHAR(500),
  NormalizedName VARCHAR(500)
);

CREATE INDEX IX_KgEntities_Type ON KgEntities (Type);
```

? **WRONG**:
```sql
CREATE TABLE kg_entities (
    id UUID PRIMARY KEY,
    name VARCHAR(500),
    normalized_name VARCHAR(500)
);

CREATE INDEX ix_kg_entities_type ON kg_entities (type);
```

### EF Core Migrations

- Use EF Core migrations for all schema changes
- Never modify applied migrations
- Always review generated migrations before applying
- Include raw SQL in migrations when needed (e.g., Apache AGE setup)

## Performance Targets

- Small files (<1MB): <1 second
- Medium files (1-5MB): <5 seconds
- Large files (5-20MB): <20 seconds
- Target: >300 chunks/second

## Git Workflow

- Work in feature branches
- Commit messages should reference phase numbers
- Keep commits focused and atomic
- Update documentation in same commit as code changes

---

## Quick Reference

**Remember**:
1. ? Update `docs/MasterPlan.md` and `docs/phases/Phase-XX.md` as you go
2. ? **ALWAYS use FluentAssertions, NEVER use `Assert.XXX`**
3. ? **ALL async methods MUST have mandatory CancellationToken parameters**
4. ? **ALL database objects MUST use PascalCase naming**
5. ? Don't create separate summary files unless specifically requested for external documentation purposes

---

**Last Updated**: January 2025  
**Version**: 2.0
