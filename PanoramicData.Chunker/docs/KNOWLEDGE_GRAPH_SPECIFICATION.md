# Knowledge Graph Support Specification for PanoramicData.Chunker

## Document Information

| Attribute | Value |
|-----------|-------|
| **Specification Version** | 2.0 |
| **Date Created** | January 2025 |
| **Status** | Draft |
| **Storage Strategy** | PostgreSQL + Apache AGE |
| **Implementation Phases** | [Phase 11-26](../MasterPlan.md#knowledge-graph-phases-extended-roadmap) |

---

## Table of Contents

1. [Executive Summary](#1-executive-summary)
2. [Background and Motivation](#2-background-and-motivation)
3. [Core Concepts](#3-core-concepts)
4. [Architecture Overview](#4-architecture-overview)
5. [Data Models](#5-data-models)
6. [Extraction Strategies](#6-extraction-strategies)
7. [Query and Traversal API](#7-query-and-traversal-api)
8. [PostgreSQL + Apache AGE Storage](#8-postgresql--apache-age-storage)
9. [Integration with Existing System](#9-integration-with-existing-system)
10. [Use Cases and Examples](#10-use-cases-and-examples)
11. [Performance Considerations](#11-performance-considerations)
12. [Testing Strategy](#12-testing-strategy)
13. [Future Enhancements](#13-future-enhancements)

---

## 1. Executive Summary

This specification outlines the addition of **Knowledge Graph (KG) capabilities** to PanoramicData.Chunker, enabling the library to extract, represent, and query semantic relationships between entities discovered in documents. The knowledge graph functionality will complement the existing hierarchical chunking system by providing:

- **Entity extraction and recognition** (named entities, concepts, keywords)
- **Relationship discovery** between entities within and across documents
- **Graph-based querying** for semantic search and knowledge exploration
- **RAG enhancement** through entity-aware retrieval and contextual understanding
- **Multi-document knowledge synthesis** across document collections

### Key Benefits

- **Enhanced RAG Performance**: Retrieve not just relevant chunks, but related entities and their context
- **Cross-Document Insights**: Discover relationships and patterns across entire document collections
- **Semantic Search**: Query by concepts, entities, and relationships, not just text similarity
- **Knowledge Synthesis**: Build comprehensive knowledge bases from unstructured documents
- **Graph-Enhanced Embeddings**: Enrich vector embeddings with structural relationship information

### Storage Strategy

This specification adopts **PostgreSQL with Apache AGE extension** as the primary graph storage solution, providing:

- **Existing Infrastructure**: Most organizations already have PostgreSQL
- **Cypher Query Language**: Industry-standard graph query language
- **Hybrid Capabilities**: Combine relational and graph data seamlessly
- **Cost Effective**: No additional infrastructure required
- **Production Ready**: Battle-tested PostgreSQL reliability

> **Implementation Note**: See [Phase 11-26 in Master Plan](../MasterPlan.md#knowledge-graph-phases-extended-roadmap) for the phased rollout strategy.

---

## 2. Background and Motivation

### 2.1. Current State

PanoramicData.Chunker currently excels at:
- Hierarchical document chunking with parent-child relationships
- Multi-format document processing (9 formats supported)
- Token-aware chunking for RAG systems
- Rich metadata extraction and quality metrics

### 2.2. The Knowledge Graph Gap

Current limitations that KG support will address:

1. **Limited Cross-Chunk Relationships**: Only parent-child hierarchical relationships exist
2. **No Entity Recognition**: Important entities (people, places, concepts) are not identified
3. **No Semantic Relationships**: "mentions", "references", "related_to" relationships are missing
4. **Single-Document Focus**: No mechanism to connect knowledge across documents
5. **Text-Only Retrieval**: Vector search doesn't leverage structural relationships

### 2.3. RAG Enhancement Opportunities

Knowledge graphs can dramatically improve RAG systems:

```
Traditional RAG:
Query ? Vector Search ? Top-K Chunks ? LLM ? Response

Graph-Enhanced RAG:
Query ? Vector + Graph Search ? 
  Top-K Chunks + Related Entities + Connected Context ? 
  LLM with enriched context ? 
  Higher quality response
```

### 2.4. Use Case Examples

**Example 1: Technical Documentation**
- Extract entities: APIs, classes, methods, parameters
- Relationships: "implements", "inherits_from", "calls", "depends_on"
- Query: "What classes use the Authentication API?"

**Example 2: Legal Documents**
- Extract entities: parties, dates, clauses, obligations
- Relationships: "party_to", "references_clause", "supersedes"
- Query: "Find all obligations for Party A in contracts after 2023"

**Example 3: Research Papers**
- Extract entities: authors, institutions, concepts, methodologies
- Relationships: "authored_by", "cites", "uses_method", "related_concept"
- Query: "Which papers by MIT researchers cite machine learning methods?"

---

## 3. Core Concepts

### 3.1. Knowledge Graph Components

```
Knowledge Graph = Entities + Relationships + Attributes

Entity: A distinct object, concept, or agent (person, place, thing, idea)
Relationship: A typed connection between two entities
Attribute: Properties and metadata describing entities and relationships
```

### 3.2. Entity Types

Entities will be categorized into types based on document context:

**Universal Types** (all documents):
- `Person` - Named individuals
- `Organization` - Companies, institutions
- `Location` - Places, addresses
- `Date` - Temporal references
- `Concept` - Abstract ideas, topics
- `Keyword` - Important terms

**Technical Document Types**:
- `Class` - Software classes
- `Method` - Functions/methods
- `API` - API endpoints
- `Parameter` - Function parameters
- `Technology` - Technologies, frameworks

**Business Document Types**:
- `Product` - Products/services
- `Metric` - KPIs, measurements
- `Process` - Business processes
- `Department` - Organizational units

**Legal Document Types**:
- `Party` - Legal parties
- `Clause` - Contract clauses
- `Obligation` - Legal obligations
- `Jurisdiction` - Legal jurisdictions

### 3.3. Relationship Types

**Structural Relationships** (document structure):
- `contains` - Chunk contains entity
- `appears_in` - Entity appears in chunk
- `follows` - Sequential relationship
- `child_of` - Hierarchical relationship (existing)

**Semantic Relationships** (meaning):
- `mentions` - Simple co-occurrence
- `references` - Explicit reference
- `related_to` - Semantic similarity
- `synonym_of` - Synonymous entities
- `part_of` - Compositional relationship

**Domain-Specific Relationships**:
- `implements` - Implementation relationship
- `inherits_from` - Inheritance
- `calls` - Method invocation
- `depends_on` - Dependency
- `authored_by` - Authorship
- `employed_by` - Employment
- `located_in` - Location
- `occurred_on` - Temporal

### 3.4. Graph Schema

```
???????????????????????????
?       Entity          ?
?     (Node)   ?
???????????????????????????
? - Id: UUID              ?
? - Type: EntityType      ?
? - Name: String ?
? - NormalizedName: String?
? - ChunkIds: UUID[]      ???????????????
? - Confidence: Double    ?           ?
? - Frequency: Int      ?             ?
? - Properties: JSONB   ?             ?
? - Metadata: JSONB    ?             ?
???????????????????????????  ?
             ?   ?
   ? connected by      ?
             ?          ?
???????????????????????????   ?
?    Relationship         ?    appears_in
?       (Edge)            ?             ?
???????????????????????????  ?
? - Id: UUID              ?  ?
? - Type: RelationshipType?     ?
? - FromEntityId: UUID???????????????????
? - ToEntityId: UUID      ?          ?
? - Weight: Double      ?       ?
? - Confidence: Double    ?     ?
? - IsDirected: Boolean   ?        ?
? - Properties: JSONB     ?    ?
? - Evidence: JSONB[]     ?        ?
???????????????????????????             ?
             ?         ?
             ? references          ?
          ?       ?
??????????????????????????? ?
?     ChunkerBase         ?  ?
?       (Chunk)     ???????????????
???????????????????????????
? - Id: UUID      ?
? - Content: Text   ?
? - SpecificType: String  ?
? - Metadata: JSONB       ?
? - ...   ?
???????????????????????????
```

---

## 4. Architecture Overview

### 4.1. Component Diagram

```
??????????????????????????????????????????????????????????????????
?           PanoramicData.Chunker           ?
?          ?
?????????????????????????????????????????????????????????    ?
?  ?          Existing Chunking Pipeline      ?    ?
?  ?  Document ? Chunker ? Chunks ? Validation ? Result  ?    ?
?  ????????????????????????????????????????????????????????    ?
?        ?           ?
?          ? Chunks      ?
?      ?              ?
?  ????????????????????????????????????????????????????????    ?
?  ?         NEW: Knowledge Graph Pipeline?    ?
?  ?      ?    ?
?  ?  ??????????????????????????????????????????????    ?    ?
?  ?  ?      Entity Extraction        ?    ?    ?
?  ?  ?  - NER (Named Entity Recognition)    ?    ?    ?
?  ?  ?  - Keyword Extraction          ?    ?    ?
?  ?  ?  - Concept Identification          ?    ?    ?
?  ?  ?  - Custom Entity Extractors       ?    ?    ?
?  ???????????????????????????????????????????????    ?    ?
?  ?   ?         ?    ?
?  ?      ?      ?    ?
?  ?  ??????????????????????????????????????????????  ?    ?
?  ?  ?   Relationship Discovery        ?    ?    ?
?  ?  ?  - Co-occurrence Analysis      ?    ?    ?
?  ?  ?  - Dependency Parsing          ?    ?    ?
?  ?  ?  - Coreference Resolution   ?    ?    ?
?  ?  ?  - Custom Relationship Extractors          ?    ?    ?
?  ?  ??????????????????????????????????????????????    ?    ?
?  ?   ?            ?    ?
?  ?             ?       ?    ?
?  ?  ??????????????????????????????????????????????    ?    ?
?  ?  ?       Graph Building   ?    ?    ?
?  ?  ?  - Entity Normalization       ?    ?    ?
?  ?  ?  - Entity Resolution/Merging   ?    ?    ?
?  ?  ?  - Relationship Weighting   ?    ?    ?
?  ?  ?  - Graph Construction          ?  ?    ?
?  ?  ??????????????????????????????????????????????    ?    ?
?  ?           ?     ?    ?
?  ?    ?         ?    ?
?  ?  ??????????????????????????????????????????????    ?    ?
?  ?  ?      PostgreSQL + Apache AGE               ? ?    ?
?  ?  ?  - Entity Storage (Vertices)       ?    ?    ?
?  ?  ?  - Relationship Storage (Edges)  ?    ?    ?
?  ?  ?  - Cypher Query Support         ?    ?    ?
?  ?  ?  - Index Management         ?    ?    ?
?  ?  ??????????????????????????????????????????????    ?    ?
?  ?    ?    ?
?  ????????????????????????????????????????????????????????    ?
?             ??
?           ? Enhanced Result     ?
?    ?                 ?
?  ????????????????????????????????????????????????????????    ?
?  ?      ChunkingResult + KnowledgeGraphResult  ?    ?
?  ?  - Chunks      ?    ?
?  ?  - Entities    ?    ?
?  ?  - Relationships ?    ?
?  ?  - Graph Statistics        ?  ?
?  ????????????????????????????????????????????????????????    ?
?             ?
??????????????????????????????????????????????????????????????????
```

### 4.2. Integration Points

The KG system integrates with existing infrastructure:

1. **ChunkingResult Enhancement**: Add `KnowledgeGraph` property
2. **ChunkingOptions Enhancement**: Add KG-specific configuration
3. **IDocumentChunker Extension**: Optional KG extraction after chunking
4. **Post-Processing Pipeline**: KG extraction as optional post-processing step

### 4.3. Extensibility Model

```csharp
// Plugin architecture for custom extractors
IEntityExtractor
  ?? INamedEntityExtractor
  ?? IKeywordExtractor
  ?? IConceptExtractor
  ?? ICustomEntityExtractor

IRelationshipExtractor
  ?? ICooccurrenceExtractor
  ?? IDependencyExtractor
  ?? ISemanticRelationshipExtractor
  ?? ICustomRelationshipExtractor
```

---

## 5. Data Models

### 5.1. Core Models

```csharp
/// <summary>
/// Represents an entity in the knowledge graph.
/// </summary>
public class Entity
{
    /// <summary>
  /// Unique identifier for this entity.
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Entity type (Person, Organization, Concept, etc.).
    /// </summary>
    public EntityType Type { get; set; }

    /// <summary>
    /// Primary name/label for this entity.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Normalized canonical name for matching.
    /// </summary>
    public string NormalizedName { get; set; } = string.Empty;

    /// <summary>
    /// Alternative names and aliases.
    /// </summary>
    public List<string> Aliases { get; set; } = new();

    /// <summary>
    /// IDs of chunks where this entity appears.
    /// </summary>
    public List<Guid> ChunkIds { get; set; } = new();

    /// <summary>
    /// Confidence score for entity extraction (0-1).
    /// </summary>
    public double Confidence { get; set; } = 1.0;

    /// <summary>
    /// Number of times this entity appears.
    /// </summary>
    public int Frequency { get; set; }

    /// <summary>
    /// Entity importance score (e.g., from PageRank).
    /// </summary>
    public double? ImportanceScore { get; set; }

    /// <summary>
    /// Additional properties specific to this entity.
    /// </summary>
    public Dictionary<string, object> Properties { get; set; } = new();

    /// <summary>
    /// Entity metadata.
    /// </summary>
    public EntityMetadata Metadata { get; set; } = new();

    /// <summary>
    /// Vector embedding for this entity (optional).
 /// </summary>
    public float[]? Embedding { get; set; }

    /// <summary>
    /// Source information (document, chunks).
    /// </summary>
    public List<EntitySource> Sources { get; set; } = new();
}

/// <summary>
/// Represents a relationship between two entities.
/// </summary>
public class Relationship
{
    /// <summary>
    /// Unique identifier for this relationship.
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Relationship type.
    /// </summary>
  public RelationshipType Type { get; set; }

    /// <summary>
    /// Source entity ID.
    /// </summary>
    public Guid FromEntityId { get; set; }

    /// <summary>
    /// Target entity ID.
    /// </summary>
    public Guid ToEntityId { get; set; }

    /// <summary>
    /// Relationship weight/strength (0-1).
    /// </summary>
    public double Weight { get; set; } = 1.0;

    /// <summary>
    /// Confidence score for this relationship (0-1).
    /// </summary>
    public double Confidence { get; set; } = 1.0;

    /// <summary>
    /// Is this relationship directed?
    /// </summary>
    public bool IsDirected { get; set; } = true;

    /// <summary>
    /// Additional properties.
    /// </summary>
    public Dictionary<string, object> Properties { get; set; } = new();

    /// <summary>
    /// Evidence for this relationship (chunk IDs, text spans).
    /// </summary>
    public List<RelationshipEvidence> Evidence { get; set; } = new();

    /// <summary>
    /// Relationship metadata.
    /// </summary>
    public RelationshipMetadata Metadata { get; set; } = new();
}

/// <summary>
/// Represents the complete knowledge graph.
/// </summary>
public class KnowledgeGraph
{
    /// <summary>
    /// All entities in the graph.
  /// </summary>
    public List<Entity> Entities { get; set; } = new();

    /// <summary>
    /// All relationships in the graph.
  /// </summary>
    public List<Relationship> Relationships { get; set; } = new();

    /// <summary>
/// Graph statistics.
    /// </summary>
    public GraphStatistics Statistics { get; set; } = new();

 /// <summary>
    /// Graph metadata.
    /// </summary>
    public GraphMetadata Metadata { get; set; } = new();

    /// <summary>
    /// Schema information (entity types, relationship types).
    /// </summary>
    public GraphSchema Schema { get; set; } = new();

    // Index structures for fast lookup
    private Dictionary<Guid, Entity> _entityIndex = new();
 private Dictionary<Guid, List<Relationship>> _entityRelationships = new();
    private Dictionary<string, List<Entity>> _nameIndex = new();

    /// <summary>
    /// Get entity by ID.
    /// </summary>
    public Entity? GetEntity(Guid id) => _entityIndex.GetValueOrDefault(id);

    /// <summary>
    /// Find entities by name (exact or partial match).
    /// </summary>
    public List<Entity> FindEntities(string name, bool exactMatch = false);

    /// <summary>
    /// Get all relationships for an entity.
    /// </summary>
    public List<Relationship> GetRelationships(Guid entityId);

    /// <summary>
    /// Get relationships of a specific type.
    /// </summary>
    public List<Relationship> GetRelationships(Guid entityId, RelationshipType type);

    /// <summary>
    /// Find shortest path between two entities.
    /// </summary>
    public List<Relationship> FindPath(Guid fromId, Guid toId, int maxHops = 5);

    /// <summary>
    /// Get subgraph containing specified entities.
    /// </summary>
    public KnowledgeGraph GetSubgraph(List<Guid> entityIds, int expansionHops = 1);

  /// <summary>
    /// Merge another graph into this one.
    /// </summary>
    public void Merge(KnowledgeGraph other);

    /// <summary>
    /// Build internal indexes for fast querying.
    /// </summary>
    public void BuildIndexes();
}

/// <summary>
/// Result containing both chunks and knowledge graph.
/// </summary>
public class KnowledgeGraphResult
{
 /// <summary>
    /// The extracted knowledge graph.
    /// </summary>
    public KnowledgeGraph Graph { get; set; } = new();

    /// <summary>
    /// Statistics about graph extraction.
    /// </summary>
    public GraphExtractionStatistics ExtractionStatistics { get; set; } = new();

    /// <summary>
    /// Warnings or issues during extraction.
    /// </summary>
    public List<GraphExtractionWarning> Warnings { get; set; } = new();

    /// <summary>
    /// Indicates if extraction was successful.
    /// </summary>
    public bool Success { get; set; } = true;
}
```

### 5.2. Supporting Models

```csharp
/// <summary>
/// Entity metadata.
/// </summary>
public class EntityMetadata
{
    public string? SourceDocument { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public string? ExtractorName { get; set; }
 public string? ExtractorVersion { get; set; }
    public Dictionary<string, object>? CustomMetadata { get; set; }
}

/// <summary>
/// Information about where an entity was found.
/// </summary>
public class EntitySource
{
    public Guid ChunkId { get; set; }
    public int StartIndex { get; set; }
    public int Length { get; set; }
    public string Context { get; set; } = string.Empty;
}

/// <summary>
/// Relationship metadata.
/// </summary>
public class RelationshipMetadata
{
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string? ExtractorName { get; set; }
    public string? ExtractionMethod { get; set; }
    public Dictionary<string, object>? CustomMetadata { get; set; }
}

/// <summary>
/// Evidence supporting a relationship.
/// </summary>
public class RelationshipEvidence
{
    public Guid ChunkId { get; set; }
public string TextSpan { get; set; } = string.Empty;
    public double Confidence { get; set; } = 1.0;
}

/// <summary>
/// Graph statistics.
/// </summary>
public class GraphStatistics
{
    public int EntityCount { get; set; }
    public int RelationshipCount { get; set; }
    public Dictionary<EntityType, int> EntityTypeDistribution { get; set; } = new();
    public Dictionary<RelationshipType, int> RelationshipTypeDistribution { get; set; } = new();
    public int ConnectedComponents { get; set; }
    public double AverageDegree { get; set; }
    public double GraphDensity { get; set; }
    public int MaxPathLength { get; set; }
}

/// <summary>
/// Graph metadata.
/// </summary>
public class GraphMetadata
{
    public string? Name { get; set; }
 public string? Description { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public string? Version { get; set; }
    public List<string>? SourceDocuments { get; set; }
public Dictionary<string, object>? CustomMetadata { get; set; }
}

/// <summary>
/// Graph schema definition.
/// </summary>
public class GraphSchema
{
    public List<EntityTypeDefinition> EntityTypes { get; set; } = new();
    public List<RelationshipTypeDefinition> RelationshipTypes { get; set; } = new();
    public Dictionary<string, PropertyDefinition> Properties { get; set; } = new();
}

/// <summary>
/// Statistics about graph extraction process.
/// </summary>
public class GraphExtractionStatistics
{
    public TimeSpan ExtractionTime { get; set; }
    public int EntitiesExtracted { get; set; }
    public int RelationshipsExtracted { get; set; }
    public int EntitiesMerged { get; set; }
    public double AverageEntityConfidence { get; set; }
    public double AverageRelationshipConfidence { get; set; }
}

/// <summary>
/// Warning during graph extraction.
/// </summary>
public class GraphExtractionWarning
{
    public string Message { get; set; } = string.Empty;
    public string? Context { get; set; }
    public WarningLevel Level { get; set; }
}

public enum WarningLevel
{
    Info,
    Warning,
    Error
}
```

### 5.3. Enumerations

```csharp
/// <summary>
/// Entity types.
/// </summary>
public enum EntityType
{
    // Universal types
    Person,
    Organization,
    Location,
    Date,
    Time,
    Money,
    Percentage,
    Concept,
    Keyword,
 
    // Technical types
    Class,
    Method,
    Function,
    API,
    Endpoint,
    Parameter,
    Variable,
    Technology,
    Framework,
    Library,
    
    // Business types
    Product,
    Service,
    Metric,
    KPI,
    Process,
Department,
    Role,
    
    // Legal types
    Party,
    Clause,
    Obligation,
    Right,
 Jurisdiction,
    LegalConcept,
    
    // Document types
    Document,
    Section,
    Chapter,
    
    // Custom
    Custom
}

/// <summary>
/// Relationship types.
/// </summary>
public enum RelationshipType
{
    // Structural
    Contains,
    AppearsIn,
    ChildOf,
    PartOf,
    Follows,
    
    // Semantic
    Mentions,
    References,
    RelatedTo,
    SynonymOf,
    AntonymOf,
    
// Technical
    Implements,
    InheritsFrom,
    Calls,
    DependsOn,
    Uses,
    Returns,
    HasParameter,
    ThrowsException,
  
    // Social/Organizational
    AuthoredBy,
    EmployedBy,
    ReportsTo,
    MemberOf,
    LeaderOf,
    
    // Temporal
    Before,
    After,
    During,
    OccurredOn,
    
    // Spatial
  LocatedIn,
    Near,
    
    // Legal
    PartyTo,
    Governs,
    Supersedes,
    Obligates,
    
    // Comparison
    SimilarTo,
    DifferentFrom,

    // Custom
    Custom
}
