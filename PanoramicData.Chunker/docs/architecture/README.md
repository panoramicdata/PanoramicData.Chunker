# PanoramicData.Chunker - Multi-Tenant RAG Architecture Summary

**Version**: 1.0  
**Date**: January 2025  
**Status**: Architecture Design Complete - Ready for Phase 10 Implementation

---

## Executive Summary

This document summarizes the complete architecture for PanoramicData.Chunker as a **multi-tenant RAG (Retrieval-Augmented Generation) system** with **Knowledge Graph integration**. The architecture supports:

? **Multi-Tenant Data Isolation** - Schema-per-tenant with Row-Level Security  
? **9 Document Formats** - Markdown, HTML, Plain Text, DOCX, PPTX, XLSX, CSV, PDF  
? **Knowledge Graph Storage** - PostgreSQL + Apache AGE  
? **Vector Search** - Embedding-based semantic retrieval  
? **Hybrid Retrieval** - Combined vector + graph search for enhanced RAG  
? **LLM Integration** - OpenAI/Azure OpenAI for enrichment and NER  
? **MCP Protocol** - Model Context Protocol for agent framework integration  
? **RBAC + Audit Logging** - Enterprise security and compliance  

---

## Architecture Documents

### 1. [Architecture Diagrams](ARCHITECTURE_DIAGRAMS.md)

**Contains**:
- High-level system architecture
- Multi-tenant data isolation model
- Knowledge graph architecture
- PostgreSQL schema design
- Technology stack mapping
- Phase dependency timeline

**Key Diagrams**:
- System Overview (MCP ? MAF ? Storage)
- Tenant Isolation (Central vs. Tenant-specific resources)
- Knowledge Graph Components (Phases 11-16)
- Data Flow (Ingestion ? Enrichment ? Graph ? Retrieval)

### 2. [Sequence Diagrams](SEQUENCE_DIAGRAMS.md)

**Contains**:
- 13 detailed sequence diagrams
- Document processing workflows
- Query processing flows
- Multi-tenant operations
- Error handling & recovery
- Security & compliance workflows

**Key Workflows**:
- Document Upload & Chunking (Phases 1-9)
- LLM Enrichment (Phase 10)
- Knowledge Graph Construction (Phases 21-23)
- Hybrid Search (Vector + Graph) - Phase 16
- SQL/OData Query Generation
- Cross-Document Entity Resolution
- Tenant Provisioning & Isolation
- GDPR Data Export

### 3. [Component Specifications](COMPONENT_SPECIFICATIONS.md)

**Contains**:
- Interface definitions (.NET 9)
- Implementation specifications
- Database schemas (SQL)
- Configuration models
- Security components

**Key Components**:
- `TenantContext` - Multi-tenant isolation
- `ILLMProvider` - LLM abstraction
- `IGraphStore` - Knowledge graph storage
- `IEntityExtractor` - NER implementation
- `IHybridRetriever` - RAG enhancement
- `IRBACService` - Authorization
- `IAuditLogger` - Compliance logging

---

## Key Architectural Decisions

### ADR-001: Multi-Tenant Isolation Strategy

**Decision**: Schema-per-tenant with Row-Level Security (RLS)

**Rationale**:
- ? **Strong Isolation**: Each tenant gets dedicated PostgreSQL schema
- ? **Performance**: Indexes scoped to tenant data
- ? **Compliance**: Data sovereignty (GDPR, SOC 2)
- ? **Scalability**: Can shard by tenant in future
- ? **Migration Complexity**: Schema changes require multi-tenant migration

**Alternatives Considered**:
- ? Shared schema + tenant_id column (weaker isolation)
- ? Database-per-tenant (operational overhead)

### ADR-002: Knowledge Graph Storage

**Decision**: PostgreSQL + Apache AGE

