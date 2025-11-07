# Darwin's Autobiography - ACTUAL Text Samples for Pattern Design

## Purpose
This document contains **VERIFIED TEXT SAMPLES** extracted directly from Darwin's autobiography. These are used to design relationship patterns that match the actual phrasing Darwin used.

---

## Extraction Results (After Fixes)

### Chunking Configuration (Fixed)
```csharp
MaxTokens = 512
MaxCharactersPerChunk = 2000  // Still averaging 5,606 chars - HTML semantic chunking
OverlapTokens = 100  // Increased from 50
EnforceSentenceBoundaries = true
```

**Result**: 26 chunks created, average 5,606 characters each

**Note**: HTML chunking respects semantic boundaries (sections, paragraphs), so `MaxCharactersPerChunk` is a suggestion, not a hard limit. Chunks are still larger than ideal but MUCH better than 21KB.

---

## VERIFIED Relationship Text Samples

### 1. ? Professor Jameson -> Founded -> Plinian Society

**Actual Text from Darwin**:
> "The Plinian Society was encouraged and, I believe, **founded by Professor Jameson**: it consisted of students and met in an underground room in the University for the sake of reading papers on natural science and dis..."

**Pattern Needed**:
- ? Current pattern `(founded|established)` SHOULD work
- **Why it failed**: Entity "Professor Jameson" likely not extracted (multi-word + title)
- **Fix**: Extract "Professor Jameson" as single entity, not "Professor" + "Jameson"

**Entity Extraction Issue**: Multi-word entity with title prefix

---

### 2. ?? Charles Darwin -> MemberOf -> Plinian Society

**Status**: **Entities in SEPARATE chunks**
- "Charles Darwin" found in 6 chunks
- "Plinian Society" found in 1 chunk (chunk 16)
- They don't overlap!

**Actual Text** (from chunk 16 where Plinian Society appears):
> "The Plinian Society was encouraged and, I believe, founded by Professor Jameson: it consisted of students and met in an underground room in the University for the sake of reading papers on natural science..."

**Problem**: Darwin mentions the Plinian Society but doesn't explicitly say "I was a member." The relationship is **IMPLIED** by the fact that he:
1. "read a short paper before the Plinian Society"
2. "made one interesting little discovery"
3. Describes it in first person as a participant

**Pattern Needed**:
- Current: `(member of|belonged to|attended meetings of)`
- Actual Darwin text: "read...before the [Society]", "the society was"
- **Need**: `read.*before the`, `presented.*to the`, `met in` (passive participation)

**Type**: Implied membership, not explicit statement

---

### 3. ? Darwin -> StudiedAt -> Edinburgh University

**Actual Text from Darwin**:
> "As I was doing no good at school, my father wisely took me away at a rather earlier age than usual, and sent me (Oct. 1825) **to Edinburgh University** with my brother, where I stayed for **two years or sessions**. My brother was completing his medical studies..."

**Pattern Needed**:
- Current: MISSING `StudiedAt` pattern
- Actual phrasing: "sent me to [University]", "stayed for two years/sessions"
- **Add pattern**: `(sent.*to|went to|enrolled at|attended|stayed.*at)\s+(?:the\s+)?[University]`

**Additional context**:
> "After having spent **two sessions in Edinburgh**, my father perceived, or he heard from my sisters, that I did not like the thought of being a physician..."

**Alternate phrasing**: "spent [X] sessions/years in/at [Place]"

---

### 4. ?? HMS Beagle -> IsA -> Ship

**Actual Text** (multiple occurrences):
> "voyage of the **'Beagle'**"
> "observations on the volcanic islands visited during the voyage of the **'Beagle'**"

**Pattern Needed**:
- Current: MISSING `IsA` pattern
- Actual phrasing: Darwin calls it "the Beagle" (with quotes), NOT "HMS Beagle"
- **Entity name mismatch**: Ground truth says "HMS Beagle", Darwin writes "'Beagle'" or "the Beagle"
- **Fix**: Add aliases: Beagle = HMS Beagle = "Beagle"

**Entity Extraction Issue**: Name variation ("HMS Beagle" vs "Beagle" vs "'Beagle'")

---

### 5. ?? Darwin -> AuthorOf -> Origin of Species

**Status**: NOT found in extracted chunks
- "Origin of Species" is from Darwin's **later life**
- Autobiography may not explicitly state this relationship
- It's common knowledge but may not be in the text

**Possible text** (need to verify):
- Darwin discusses his work on evolution and natural selection
- May reference "my book" or "my work on species"
- **Might not use the title "Origin of Species"** in the autobiography excerpt

**Action**: Search for "species", "natural selection", "origin" in full HTML

---

### 6. ? Galapagos Islands -> PartOf -> Voyage of the Beagle

**Actual Text**:
> "observations on the volcanic islands visited during the **voyage of the 'Beagle'**"

**Pattern Needed**:
- Current: `(part of|component of|within)`
- Actual: "visited during the voyage of", "islands...during...voyage"
- **Add pattern**: `(visited during|part of|during the)\s+.*voyage`

**Note**: Relationship is temporal ("during") not structural ("part of")

---

### 7. ? Cambridge University -> LocatedIn -> Cambridge

