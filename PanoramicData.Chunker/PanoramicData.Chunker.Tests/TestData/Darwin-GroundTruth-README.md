# Darwin Ground Truth Dataset - Annotation Guidelines

## Overview

This document describes the annotation guidelines used to create the ground truth dataset for validating the knowledge graph extraction pipeline.

## Source Document

**Title**: The Autobiography of Charles Darwin  
**URL**: https://www.gutenberg.org/files/2010/2010-h/2010-h.htm  
**Publisher**: Project Gutenberg  
**Status**: Public Domain

## Annotation Process

### Step 1: Reading and Selection
Read the autobiography systematically, focusing on these sections:
- **Childhood** (Early life in Shrewsbury)
- **Edinburgh University** (1825-1827): Medical studies, Plinian Society
- **Cambridge University** (1828-1831): Study for clergy, meeting Henslow
- **The Voyage of the Beagle** (1831-1836): Five-year circumnavigation
- **Post-Voyage Work**: Barnacles, geological studies
- **Later Life**: Writing Origin of Species, correspondence with scientists

### Step 2: Relationship Extraction
Extract relationships that meet these criteria:
1. **Explicitly stated** in the text OR
2. **Strongly implied** by context OR
3. **Reasonably inferred** from multiple mentions

### Step 3: Confidence Scoring

| Confidence | Criteria | Example |
|------------|----------|---------|
| **1.0** | Explicitly stated with clear language | "Darwin founded the society" |
| **0.9** | Strongly implied or stated indirectly | "Darwin attended meetings regularly" |
| **0.8** | Reasonable inference from context | "Darwin was influenced by Grant's ideas" |
| **0.7 or below** | Weak inference | **Avoid** - too speculative |

### Step 4: Relationship Type Selection

Choose the **most specific** relationship type available:

**Hierarchy of Specificity**:
- `Founded` > `MemberOf` > `RelatedTo`
- `StudiedAt` > `LocatedIn`
- `AuthorOf` > `Wrote` > `Mentions`
- `MentorOf` > `CollaboratesWith` > `Knows`

**Example**:
- ? Wrong: `Darwin RelatedTo Edinburgh University`
- ? Correct: `Darwin StudiedAt Edinburgh University`

## Relationship Types Used

### People Relationships
- `FatherOf`, `GrandfatherOf`, `MarriedTo` - Family relationships
- `MentorOf`, `TaughtBy`, `InfluencedBy` - Educational relationships
- `CollaboratesWith`, `Corresponded` - Professional relationships
- `Invited`, `SupportedBy` - Interaction relationships

### Organizational Relationships
- `MemberOf` - Membership in organization
- `WorksFor` - Employment relationship
- `Founded`, `Established` - Creation relationship

### Location Relationships
- `LocatedIn`, `PartOf` - Geographic hierarchy
- `BornIn`, `LivedIn` - Residence
- `VisitedBy`, `Visited` - Travel

### Work Relationships
- `AuthorOf`, `Wrote` - Authorship
- `Studied`, `StudiedBy` - Research focus
- `Collected`, `Observed`, `Discovered` - Scientific activities

### Vessel/Travel Relationships
- `TraveledOn` - Voyaged on vessel
- `Manages`, `Commands` - Leadership of vessel

### Conceptual Relationships
- `Developed`, `DiscoveredBy` - Intellectual contribution
- `Influenced` - Impact on field

## Quality Criteria

### ? Include relationships that are:
- **Factually accurate** (verified in text)
- **Important** to Darwin's story
- **Clearly stated** or strongly implied
- **Diverse** in type and category
- **Verifiable** by future annotators

### ? Exclude relationships that are:
- **Speculative** or weakly inferred
- **Trivial** or unimportant
- **Ambiguous** in direction or meaning
- **Redundant** (duplicate information)
- **Too general** when specific type available

## Examples

### Good Annotations

