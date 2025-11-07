# Baseline Results - Ground Truth Comparison

## ?? Test Execution Summary

**Date**: January 2025  
**Test**: `ExtractedGraph_ShouldMatch_GroundTruthRelationships`  
**Document**: Charles Darwin's Autobiography (Project Gutenberg)  
**Status**: ?? **FAILED** - Recall below 10% threshold

---

## ?? Extraction Statistics

| Metric | Value |
|--------|-------|
| Document Size | 160,183 characters |
| Chunks Created | 26 |
| Entities Extracted | 641 |
| Relationships Extracted | 5,032 |
| Ground Truth Relationships | 50 |

---

## ?? Quality Metrics

### Overall Performance

| Metric | Actual | Expected Baseline | Target (Phase 3) | Status |
|--------|--------|-------------------|------------------|---------|
| **Recall** | **2.0%** | 10-30% | 90%+ | ? **Critical** |
| **Precision** | **0.02%** | 5-15% | 60%+ | ? **Critical** |
| **F1 Score** | **0.04%** | 5-20% | 70%+ | ? **Critical** |
| **True Positives** | 1 | 5-15 | 45+ | ? |
| **False Negatives** | 49 | 35-45 | <5 | ? |
| **False Positives** | 5,031 | High | <500 | ? **Extreme** |

### Key Observations

1. ?? **Recall is critically low (2%)** - Only 1 out of 50 relationships found
2. ?? **Precision is near-zero (0.02%)** - 5,031 false positives
3. ?? **Performance worse than expected** - Expected 10-30% recall, got 2%

---

## ?? Failure Analysis

### Top 10 Missed Relationships

| # | Entity1 | Relationship | Entity2 | Reason | Category |
|---|---------|--------------|---------|--------|----------|
| 1 | Professor Jameson | Founded | Plinian Society | No relationship detected | RelationshipNotDetected |
| 2 | Charles Darwin | MemberOf | Plinian Society | No relationship detected | RelationshipNotDetected |
| 3 | HMS Beagle | IsA | Ship | Entity 'HMS Beagle' not extracted | EntityNotExtracted |
| 4 | Darwin | AuthorOf | Origin of Species | No relationship detected | RelationshipNotDetected |
| 5 | Captain FitzRoy | Manages | HMS Beagle | Entity 'HMS Beagle' not extracted | EntityNotExtracted |
| 6 | Edinburgh University | LocatedIn | Edinburgh | No relationship detected | RelationshipNotDetected |
| 7 | Darwin | StudiedAt | Edinburgh University | No relationship detected | RelationshipNotDetected |
| 8 | Robert Grant | WorksFor | Edinburgh University | Entity 'Robert Grant' not extracted | EntityNotExtracted |
| 9 | Darwin | InfluencedBy | Robert Grant | Entity 'Robert Grant' not extracted | EntityNotExtracted |
| 10 | Galapagos Islands | PartOf | Voyage of the Beagle | No relationship detected | RelationshipNotDetected |

### Miss Categories Distribution

| Category | Count | Percentage | Root Cause |
|----------|-------|------------|------------|
| **RelationshipNotDetected** | ~35 | **70%** | Missing relationship patterns (Founded, MemberOf, StudiedAt, etc.) |
| **EntityNotExtracted** | ~15 | **30%** | Multi-word proper nouns missed (HMS Beagle, Robert Grant, etc.) |

---

## ?? Critical Issues Identified

### Issue 1: Missing Relationship Patterns (70% of failures)

**Problem**: Most ground truth relationship types have **no pattern matchers**

**Missing Patterns**:
- ? **Founded** - "Professor Jameson founded Plinian Society"
- ? **MemberOf** - "Darwin was a member of Plinian Society"
- ? **StudiedAt** - "Darwin studied at Edinburgh University"
- ? **IsA** - "HMS Beagle is a ship"
- ? **AuthorOf** - "Darwin authored Origin of Species"
- ? **LocatedIn** - "Edinburgh University located in Edinburgh"
- ? **InfluencedBy** - "Darwin influenced by Robert Grant"
- ? **PartOf** - "Galapagos Islands part of Voyage of the Beagle"

**Current Pattern Coverage**:
```csharp
// PatternBasedRelationshipExtractor currently has:
- Founded ? (exists but not matching)
- MemberOf ? (exists but not matching)
- LocatedIn ? (exists but not matching)
- WorksFor ?
- AuthorOf ? (exists but not matching)
- PartOf ? (exists but not matching)
- Creates, Uses, CollaboratesWith, Owns, Manages, Influences, Supports, RelatedTo
```

**Root Cause**: Patterns exist but are **too strict** or **don't match Darwin's autobiography text**

---

### Issue 2: Multi-Word Entity Extraction (30% of failures)

**Problem**: Multi-word proper nouns not being extracted as single entities

**Failed Extractions**:
- "HMS Beagle" (extracted as separate: "HMS", "Beagle")
- "Robert Grant" (extracted as "Robert", "Grant", or not at all)
- "Professor Jameson" (extracted as "Professor", "Jameson", or not at all)
- "Captain FitzRoy" (extracted as "Captain", "FitzRoy", or not at all)
- "Edinburgh University" (may be split)
- "Cambridge University" (may be split)

**Root Cause**: 
- `CapitalizationEntityExtractor` doesn't preserve multi-word phrases
- `SimpleKeywordExtractor` treats each word separately

---

### Issue 3: False Positives Explosion (5,031 spurious relationships)

**Problem**: Co-occurrence pattern creating massive false positives

**Analysis**:
- 641 entities × 641 entities = ~410,000 possible pairs
- 5,032 relationships extracted (1.2% of possible pairs)
- Co-occurrence within 500 characters generating low-confidence relationships