**Rationale**:
- ? **Single Database**: Graph + relational + vector in PostgreSQL
- ? **Cypher Support**: Industry-standard graph query language
- ? **ACID Compliance**: Transactional guarantees
- ? **Open Source**: No vendor lock-in
- ? **Maturity**: AGE is newer than Neo4j

**Alternatives Considered**:
- ? Neo4j (separate database, licensing costs)
- ? GraphQL over RDBMS (limited graph algorithms)

### ADR-003: Vector Database Selection

**Decision**: PostgreSQL with pgvector (short-term), Qdrant/Milvus (future)

**Rationale**:
- ? **Phase 10-21**: pgvector extension (same database, simpler)
- ? **Phase 16+**: Dedicated vector DB for scale (Qdrant/Milvus)
- ? **Gradual Migration**: Start simple, scale when needed

### ADR-004: LLM Provider Strategy

**Decision**: Abstraction layer (`ILLMProvider`) with OpenAI/Azure OpenAI primary

**Rationale**:
- ? **Flexibility**: Support multiple providers (OpenAI, Azure, Claude, local)
- ? **Cost Optimization**: Switch providers based on tenant config
- ? **Fallback**: Retry with alternative provider on rate limits

**Configuration**:
```json
{
  "tenant-a": { "llm_model": "gpt-4", "provider": "azure" },
  "tenant-b": { "llm_model": "gpt-3.5-turbo", "provider": "openai" }
}
```

### ADR-005: MCP vs. Custom Protocol

**Decision**: Adopt Model Context Protocol (MCP)

**Rationale**:
- ? **Microsoft Standard**: Native integration with MAF
- ? **Tool Discovery**: Automatic tool registration
- ? **Resource Protocol**: Standardized document access
- ? **Future-Proof**: Industry adoption growing

---

## Implementation Phases

### Phase 10: LLM Integration (2-3 weeks) ?? **START HERE**

**Deliverables**:
- [ ] `ILLMProvider` interface + OpenAI implementation
- [ ] `IPromptManager` with tenant overrides
- [ ] `ChunkEnricher` for summaries + keywords + preliminary NER
- [ ] `TenantContext` infrastructure
- [ ] Batch processing for cost optimization
- [ ] 20+ tests (LLM enrichment, prompt management)

**Success Criteria**:
- ? Enrich 1000 chunks in <60 seconds (batched)
- ? Tenant-specific prompt overrides working
- ? Cost tracking per tenant
- ? Rate limit handling with exponential backoff

### Phase 21: Knowledge Graph Foundation (3 weeks)

**Deliverables**:
- [ ] PostgreSQL + Apache AGE setup
- [ ] `create_tenant_schema()` migration function
- [ ] `IGraphStore` implementation
- [ ] Entity and Relationship models
- [ ] Basic entity extraction from enriched chunks
- [ ] 15+ tests (graph CRUD, tenant isolation)

**Success Criteria**:
- ? Create tenant graph in <5 seconds
- ? Store 1000 entities + 500 relationships <10 seconds
- ? Query graph (2-hop traversal) <200ms
- ? Zero cross-tenant data leakage

### Phase 12: Named Entity Recognition (3 weeks)

**Deliverables**:
- [ ] `IEntityExtractor` LLM-based implementation
- [ ] `IEntityDeduplicator` with fuzzy matching
- [ ] Support for 7 entity types (Person, Org, Location, Product, Event, Date, Vulnerability)
- [ ] Confidence scoring (0.0-1.0)
- [ ] 25+ tests (NER accuracy, deduplication)

**Success Criteria**:
- ? NER Precision >80% on test corpus
- ? NER Recall >70%
- ? Deduplication accuracy >90%
- ? Process 100 chunks in <120 seconds

### Phase 23: Relationship Extraction (3 weeks)

**Deliverables**:
- [ ] `IRelationshipExtractor` implementation
- [ ] Coreference resolution (e.g., "John" ? "he")
- [ ] Dependency parsing for verb-based relationships
- [ ] Domain-specific extractors (vulnerability, org chart)
- [ ] 20+ tests (relationship accuracy)