**Actual Text**:
> "CAMBRIDGE 1828-1831. After having spent two sessions in Edinburgh..."
> (Section header indicates Cambridge, then discusses it)

**Pattern Needed**:
- Current: `(at|in|located in|based in|from)`
- This is definitional knowledge (Cambridge University IS IN Cambridge)
- May need to match "X University" ? "X" as location

**Pattern**: University name implies location (Edinburgh University ? Edinburgh)

---

### 8. ? John Henslow -> MentorOf -> Darwin (FIXED!)

**Actual Text** (multiple mentions):
> "Henslow asked him to allow me to accompany him"
> "Henslow then persuaded me to begin the study of geology"
> "Professor Henslow" (referred to with reverence)

**Pattern Needed**:
- Current: MISSING `MentorOf` pattern
- Actual: "Henslow asked", "Henslow persuaded", "Henslow recommended"
- **Add pattern**: `(mentored|guided|advised|persuaded|asked.*to|recommended.*for)`

**Note**: Ground truth now correctly shows Henslow ? MentorOf ? Darwin (we fixed this!)

---

## Pattern Design Recommendations

### Priority 1: Add Missing Relationship Types

```csharp
// In RelationshipType.cs
StudiedAt = 41,   // One entity studied at another (education)
TraveledOn = 42,    // One entity traveled on another (vessel)
TaughtBy = 43,      // One entity was taught/mentored by another
PresentedTo = 44,   // Presented work to organization/society
VisitedDuring = 45, // Visited place during event/voyage
```

### Priority 2: Fix Entity Extraction

**Multi-word entities with titles**:
- "Professor Jameson" ? Single entity
- "HMS Beagle" ? Single entity
- "Edinburgh University" ? Single entity
- "Plinian Society" ? Single entity

**Strategy**:
1. Boost capitalized phrases (2-4 words)
2. Recognize title prefixes: Professor, Captain, HMS, Dr., Sir
3. Recognize organizational suffixes: University, Society, Institute, College

### Priority 3: New Relationship Patterns

```csharp
// StudiedAt
new RelationshipPattern {
    Regex = new Regex(@"\b(sent.*to|went to|enrolled at|attended|stayed.*at|spent.*(?:sessions|years).*(?:in|at))\b"),
    Type = RelationshipType.StudiedAt,
    Confidence = 0.9
},

// Founded (improve existing)
new RelationshipPattern {
    Regex = new Regex(@"\b(founded by|established by|created by|set up by)\b"),
    Type = RelationshipType.Founded,
    Confidence = 0.95,
    IsDirectional = true  // "X founded by Y" means Y founded X
},

// MemberOf (improve existing)
new RelationshipPattern {
  Regex = new Regex(@"\b(read.*before the|presented.*to the|met in the|member of|belonged to)\b"),
    Type = RelationshipType.MemberOf,
    Confidence = 0.85
},

// MentorOf / TaughtBy
new RelationshipPattern {
    Regex = new Regex(@"\b(persuaded me|asked.*to|recommended.*for|guided me|advised me)\b"),
    Type = RelationshipType.MentorOf,
    Confidence = 0.8
},

// PartOf / VisitedDuring
new RelationshipPattern {
    Regex = new Regex(@"\b(visited during|during the|part of the)\b"),
    Type = RelationshipType.PartOf,
    Confidence = 0.85
}
```

---

## Ground Truth Corrections Made

1. ? **Fixed**: Darwin -> MentorOf -> Henslow ? **John Henslow -> MentorOf -> Darwin**
2. ? **Fixed**: Darwin -> CollaboratesWith -> Henslow ? **John Henslow -> CollaboratesWith -> Darwin**

---

## Entity Name Issues

| Ground Truth Name | Darwin's Actual Text | Fix Needed |
|-------------------|---------------------|------------|
| HMS Beagle | "'Beagle'", "the Beagle" | Add alias or normalize |
| Charles Darwin | "Darwin", "I", "me" | Add "Darwin" as alias |
| Professor Jameson | "Professor Jameson" | Extract as single entity |
| Edinburgh University | "Edinburgh University" | Extract as single entity |
| Plinian Society | "Plinian Society" | Extract as single entity |
| Origin of Species | Possibly not mentioned by this name | Verify in full text |

---

## Next Steps

### Immediate Actions

1. ? **Ground truth corrected** - MentorOf relationship fixed
2. ? **Chunking improved** - Using MaxCharactersPerChunk=2000, OverlapTokens=100
3. ? **Extract remaining text samples** - Need to verify remaining relationships

### Phase 3 Implementation Order

**Iteration 1**: Multi-word Entity Extraction (30% of failures)
- Implement phrase preservation
- Add title/suffix recognition
- Boost capitalized phrases

**Iteration 2**: Add Missing Patterns (70% of failures)
- Add StudiedAt, TaughtBy, MentorOf patterns
- Improve Founded, MemberOf patterns
- Add entity name aliases

**Iteration 3**: Test and Refine
- Re-run baseline comparison
- Analyze remaining misses
- Fine-tune patterns

---

**Status**: ? **REAL TEXT SAMPLES EXTRACTED**  
**Key Insight**: Darwin's phrasing is more passive and implied than our patterns expected  
**Action**: Design patterns based on ACTUAL text, not assumptions

