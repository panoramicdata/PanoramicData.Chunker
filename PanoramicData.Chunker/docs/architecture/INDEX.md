# PanoramicData.Chunker - Architecture Documentation Index

Welcome to the comprehensive architecture documentation for PanoramicData.Chunker. This index will help you navigate the complete technical specifications for the multi-tenant RAG system with Knowledge Graph integration.

---

## ?? Documentation Structure

```
docs/architecture/
??? README.md  ? Start here for executive summary
??? ARCHITECTURE_DIAGRAMS.md  ? Visual architecture (Mermaid diagrams)
??? SEQUENCE_DIAGRAMS.md               ? Workflow details (13 sequence diagrams)
??? COMPONENT_SPECIFICATIONS.md        ? Code interfaces and schemas
??? INDEX.md  ? This file
```

---

## ?? Quick Start Guides

### For Architects
**Goal**: Understand system design decisions

1. Read [Architecture Summary](README.md#key-architectural-decisions)
2. Review [High-Level System Architecture](ARCHITECTURE_DIAGRAMS.md#high-level-system-architecture)
3. Study [ADRs](README.md#key-architectural-decisions) (Architecture Decision Records)
4. Review [Multi-Tenant Data Architecture](ARCHITECTURE_DIAGRAMS.md#multi-tenant-data-architecture)

### For Developers
**Goal**: Implement Phase 10-26 components

1. Read [Component Specifications](COMPONENT_SPECIFICATIONS.md)
2. Review [Sequence Diagrams](SEQUENCE_DIAGRAMS.md) for your phase:
   - Phase 10: [Document Enrichment Flow](SEQUENCE_DIAGRAMS.md#2-document-enrichment-with-llm-phase-11)
   - Phase 11-23: [Knowledge Graph Construction](SEQUENCE_DIAGRAMS.md#3-knowledge-graph-construction-phase-21-23)
   - Phase 24: [Graph Query API](COMPONENT_SPECIFICATIONS.md#igraphquerybuilder)
   - Phase 16: [Hybrid Search](SEQUENCE_DIAGRAMS.md#4-hybrid-search-vector--graph---phase-26)
3. Study [Interface Definitions](COMPONENT_SPECIFICATIONS.md) for your components
4. Check [PostgreSQL Schema](COMPONENT_SPECIFICATIONS.md#postgresql--apache-age-configuration)

### For Product Managers
**Goal**: Understand use cases and business value

1. Read [Executive Summary](README.md#executive-summary)
2. Review [Use Case Flows](README.md#use-case-flows)
3. Study [Success Metrics](README.md#success-metrics)
4. Check [Cost Estimates](README.md#cost-estimates)
5. Review [Implementation Phases](README.md#implementation-phases)

### For DevOps/SRE
**Goal**: Set up infrastructure and monitoring

1. Read [Technology Stack](README.md#technology-stack)
2. Review [Tenant Provisioning](SEQUENCE_DIAGRAMS.md#7-tenant-provisioning)
3. Study [Security & Compliance](README.md#security--compliance)
4. Check [Performance Targets](README.md#performance-targets)
5. Review [Multi-Tenant Isolation](SEQUENCE_DIAGRAMS.md#8-tenant-isolation-validation)

---

## ?? Diagram Reference

### System Architecture Diagrams

| Diagram | Document | Section | Purpose |
|---------|----------|---------|---------|
| **High-Level System Architecture** | [ARCHITECTURE_DIAGRAMS.md](ARCHITECTURE_DIAGRAMS.md#high-level-system-architecture) | Overview | Complete system with MCP, MAF, Storage layers |
| **Multi-Tenant Data Architecture** | [ARCHITECTURE_DIAGRAMS.md](ARCHITECTURE_DIAGRAMS.md#multi-tenant-data-architecture) | Tenant Isolation | Schema-per-tenant, RLS policies |
| **PostgreSQL Schema (ERD)** | [ARCHITECTURE_DIAGRAMS.md](ARCHITECTURE_DIAGRAMS.md#postgresql-multi-tenant-schema) | Database Design | Entity-Relationship diagram |
| **Knowledge Graph Architecture** | [ARCHITECTURE_DIAGRAMS.md](ARCHITECTURE_DIAGRAMS.md#knowledge-graph-architecture) | KG Components | Phases 11, 21-26 integration |
| **Phase Dependencies (Gantt)** | [ARCHITECTURE_DIAGRAMS.md](ARCHITECTURE_DIAGRAMS.md#phase-implementation-dependencies) | Timeline | 16-19 week roadmap |

### Workflow Sequence Diagrams

| Workflow | Document | Use Case | Phases |
|----------|----------|----------|--------|
| **Document Upload & Chunking** | [SEQUENCE_DIAGRAMS.md](SEQUENCE_DIAGRAMS.md#1-document-upload--chunking-phase-1-9) | Document ingestion | 1-9 |
| **LLM Enrichment** | [SEQUENCE_DIAGRAMS.md](SEQUENCE_DIAGRAMS.md#2-document-enrichment-with-llm-phase-11) | Chunk summarization | 11 |
| **Knowledge Graph Construction** | [SEQUENCE_DIAGRAMS.md](SEQUENCE_DIAGRAMS.md#3-knowledge-graph-construction-phase-21-23) | Entity/relationship extraction | 21-23 |
| **Hybrid Search (Vector+Graph)** | [SEQUENCE_DIAGRAMS.md](SEQUENCE_DIAGRAMS.md#4-hybrid-search-vector--graph---phase-26) | RAG retrieval | 26 |
| **SQL Query Generation** | [SEQUENCE_DIAGRAMS.md](SEQUENCE_DIAGRAMS.md#5-sql-query-generation-from-natural-language) | Natural language to SQL | 26 |
| **Cross-Document Entity Resolution** | [SEQUENCE_DIAGRAMS.md](SEQUENCE_DIAGRAMS.md#6-cross-document-entity-resolution) | Entity deduplication | 22 |
| **Tenant Provisioning** | [SEQUENCE_DIAGRAMS.md](SEQUENCE_DIAGRAMS.md#7-tenant-provisioning) | New tenant setup | 21 |
| **Tenant Isolation Validation** | [SEQUENCE_DIAGRAMS.md](SEQUENCE_DIAGRAMS.md#8-tenant-isolation-validation) | Security testing | 19 |
| **LLM Rate Limit Handling** | [SEQUENCE_DIAGRAMS.md](SEQUENCE_DIAGRAMS.md#9-llm-rate-limit--retry-logic) | Error recovery | 11 |
| **GDPR Data Export** | [SEQUENCE_DIAGRAMS.md](SEQUENCE_DIAGRAMS.md#13-data-export-for-gdpr-compliance) | Compliance | 19, 25 |

---

## ?? Component Reference

### Core Infrastructure (Phase 10)

| Component | Document | Section | Purpose |
|-----------|----------|---------|---------|
| **TenantContext** | [COMPONENT_SPECIFICATIONS.md](COMPONENT_SPECIFICATIONS.md#tenantcontext) | Multi-Tenant | Tenant isolation context |
| **ITenantService** | [COMPONENT_SPECIFICATIONS.md](COMPONENT_SPECIFICATIONS.md#itenantservice) | Multi-Tenant | Tenant provisioning |
| **TenantIsolationMiddleware** | [COMPONENT_SPECIFICATIONS.md](COMPONENT_SPECIFICATIONS.md#tenantisolationmiddleware) | Multi-Tenant | ASP.NET Core middleware |

### Storage Components (Phase 11)

| Component | Document | Section | Purpose |
|-----------|----------|---------|---------|
| **PostgreSQL Schema Migration** | [COMPONENT_SPECIFICATIONS.md](COMPONENT_SPECIFICATIONS.md#schema-migration-script) | Storage | Tenant schema setup |
| **IGraphStore** | [COMPONENT_SPECIFICATIONS.md](COMPONENT_SPECIFICATIONS.md#igraphstore-interface) | Storage | Graph database abstraction |
| **IVectorStore** | [COMPONENT_SPECIFICATIONS.md](COMPONENT_SPECIFICATIONS.md#vector-database-configuration) | Storage | Vector search abstraction |

### LLM Components (Phase 10)

| Component | Document | Section | Purpose |
|-----------|----------|---------|---------|
| **ILLMProvider** | [COMPONENT_SPECIFICATIONS.md](COMPONENT_SPECIFICATIONS.md#illmprovider) | LLM | LLM abstraction (OpenAI, Azure) |
| **ChunkEnricher** | [COMPONENT_SPECIFICATIONS.md](COMPONENT_SPECIFICATIONS.md#chunkenricher) | LLM | Summarization, keywords, NER |
| **IPromptManager** | [COMPONENT_SPECIFICATIONS.md](COMPONENT_SPECIFICATIONS.md#ipromptmanager) | LLM | Tenant-specific prompts |

### Knowledge Graph Components (Phase 11-23)

| Component | Document | Section | Purpose |
|-----------|----------|---------|---------|
| **Entity** | [COMPONENT_SPECIFICATIONS.md](COMPONENT_SPECIFICATIONS.md#entity) | KG Models | Entity data model |
| **Relationship** | [COMPONENT_SPECIFICATIONS.md](COMPONENT_SPECIFICATIONS.md#relationship) | KG Models | Relationship data model |
| **IEntityExtractor** | [COMPONENT_SPECIFICATIONS.md](COMPONENT_SPECIFICATIONS.md#ientityextractor) | KG | LLM-based NER |
| **IEntityDeduplicator** | [COMPONENT_SPECIFICATIONS.md](COMPONENT_SPECIFICATIONS.md#ientitydeduplicator) | KG | Entity deduplication |
| **IRelationshipExtractor** | [COMPONENT_SPECIFICATIONS.md](COMPONENT_SPECIFICATIONS.md#irelationshipextractor) | KG | Relationship extraction |

### Query & Retrieval (Phase 24, 26)

| Component | Document | Section | Purpose |
|-----------|----------|---------|---------|
| **IGraphQueryBuilder** | [COMPONENT_SPECIFICATIONS.md](COMPONENT_SPECIFICATIONS.md#igraphquerybuilder) | Query | LINQ-style graph queries |
| **ICypherQueryEngine** | [COMPONENT_SPECIFICATIONS.md](COMPONENT_SPECIFICATIONS.md#icypherqueryengine) | Query | Cypher query execution |
| **IHybridRetriever** | [COMPONENT_SPECIFICATIONS.md](COMPONENT_SPECIFICATIONS.md#ihybridretriever) | Retrieval | Vector + Graph search |

### Security & Compliance (Phase 19)

| Component | Document | Section | Purpose |
|-----------|----------|---------|---------|
| **IRBACService** | [COMPONENT_SPECIFICATIONS.md](COMPONENT_SPECIFICATIONS.md#irbacservice) | Security | Multi-tenant authorization |
| **IAuditLogger** | [COMPONENT_SPECIFICATIONS.md](COMPONENT_SPECIFICATIONS.md#iauditlogger) | Security | Compliance logging |

---

## ?? Use Case Walkthroughs

### Use Case 1: Help Query - "How do I view last February's report?"

**Documents to Read**:
1. [Use Case Flow](README.md#use-case-1-general-help-query) - High-level overview
2. [Hybrid Search Sequence Diagram](SEQUENCE_DIAGRAMS.md#4-hybrid-search-vector--graph---phase-26) - Detailed flow
3. [IHybridRetriever Interface](COMPONENT_SPECIFICATIONS.md#ihybridretriever) - Implementation spec

**Key Technologies**:
- Vector search (pgvector/Qdrant)
- Graph traversal (Apache AGE)
- LLM generation (GPT-4)
- Response ranking

### Use Case 2: SQL Query Assistance - "How many vulnerabilities over 7.0 last month?"

**Documents to Read**:
1. [Use Case Flow](README.md#use-case-2-sql-query-assistance) - High-level overview
2. [SQL Generation Sequence Diagram](SEQUENCE_DIAGRAMS.md#5-sql-query-generation-from-natural-language) - Detailed flow
3. [Knowledge Graph Schema](ARCHITECTURE_DIAGRAMS.md#postgresql-multi-tenant-schema) - Entity relationships

**Key Technologies**:
- Intent analysis (LLM)
- Schema introspection (PostgreSQL)
- Query generation
- RBAC validation
- Audit logging

### Use Case 3: OData Query - "Which customers are over 7.0 CVSS?"

**Documents to Read**:
1. [Use Case Flow](README.md#use-case-3-odata-query-assistance) - High-level overview
2. [Sequence Diagram reference](SEQUENCE_DIAGRAMS.md#use-case-3-odata-query-assistance) - Detailed flow (in main docs)
3. [Graph Query API](COMPONENT_SPECIFICATIONS.md#igraphquerybuilder) - Query building

**Key Technologies**:
- Graph relationship traversal
- OData query generation
- Tenant-scoped filtering
- Result presentation

---

## ?? Security Documentation

### Multi-Tenant Isolation

**Documents**:
- [Tenant Isolation Architecture](ARCHITECTURE_DIAGRAMS.md#tenant-isolation-model)
- [Tenant Provisioning Workflow](SEQUENCE_DIAGRAMS.md#7-tenant-provisioning)
- [Isolation Validation Tests](SEQUENCE_DIAGRAMS.md#8-tenant-isolation-validation)
- [TenantContext Implementation](COMPONENT_SPECIFICATIONS.md#tenantcontext)

**Key Controls**:
1. X-Tenant-ID header validation
2. PostgreSQL Row-Level Security (RLS)
3. Schema-per-tenant isolation
4. Vector DB partitioning
5. Blob storage containers

### RBAC & Authorization

**Documents**:
- [IRBACService Interface](COMPONENT_SPECIFICATIONS.md#irbacservice)
- [Authorization Flow](SEQUENCE_DIAGRAMS.md#use-case-2-sql-query-assistance) (see guardrails)
- [Security Model](README.md#security--compliance)

**Roles**:
- Viewer (read-only)
- Editor (read + write)
- Admin (manage users + config)
- SuperAdmin (cross-tenant access)

### Audit Logging

**Documents**:
- [IAuditLogger Interface](COMPONENT_SPECIFICATIONS.md#iauditlogger)
- [GDPR Export Flow](SEQUENCE_DIAGRAMS.md#13-data-export-for-gdpr-compliance)
- [Compliance Requirements](README.md#gdpr-compliance)

**Logged Events**:
- All queries (text, results count, latency)
- Data exports (user, tenant, record count)
- Access denials (security incidents)
- Configuration changes

---

## ?? Performance & Optimization

### Performance Targets

See [Performance Targets Table](README.md#performance-targets)

**Key Metrics**:
- Document chunking: <1s per MB
- LLM enrichment: <60s per 1000 chunks (batched)
- Graph query (2-hop): <200ms
- Hybrid retrieval: <600ms

### Optimization Strategies

**Documents**:
- [Caching Strategy](SEQUENCE_DIAGRAMS.md#11-caching-strategy-phase-13)
- [Batch Processing](SEQUENCE_DIAGRAMS.md#12-batch-processing-pipeline)
- [Cost Estimates](README.md#cost-estimates)

**Techniques**:
- Redis caching (hot queries)
- Batch LLM calls (50% cost reduction)
- Stale-while-revalidate pattern
- Parallel processing (Phase 13)

---

## ?? Cost Analysis

### LLM Costs

See [LLM Cost Estimates](README.md#llm-costs-phase-11)

**Per Tenant Per Month**: ~$520
- Summarization: $100
- Keywords: $180
- NER: $240

**Optimization**:
- Use GPT-3.5-turbo for simple tasks (5x cheaper)
- Batch API (50% discount)
- Cache frequent queries

### Infrastructure Costs

See [Infrastructure Costs Table](README.md#infrastructure-costs-per-tenant)

**Per Tenant Per Month**: ~$145
- PostgreSQL (100GB): $25
- Vector DB (1M vectors): $50
- Storage: $5
- Cache: $15
- Compute: $50

**Total**: ~$665/month per tenant

---

## ??? Implementation Roadmap

### Phase Timeline

See [Gantt Chart](ARCHITECTURE_DIAGRAMS.md#phase-implementation-dependencies)

**16-19 Weeks Total**:
1. Phase 10 (LLM): 2-3 weeks
2. Phase 11 (KG Foundation): 3 weeks
3. Phase 12 (NER): 3 weeks
4. Phase 23 (Relationships): 3 weeks
5. Phase 24 (Query API): 3 weeks
6. Phase 25 (Persistence): 2 weeks
7. Phase 16 (RAG): 2 weeks

### Current Status

**Completed**: Phases 0-9 (Document chunking for 9 formats)  
**Next**: Phase 10 (LLM Integration) ??  
**Goal**: Q3 2025 - Complete Knowledge Graph system

---

## ?? Document Versions

| Document | Version | Last Updated | Status |
|----------|---------|--------------|--------|
| README.md | 1.0 | Jan 2025 | ? Complete |
| ARCHITECTURE_DIAGRAMS.md | 1.0 | Jan 2025 | ? Complete |
| SEQUENCE_DIAGRAMS.md | 1.0 | Jan 2025 | ? Complete |
| COMPONENT_SPECIFICATIONS.md | 1.0 | Jan 2025 | ? Complete |
| INDEX.md | 1.0 | Jan 2025 | ? Complete |

---

## ?? Contributing

### Document Update Process

1. **Propose Changes**: Open GitHub issue with "Architecture" label
2. **Review**: Team review (architects + lead developers)
3. **Update**: Modify relevant documents
4. **Version**: Increment version number
5. **Communicate**: Announce changes in team standup

### Review Schedule

- **Weekly**: Phase implementation progress
- **Bi-weekly**: Architecture refinements
- **Monthly**: ADR review (new decisions)
- **Quarterly**: Full architecture audit

---

## ?? Contact & Support

### Architecture Questions

**Slack**: #panoramicdata-chunker-architecture  
**Email**: architecture@panoramicdata.com

### Document Feedback

**GitHub Issues**: https://github.com/panoramicdata/PanoramicData.Chunker/issues  
**Label**: `documentation` + `architecture`

---

**Last Updated**: January 2025  
**Document Owner**: PanoramicData Architecture Team  
**Next Review**: After Phase 10 Completion