**Success Criteria**:
- ? Relationship Precision >70%
- ? Relationship Recall >60%
- ? Extract 50 relationships from 100 chunks in <180 seconds

### Phase 24: Graph Query API (3 weeks)

**Deliverables**:
- [ ] LINQ-style query builder (`IGraphQueryBuilder`)
- [ ] Cypher query engine (`ICypherQueryEngine`)
- [ ] Graph traversal algorithms (BFS, DFS, shortest path)
- [ ] PageRank, community detection (optional)
- [ ] Query validation (security)
- [ ] 30+ tests (query correctness, performance)

**Success Criteria**:
- ? LINQ queries compile to efficient Cypher
- ? 2-hop traversal <200ms for graph of 10K nodes
- ? Cypher injection prevention (100% safe)
- ? Support for complex queries (MATCH, WHERE, ORDER BY, LIMIT)

### Phase 25: Graph Persistence (2 weeks)

**Deliverables**:
- [ ] Graph serialization (JSON-LD, GraphML, RDF)
- [ ] Bulk import/export
- [ ] Graph versioning
- [ ] Backup/restore per tenant
- [ ] 15+ tests (serialization roundtrip)

**Success Criteria**:
- ? Export 10K node graph in <10 seconds
- ? Import 10K node graph in <20 seconds
- ? Serialization preserves all properties + relationships

### Phase 16: RAG Enhancement (2 weeks)

**Deliverables**:
- [ ] `IHybridRetriever` (vector + graph)
- [ ] Context expansion via graph traversal
- [ ] Graph-aware reranking
- [ ] Metrics (vector_time, graph_time, rerank_time)
- [ ] 20+ tests (retrieval quality)

**Success Criteria**:
- ? Hybrid search outperforms vector-only by >15% (NDCG@10)
- ? Context expansion improves LLM accuracy by >10%
- ? Total retrieval time <600ms (vector: 100ms, graph: 200ms, rerank: 300ms)

**Total Timeline**: 16-19 weeks (Phase 10 ? Phase 16)

---

## Technology Stack

| Component | Technology | Version | Purpose |
|-----------|-----------|---------|---------|
| **Runtime** | .NET | 9.0 | Application framework |
| **Database** | PostgreSQL | 16+ | Primary data store |
| **Graph DB** | Apache AGE | 1.5+ | Graph extension for PostgreSQL |
| **Vector DB** | pgvector (short-term)<br/>Qdrant (future) | latest | Semantic search |
| **LLM Provider** | OpenAI SDK<br/>Azure.AI.OpenAI | latest | LLM integration |
| **Message Queue** | RabbitMQ / Azure Service Bus | latest | Async processing |
| **Cache** | Redis | 7+ | Response caching, rate limiting |
| **Agent Framework** | Microsoft Agent Framework (MAF) | preview | Agent orchestration |
| **Protocol** | Model Context Protocol (MCP) | 1.0 | Tool/resource standard |
| **Auth** | ASP.NET Core Identity | .NET 9 | Multi-tenant RBAC |
| **Logging** | Serilog + Seq / App Insights | latest | Audit + observability |

---

## Security & Compliance

### Multi-Tenant Isolation

```
???????????????????????????????????????????????????
? Tenant Isolation Layers      ?
???????????????????????????????????????????????????
? 1. Network: X-Tenant-ID header validation      ?
? 2. Application: TenantContext middleware       ?
? 3. Database: Row-Level Security (RLS)?
? 4. Storage: Partition/Container per tenant     ?
? 5. Cache: Tenant-prefixed keys           ?
???????????????????????????????????????????????????
```

**Validation Tests**:
- ? User from Tenant A cannot access Tenant B data
- ? Invalid tenant ID ? 400 Bad Request
- ? Suspended tenant ? 403 Forbidden
- ? SQL injection attempts ? Blocked by parameterized queries
- ? Cypher injection attempts ? Validated by query parser

