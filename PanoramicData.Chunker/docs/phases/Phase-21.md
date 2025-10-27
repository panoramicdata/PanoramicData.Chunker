# Phase 21: Knowledge Graph Foundation

[?? Back to Master Plan](../MasterPlan.md)

---

## Phase Information

| Attribute | Value |
|-----------|-------|
| **Phase Number** | 21 |
| **Status** | ?? **PENDING** |
| **Duration** | 3 weeks |
| **Prerequisites** | Phase 9 (PDF) complete |
| **Test Count** | 30+ |
| **Documentation** | ?? Pending |
| **LOC Estimate** | ~3,500 |

---

## Objective

Establish the foundational data models, interfaces, and basic entity extraction capabilities for the Knowledge Graph system. Set up PostgreSQL + Apache AGE infrastructure.

---

## Why This Phase?

- **Foundation First**: Build core data structures before advanced features
- **PostgreSQL Setup**: Establish database infrastructure early
- **Basic Extraction**: Prove concept with simple keyword extraction
- **Zero Regressions**: Ensure KG is optional and doesn't break existing functionality

---

## Tasks

### 21.1. Core Data Models ? PENDING

- [ ] Implement `Entity` class with all properties
- [ ] Implement `Relationship` class with evidence tracking
- [ ] Implement `KnowledgeGraph` class with indexing
- [ ] Implement `EntityMetadata` and `RelationshipMetadata`
- [ ] Implement `EntitySource` and `RelationshipEvidence`
- [ ] Implement `GraphStatistics` and `GraphMetadata`
- [ ] Implement `GraphSchema` with type definitions
- [ ] Implement `KnowledgeGraphResult` class
- [ ] Implement `GraphExtractionStatistics` class

### 21.2. Enumerations ? PENDING

- [ ] Implement `EntityType` enum (40+ types)
- [ ] Implement `RelationshipType` enum (30+ types)
- [ ] Add XML documentation for all enum values

### 21.3. Core Interfaces ? PENDING

- [ ] Define `IEntityExtractor` interface
- [ ] Define `IRelationshipExtractor` interface
- [ ] Define `IEntityNormalizer` interface
- [ ] Define `IEntityResolver` interface
- [ ] Define `IGraphStore` interface
- [ ] Define `IGraphSerializer` interface
- [ ] Define `INERProvider` interface

### 21.4. Basic Entity Extraction ? PENDING

- [ ] Implement `SimpleKeywordExtractor` class
  - TF-IDF algorithm
  - Stop word filtering
  - Frequency counting
  - Confidence scoring
- [ ] Implement `EntityExtractionPipeline` class
- [ ] Implement `BasicEntityNormalizer` class
- [ ] Implement `EntityResolver` class

### 21.5. Basic Relationship Extraction ? PENDING

- [ ] Implement `CooccurrenceRelationshipExtractor` class
  - Same-chunk co-occurrence
  - Window-based detection
  - Weight calculation
  - Evidence tracking

### 21.6. Knowledge Graph Builder ? PENDING

- [ ] Implement `KnowledgeGraphBuilder` class
- [ ] Entity collection and deduplication
- [ ] Relationship building
- [ ] Index building
- [ ] Statistics calculation

### 21.7. Configuration ? PENDING

- [ ] Add `EnableKnowledgeGraph` to `ChunkingOptions`
- [ ] Implement `KnowledgeGraphOptions` class
- [ ] Add all configuration properties
- [ ] Add validation logic

### 21.8. PostgreSQL + Apache AGE Setup ? PENDING

- [ ] Create database schema SQL scripts
  - `001_create_extension.sql`
  - `002_create_graph.sql`
  - `003_create_tables.sql`
  - `004_create_indexes.sql`
- [ ] Implement `PostgresAgeGraphStore` class (basic)
- [ ] Connection management
- [ ] Basic CRUD operations
- [ ] Transaction support

### 21.9. Integration ? PENDING

- [ ] Extend `ChunkingResult` with `KnowledgeGraphResult`
- [ ] Add KG extraction to chunking pipeline
- [ ] Ensure backward compatibility
- [ ] Add feature flag support

### 21.10. Testing ? PENDING

- [ ] Unit tests for `Entity` class
- [ ] Unit tests for `Relationship` class
- [ ] Unit tests for `KnowledgeGraph` class
- [ ] Unit tests for `SimpleKeywordExtractor`
- [ ] Unit tests for `CooccurrenceRelationshipExtractor`
- [ ] Unit tests for `KnowledgeGraphBuilder`
- [ ] Integration tests for PostgreSQL setup
- [ ] End-to-end test with basic extraction

### 21.11. Documentation ? PENDING

- [ ] Update `MasterPlan.md` with Phase 21
- [ ] Create `Phase-21.md` documentation
- [ ] XML documentation for all public APIs
- [ ] PostgreSQL setup guide
- [ ] Basic usage examples

---

## Deliverables

