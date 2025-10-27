# PanoramicData.Chunker - Detailed Sequence Diagrams

## Table of Contents
1. [Document Processing Workflows](#document-processing-workflows)
2. [Query Processing Workflows](#query-processing-workflows)
3. [Knowledge Graph Construction](#knowledge-graph-construction)
4. [Multi-Tenant Operations](#multi-tenant-operations)
5. [Error Handling & Recovery](#error-handling--recovery)

---

## Document Processing Workflows

### 1. Document Upload & Chunking (Phase 1-9)

```mermaid
sequenceDiagram
  participant User
    participant API
    participant Auth
    participant ChunkerFactory
    participant PdfChunker
    participant DocxChunker
    participant TokenCounter
    participant Storage
    
    User->>API: POST /documents (PDF file)
    API->>Auth: Validate Token + Tenant
 Auth-->>API: ? tenant-a, user-123
    
    API->>Storage: Store Raw File (tenant-a/docs/file.pdf)
    Storage-->>API: ? doc_id: abc-123
    
    API->>ChunkerFactory: CreateChunker(stream, "application/pdf")
    ChunkerFactory->>ChunkerFactory: Detect Format (PDF signature)
    ChunkerFactory->>PdfChunker: new PdfDocumentChunker()
    ChunkerFactory-->>API: IPdfDocumentChunker
    
    API->>PdfChunker: ChunkAsync(stream, options)
    
    loop For each page
        PdfChunker->>PdfChunker: Extract Text from Page
   PdfChunker->>PdfChunker: Detect Paragraphs (double-newline)
        PdfChunker->>PdfChunker: Detect Headings (CAPS heuristic)
        
        loop For each paragraph
            PdfChunker->>TokenCounter: CountTokens(paragraph)
     TokenCounter-->>PdfChunker: token_count: 150
            
        alt Chunk too large
       PdfChunker->>PdfChunker: Split on sentence boundaries
    end
   
            PdfChunker->>PdfChunker: Create PdfParagraphChunk
        end
    end
    
    PdfChunker-->>API: List<Chunk> (20 chunks)
    
    API->>Storage: Store Chunks (tenant-a/chunks/abc-123.json)
    Storage-->>API: ? Stored
    
    API-->>User: {doc_id: "abc-123", chunks: 20, status: "ready"}
    
    Note over User,Storage: Processing Time: ~2 seconds for 20-page PDF
```

### 2. Document Enrichment with LLM (Phase 10)

```mermaid
sequenceDiagram
    participant Queue as Task Queue
    participant Worker as Background Worker
    participant ChunkStore
    participant LLMProvider
    participant PromptManager
  participant EnrichedStore
    participant Cache
    
    Queue->>Worker: Process Document (doc_id: abc-123, tenant: a)
    
    Worker->>ChunkStore: GetChunks(doc_id, tenant)
    ChunkStore-->>Worker: 20 chunks
    
    loop For each chunk
        Worker->>Cache: Check Enrichment Cache
        
    alt Cache Hit
            Cache-->>Worker: Cached Enrichment
else Cache Miss
   Worker->>PromptManager: GetPrompt("summarize", tenant)
            PromptManager-->>Worker: Prompt Template
     
    Worker->>Worker: Render Prompt (chunk + template)
       
 Worker->>LLMProvider: GenerateAsync(prompt, model: "gpt-4")
  
        Note over LLMProvider: Rate Limiting:<br/>Max 100 req/min
      
          LLMProvider-->>Worker: {summary, keywords, entities_prelim}
   
         Worker->>Cache: Store Enrichment (TTL: 24h)
  end
        
        Worker->>EnrichedStore: UpdateChunk(chunk_id, enrichment)
        EnrichedStore-->>Worker: ? Updated
    end
    
    Worker->>Queue: Mark Complete (doc_id)
    
    Note over Queue,Cache: Total Time: ~30 seconds for 20 chunks (batched)
```

### 3. Knowledge Graph Construction (Phase 11-23)

```mermaid
sequenceDiagram
    participant Enriched as Enriched Chunks
    participant NER as Entity Extractor
    participant LLM
    participant Deduplicator
    participant RelExtractor
    participant Coref as Coreference Resolver
    participant GraphBuilder
  participant PostgreSQL
    
    Enriched->>NER: Extract Entities (chunk_1, tenant-a)
    
 NER->>LLM: Prompt: "Extract entities from: {text}"
    LLM-->>NER: {persons: ["John Smith"], orgs: ["Acme Corp"]}
    
  NER->>Deduplicator: Deduplicate("John Smith", tenant-a)
    
    Deduplicator->>PostgreSQL: SELECT * FROM tenant_a.entities WHERE name ILIKE '%John Smith%'
    PostgreSQL-->>Deduplicator: Existing: [{id: e-1, name: "John A. Smith"}]
    
    Deduplicator->>Deduplicator: Fuzzy Match (similarity: 0.92)
  
    alt High Similarity (>0.9)
        Deduplicator-->>NER: Use Existing: e-1
    else New Entity
        Deduplicator->>PostgreSQL: INSERT INTO tenant_a.entities
        PostgreSQL-->>Deduplicator: New ID: e-10
  Deduplicator-->>NER: New Entity: e-10
    end
    
    NER-->>Enriched: Entities: [e-1 (Person), e-2 (Org)]
    
    Enriched->>RelExtractor: Extract Relationships (chunk_1, entities)
    
    RelExtractor->>Coref: Resolve References (chunk_1)
    Coref-->>RelExtractor: Coreference Chains: [["John", "he"], ["Acme", "company"]]
    
    RelExtractor->>LLM: Prompt: "What is the relationship between {e-1} and {e-2}?"
    LLM-->>RelExtractor: {type: "works_at", confidence: 0.85, properties: {role: "CEO"}}
    
    RelExtractor->>GraphBuilder: AddRelationship(e-1, e-2, "works_at", tenant-a)
    
    GraphBuilder->>PostgreSQL: SELECT * FROM ag_catalog.ag_graph WHERE graph_name = 'tenant_a'
    
    alt Graph Exists
     GraphBuilder->>PostgreSQL: SELECT * FROM cypher('tenant_a', $$ MATCH (n {id: $e_1}) RETURN n $$)
    else Graph Not Exists
   GraphBuilder->>PostgreSQL: SELECT create_graph('tenant_a')
    end
    
    GraphBuilder->>PostgreSQL: SELECT * FROM cypher('tenant_a', $$<br/>MERGE (p:Person {id: $e_1})<br/>MERGE (o:Organization {id: $e_2})<br/>MERGE (p)-[r:WORKS_AT {confidence: $conf}]->(o)<br/>RETURN r<br/>$$) AS (relationship agtype)
    
    PostgreSQL-->>GraphBuilder: ? Relationship Created
    
    GraphBuilder-->>RelExtractor: ? Added to Graph
    
    Note over Enriched,PostgreSQL: Graph Construction: ~5 seconds per document
```

---

## Query Processing Workflows

### 4. Hybrid Search (Vector + Graph) - Phase 16

```mermaid
sequenceDiagram
    participant User
    participant QueryAPI
  participant QueryParser
    participant VectorDB
    participant GraphDB as PostgreSQL+AGE
    participant Reranker
    participant ContextExpander
    participant LLM
    
    User->>QueryAPI: "Find all reports about Acme Corp vulnerabilities"
    
    QueryAPI->>QueryParser: Parse Query (tenant-a)
    QueryParser-->>QueryAPI: {entities: ["Acme Corp"], intent: "search", filters: ["vulnerability"]}
    
  par Vector Search
        QueryAPI->>VectorDB: Search("Acme Corp vulnerabilities", tenant: a, k=20)
        VectorDB-->>QueryAPI: Vector Results (20 chunks, scores 0.7-0.9)
    and Graph Search
        QueryAPI->>GraphDB: Cypher: MATCH (o:Organization {name: "Acme Corp"})-[:HAS_VULNERABILITY]->(v:Vulnerability) RETURN v
        GraphDB-->>QueryAPI: Graph Results (5 vulnerabilities)
    end
    
    QueryAPI->>Reranker: RerankResults(vectorResults, graphResults)
    
    Reranker->>Reranker: Calculate Hybrid Score<br/>= 0.6 * vector_score + 0.4 * graph_score
    
  Reranker-->>QueryAPI: Ranked Results (Top 10)
    
    QueryAPI->>ContextExpander: ExpandContext(results, tenant-a)
    
    loop For each result
        ContextExpander->>GraphDB: Cypher: MATCH (n {id: $entity_id})-[r]-(related) RETURN related LIMIT 5
        GraphDB-->>ContextExpander: Related Entities (neighbors)
    end
    
    ContextExpander-->>QueryAPI: Expanded Context (chunks + entity graph)
    
    QueryAPI->>LLM: Generate Answer (query + expanded_context)
    LLM-->>QueryAPI: "Acme Corp has 3 critical vulnerabilities (CVSS > 7.0):\n1. CVE-2024-1234...\n2. ..."
    
    QueryAPI-->>User: {answer, sources: [chunk_ids], entities: [entity_ids], confidence: 0.92}
    
    Note over User,LLM: Query Time: ~600ms (vector: 100ms, graph: 200ms, LLM: 300ms)
```

### 5. SQL Query Generation from Natural Language

```mermaid
sequenceDiagram
    participant User
    participant ChatUI
    participant Agent
    participant SchemaService
    participant GraphDB
    participant SQLGenerator
    participant Validator
    participant RDBMS
    participant AuditLog
    
    User->>ChatUI: "Show me all high-severity vulnerabilities from Q4 2024"
    
    ChatUI->>Agent: Process Query (tenant-a, user-123)
    
    Agent->>SchemaService: GetSchema(tenant-a, "vulnerabilities")
    SchemaService-->>Agent: Schema: {table: "vulnerabilities", columns: [id, cvss_score, discovered_at, ...]}
    
    Agent->>GraphDB: GetSemanticMapping("high-severity")
  GraphDB-->>Agent: Mapping: {high_severity: "cvss_score > 7.0"}
  
    Agent->>SQLGenerator: GenerateSQL(query, schema, mappings)
    
    SQLGenerator->>SQLGenerator: Parse: entities=["vulnerability"], filters=["high-severity", "Q4 2024"]
    SQLGenerator->>SQLGenerator: Build SQL AST
    
    SQLGenerator-->>Agent: SQL: SELECT * FROM vulnerabilities WHERE cvss_score > 7.0 AND discovered_at BETWEEN '2024-10-01' AND '2024-12-31'
    
    Agent->>Validator: ValidateSQL(sql, tenant-a)
    
    Validator->>Validator: Check: No DELETE/UPDATE/DROP
    Validator->>Validator: Check: Only tenant_a tables
    Validator->>Validator: Check: RBAC permissions (user-123)
    
    alt Validation Failed
        Validator-->>Agent: ? Error: Unauthorized table access
        Agent-->>User: "Cannot access that table"
    else Validation Passed
        Validator-->>Agent: ? SQL Safe
        
        Agent->>ChatUI: Show SQL + Ask Confirmation
   ChatUI-->>User: "I'll run: {SQL}. Continue?"
        
        User->>ChatUI: "Yes"
        ChatUI->>Agent: ExecuteConfirmed(sql)
        
  Agent->>AuditLog: Log: {user: 123, tenant: a, sql, timestamp, confirmation_id}
 
        Agent->>RDBMS: ExecuteQuery(sql, tenant-a connection)
        RDBMS-->>Agent: Results (42 rows)
        
        Agent->>AuditLog: Log: {result_count: 42, execution_time: 250ms}
        
        Agent-->>ChatUI: Results + Metadata
        ChatUI-->>User: Display Table (42 vulnerabilities) + Export Button
    end
    
    Note over User,AuditLog: Guardrail: User confirmation + Audit logging required
```

### 6. Cross-Document Entity Resolution

```mermaid
sequenceDiagram
    participant Doc1 as Document 1
    participant Doc2 as Document 2
    participant EntityExtractor
    participant Deduplicator
    participant GraphDB
    participant Linker as Entity Linker
    
    Doc1->>EntityExtractor: Extract Entities
    EntityExtractor-->>Doc1: [Person: "Dr. Jane Smith"]
    
  Doc1->>Deduplicator: Deduplicate("Dr. Jane Smith", tenant-a)
    Deduplicator->>GraphDB: MATCH (p:Person) WHERE p.name =~ '.*Jane.*Smith.*' RETURN p
    GraphDB-->>Deduplicator: No matches
    
    Deduplicator->>GraphDB: CREATE (p:Person {id: e-100, name: "Dr. Jane Smith", source_doc: doc-1})
    GraphDB-->>Deduplicator: ? Created e-100
    
    Deduplicator-->>Doc1: entity_id: e-100
    
    Note over Doc1,GraphDB: Later, Document 2 is processed...
    
    Doc2->>EntityExtractor: Extract Entities
    EntityExtractor-->>Doc2: [Person: "Jane A. Smith, PhD"]
    
    Doc2->>Deduplicator: Deduplicate("Jane A. Smith, PhD", tenant-a)
    Deduplicator->>GraphDB: MATCH (p:Person) WHERE p.name =~ '.*Jane.*Smith.*' RETURN p
    GraphDB-->>Deduplicator: Found: e-100 ("Dr. Jane Smith")
    
Deduplicator->>Linker: CalculateSimilarity("Jane A. Smith, PhD", "Dr. Jane Smith")
    
    Linker->>Linker: Token Similarity: 0.85
    Linker->>Linker: Edit Distance: 0.75
    Linker->>Linker: Semantic Embedding Similarity: 0.92
    Linker->>Linker: Combined Score: 0.87
 
    alt High Confidence (>0.85)
        Linker-->>Deduplicator: ? Same Entity (confidence: 0.87)
        
        Deduplicator->>GraphDB: MATCH (p:Person {id: e-100})<br/>SET p.aliases = p.aliases + ["Jane A. Smith, PhD"]<br/>SET p.source_docs = p.source_docs + [doc-2]<br/>SET p.confidence = (p.confidence + 0.87) / 2
        
        GraphDB-->>Deduplicator: ? Updated e-100
        Deduplicator-->>Doc2: entity_id: e-100 (merged)
   
 else Medium Confidence (0.6-0.85)
     Linker-->>Deduplicator: ? Possible Match (confidence: 0.75)
        
        Deduplicator->>GraphDB: CREATE (p2:Person {id: e-101, name: "Jane A. Smith, PhD"})<br/>CREATE (p2)-[:POSSIBLY_SAME_AS {confidence: 0.75}]->(e-100)
     
        GraphDB-->>Deduplicator: ? Created e-101 with link
        Deduplicator-->>Doc2: entity_id: e-101 (tentative)
        
    else Low Confidence (<0.6)
        Linker-->>Deduplicator: ? Different Entity
        
     Deduplicator->>GraphDB: CREATE (p2:Person {id: e-101, name: "Jane A. Smith, PhD"})
        GraphDB-->>Deduplicator: ? Created e-101
        Deduplicator-->>Doc2: entity_id: e-101 (new)
    end
    
  Note over Doc2,GraphDB: Entity Linking enables cross-document queries
```

---

## Multi-Tenant Operations

### 7. Tenant Provisioning

```mermaid
sequenceDiagram
    participant Admin
    participant TenantAPI
    participant TenantService
    participant PostgreSQL
    participant VectorDB
    participant BlobStorage
    participant ConfigStore
    
    Admin->>TenantAPI: POST /tenants {name: "tenant-b", plan: "enterprise"}
    
    TenantAPI->>TenantService: CreateTenant("tenant-b")
    
    TenantService->>PostgreSQL: CREATE SCHEMA tenant_b
    PostgreSQL-->>TenantService: ? Schema Created
    
    TenantService->>PostgreSQL: CREATE TABLE tenant_b.entities (...)<br/>CREATE TABLE tenant_b.relationships (...)<br/>CREATE TABLE tenant_b.chunks (...)
    PostgreSQL-->>TenantService: ? Tables Created
    
    TenantService->>PostgreSQL: SELECT create_graph('tenant_b')
    PostgreSQL-->>TenantService: ? Graph Created
    
TenantService->>PostgreSQL: CREATE POLICY tenant_b_isolation ON tenant_b.entities<br/>USING (tenant_id = 'tenant-b')
    PostgreSQL-->>TenantService: ? RLS Policy Created
    
    TenantService->>VectorDB: CreatePartition("tenant-b")
    VectorDB-->>TenantService: ? Partition Created
    
    TenantService->>BlobStorage: CreateContainer("tenant-b")
    BlobStorage-->>TenantService: ? Container Created
    
    TenantService->>ConfigStore: SET tenant_b:config {llm_model: "gpt-4", max_chunks: 1000}
    ConfigStore-->>TenantService: ? Config Stored
    
    TenantService->>TenantService: Generate API Keys (primary, secondary)
    
    TenantService-->>TenantAPI: {tenant_id: "tenant-b", api_key: "tb_xxx", status: "active"}
    
  TenantAPI-->>Admin: Tenant Created Successfully
  
    Note over Admin,ConfigStore: Provisioning Time: ~5 seconds
```

### 8. Tenant Isolation Validation

```mermaid
sequenceDiagram
    participant User_A as User (Tenant A)
participant User_B as User (Tenant B)
    participant API
    participant RBAC
    participant PostgreSQL
    participant VectorDB
    
    User_A->>API: GET /documents (X-Tenant-ID: tenant-a, Token: user-a-token)
    
    API->>RBAC: ValidateAccess(user-a-token, tenant-a)
 RBAC->>RBAC: Decode JWT: {user_id: a-1, tenant_id: tenant-a}
    RBAC-->>API: ? Authorized (tenant-a)
    
    API->>PostgreSQL: SET app.tenant_id = 'tenant-a'<br/>SELECT * FROM tenant_a.chunks WHERE user_id = 'a-1'
    
    Note over PostgreSQL: Row-Level Security applies:<br/>Only returns tenant-a data
    
    PostgreSQL-->>API: 10 documents (tenant-a only)
    API-->>User_A: Documents: [doc-1, doc-2, ...]
    
    Note over User_A,VectorDB: ---
    
    User_B->>API: GET /documents (X-Tenant-ID: tenant-a, Token: user-b-token)
    
    API->>RBAC: ValidateAccess(user-b-token, tenant-a)
    RBAC->>RBAC: Decode JWT: {user_id: b-1, tenant_id: tenant-b}
    
    RBAC->>RBAC: Check: token.tenant_id != request.tenant_id
    RBAC-->>API: ? 403 Forbidden: Tenant mismatch
    
    API-->>User_B: Error: "Access denied to tenant-a resources"
    
    Note over User_B,VectorDB: Tenant isolation enforced
    
    Note over User_A,VectorDB: ---
    
    User_B->>API: GET /search?q=vulnerabilities (X-Tenant-ID: tenant-b, Token: user-b-token)
    
    API->>RBAC: ValidateAccess(user-b-token, tenant-b)
    RBAC-->>API: ? Authorized (tenant-b)
  
    API->>VectorDB: Search("vulnerabilities", partition: tenant-b)
    
    Note over VectorDB: Partition isolation:<br/>Only searches tenant-b vectors
    
    VectorDB-->>API: Results from tenant-b only
    
    API->>PostgreSQL: MATCH (n)-[r]-(m) WHERE n.tenant_id = 'tenant-b' RETURN r
    
    PostgreSQL-->>API: Graph results (tenant-b only)

    API-->>User_B: Combined Results (tenant-b isolated)
    
    Note over User_A,VectorDB: ? Zero cross-tenant data leakage
```

---

## Error Handling & Recovery

### 9. LLM Rate Limit & Retry Logic

```mermaid
sequenceDiagram
    participant Worker
    participant Queue
    participant LLM
  participant Metrics
    participant AlertSystem
    
    Worker->>Queue: Dequeue Enrichment Task (chunk-1)
    
    loop Retry Logic (max 3 attempts)
        Worker->>LLM: GenerateSummary(chunk-1)
    
        alt Success
            LLM-->>Worker: Summary: "This document discusses..."
  Worker->>Metrics: RecordSuccess(latency: 500ms)
        Note over Worker: Break retry loop
     
        else Rate Limit (429)
          LLM-->>Worker: ? 429 Too Many Requests (retry-after: 60s)
  
       Worker->>Metrics: RecordRateLimit()
            Worker->>Worker: Exponential Backoff (attempt: 1, wait: 2^1 = 2s)
  
 Note over Worker: Wait 2 seconds...
   
            Worker->>LLM: Retry GenerateSummary(chunk-1)
      LLM-->>Worker: ? 429 Too Many Requests (retry-after: 60s)
  
      Worker->>Worker: Exponential Backoff (attempt: 2, wait: 2^2 = 4s)
      
      Note over Worker: Wait 4 seconds...
            
     Worker->>LLM: Retry GenerateSummary(chunk-1)
            LLM-->>Worker: ? 429 Too Many Requests
            
          Worker->>Worker: Max retries exhausted
   Worker->>Queue: RequeueTask(chunk-1, delay: 60s)
  Worker->>Metrics: RecordFailure(reason: "rate_limit")
        
        else API Error (500)
            LLM-->>Worker: ? 500 Internal Server Error
        
        Worker->>Metrics: RecordAPIError()
     Worker->>Worker: Exponential Backoff (attempt: 1, wait: 2s)
         
          Worker->>LLM: Retry GenerateSummary(chunk-1)
    LLM-->>Worker: ? Summary: "..."
            
    Worker->>Metrics: RecordSuccess(retries: 1)
       
   else Timeout
          Note over LLM: No response after 30s
         
     Worker->>Worker: Timeout Exception
            Worker->>Metrics: RecordTimeout()
            Worker->>Queue: RequeueTask(chunk-1, delay: 10s)
            
        end
    end
    
    alt Too Many Failures
        Metrics->>Metrics: Check: failure_rate > 50% in last 5 min
        Metrics->>AlertSystem: Trigger Alert: High LLM Failure Rate
 AlertSystem-->>Metrics: ? Alert Sent
    end
```

### 10. Knowledge Graph Consistency Check

```mermaid
sequenceDiagram
    participant Scheduler
    participant Validator
  participant GraphDB
    participant ChunkStore
    participant RepairService
    participant AuditLog
    
    Scheduler->>Validator: Run Consistency Check (tenant-a)
    
    Validator->>GraphDB: MATCH (e:Entity) WHERE NOT EXISTS((e)-[]-()) RETURN count(e)
    GraphDB-->>Validator: Orphaned Entities: 5
    
    Validator->>GraphDB: MATCH (e:Entity) WHERE e.source_chunk_id IS NOT NULL RETURN e
    GraphDB-->>Validator: Entities: 1000
    
    loop For each entity
        Validator->>ChunkStore: ChunkExists(entity.source_chunk_id, tenant-a)
        
        alt Chunk Deleted
            ChunkStore-->>Validator: ? Chunk Not Found
     
         Validator->>Validator: Mark Entity as Orphaned
         Validator->>AuditLog: Log Inconsistency (entity_id, reason: "missing_chunk")
  
   else Chunk Exists
            ChunkStore-->>Validator: ? Chunk Found
        end
    end
    
    Validator->>GraphDB: MATCH ()-[r:WORKS_AT]->() WHERE r.confidence < 0.5 RETURN count(r)
    GraphDB-->>Validator: Low-Confidence Relationships: 12
    
    Validator-->>Scheduler: Report: {orphaned_entities: 5, missing_chunks: 3, low_confidence_rels: 12}
    
    Scheduler->>RepairService: RepairInconsistencies(tenant-a, issues)
    
    alt Auto-Repair Enabled
        RepairService->>GraphDB: MATCH (e:Entity) WHERE e.orphaned = true<br/>DELETE e
        GraphDB-->>RepairService: ? Deleted 5 orphaned entities
  
        RepairService->>GraphDB: MATCH ()-[r]->() WHERE r.confidence < 0.5<br/>DELETE r
      GraphDB-->>RepairService: ? Deleted 12 low-confidence relationships
    
  RepairService->>AuditLog: Log Repairs (deleted: 17, tenant: a)
        
    else Manual Review Required
    RepairService->>AuditLog: Flag for Manual Review (issue_count: 17)
    end
    
    RepairService-->>Scheduler: Repair Complete
    
    Note over Scheduler,AuditLog: Scheduled Daily at 2 AM UTC
```

---

## Performance Optimization Workflows

### 11. Caching Strategy (Phase 13)

```mermaid
sequenceDiagram
    participant User
    participant API
    participant Cache as Redis Cache
    participant VectorDB
    participant GraphDB
    participant LLM
    
    User->>API: Query: "Acme Corp vulnerabilities"
  
    API->>API: Generate Cache Key: hash(query + tenant + user_permissions)
    API->>Cache: GET cache_key
  
    alt Cache Hit (Fresh)
        Cache-->>API: Cached Response (TTL: 50s remaining)
        API-->>User: Results (served from cache, ~10ms)
      
    else Cache Hit (Stale)
        Cache-->>API: Stale Response (TTL: expired)
      
        API-->>User: Return Stale Results (fast response)
   
        par Background Refresh
        API->>VectorDB: Refresh Vector Search
            VectorDB-->>API: New Vector Results
            
      API->>GraphDB: Refresh Graph Query
    GraphDB-->>API: New Graph Results
          
            API->>LLM: Regenerate Summary
  LLM-->>API: New Summary
            
            API->>Cache: SET cache_key, new_response, TTL: 300s
        end
        
  else Cache Miss
        API->>VectorDB: Vector Search
 VectorDB-->>API: Vector Results (100ms)
        
        API->>GraphDB: Graph Query
      GraphDB-->>API: Graph Results (200ms)
        
        API->>LLM: Generate Summary
        LLM-->>API: Summary (500ms)
    
        API->>Cache: SET cache_key, response, TTL: 300s
        Cache-->>API: ? Cached
  
        API-->>User: Results (total: 800ms)
    end
    
    Note over User,LLM: Cache Strategy:<br/>- Hot queries: <10ms (cache hit)<br/>- Stale-while-revalidate: <50ms<br/>- Cold queries: ~800ms
```

### 12. Batch Processing Pipeline

```mermaid
sequenceDiagram
    participant Upload
    participant Queue as Message Queue
    participant Chunker
    participant LLM
    participant NER
    participant Graph
    participant Notification
    
    Upload->>Queue: Enqueue 100 Documents (tenant-a)
    
    loop Batch Processing (chunks of 10)
        Queue->>Chunker: Dequeue Batch (10 docs)
    
        par Parallel Chunking
            Chunker->>Chunker: Process Doc 1-10 (concurrent)
     end
    
        Chunker-->>Queue: Chunks Ready (200 chunks)
        
        Queue->>LLM: Enqueue LLM Enrichment (200 chunks)
  
        loop Batch API Call (20 chunks per request)
            LLM->>LLM: Batch Enrich (chunks 1-20)
       LLM->>LLM: Batch Enrich (chunks 21-40)
         Note over LLM: ... (10 batches total)
        end
        
        LLM-->>Queue: Enrichment Complete (200 chunks)
        
  Queue->>NER: Enqueue NER (200 enriched chunks)
        
  par Parallel NER
         NER->>NER: Extract Entities (chunks 1-50)
     NER->>NER: Extract Entities (chunks 51-100)
      NER->>NER: Extract Entities (chunks 101-150)
   NER->>NER: Extract Entities (chunks 151-200)
        end
        
        NER-->>Queue: Entities Extracted (500 entities)
        
  Queue->>Graph: Build Graph (500 entities, tenant-a)
      
        Graph->>Graph: Deduplicate Entities (500 ? 300 unique)
        Graph->>Graph: Extract Relationships (150 relationships)
        Graph->>Graph: Insert into PostgreSQL+AGE
        
  Graph-->>Queue: Graph Updated
    end
    
  Queue->>Notification: All 100 Documents Processed
    Notification->>Upload: Email: "Processing Complete (100 docs, 2000 chunks, 3000 entities)"
    
    Note over Upload,Notification: Total Time: ~10 minutes for 100 documents<br/>(vs 50 minutes sequential)
```

---

## Security & Compliance

### 13. Data Export for GDPR Compliance

```mermaid
sequenceDiagram
    participant User
    participant ComplianceAPI
    participant RBAC
    participant PostgreSQL
    participant VectorDB
    participant BlobStorage
    participant Encryption
    participant AuditLog
    
    User->>ComplianceAPI: POST /export-data {user_id: "user-123", tenant: "tenant-a"}
    
    ComplianceAPI->>RBAC: ValidateDataExportPermission(user-123, tenant-a)
  RBAC-->>ComplianceAPI: ? Authorized (admin or self)
    
    ComplianceAPI->>AuditLog: Log Export Request (user-123, initiator: admin-456)
    
    ComplianceAPI->>PostgreSQL: SELECT * FROM tenant_a.chunks WHERE created_by = 'user-123'
  PostgreSQL-->>ComplianceAPI: Chunks (50 records)
    
    ComplianceAPI->>PostgreSQL: MATCH (e:Entity {created_by: 'user-123'}) RETURN e
    PostgreSQL-->>ComplianceAPI: Entities (20 nodes)
    
    ComplianceAPI->>PostgreSQL: MATCH (:Entity {created_by: 'user-123'})-[r]-() RETURN r
    PostgreSQL-->>ComplianceAPI: Relationships (15 edges)
    
    ComplianceAPI->>VectorDB: GetEmbeddings(user: user-123, tenant: a)
    VectorDB-->>ComplianceAPI: Embeddings (50 vectors)
    
    ComplianceAPI->>BlobStorage: ListFiles(tenant-a/user-123/)
    BlobStorage-->>ComplianceAPI: Files (10 documents)
    
    ComplianceAPI->>ComplianceAPI: Aggregate Data (chunks + entities + files + embeddings)
    
    ComplianceAPI->>Encryption: EncryptArchive(data, password: user_provided)
    Encryption-->>ComplianceAPI: Encrypted ZIP (user-123-export.zip.enc)
    
    ComplianceAPI->>BlobStorage: StoreExport(tenant-a/exports/user-123-export.zip.enc, TTL: 7 days)
    BlobStorage-->>ComplianceAPI: Download URL (signed, expires: 7 days)
    
    ComplianceAPI->>AuditLog: Log Export Complete (user-123, file_size: 15MB, records: 145)
    
    ComplianceAPI-->>User: {download_url, expires_at, records_exported: 145, file_size: "15MB"}
    
    Note over User,AuditLog: GDPR Right to Data Portability fulfilled
```

---

**Document Version**: 1.0  
**Last Updated**: January 2025  
**Related Documents**: 
- [Architecture Diagrams](ARCHITECTURE_DIAGRAMS.md)
- [Phase 10 Specification](../phases/Phase-11.md)
- [Phase 11-26 Specifications](../phases/)
