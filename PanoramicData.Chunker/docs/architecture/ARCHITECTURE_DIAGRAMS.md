# PanoramicData.Chunker - Architecture Diagrams

## Table of Contents
1. [High-Level System Architecture](#high-level-system-architecture)
2. [Multi-Tenant Data Architecture](#multi-tenant-data-architecture)
3. [Knowledge Graph Architecture](#knowledge-graph-architecture)
4. [Sequence Diagrams](#sequence-diagrams)
   - [Use Case 1: Help Query](#use-case-1-help-query)
   - [Use Case 2: SQL Query Assistance](#use-case-2-sql-query-assistance)
   - [Use Case 3: OData Query Assistance](#use-case-3-odata-query-assistance)
5. [Component Interaction Diagrams](#component-interaction-diagrams)

---

## High-Level System Architecture

### Overview Diagram

```mermaid
graph TB
    subgraph "Client Layer"
     UI[UI Chat Interface]
        API[REST API / OData]
    end
    
    subgraph "Microsoft Agent Framework"
     MAF[Agent Orchestrator]
 AgentB[Business Agent]
        Tools[Toolset]
     Config[Agent Configuration]
        RBAC[RBAC Service]
    end
    
    subgraph "MCP Layer (Model Context Protocol)"
        MCP[MCP Server]
        CLMS[Classification Service]
        RET[Retrieval Orchestrator]
        STO[Storage Orchestrator]
    end
    
    subgraph "PanoramicData.Chunker Core"
        Factory[ChunkerFactory]
     LLM[LLM Enricher]
        KG[Knowledge Graph Builder]
        EMB[Embedding Generator]
    end
    
    subgraph "Storage Layer - Central (C)"
        C_Models[Shared Models]
        C_Schema[KG Schema]
        C_Prompts[Prompt Templates]
    end
    
    subgraph "Storage Layer - Tenant A (Tn-A)"
        TA_VDB[(Vector DB<br/>Embeddings)]
        TA_PG[(PostgreSQL + AGE<br/>Knowledge Graph)]
      TA_RDBMS[(RDBMS<br/>Metadata)]
        TA_FS[(Filesystem<br/>Raw Docs)]
    end
    
    subgraph "Storage Layer - Tenant B (Tn-B)"
        TB_VDB[(Vector DB<br/>Embeddings)]
        TB_PG[(PostgreSQL + AGE<br/>Knowledge Graph)]
        TB_RDBMS[(RDBMS<br/>Metadata)]
     TB_FS[(Filesystem<br/>Raw Docs)]
    end
    
    UI --> MAF
    API --> MCP
    
    MAF --> Tools
    MAF --> AgentB
    MAF --> Config
    MAF --> RBAC
    
    Tools --> MCP
    AgentB --> MCP
    
    MCP --> CLMS
    MCP --> RET
    MCP --> STO
    
    RET --> Factory
    RET --> LLM
    RET --> KG
    
    STO --> Factory
    
    Factory --> EMB
    LLM --> C_Prompts
    KG --> C_Schema
    EMB --> C_Models
    
    RBAC -.Tenant A.-> TA_VDB
    RBAC -.Tenant A.-> TA_PG
    RBAC -.Tenant A.-> TA_RDBMS
    RBAC -.Tenant A.-> TA_FS
    
    RBAC -.Tenant B.-> TB_VDB
    RBAC -.Tenant B.-> TB_PG
    RBAC -.Tenant B.-> TB_RDBMS
  RBAC -.Tenant B.-> TB_FS
    
    STO --> TA_FS
    STO --> TB_FS
    
    RET --> TA_VDB
    RET --> TA_PG
    RET --> TB_VDB
    RET --> TB_PG
    
    style MAF fill:#e1f5e1
    style MCP fill:#e1e5f5
    style Factory fill:#f5e1e1
 style TA_PG fill:#ffe1cc
    style TB_PG fill:#ffe1cc
```

---

## Multi-Tenant Data Architecture

### Tenant Isolation Model

```mermaid
graph TB
    subgraph "Tenant Routing Layer"
   Router[Tenant Router<br/>X-Tenant-ID Header]
    TenantSvc[Tenant Service]
    end
    
    subgraph "Central Resources (Shared)"
        C_Models[Embedding Models<br/>GPT-4, etc.]
        C_Schema[KG Ontology<br/>Entity/Relationship Types]
        C_Prompts[Prompt Library<br/>Templates]
        C_TokenCounter[Token Counters<br/>OpenAI, Claude]
    end
    
    subgraph "Tenant A Data Partition"
        TA_Schema[PostgreSQL Schema: tenant_a]
     TA_Partition[Vector DB Partition: tenant_a]
        TA_Storage[Blob Storage: tenant-a/]
 TA_Cache[Redis Cache: tenant_a:*]
    end
  
    subgraph "Tenant B Data Partition"
        TB_Schema[PostgreSQL Schema: tenant_b]
        TB_Partition[Vector DB Partition: tenant_b]
        TB_Storage[Blob Storage: tenant-b/]
        TB_Cache[Redis Cache: tenant_b:*]
    end
    
    subgraph "Data Sovereignty Controls"
        RLS[Row-Level Security]
Audit[Audit Logger]
        Encryption[Encryption Service]
    end
    
    Router --> TenantSvc
    
  TenantSvc --> C_Models
    TenantSvc --> C_Schema
    TenantSvc --> C_Prompts
    TenantSvc --> C_TokenCounter
    
    TenantSvc --> TA_Schema
    TenantSvc --> TA_Partition
    TenantSvc --> TA_Storage
    TenantSvc --> TA_Cache
    
    TenantSvc --> TB_Schema
    TenantSvc --> TB_Partition
    TenantSvc --> TB_Storage
    TenantSvc --> TB_Cache
  
    TA_Schema --> RLS
    TB_Schema --> RLS
    
    TenantSvc --> Audit
  TA_Storage --> Encryption
    TB_Storage --> Encryption
    
    style C_Models fill:#d4edda
    style C_Schema fill:#d4edda
style TA_Schema fill:#fff3cd
    style TB_Schema fill:#f8d7da
    style RLS fill:#cfe2ff
    style Audit fill:#cfe2ff
```

### PostgreSQL Multi-Tenant Schema

```mermaid
erDiagram
    CENTRAL_SCHEMA {
        int entity_type_id PK
        string name
        string description
        json schema
    }
    
    CENTRAL_RELATIONSHIP_TYPES {
        int relationship_type_id PK
        string name
        string description
        json constraints
 }
    
    TENANT_A_ENTITIES {
        uuid entity_id PK
        string tenant_id
        int entity_type_id FK
        string name
        json properties
        timestamp created_at
    }
    
    TENANT_A_RELATIONSHIPS {
        uuid relationship_id PK
   string tenant_id
        uuid source_entity_id FK
  uuid target_entity_id FK
        int relationship_type_id FK
        json properties
        float confidence
    }
    
    TENANT_A_CHUNKS {
    uuid chunk_id PK
 string tenant_id
        string document_id
        string chunk_type
   text content
        json metadata
     vector embedding
}
    
    TENANT_B_ENTITIES {
        uuid entity_id PK
        string tenant_id
     int entity_type_id FK
        string name
     json properties
        timestamp created_at
    }
    
    CENTRAL_SCHEMA ||--o{ TENANT_A_ENTITIES : defines
    CENTRAL_SCHEMA ||--o{ TENANT_B_ENTITIES : defines
    CENTRAL_RELATIONSHIP_TYPES ||--o{ TENANT_A_RELATIONSHIPS : defines
    
    TENANT_A_ENTITIES ||--o{ TENANT_A_RELATIONSHIPS : source
    TENANT_A_ENTITIES ||--o{ TENANT_A_RELATIONSHIPS : target
    TENANT_A_CHUNKS ||--o{ TENANT_A_ENTITIES : extracted_from
```

---

## Knowledge Graph Architecture

### Knowledge Graph Component Model

```mermaid
graph TB
    subgraph "Phase 10: LLM Integration"
        LLM[LLM Provider]
 Summary[Chunk Summarizer]
        Keywords[Keyword Extractor]
      PrelimNER[Preliminary NER]
    end
    
    subgraph "Phase 11: KG Foundation"
        KGModels[Graph Models<br/>Node, Edge, Graph]
     BasicExtract[Basic Entity Extractor]
  PGAGE[PostgreSQL + AGE]
     Migration[Schema Migration]
    end
    
    subgraph "Phase 12: Named Entity Recognition"
        NER[LLM-based NER]
        EntityTypes[Person, Org, Location<br/>Product, Event, Date]
    Deduplication[Entity Deduplication]
        Confidence[Confidence Scoring]
  end
    
    subgraph "Phase 23: Relationships"
     RelExtract[Relationship Extractor]
        Coref[Coreference Resolution]
        DepParse[Dependency Parsing]
        Domain[Domain-Specific Rules]
    end
    
    subgraph "Phase 24: Query API"
        LINQ[LINQ-style API]
        Cypher[Cypher Query Engine]
        Traversal[Graph Traversal]
        Algorithms[PageRank, Community Detection]
    end
    
    subgraph "Phase 15: Persistence"
     Serialize[Graph Serialization<br/>JSON-LD, RDF, GraphML]
        Import[Bulk Import/Export]
        Versioning[Graph Versioning]
    end
    
    subgraph "Phase 16: RAG Enhancement"
        HybridSearch[Hybrid Search<br/>Vector + Graph]
 ContextExpand[Context Expansion]
        Reranking[Graph-Aware Reranking]
    end
    
    LLM --> Summary
    LLM --> Keywords
    LLM --> PrelimNER
    
PrelimNER --> BasicExtract
    BasicExtract --> KGModels
    KGModels --> PGAGE
    
    LLM --> NER
    NER --> EntityTypes
    EntityTypes --> Deduplication
    Deduplication --> Confidence
    Confidence --> PGAGE
    
    EntityTypes --> RelExtract
    RelExtract --> Coref
    RelExtract --> DepParse
    RelExtract --> Domain
    Domain --> PGAGE
    
    PGAGE --> LINQ
    PGAGE --> Cypher
    LINQ --> Traversal
    Cypher --> Traversal
    Traversal --> Algorithms
    
    PGAGE --> Serialize
    Serialize --> Import
Import --> Versioning
    
    Traversal --> HybridSearch
    PGAGE --> ContextExpand
    ContextExpand --> Reranking
    
    style LLM fill:#e1f5e1
    style PGAGE fill:#ffe1cc
    style HybridSearch fill:#e1e5f5
```

### Knowledge Graph Data Flow

```mermaid
flowchart LR
    subgraph "Document Ingestion"
        Doc[Document] --> Chunker[Chunker]
        Chunker --> Chunks[Chunks]
    end
    
    subgraph "LLM Enrichment (Phase 10)"
        Chunks --> Summarize[Summarize]
        Chunks --> Extract[Extract Keywords]
        Chunks --> PrelimNER[Preliminary NER]
    end
    
    subgraph "Entity Extraction (Phase 12)"
        PrelimNER --> NER[Full NER]
        NER --> Entities[Entities<br/>Person, Org, etc.]
        Entities --> Dedupe[Deduplicate]
    end
    
    subgraph "Relationship Extraction (Phase 23)"
   Entities --> RelEx[Relationship Extractor]
        Chunks --> Coref[Coreference]
        Coref --> RelEx
        RelEx --> Relationships[Relationships<br/>works_at, located_in]
    end
    
    subgraph "Graph Construction (Phase 11-25)"
        Dedupe --> Nodes[(Graph Nodes)]
        Relationships --> Edges[(Graph Edges)]
 Nodes --> PG[(PostgreSQL<br/>+ AGE)]
        Edges --> PG
    end
    
    subgraph "Retrieval (Phase 16)"
        Query[User Query] --> VecSearch[Vector Search]
    Query --> GraphSearch[Graph Search]
        VecSearch --> Hybrid[Hybrid Ranker]
     GraphSearch --> Hybrid
        PG --> GraphSearch
        Hybrid --> Results[Ranked Results]
    end
    
    style Doc fill:#e1f5e1
    style PG fill:#ffe1cc
    style Results fill:#e1e5f5
```

---

## Sequence Diagrams

### Use Case 1: Help Query
*"How do I view last February's report?"*

```mermaid
sequenceDiagram
    participant User
    participant UI
    participant MAF as Microsoft Agent Framework
    participant RBAC
    participant RET as Retrieval Orchestrator
    participant VDB as Vector DB (Tenant-A)
    participant KG as Knowledge Graph (Tenant-A)
    participant LLM
    participant Audit
    
    User->>UI: "How do I view last February's report?"
    UI->>MAF: Query + X-Tenant-ID: tenant-a
    
    MAF->>RBAC: Validate User + Tenant Access
  RBAC-->>MAF: ? Authorized
    
    MAF->>RET: Retrieve Context (tenant-a)
    
    par Hybrid Search
        RET->>VDB: Vector Search ("February report")
 VDB-->>RET: Top-K Chunks (k=10)
    and
 RET->>KG: Graph Query (MATCH entities)
      KG-->>RET: Related Entities (Report, Date, User)
    end
    
    RET->>RET: Merge & Rerank Results
    RET-->>MAF: Ranked Context (5 chunks + entities)
    
    MAF->>LLM: Generate Response + Context
    LLM-->>MAF: "To view February's report, go to Reports > 2024 > February..."
    
    MAF->>Audit: Log Query + Response
    Audit-->>MAF: ? Logged
    
    MAF-->>UI: Response + Sources
    UI-->>User: Display Answer + Citations
    
    Note over User,Audit: Total Time: ~800ms
```

### Use Case 2: SQL Query Assistance
*"How many vulnerabilities over 7.0 were there last month?"*

```mermaid
sequenceDiagram
    participant User
    participant UI
    participant MAF
    participant RBAC
    participant AgentB as Business Agent
    participant KG as Knowledge Graph
    participant SchemaService
    participant SQLGenerator
    participant RDBMS
    participant Audit
    
    User->>UI: "How many vulnerabilities over 7.0 last month?"
    UI->>MAF: Query + X-Tenant-ID: tenant-a
    
    MAF->>RBAC: Validate User + Tenant
    RBAC-->>MAF: ? Authorized (read-only)
    
    MAF->>AgentB: Analyze Intent (SQL query needed)
    AgentB-->>MAF: Intent: Count vulnerabilities (CVSS > 7.0, date filter)
    
    MAF->>KG: Get Schema Entities (Vulnerability, CVSS)
    KG-->>MAF: Entities: [Vulnerability{cvss_score, discovered_date}]
    
    MAF->>SchemaService: Get RDBMS Schema (tenant-a)
    SchemaService-->>MAF: Table: vulnerabilities (cvss_score FLOAT, discovered_at TIMESTAMP)
    
 MAF->>SQLGenerator: Generate SQL
    SQLGenerator-->>MAF: SELECT COUNT(*) FROM vulnerabilities WHERE cvss_score > 7.0 AND discovered_at >= DATE_SUB(NOW(), INTERVAL 1 MONTH)
    
    MAF->>User: "I'll run this query: [SQL]. Continue?"
    User->>UI: "Yes"
    
    UI->>MAF: Execute Query
    MAF->>RBAC: Validate Query (no DELETE/UPDATE)
  RBAC-->>MAF: ? Query Safe
    
MAF->>RDBMS: Execute SQL (tenant-a connection)
    RDBMS-->>MAF: Result: 42 vulnerabilities
    
    MAF->>Audit: Log Query + Result + User Confirmation
  Audit-->>MAF: ? Logged
    
    MAF-->>UI: "42 vulnerabilities with CVSS > 7.0 in the last month"
    UI-->>User: Display Result + Query Used
    
    Note over User,Audit: Guardrail: User confirmation required
```

### Use Case 3: OData Query Assistance
*"Which customers are currently over 7.0 CVSS?"*

```mermaid
sequenceDiagram
    participant User
    participant UI
    participant MAF
    participant RBAC
participant MCP
    participant KG as Knowledge Graph
    participant ODataService
    participant API as OData API
    participant Audit
  
    User->>UI: "Which customers are over 7.0 CVSS?"
 UI->>MAF: Query + X-Tenant-ID: tenant-a
    
    MAF->>RBAC: Validate User + Tenant
 RBAC-->>MAF: ? Authorized
    
    MAF->>MCP: Analyze Query (OData needed)
    MCP-->>MAF: Intent: Filter Customers by CVSS
    
    MAF->>KG: Get Relationships (Customer -has-> Vulnerability)
    KG-->>MAF: Graph: Customer --has_vulnerability--> Vulnerability{cvss_score}
    
    MAF->>ODataService: Get OData Schema
    ODataService-->>MAF: Entities: Customer, Vulnerability (navigable)
    
    MAF->>ODataService: Generate OData Query
    ODataService-->>MAF: /Customers?$filter=Vulnerabilities/any(v: v/CvssScore gt 7.0)&$expand=Vulnerabilities($filter=CvssScore gt 7.0)
    
    MAF->>User: "I'll query: [OData URL]. Continue?"
    User->>UI: "Yes"
    
    UI->>MAF: Execute Query
    MAF->>RBAC: Validate RBAC (tenant-a, read access)
  RBAC-->>MAF: ? Authorized
    
    MAF->>API: GET OData Query (tenant-a context)
    API-->>MAF: Results: [Customer1, Customer2, ...]
    
    MAF->>Audit: Log Query + Result Count + User ID
 Audit-->>MAF: ? Logged
    
    MAF-->>UI: "5 customers have vulnerabilities over 7.0: [List]"
    UI-->>User: Display Table + Export Option
  
    Note over User,Audit: Guardrail: RBAC validated, audit logged
```

### Document Ingestion Flow (Cold Path)

```mermaid
sequenceDiagram
    participant User
    participant API
    participant MCP
    participant RBAC
    participant Chunker as ChunkerFactory
    participant LLM as LLM Enricher
    participant NER as Entity Extractor
    participant KG as KG Builder
    participant VDB as Vector DB
    participant PG as PostgreSQL+AGE
    participant FS as File Storage
    participant Audit
    
    User->>API: Upload Document (tenant-a)
    API->>MCP: Ingest Document + Metadata
    
    MCP->>RBAC: Validate Upload Permission
    RBAC-->>MCP: ? Authorized
  
    MCP->>FS: Store Raw Document (tenant-a/)
    FS-->>MCP: ? Stored (doc_id: 12345)
    
    MCP->>Chunker: Chunk Document (auto-detect format)
    Chunker-->>MCP: Chunks (20 chunks)
    
    par LLM Enrichment
        MCP->>LLM: Enrich Chunks (tenant-a prompts)
     LLM-->>MCP: Summaries + Keywords
    and
      MCP->>NER: Extract Entities (tenant-a)
        NER-->>MCP: Entities (Person: 5, Org: 3, Location: 2)
    end
    
    MCP->>KG: Build Knowledge Graph (tenant-a)
    KG->>PG: Insert Entities (schema: tenant_a)
    PG-->>KG: ? Inserted (10 nodes)
 KG->>PG: Insert Relationships (schema: tenant_a)
    PG-->>KG: ? Inserted (15 edges)
  
    MCP->>VDB: Store Embeddings (partition: tenant-a)
    VDB-->>MCP: ? Indexed (20 vectors)
    
 MCP->>Audit: Log Ingestion (doc_id, tenant, stats)
    Audit-->>MCP: ? Logged
    
    MCP-->>API: Ingestion Complete (doc_id: 12345)
    API-->>User: "Document processed: 20 chunks, 10 entities, 15 relationships"
    
    Note over User,Audit: Cold Path: ~5 seconds for 20 chunks
```

---

## Component Interaction Diagrams

### Phase 10-26 Integration Flow

```mermaid
graph TB
    subgraph "Input"
    Chunks[Document Chunks]
    end
    
    subgraph "Phase 10: LLM Integration"
        LLM[LLM Provider<br/>OpenAI, Azure OpenAI]
        Summarizer[Chunk Summarizer]
        KeywordEx[Keyword Extractor]
        PrelimNER[Preliminary NER]
        
LLM --> Summarizer
        LLM --> KeywordEx
    LLM --> PrelimNER
    end
    
    subgraph "Phase 11: KG Foundation"
     GraphModel[Graph Data Model<br/>Node, Edge, Property]
      BasicExtractor[Basic Entity Extractor]
    AGESetup[PostgreSQL + AGE Setup]
        
        PrelimNER --> BasicExtractor
        BasicExtractor --> GraphModel
        GraphModel --> AGESetup
    end
    
    subgraph "Phase 12: NER"
        FullNER[LLM-based NER<br/>GPT-4 + Prompts]
        EntityDB[Entity Database<br/>Person, Org, Location]
        Deduplication[Entity Deduplication<br/>Fuzzy Matching]
 
        LLM --> FullNER
      FullNER --> EntityDB
        EntityDB --> Deduplication
        Deduplication --> AGESetup
    end
    
    subgraph "Phase 23: Relationships"
     RelationshipEx[Relationship Extractor]
        Coreference[Coreference Resolution]
        Dependency[Dependency Parser]
        
    FullNER --> RelationshipEx
      Chunks --> Coreference
  Coreference --> RelationshipEx
        RelationshipEx --> Dependency
  Dependency --> AGESetup
    end
    
    subgraph "Phase 24: Query API"
        LINQ[LINQ Query Builder]
        CypherEngine[Cypher Query Engine]
 Traversal[Graph Traversal API]
        
        AGESetup --> LINQ
        AGESetup --> CypherEngine
        LINQ --> Traversal
        CypherEngine --> Traversal
    end

    subgraph "Phase 15: Persistence"
        Serialization[Graph Serialization<br/>JSON-LD, GraphML]
        BulkIO[Bulk Import/Export]
     Versioning[Version Control]
        
      AGESetup --> Serialization
        Serialization --> BulkIO
   BulkIO --> Versioning
    end
    
    subgraph "Phase 16: RAG"
  HybridRetriever[Hybrid Retriever<br/>Vector + Graph]
        ContextExpansion[Context Expansion<br/>Graph Traversal]
        Reranker[Graph-Aware Reranker]
        
        Traversal --> HybridRetriever
        Traversal --> ContextExpansion
        ContextExpansion --> Reranker
    end
    
    subgraph "Output"
        Results[Enhanced Results<br/>Context + Entities + Relationships]
    end
    
    Chunks --> Summarizer
    Chunks --> KeywordEx
 
    Reranker --> Results
    
  style LLM fill:#e1f5e1
    style AGESetup fill:#ffe1cc
    style HybridRetriever fill:#e1e5f5
    style Results fill:#d4edda
```

### RBAC + Audit Flow

```mermaid
graph TB
    subgraph "Request Flow"
      Request[User Request<br/>X-Tenant-ID: tenant-a]
    end
    
    subgraph "RBAC Validation"
  ExtractTenant[Extract Tenant Context]
        ValidateUser[Validate User Membership]
        CheckPermission[Check Permission]
   ValidateAction[Validate Action Safety]
    end
    
    subgraph "Action Types"
        ReadOnly[Read-Only<br/>Query, Search]
        WriteAction[Write<br/>Upload, Update]
        DestructiveAction[Destructive<br/>Delete, Drop]
    end
    
    subgraph "Guardrails"
        AutoApprove[Auto-Approve]
        RequireConfirm[Require User Confirmation]
        RequireAdmin[Require Admin Approval]
        BlockAction[Block Action]
    end
    
    subgraph "Audit Logging"
        LogAttempt[Log Access Attempt]
        LogAction[Log Action Taken]
        LogResult[Log Result + Data Sources]
        AlertSecTeam[Alert Security Team]
    end
    
    subgraph "Data Access"
    TenantData[Tenant-Specific Data<br/>VDB, KG, RDBMS, FS]
    end
    
    Request --> ExtractTenant
    ExtractTenant --> ValidateUser
    ValidateUser --> CheckPermission
    CheckPermission --> ValidateAction
    
    ValidateAction --> ReadOnly
    ValidateAction --> WriteAction
    ValidateAction --> DestructiveAction
    
    ReadOnly --> AutoApprove
    WriteAction --> RequireConfirm
  DestructiveAction --> RequireAdmin
    DestructiveAction --> BlockAction
    
    AutoApprove --> LogAttempt
    RequireConfirm --> LogAttempt
    RequireAdmin --> LogAttempt
    BlockAction --> AlertSecTeam
    
    LogAttempt --> TenantData
    TenantData --> LogAction
    LogAction --> LogResult
    
    AlertSecTeam --> LogResult
    
    style Request fill:#e1f5e1
    style BlockAction fill:#f8d7da
    style AutoApprove fill:#d4edda
    style TenantData fill:#ffe1cc
```

---

## Technology Stack Summary

### Phase Implementation Dependencies

```mermaid
gantt
    title Knowledge Graph Implementation Timeline
    dateFormat YYYY-MM-DD
    section Phase 10
    LLM Integration    :p11, 2025-01-15, 3w
    section Phase 11
  KG Foundation            :p21, after p11, 3w
    section Phase 12
NER Integration    :p22, after p21, 3w
section Phase 23
    Relationships            :p23, after p22, 3w
    section Phase 24
    Query API        :p24, after p23, 3w
    section Phase 15
    Persistence   :p25, after p24, 2w
    section Phase 16
    RAG Enhancement          :p26, after p25, 2w
```

### Component Technology Mapping

| Component | Technology | Phase | Purpose |
|-----------|-----------|-------|---------|
| **LLM Provider** | OpenAI SDK, Azure.AI.OpenAI | 11 | Text generation, NER, summarization |
| **Vector Database** | Qdrant, Milvus, or Pinecone | 12 | Semantic search via embeddings |
| **Graph Database** | PostgreSQL + Apache AGE | 21 | Knowledge graph storage |
| **Message Queue** | RabbitMQ or Azure Service Bus | 13 | Async processing pipeline |
| **Cache Layer** | Redis | 13 | Response caching, rate limiting |
| **RBAC** | Custom + ASP.NET Core Identity | 19 | Multi-tenant authorization |
| **Audit Logging** | Serilog + Seq or Application Insights | 19 | Compliance logging |
| **MCP Server** | Microsoft.Extensions.AI + MCP SDK | 17 | Tool/resource protocol |
| **Agent Framework** | Semantic Kernel or AutoGen | 17 | Agent orchestration |

---

## Next Steps

### Documentation Tasks
1. ? Architecture diagrams created
2. ? Create detailed component specifications
3. ? Design API contracts (REST + MCP)
4. ? Create database schema migration scripts
5. ? Write security model documentation

### Implementation Tasks
1. ? Start Phase 10: LLM Integration
2. ? Set up PostgreSQL + AGE development environment
3. ? Design `TenantContext` infrastructure
4. ? Create multi-tenant test fixtures
5. ? Implement RBAC foundation

### Architecture Decision Records
1. ? ADR-001: Multi-Tenant Isolation Strategy (Schema vs RLS)
2. ? ADR-002: Vector Database Selection
3. ? ADR-003: LLM Provider Strategy (OpenAI vs Azure vs Local)
4. ? ADR-004: Graph Query Language (LINQ vs Cypher vs Both)
5. ? ADR-005: MCP vs Custom Protocol

---

**Document Version**: 1.0  
**Last Updated**: January 2025  
**Author**: PanoramicData Development Team  
**Status**: Draft - Ready for Phase 10 Implementation
