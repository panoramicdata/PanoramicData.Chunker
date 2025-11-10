# Phase 12: Named Entity Recognition Enhancement - Progress Report

**Date**: January 2025  
**Status**: IN PROGRESS - Priority 1 & 2 Complete, Diagnosis Needed

---

## ? Completed Work

### Priority 1: Enhanced CapitalizationEntityExtractor (COMPLETE)

**Changes Made**:
1. ? Added proper noun dictionary (100+ common entities)
   - Person names (Darwin, Jameson, Grant, Henslow, FitzRoy)
   - Places (Edinburgh, Cambridge, Galapagos, Plymouth)
   - Organizations (Plinian, Beagle, Royal, Geological)
   - Common first/last names

2. ? Added title/prefix recognition
   - Professor, Captain, Dr., Sir, Lord, HMS, USS, etc.
   - Organizational suffixes (University, Society, Institute, etc.)

3. ? Fixed sentence-start entity detection
   - Now extracts entities at sentence boundaries when they match patterns
   - Title prefixes automatically trigger extraction
   - Dictionary words trigger extraction
   - Multi-word sequences extracted even at sentence start

4. ? Enhanced confidence scoring
   - +0.15 for dictionary words
   - +0.10 for title prefixes
   - +0.10 for multi-word entities
   - +0.10 for organizational suffixes

**Results**:
- ? **ALL 4 test entities now found**: Plinian Society, Professor Jameson, Edinburgh University, Origin
- ? **Entity count increased**: 533 ? 888 entities (+67%)
- ? **Version bumped**: 1.0 ? 2.0

### Priority 2: Enhanced PatternBasedRelationshipExtractor (COMPLETE)

**Changes Made**:
1. ? Added 24 new relationship types to `RelationshipType.cs`
   - StudiedAt, TraveledOn, MentorOf, PresentedTo, VisitedDuring
   - BornIn, FatherOf, MotherOf, MarriedTo, Visited, Discovered
   - Observed, Studied, Collected, Wrote, Developed, Proposed
   - InfluencedBy, LivedIn, Corresponded, SupportedBy, Invited

2. ? Added 35+ relationship patterns (was 17)
   - Phase 12 high-priority patterns based on Darwin text analysis
 - FoundedByPassivePattern, StudiedAtPattern, MentorOfPattern
- PresentedToPattern, TraveledOnPattern, WorksAtUniversityPattern
   - WrotePattern, VisitedPattern, DiscoveredPattern, etc.

3. ? Enhanced existing patterns
   - Better passive voice handling ("founded by", "influenced by")
   - Better educational patterns ("sent to", "studied at", "spent sessions at")
   - Better biographical patterns ("mentored", "persuaded me", "asked to")

4. ? Updated documentation and version
   - Version bumped: 1.0 ? 2.0
   - Comprehensive documentation of Phase 12 enhancements
   - Supported types expanded from 16 to 35+

**Results**:
- ? **Pattern count increased**: 17 ? 35+ patterns
- ? **Relationship types expanded**: 40 ? 64 types
- ? **Relationships extracted increased**: 12,299 ? 13,071 (+6%)

---

## ? Issue: Recall Still 2%

### Problem

Despite successfully extracting more entities (888) and more relationships (13,071), **recall is still 2% (1/50 ground truth relationships found)**.

All misses show the same reason:
```
Professor Jameson -> Founded -> Plinian Society
  Reason: No relationship detected between entities
Charles Darwin -> MemberOf -> Plinian Society
  Reason: No relationship detected between entities
...
```

**This means**:
1. ? Entities ARE being extracted ("Professor Jameson", "Plinian Society")
2. ? But relationships between them are NOT being detected

### Root Cause Hypotheses

#### Hypothesis 1: Entity Name Mismatch
The extracted entity names don't exactly match the ground truth names.

**Example**:
- Ground truth: "Professor Jameson"
- Extracted: "Jameson" (without "Professor")
- Match fails because we're doing exact name comparison

**Evidence**: The `FindEntity()` method in `GroundTruthComparison` has fuzzy matching, but it may not handle this case.

#### Hypothesis 2: Entities in Different Chunks
The entities are in the same large chunk (~5.6KB), but far apart (>500 characters).

**Evidence**: From earlier diagnosis:
- Chunks are ~5,600 characters (HTML semantic chunking)
- `maxDistance = 500` characters
- If "Professor Jameson" and "Plinian Society" are 1,000 characters apart, no relationship is created

**Fix**: Increase `maxDistance` from 500 ? 2000 for HTML chunks

#### Hypothesis 3: Pattern Not Matching
The pattern exists but the actual text between entities doesn't match.

**Example**:
- Darwin text: "The Plinian Society was encouraged and, I believe, founded by Professor Jameson"
- Pattern: `FoundedByPassivePattern` = `@"\b(founded\s+by|established\s+by...)"`
- **Should match!**

**But wait**: The text is "Society...founded by...Professor", so:
- First entity: "Plinian Society"
- Second entity: "Professor Jameson"
- Text between: "was encouraged and, I believe, founded by"
- Pattern should match "founded by" ?

**So why no match?** Needs investigation.

#### Hypothesis 4: Entity Extraction Drops Prefixes
`CapitalizationEntityExtractor` might extract "Jameson" separately from "Professor".

**Test Needed**: Check actual extracted entity names.

---

## ?? Next Steps

### Immediate Diagnostic (Priority 0)

**Create debug test to show**:
1. What entities are actually extracted (exact names)
2. Which chunks they appear in
3. What text is between them
4. Why patterns aren't matching