| Deliverable | Status | Location |
|-------------|--------|----------|
| `Entity` class | ? Pending | `Models/Entity.cs` |
| `Relationship` class | ? Pending | `Models/Relationship.cs` |
| `KnowledgeGraph` class | ? Pending | `Models/KnowledgeGraph.cs` |
| Supporting models | ? Pending | `Models/` |
| `EntityType` enum | ? Pending | `Models/EntityType.cs` |
| `RelationshipType` enum | ? Pending | `Models/RelationshipType.cs` |
| Core interfaces | ? Pending | `Interfaces/` |
| `SimpleKeywordExtractor` | ? Pending | `KnowledgeGraph/Extractors/` |
| `CooccurrenceRelationshipExtractor` | ? Pending | `KnowledgeGraph/Extractors/` |
| `KnowledgeGraphBuilder` | ? Pending | `KnowledgeGraph/` |
| `KnowledgeGraphOptions` | ? Pending | `Configuration/` |
| PostgreSQL schema scripts | ? Pending | `sql/schema/` |
| `PostgresAgeGraphStore` | ? Pending | `KnowledgeGraph/Storage/` |
| 30+ unit tests | ? Pending | `Tests/Unit/KnowledgeGraph/` |
| Integration tests | ? Pending | `Tests/Integration/KnowledgeGraph/` |
| Documentation | ? Pending | `docs/` |

---

## Technical Details

### Entity Extraction Algorithm

```
1. Input: List<ChunkerBase> chunks
2. For each chunk:
   a. Tokenize content
   b. Remove stop words
   c. Calculate word frequencies
 d. Apply TF-IDF weighting
3. Select top N terms as keyword entities
4. Assign confidence scores based on TF-IDF
5. Track source chunks for each entity
6. Output: List<Entity>
```

### Co-occurrence Relationship Algorithm

```
1. Input: List<Entity> entities, List<ChunkerBase> chunks
2. For each chunk:
   a. Find all entities appearing in chunk
   b. For each entity pair:
      - Create "mentions" relationship
      - Weight based on proximity
   - Track chunk as evidence
3. Aggregate relationships across chunks
4. Calculate confidence scores
5. Output: List<Relationship>
```

### PostgreSQL Schema Design

```
kg_entities (relational table)
?? id: UUID PRIMARY KEY
?? type: VARCHAR(50)
?? name: VARCHAR(500)
?? normalized_name: VARCHAR(500)
?? confidence: DECIMAL(3,2)
?? frequency: INT
?? properties: JSONB
?? metadata: JSONB

kg_relationships (relational table)
?? id: UUID PRIMARY KEY
?? type: VARCHAR(50)
?? from_entity_id: UUID FK
?? to_entity_id: UUID FK
?? weight: DECIMAL(3,2)
?? confidence: DECIMAL(3,2)
?? properties: JSONB

AGE Graph (knowledge_graph)
?? Entity vertices (sync with kg_entities)
?? Relationship edges (sync with kg_relationships)
```

---

## Success Criteria

? **Data Models**:
- All core classes implemented with XML docs
- All enumerations defined
- All interfaces defined

? **Basic Extraction**:
- Extract keyword entities from chunks
- Build co-occurrence relationships
- Confidence scores assigned

? **PostgreSQL + AGE**:
- Database schema created
- Extension installed and tested
- Basic CRUD operations working
- Graph stored in AGE

? **Integration**:
- KG extraction optional (feature flag)
- Zero regressions in existing tests
- Backward compatible API

? **Testing**:
- 30+ unit tests passing
- Integration tests with PostgreSQL
- All tests documented

? **Documentation**:
- Phase documentation complete
- API documentation (XML)
- PostgreSQL setup guide

---

## Performance Targets

| Operation | Target | Notes |
|-----------|--------|-------|
| Entity extraction (100 chunks) | < 2 seconds | Simple keyword extraction |
| Relationship extraction | < 1 second | Co-occurrence only |
| Graph building | < 500ms | 100 entities, 50 relationships |
| PostgreSQL insert (100 entities) | < 200ms | Bulk insert |
| PostgreSQL query (simple) | < 50ms | Single entity lookup |

---

## Dependencies

**External Libraries**:
- `Npgsql` 8.0+ (PostgreSQL driver)
- `System.Text.Json` (included in .NET 9)

**Database**:
- PostgreSQL 11+ installed
- Apache AGE extension installed
- Database created with AGE enabled

**Internal**:
- All existing chunking infrastructure
- Token counting system (existing)

---

## Example Usage

```csharp
// Enable knowledge graph extraction
var options = new ChunkingOptions
{
    MaxTokens = 512,
    EnableKnowledgeGraph = true,
    KnowledgeGraphOptions = new KnowledgeGraphOptions
    {
      EnableKeywordExtraction = true,
        EnableRelationshipExtraction = true,
        MinEntityConfidence = 0.5
    }
};

// Chunk document with KG extraction
var result = await DocumentChunker.ChunkFileAsync("document.pdf", options);

// Access knowledge graph
if (result.KnowledgeGraph?.Success == true)
{
    var graph = result.KnowledgeGraph.Graph;
    
    Console.WriteLine($"Entities: {graph.Entities.Count}");
    Console.WriteLine($"Relationships: {graph.Relationships.Count}");
 
    // Find keyword entities
    var keywords = graph.Entities
     .Where(e => e.Type == EntityType.Keyword)
        .OrderByDescending(e => e.Frequency)
        .Take(10)
    .ToList();
    
    foreach (var keyword in keywords)
    {
        Console.WriteLine($"{keyword.Name}: {keyword.Frequency} occurrences");
    }
}
```

---

## Migration Guide

No migrations required - this is a new optional feature.

**For Existing Users**:
- Knowledge graph extraction is disabled by default
- No breaking changes to existing API
- New properties added to `ChunkingResult` (backward compatible)

**For New Users**:
- Install PostgreSQL + Apache AGE
- Run schema migration scripts
- Enable knowledge graph in options

---

## Status: **?? PENDING**

**Ready to Start**: After Phase 9 complete

**Estimated Start Date**: Q1 2025

---

[?? Back to Master Plan](../MasterPlan.md) | [Next Phase: Phase 22 ?](Phase-22.md)