**Contributing Factors**:
1. **minConfidence = 0.5** - Too low, accepting weak patterns
2. **maxDistance = 500** - Too large, entities far apart treated as related
3. **Proximity relationships** - Every entity near another gets `Mentions` or `CooccursWith`

---

## ?? Required Improvements (Priority Order)

### Priority 1: Fix Relationship Pattern Matching (Target: +40% recall)

**Actions**:
1. Review existing patterns - they exist but don't match
2. Add case variations: "member of" vs "member-of"
3. Add passive voice: "was founded by" vs "founded"
4. Add contextual patterns: "studied at" vs "went to" vs "attended"
5. Test patterns against actual Darwin text excerpts

**Expected Impact**: 70% of misses ? Should fix ~35 relationships ? +70% recall

---

### Priority 2: Fix Multi-Word Entity Extraction (Target: +20% recall)

**Actions**:
1. Implement phrase preservation in `CapitalizationEntityExtractor`
2. Add title recognition: "Professor X", "Captain Y", "HMS Z"
3. Merge adjacent capitalized words: "Robert Grant" as single entity
4. Boost confidence for organizational terms: "University", "Society"

**Expected Impact**: 30% of misses ? Should fix ~15 relationships ? +30% recall

---

### Priority 3: Reduce False Positives (Target: <500 relationships)

**Actions**:
1. Increase `minConfidence` from 0.5 to 0.7
2. Reduce `maxDistance` from 500 to 200 characters
3. Disable proximity relationships for low-confidence entities
4. Require at least one pattern match (no pure co-occurrence)

**Expected Impact**: Reduce from 5,032 to <500 relationships ? Precision 90%+

---

## ?? Phase 3 Improvement Plan

### Iteration 1: Relationship Patterns (Week 3, Day 1-2)

**Goal**: Fix pattern matching to capture ground truth relationships

**Tasks**:
1. ? Add new relationship types: `StudiedAt`, `TraveledOn`, `Discovered`, `TaughtBy`, `InfluencedBy`
2. ? Review and fix existing patterns (Founded, MemberOf, LocatedIn, etc.)
3. ? Add passive voice variants
4. ? Test against Darwin text excerpts

**Expected Result**: Recall 40-60%

---

### Iteration 2: Entity Extraction (Week 3, Day 3-4)

**Goal**: Extract multi-word proper nouns as single entities

**Tasks**:
1. ? Create `HybridEntityExtractorOptions` with phrase preservation
2. ? Implement title recognition (Professor, Captain, HMS)
3. ? Boost organizational terms (University, Society, Institute)
4. ? Merge adjacent capitalized words

**Expected Result**: Recall 60-80%

---

### Iteration 3: False Positive Reduction (Week 3, Day 5)

**Goal**: Improve precision while maintaining recall

**Tasks**:
1. ? Increase confidence threshold (0.5 ? 0.7)
2. ? Reduce max distance (500 ? 200)
3. ? Disable weak proximity relationships

**Expected Result**: Recall 70-90%, Precision 60%+

---

### Iteration 4: Fine-Tuning (Week 4, Day 1-2)

**Goal**: Achieve 90%+ recall target

**Tasks**:
1. ? Increase chunking overlap (50 ? 100 tokens)
2. ? Add entity aliases (Darwin = Charles Darwin)
3. ? Improve entity type classification
4. ? Handle edge cases from remaining misses

**Expected Result**: Recall 90%+, Precision 60%+, F1 70%+

---

## ?? Specific Pattern Improvements Needed

### Pattern Analysis from Ground Truth

| Ground Truth Pattern | Example from Darwin | Current Pattern | Status |
|----------------------|---------------------|-----------------|---------|
| `Founded` | "Jameson founded the Plinian Society" | `(founded\|established\|created)` | ? Not matching |
| `MemberOf` | "I attended meetings of the Plinian Society" | `(member of\|belonged to\|attended)` | ? Not matching |
| `StudiedAt` | "studied at Edinburgh" | **MISSING** | ? No pattern |
| `IsA` | "The Beagle was a ten-gun brig" | **MISSING** | ? No pattern |
| `AuthorOf` | "Darwin wrote the Origin of Species" | `(wrote\|authored\|published)` | ? Not matching |
| `LocatedIn` | "Edinburgh in Scotland" | `(at\|in\|located in)` | ? Not matching |
| `InfluencedBy` | "Grant influenced my views" | **MISSING** | ? No pattern |

**Action Item**: Extract actual sentences from Darwin's autobiography containing these relationships and test patterns against them.

---

## ?? Next Steps

### Immediate Actions (Before Phase 3)

1. ? **Extract text samples** from Darwin containing each relationship type
2. ? **Test current patterns** against these samples to understand why they fail
3. ? **Design improved patterns** based on actual text

### Phase 3 Start Criteria

- ? Baseline results captured
- ? Failure analysis complete
- ? Text samples extracted
- ? Pattern improvements designed
- ? Implementation ready to begin

---

## ?? References

- Ground Truth File: `PanoramicData.Chunker.Tests/TestData/Darwin-GroundTruth.txt`
- Test File: `PanoramicData.Chunker.Tests/Integration/KnowledgeGraph/GroundTruthComparisonTests.cs`
- Comparison Logic: `PanoramicData.Chunker.Tests/Helpers/GroundTruthComparison.cs`

---

**Status**: ?? **BASELINE CAPTURED - CRITICAL ISSUES IDENTIFIED**  
**Next Phase**: Phase 3 - Iterative Improvement  
**Priority**: Fix relationship pattern matching (70% of failures)  
**Target**: Recall 90%+, Precision 60%+