### Audit Logging

**All Logged Events**:
- Document uploads (who, when, size, tenant)
- Queries (query text, results count, latency, tenant, user)
- Entity/relationship creation (confidence, source, tenant)
- Data exports (GDPR compliance)
- Access denials (security alerts)
- Configuration changes (admin actions)

**Retention**: 90 days (hot), 7 years (cold archive for compliance)

### GDPR Compliance

**Implemented Features**:
- ? Right to Access: Export all user data (Phase 25)
- ? Right to Erasure: Delete user data + cascade to graph
- ? Data Portability: JSON-LD export format
- ? Consent Management: Tenant-level consent tracking
- ? Breach Notification: Audit log monitoring + alerts

---

## Use Case Flows

### Use Case 1: General Help Query

**User**: "How do I view last February's report?"

**Flow**:
1. UI ? MAF (X-Tenant-ID: tenant-a)
2. RBAC validates user belongs to tenant-a
3. Hybrid Retriever:
   - Vector search: "February report" ? 10 chunks
   - Graph query: `MATCH (r:Report {month: "February"})` ? 2 entities
4. Rerank results (0.6 * vector + 0.4 * graph)
5. LLM generates answer with context
6. Audit log: query + response + sources
7. UI displays answer + citations

**Response Time**: ~800ms (vector: 100ms, graph: 200ms, LLM: 500ms)

### Use Case 2: SQL Query Assistance

**User**: "How many vulnerabilities over 7.0 were there last month?"

**Flow**:
1. UI ? MAF ? Business Agent
2. Intent analysis: SQL query needed
3. Graph lookup: Vulnerability entity schema
4. SQL Generator: `SELECT COUNT(*) FROM vulnerabilities WHERE cvss_score > 7.0 AND discovered_at >= DATE_SUB(NOW(), INTERVAL 1 MONTH)`
5. **Guardrail**: Show SQL to user ? Require confirmation
6. User confirms ? RBAC validates (read-only query)
7. Execute query ? Result: 42 vulnerabilities
8. Audit log: SQL + result + user confirmation
9. UI displays result + query used

**Security**: No UPDATE/DELETE allowed, tenant-scoped tables only

### Use Case 3: OData Query Assistance

**User**: "Which customers are currently over 7.0 CVSS?"

**Flow**:
1. UI ? MAF ? MCP
2. Graph query: `MATCH (c:Customer)-[:HAS_VULNERABILITY]->(v:Vulnerability {cvss_score > 7.0})`
3. OData Generator: `/Customers?$filter=Vulnerabilities/any(v: v/CvssScore gt 7.0)`
4. **Guardrail**: Show OData query ? Require confirmation
5. User confirms ? Execute OData API call (tenant-scoped)
6. Results: 5 customers
7. Audit log: query + result count
8. UI displays table + export button

---

## Performance Targets

| Operation | Target | Current (Post-Phase 9) | Post-Phase 16 Goal |
|-----------|--------|------------------------|-------------------|
| **Document Chunking** | <1s per MB | 0.5s per MB | 0.3s per MB (Phase 13) |
| **LLM Enrichment** | <60s per 1000 chunks | N/A | <60s (batched) |
| **Entity Extraction** | <2s per chunk | N/A | <1.2s (optimized) |
| **Graph Query (2-hop)** | <200ms | N/A | <150ms |
| **Vector Search (top-10)** | <100ms | N/A | <80ms |
| **Hybrid Retrieval** | <600ms | N/A | <500ms |
| **Full Document Ingestion** | <10s per doc | ~2s (chunking only) | <8s (chunk + enrich + graph) |

---

## Cost Estimates

### LLM Costs (Phase 10+)

**Assumptions**:
- 1000 documents/month per tenant
- Average 20 chunks per document = 20,000 chunks/month
- GPT-4 Turbo pricing: $0.01 per 1K input tokens, $0.03 per 1K output tokens