```tsv
Entity1	RelationType	Entity2	Confidence	Section	Notes
Darwin	StudiedAt	Edinburgh University	1.0	Education	Medical studies 1825-1827
Professor Jameson	Founded	Plinian Society	1.0	Edinburgh	Explicitly stated in text
Darwin	TraveledOn	HMS Beagle	1.0	Voyage	Five-year voyage 1831-1836
```

**Why good**:
- Specific relationship types
- High confidence (explicit or clear)
- Important to Darwin's biography
- Well-documented with section and notes

### Poor Annotations (Avoid)

```tsv
# Example 1: Too vague
Darwin	RelatedTo	Scotland	0.6	Education	Visited once

# Example 2: Too speculative
Darwin	InfluencedBy	Random Person	0.5	Unknown	Might have met

# Example 3: Wrong type selection
Darwin	Mentions	HMS Beagle	1.0	Voyage	Should be "TraveledOn"
```

**Why poor**:
- Low confidence or speculative
- Vague relationship types
- Wrong type selection
- Missing important context

## Target Distribution

Aim for balanced coverage across categories:

| Category | Target Count | Purpose |
|----------|--------------|---------|
| People | 15-20 | Test person entity extraction |
| Organizations | 10-15 | Test organization detection |
| Places | 10-15 | Test location extraction |
| Concepts | 5-10 | Test abstract entity extraction |
| Works | 5-10 | Test publication detection |
| Vessels | 2-5 | Test specialized entities |
| Events | 5-10 | Test temporal relationships |

**Total**: 50-100 relationships

## Validation Checklist

Before finalizing an annotation, verify:

- [ ] Relationship is factually accurate (found in text)
- [ ] Confidence score is appropriate (1.0, 0.9, or 0.8)
- [ ] Relationship type is most specific available
- [ ] Section field indicates where relationship appears
- [ ] Notes provide justification or context
- [ ] Entity names match how they appear in text
- [ ] No duplicate relationships
- [ ] TSV format is correct (6 columns, tab-separated)

## Common Pitfalls

### Pitfall 1: Over-inference
**Problem**: Adding relationships not clearly stated in text  
**Solution**: Only include if confidence ? 0.8

### Pitfall 2: Wrong entity names
**Problem**: Using modern names instead of text names  
**Solution**: Use names as they appear in document ("HMS Beagle", not "Beagle")

### Pitfall 3: Redundant relationships
**Problem**: Multiple similar relationships between same entities  
**Solution**: Choose the most specific/important one

### Pitfall 4: Missing context
**Problem**: No notes explaining why relationship was chosen  
**Solution**: Always add justification in Notes column

## Revision Process

1. **First pass**: Extract 50-75 relationships quickly
2. **Review**: Check against quality criteria
3. **Refinement**: Add 25-50 more to reach target
4. **Validation**: Verify TSV format, no duplicates
5. **Final check**: Ensure balanced distribution

## File Format

**Filename**: `Darwin-GroundTruth.txt`

**Format**: TSV (Tab-Separated Values)

**Columns**:
1. `Entity1` - First entity (source)
2. `RelationType` - Relationship type (PascalCase)
3. `Entity2` - Second entity (target)
4. `Confidence` - Confidence score (0.8-1.0)
5. `Section` - Document section
6. `Notes` - Justification/context

**Example Line**:
```tsv
Darwin	StudiedAt	Cambridge University	1.0	Education	Christ's College (1828-1831)
```

## References

- Darwin's Autobiography: https://www.gutenberg.org/files/2010/2010-h/2010-h.htm
- RelationshipType enum: `PanoramicData.Chunker/Models/KnowledgeGraph/RelationshipType.cs`
- EntityType enum: `PanoramicData.Chunker/Models/KnowledgeGraph/EntityType.cs`

---

**Version**: 1.0  
**Date**: January 2025  
**Annotators**: Development Team  
**Status**: Complete

