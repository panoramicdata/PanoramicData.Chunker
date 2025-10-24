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
?   ??? Phase-00.md         ? Foundation
?   ??? Phase-01.md         ? Markdown (include implementation, tests, results)
?   ??? Phase-02.md         ? HTML
?   ??? ...
?   ??? Phase-20.md         ? Release
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

## Code Quality Standards

- Zero compilation errors
- Zero warnings (PPTX-related)
- All tests passing
- >80% code coverage
- XML docs on all public APIs
- Follow established patterns from prior phases

## Naming Conventions

- Chunker classes: `{Type}DocumentChunker` (e.g., `PptxDocumentChunker`)
- Chunk types: `{Type}{Purpose}Chunk` (e.g., `PptxSlideChunk`, `PptxTitleChunk`)
- Test classes: `{ClassName}Tests` (e.g., `PptxDocumentChunkerTests`)
- Test methods: `{MethodName}_Should{ExpectedBehavior}_When{Condition}`

## Testing Standards

- Write tests before or alongside implementation
- Use FluentAssertions for assertions
- Provide detailed test output via ITestOutputHelper
- Tests should skip gracefully when test files are missing
- Include edge cases (empty files, large files, invalid content)

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

**Remember**: Update `docs/MasterPlan.md` and `docs/phases/Phase-XX.md` as you go. Don't create separate summary files unless specifically requested for external documentation purposes.