**Per Tenant Per Month**:
- Chunk summarization: 20K chunks � 500 tokens input � $0.01/1K = **$100**
- Keyword extraction: 20K chunks � 300 tokens output � $0.03/1K = **$180**
- Entity extraction (NER): 20K chunks � 400 tokens output � $0.03/1K = **$240**
- **Total LLM cost: ~$520/month per tenant**

**Optimization Strategies**:
- Use GPT-3.5-turbo for simple tasks (5x cheaper)
- Batch API calls (50% discount)
- Cache frequent queries (Redis)
- Tenant tier pricing (free tier = gpt-3.5, enterprise = gpt-4)

### Infrastructure Costs (per tenant)

| Resource | Cost/Month |
|----------|-----------|
| PostgreSQL (100GB) | $25 |
| Vector DB (Qdrant Cloud, 1M vectors) | $50 |
| Blob Storage (100GB) | $5 |
| Redis Cache (1GB) | $15 |
| Compute (2 CPU, 4GB RAM) | $50 |
| **Total Infrastructure** | **$145** |

**Total Cost per Tenant**: ~$665/month (LLM + Infrastructure)

---

## Next Steps

### Immediate Actions (Week 1)

1. ? **Architecture Review** - Review this document with team
2. ? **Phase 10 Kickoff** - Create feature branch `phase-11-llm-integration`
3. ? **Development Environment** - Set up PostgreSQL 16 + Apache AGE locally
4. ? **OpenAI API Key** - Obtain Azure OpenAI or OpenAI API credentials
5. ? **Create TenantContext** - Implement core multi-tenant infrastructure

### Week 2-3: Phase 10 Implementation

1. ? Implement `ILLMProvider` + OpenAI client
2. ? Implement `IPromptManager` with Redis cache
3. ? Implement `ChunkEnricher` with batch processing
4. ? Write 20+ tests (unit + integration)
5. ? Update documentation (Phase-11.md completion)

### Week 4-6: Phase 21 Implementation

1. ? Install Apache AGE extension
2. ? Create tenant schema migration function
3. ? Implement `IGraphStore` with Cypher queries
4. ? Implement basic entity extraction
5. ? Write 15+ tests (graph operations, tenant isolation)

### Month 2-4: Phases 22-26

Continue with NER, relationships, query API, persistence, and RAG enhancement as specified in phase documents.

---

## Success Metrics

### Technical Metrics

- ? Zero cross-tenant data leakage (validated by tests)
- ? >80% code coverage across all components
- ? All phase success criteria met
- ? Performance targets achieved (see table above)
- ? Security audits passed (RBAC, RLS, input validation)

### Business Metrics

- ? 10+ tenants onboarded successfully
- ? >95% uptime (SLA)
- ? <2% LLM error rate
- ? >90% user satisfaction (query relevance)
- ? Cost per tenant <$700/month

### Knowledge Graph Quality

- ? Entity extraction precision >80%
- ? Entity extraction recall >70%
- ? Relationship extraction precision >70%
- ? Cross-document entity resolution accuracy >85%
- ? Hybrid retrieval NDCG@10 improvement >15% vs. vector-only

---

## Related Documents

- [Master Plan](../MasterPlan.md) - Project roadmap
- [Architecture Diagrams](ARCHITECTURE_DIAGRAMS.md) - Visual architecture
- [Sequence Diagrams](SEQUENCE_DIAGRAMS.md) - Workflow details
- [Component Specifications](COMPONENT_SPECIFICATIONS.md) - Code interfaces
- [Phase 10 Specification](../phases/Phase-11.md) - LLM integration details
- [Knowledge Graph Specification V2](../KNOWLEDGE_GRAPH_SPECIFICATION_V2.md) - KG design

---

**Document Status**: ? Complete - Ready for Implementation  
**Review Status**: Pending Team Review  
**Approval**: Pending Stakeholder Sign-off  
**Next Review Date**: After Phase 10 Completion