```csharp
[Fact]
public async Task Debug_ShowEntityNamesAndChunkPositions()
{
    // Extract graph
    var graph = await ExtractDarwinKnowledgeGraphAsync();
    
    // Find specific entities
  var jameson = graph.Entities.FirstOrDefault(e => e.Name.Contains("Jameson"));
    var plinian = graph.Entities.FirstOrDefault(e => e.Name.Contains("Plinian"));
    
    _output.WriteLine($"Jameson entity: {jameson?.Name ?? "NOT FOUND"}");
    _output.WriteLine($"Plinian entity: {plinian?.Name ?? "NOT FOUND"}");
    
    // Show their positions
    if (jameson != null && plinian != null)
    {
        _output.WriteLine($"\nJameson sources: {jameson.Sources.Count}");
     foreach (var source in jameson.Sources)
        {
         _output.WriteLine($"  Chunk {source.ChunkId}: Position {source.Position}");
        }
      
        _output.WriteLine($"\nPlinian sources: {plinian.Sources.Count}");
        foreach (var source in plinian.Sources)
        {
      _output.WriteLine($"  Chunk {source.ChunkId}: Position {source.Position}");
        }
   
        // Check if in same chunk
        var sameChunks = jameson.Sources
            .Select(s => s.ChunkId)
            .Intersect(plinian.Sources.Select(s => s.ChunkId))
  .ToList();
  
        _output.WriteLine($"\nSame chunks: {sameChunks.Count}");
    }
    
    // Show relationships
    var relationships = graph.GetRelationships(jameson.Id);
    _output.WriteLine($"\nJameson relationships: {relationships.Count}");
}
```

### Priority 3: Fix Entity Matching (After Diagnostic)

Based on diagnostic results, implement one of:

**Fix 1: Increase maxDistance**
```csharp
var relationshipExtractor = new PatternBasedRelationshipExtractor(
    maxDistance: 2000,  // Increased from 500
    minConfidence: 0.5);
```

**Fix 2: Better Alias Handling in HybridEntityExtractor**
```csharp
// Generate bidirectional aliases
if (name.StartsWith("Professor "))
{
    aliases.Add(name[10..]); // "Professor Jameson" ? "Jameson"
    // AND REVERSE
    // "Jameson" should also generate "Professor Jameson"
}
```

**Fix 3: Entity Name Normalization in Comparison**
```csharp
// When comparing ground truth "Professor Jameson" to extracted "Jameson"
// Strip titles before comparison
var normalizedName = RemoveTitles(name);  // "Professor Jameson" ? "Jameson"
```

### Priority 4: Test and Measure

After fixes:
1. Run ground truth comparison test
2. Document new metrics
3. Verify recall improves to 20-30% (intermediate goal)
4. If successful, continue with Priority 5

### Priority 5: Fuzzy Entity Matching (If Needed)

If recall still low, implement Levenshtein distance matching:
```csharp
// In GroundTruthComparison.FindEntity()
entity = graph.Entities.FirstOrDefault(e =>
{
    var distance = LevenshteinDistance(name, e.Name);
    return distance <= 3;  // Allow 3-character difference
});
```

---

## ?? Current Metrics

| Metric | Before Phase 12 | After Priority 1-2 | Target | Status |
|--------|----------------|-------------------|--------|--------|
| Entities Extracted | 533 | **888** (+67%) | - | ? Improved |
| Relationships Extracted | 12,299 | **13,071** (+6%) | - | ? Improved |
| Recall | 2% | **2%** (no change) | 50-60% | ? Needs work |
| Precision | 0.01% | **0.01%** (no change) | 30-40% | ? Needs work |
| F1 Score | 0.04% | **0.04%** (no change) | 40-50% | ? Needs work |
| False Positives | 12,545 | **13,070** (+525) | <100 | ? Worse |

**Conclusion**: Entity extraction is better, but relationship detection is still broken.

---

## ?? Key Insights

1. **Entity Extraction SUCCESS**: We went from missing 3/4 test entities to finding all 4.
2. **Relationship Detection FAILURE**: Despite better patterns, relationships still not found.
3. **Root Cause Unknown**: Need diagnostic test to understand why patterns aren't matching.
4. **False Positives Increasing**: More relationships being created, but wrong ones.

**The Gap**: We have all the pieces (entities, patterns, extractors), but they're not connecting properly. This suggests a **matching/comparison issue**, not an extraction issue.

---

## ?? Success Criteria Tracking

- [ ] Recall improved to 50-60% (currently 2%)
- [x] **Entity extraction improved** (533 ? 888 entities)
- [x] **Pattern library expanded** (17 ? 35+ patterns)
- [ ] Precision improved to 30-40% (currently 0.01%)
- [ ] F1 Score improved to 40-50% (currently 0.04%)
- [ ] False positives reduced to <100 (currently 13,070)
- [ ] Performance maintained (<1s for 100 chunks) - UNKNOWN
- [ ] 30+ tests passing - DEFERRED

---

## ?? Files Modified

### Phase 12 Changes
1. ? `CapitalizationEntityExtractor.cs` - Enhanced with dictionary, titles, better sentence-start handling
2. ? `RelationshipType.cs` - Added 24 new relationship types
3. ? `PatternBasedRelationshipExtractor.cs` - Added 35+ patterns, enhanced documentation

### Next Files to Modify
4. ?? `GroundTruthComparisonTests.cs` - Add diagnostic test
5. ?? `GroundTruthComparison.cs` - May need to improve entity matching
6. ?? `HybridEntityExtractor.cs` - May need bidirectional aliases

---

**Status**: ?? **BLOCKED** - Need diagnostic information to proceed
**Blocker**: Don't know why relationship patterns aren't matching  
**Next Action**: Create and run diagnostic test to show entity names and positions

