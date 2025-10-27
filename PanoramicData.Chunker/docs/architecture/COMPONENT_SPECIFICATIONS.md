# PanoramicData.Chunker - Component Specifications

## Table of Contents
1. [Multi-Tenant Infrastructure](#multi-tenant-infrastructure)
2. [Storage Components](#storage-components)
3. [LLM Integration Components](#llm-integration-components)
4. [Knowledge Graph Components](#knowledge-graph-components)
5. [Query & Retrieval Components](#query--retrieval-components)
6. [Security & Compliance Components](#security--compliance-components)

---

## Multi-Tenant Infrastructure

### TenantContext

**Phase**: 11  
**Namespace**: `PanoramicData.Chunker.Core.MultiTenant`

```csharp
/// <summary>
/// Encapsulates tenant-specific context for data isolation and configuration.
/// </summary>
public record TenantContext
{
    /// <summary>
    /// Unique tenant identifier (e.g., "tenant-a", "acme-corp").
    /// </summary>
    public required string TenantId { get; init; }

    /// <summary>
    /// PostgreSQL schema name for tenant-isolated data.
    /// </summary>
    public required string Schema { get; init; }

    /// <summary>
    /// Tenant-specific database connection string (with RLS enabled).
    /// </summary>
    public required string ConnectionString { get; init; }

  /// <summary>
    /// Vector database partition identifier for tenant isolation.
 /// </summary>
    public required string VectorPartition { get; init; }

    /// <summary>
    /// Blob storage container name for tenant documents.
    /// </summary>
    public required string StorageContainer { get; init; }

    /// <summary>
    /// Tenant-specific configuration (LLM model, max chunks, etc.).
    /// </summary>
    public required IReadOnlyDictionary<string, string> Configuration { get; init; }

    /// <summary>
    /// Subscription plan (free, professional, enterprise).
    /// </summary>
    public required string Plan { get; init; }

    /// <summary>
    /// Tenant status (active, suspended, deleted).
    /// </summary>
    public required TenantStatus Status { get; init; }

    /// <summary>
    /// Tenant creation timestamp.
    /// </summary>
    public required DateTimeOffset CreatedAt { get; init; }
}

public enum TenantStatus
{
    Active,
    Suspended,
    Deleted
}
```

### ITenantService

```csharp
/// <summary>
/// Service for managing tenant provisioning and configuration.
/// </summary>
public interface ITenantService
{
    /// <summary>
    /// Provisions a new tenant with isolated resources.
    /// </summary>
    Task<TenantContext> CreateTenantAsync(
        string tenantId,
   string plan,
 CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves tenant context by ID.
    /// </summary>
    Task<TenantContext> GetTenantAsync(
     string tenantId,
 CancellationToken cancellationToken = default);

    /// <summary>
  /// Updates tenant configuration.
    /// </summary>
    Task UpdateTenantConfigAsync(
     string tenantId,
     IDictionary<string, string> configuration,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Soft-deletes a tenant and schedules data purge.
    /// </summary>
    Task DeleteTenantAsync(
        string tenantId,
        CancellationToken cancellationToken = default);

    /// <summary>
  /// Lists all tenants (admin only).
    /// </summary>
    Task<IEnumerable<TenantContext>> ListTenantsAsync(
        CancellationToken cancellationToken = default);
}
```

### TenantIsolationMiddleware

```csharp
/// <summary>
/// ASP.NET Core middleware to extract and validate tenant context from requests.
/// </summary>
public class TenantIsolationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ITenantService _tenantService;

    public TenantIsolationMiddleware(
        RequestDelegate next,
        ITenantService tenantService)
    {
      _next = next;
        _tenantService = tenantService;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Extract tenant ID from header or JWT claim
        var tenantId = context.Request.Headers["X-Tenant-ID"].FirstOrDefault()
            ?? context.User.FindFirst("tenant_id")?.Value;

        if (string.IsNullOrEmpty(tenantId))
{
     context.Response.StatusCode = 400;
    await context.Response.WriteAsync("Missing tenant identifier");
            return;
        }

      // Load tenant context
        var tenant = await _tenantService.GetTenantAsync(tenantId);

        if (tenant.Status != TenantStatus.Active)
    {
        context.Response.StatusCode = 403;
  await context.Response.WriteAsync($"Tenant {tenantId} is {tenant.Status}");
            return;
        }

        // Store in HttpContext for downstream access
        context.Items["TenantContext"] = tenant;

        await _next(context);
    }
}
```

---

## Storage Components

### PostgreSQL + Apache AGE Configuration

**Phase**: 21  
**Namespace**: `PanoramicData.Chunker.Storage.Graph`

#### Schema Migration Script

```sql
-- Central schema (shared across all tenants)
CREATE SCHEMA IF NOT EXISTS central;

-- Central entity types
CREATE TABLE central.entity_types (
    entity_type_id SERIAL PRIMARY KEY,
    name VARCHAR(100) NOT NULL UNIQUE,
    description TEXT,
    schema JSONB,
    created_at TIMESTAMP DEFAULT NOW()
);

INSERT INTO central.entity_types (name, description) VALUES
    ('Person', 'Individual human entity'),
    ('Organization', 'Company or institution'),
    ('Location', 'Geographic location'),
    ('Product', 'Product or service'),
    ('Event', 'Temporal event'),
    ('Date', 'Date or time reference'),
    ('Vulnerability', 'Security vulnerability');

-- Central relationship types
CREATE TABLE central.relationship_types (
    relationship_type_id SERIAL PRIMARY KEY,
    name VARCHAR(100) NOT NULL UNIQUE,
    description TEXT,
    source_entity_type_id INT REFERENCES central.entity_types(entity_type_id),
    target_entity_type_id INT REFERENCES central.entity_types(entity_type_id),
    constraints JSONB,
 created_at TIMESTAMP DEFAULT NOW()
);

INSERT INTO central.relationship_types (name, source_entity_type_id, target_entity_type_id) VALUES
    ('WORKS_AT', 1, 2),       -- Person -> Organization
    ('LOCATED_IN', 2, 3),     -- Organization -> Location
    ('HAS_VULNERABILITY', 2, 7), -- Organization -> Vulnerability
    ('AFFECTS', 7, 4);        -- Vulnerability -> Product

-- Tenant-specific schema template (executed per tenant)
CREATE OR REPLACE FUNCTION create_tenant_schema(tenant_id TEXT)
RETURNS VOID AS $$
BEGIN
    -- Create tenant schema
    EXECUTE format('CREATE SCHEMA IF NOT EXISTS %I', tenant_id);

    -- Create entities table
    EXECUTE format('
        CREATE TABLE %I.entities (
    entity_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
            tenant_id TEXT NOT NULL DEFAULT %L,
            entity_type_id INT REFERENCES central.entity_types(entity_type_id),
    name TEXT NOT NULL,
  properties JSONB,
          aliases TEXT[],
      source_chunks UUID[],
        confidence FLOAT CHECK (confidence BETWEEN 0 AND 1),
            created_at TIMESTAMP DEFAULT NOW(),
 updated_at TIMESTAMP DEFAULT NOW()
        )', tenant_id, tenant_id);

    -- Create relationships table
    EXECUTE format('
        CREATE TABLE %I.relationships (
          relationship_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
      tenant_id TEXT NOT NULL DEFAULT %L,
         source_entity_id UUID REFERENCES %I.entities(entity_id) ON DELETE CASCADE,
            target_entity_id UUID REFERENCES %I.entities(entity_id) ON DELETE CASCADE,
          relationship_type_id INT REFERENCES central.relationship_types(relationship_type_id),
    properties JSONB,
            confidence FLOAT CHECK (confidence BETWEEN 0 AND 1),
    created_at TIMESTAMP DEFAULT NOW()
 )', tenant_id, tenant_id, tenant_id, tenant_id);

    -- Create chunks table
    EXECUTE format('
        CREATE TABLE %I.chunks (
            chunk_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
 tenant_id TEXT NOT NULL DEFAULT %L,
    document_id UUID NOT NULL,
            chunk_type TEXT NOT NULL,
  content TEXT NOT NULL,
            metadata JSONB,
            embedding VECTOR(1536),  -- OpenAI ada-002 dimension
  token_count INT,
       created_by TEXT,
            created_at TIMESTAMP DEFAULT NOW()
        )', tenant_id, tenant_id);

    -- Create indexes
    EXECUTE format('CREATE INDEX idx_%I_entities_type ON %I.entities(entity_type_id)', tenant_id, tenant_id);
    EXECUTE format('CREATE INDEX idx_%I_entities_name ON %I.entities USING GIN(name gin_trgm_ops)', tenant_id, tenant_id);
    EXECUTE format('CREATE INDEX idx_%I_relationships_source ON %I.relationships(source_entity_id)', tenant_id, tenant_id);
    EXECUTE format('CREATE INDEX idx_%I_relationships_target ON %I.relationships(target_entity_id)', tenant_id, tenant_id);
    EXECUTE format('CREATE INDEX idx_%I_chunks_document ON %I.chunks(document_id)', tenant_id, tenant_id);
    EXECUTE format('CREATE INDEX idx_%I_chunks_embedding ON %I.chunks USING ivfflat(embedding vector_cosine_ops)', tenant_id, tenant_id);

    -- Enable Row-Level Security
    EXECUTE format('ALTER TABLE %I.entities ENABLE ROW LEVEL SECURITY', tenant_id);
    EXECUTE format('ALTER TABLE %I.relationships ENABLE ROW LEVEL SECURITY', tenant_id);
    EXECUTE format('ALTER TABLE %I.chunks ENABLE ROW LEVEL SECURITY', tenant_id);

    -- Create RLS policies
    EXECUTE format('
        CREATE POLICY tenant_isolation_entities ON %I.entities
 USING (tenant_id = current_setting(''app.tenant_id'', true))
    ', tenant_id);

    EXECUTE format('
      CREATE POLICY tenant_isolation_relationships ON %I.relationships
      USING (tenant_id = current_setting(''app.tenant_id'', true))
    ', tenant_id);

    EXECUTE format('
 CREATE POLICY tenant_isolation_chunks ON %I.chunks
        USING (tenant_id = current_setting(''app.tenant_id'', true))
    ', tenant_id);

    -- Create Apache AGE graph
    PERFORM create_graph(tenant_id);

END;
$$ LANGUAGE plpgsql;
```

#### IGraphStore Interface

```csharp
/// <summary>
/// Abstraction for graph database operations with tenant isolation.
/// </summary>
public interface IGraphStore
{
    /// <summary>
  /// Creates a new entity in the tenant's graph.
    /// </summary>
    Task<Guid> CreateEntityAsync(
   Entity entity,
        TenantContext tenant,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a relationship between two entities.
    /// </summary>
    Task<Guid> CreateRelationshipAsync(
        Relationship relationship,
        TenantContext tenant,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes a Cypher query against the tenant's graph.
    /// </summary>
    Task<IEnumerable<T>> QueryAsync<T>(
        string cypherQuery,
     IDictionary<string, object> parameters,
     TenantContext tenant,
    CancellationToken cancellationToken = default);

    /// <summary>
    /// Finds entities by name (fuzzy match).
    /// </summary>
    Task<IEnumerable<Entity>> FindEntitiesByNameAsync(
        string name,
        double similarityThreshold,
        TenantContext tenant,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets neighbors of an entity (graph traversal).
    /// </summary>
    Task<IEnumerable<Entity>> GetNeighborsAsync(
        Guid entityId,
     int maxDepth,
        TenantContext tenant,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes an entity and all its relationships.
    /// </summary>
    Task DeleteEntityAsync(
        Guid entityId,
        TenantContext tenant,
    CancellationToken cancellationToken = default);
}
```

### Vector Database Configuration

**Phase**: 12  
**Namespace**: `PanoramicData.Chunker.Storage.Vector`

```csharp
/// <summary>
/// Configuration for tenant-aware vector database (Qdrant example).
/// </summary>
public class VectorStoreOptions
{
  public required string Endpoint { get; init; }
    public required string ApiKey { get; init; }
    public required int Dimension { get; init; } = 1536; // OpenAI ada-002
    public required string DistanceMetric { get; init; } = "Cosine";
}

public interface IVectorStore
{
    /// <summary>
    /// Indexes a chunk's embedding in the tenant's partition.
    /// </summary>
Task IndexAsync(
     Guid chunkId,
        float[] embedding,
        IDictionary<string, object> metadata,
     TenantContext tenant,
CancellationToken cancellationToken = default);

    /// <summary>
    /// Performs vector similarity search within tenant partition.
    /// </summary>
    Task<IEnumerable<SearchResult>> SearchAsync(
        float[] queryEmbedding,
 int topK,
        TenantContext tenant,
    IDictionary<string, object>? filters = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes all vectors for a document.
    /// </summary>
    Task DeleteByDocumentAsync(
        Guid documentId,
        TenantContext tenant,
    CancellationToken cancellationToken = default);
}

public record SearchResult(
    Guid ChunkId,
    float Score,
    IDictionary<string, object> Metadata);
```

---

## LLM Integration Components

### ILLMProvider

**Phase**: 11  
**Namespace**: `PanoramicData.Chunker.LLM`

```csharp
/// <summary>
/// Abstraction for LLM providers (OpenAI, Azure OpenAI, local models).
/// </summary>
public interface ILLMProvider
{
    /// <summary>
    /// Generates text completion from a prompt.
    /// </summary>
  Task<LLMResponse> GenerateAsync(
        string prompt,
    LLMOptions options,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates embeddings for text.
    /// </summary>
    Task<float[]> GenerateEmbeddingAsync(
        string text,
        CancellationToken cancellationToken = default);

/// <summary>
    /// Batch generation (cost optimization).
    /// </summary>
    Task<IEnumerable<LLMResponse>> GenerateBatchAsync(
   IEnumerable<string> prompts,
        LLMOptions options,
   CancellationToken cancellationToken = default);
}

public record LLMOptions
{
    public string Model { get; init; } = "gpt-4";
    public double Temperature { get; init; } = 0.7;
    public int MaxTokens { get; init; } = 1000;
    public double TopP { get; init; } = 1.0;
    public int FrequencyPenalty { get; init; } = 0;
    public int PresencePenalty { get; init; } = 0;
  public string[]? StopSequences { get; init; }
}

public record LLMResponse
{
    public required string Text { get; init; }
    public required int TokensUsed { get; init; }
public required string Model { get; init; }
public required string FinishReason { get; init; }
    public double? Confidence { get; init; }
}
```

### ChunkEnricher

```csharp
/// <summary>
/// Enriches chunks with summaries, keywords, and preliminary entities.
/// </summary>
public class ChunkEnricher : IChunkEnricher
{
    private readonly ILLMProvider _llmProvider;
    private readonly IPromptManager _promptManager;
    private readonly ILogger<ChunkEnricher> _logger;

    public async Task<EnrichedChunk> EnrichAsync(
    Chunk chunk,
        TenantContext tenant,
        CancellationToken cancellationToken = default)
    {
        // Get tenant-specific or default prompts
  var summarizePrompt = await _promptManager.GetPromptAsync(
  "summarize",
            tenant,
    cancellationToken);

        var keywordsPrompt = await _promptManager.GetPromptAsync(
         "extract_keywords",
            tenant,
     cancellationToken);

        var entitiesPrompt = await _promptManager.GetPromptAsync(
            "extract_entities_preliminary",
       tenant,
    cancellationToken);

        // Render prompts with chunk content
   var summarizeRendered = summarizePrompt.Replace("{content}", chunk.Content);
      var keywordsRendered = keywordsPrompt.Replace("{content}", chunk.Content);
   var entitiesRendered = entitiesPrompt.Replace("{content}", chunk.Content);

        // Batch LLM calls for efficiency
        var responses = await _llmProvider.GenerateBatchAsync(
    new[] { summarizeRendered, keywordsRendered, entitiesRendered },
     new LLMOptions { Model = tenant.Configuration.GetValueOrDefault("llm_model", "gpt-4") },
   cancellationToken);

      var responseList = responses.ToList();

  return new EnrichedChunk
        {
       ChunkId = chunk.Id,
   Summary = responseList[0].Text,
            Keywords = ParseKeywords(responseList[1].Text),
            PreliminaryEntities = ParseEntities(responseList[2].Text),
   TokensUsed = responseList.Sum(r => r.TokensUsed)
     };
    }

    private List<string> ParseKeywords(string llmOutput)
    {
    // Parse comma-separated keywords or JSON array
        return llmOutput
            .Split(',')
            .Select(k => k.Trim())
       .Where(k => !string.IsNullOrEmpty(k))
            .ToList();
    }

    private List<PreliminaryEntity> ParseEntities(string llmOutput)
    {
// Parse JSON output from LLM
   try
        {
    return JsonSerializer.Deserialize<List<PreliminaryEntity>>(llmOutput)
      ?? new List<PreliminaryEntity>();
   }
        catch
     {
            _logger.LogWarning("Failed to parse entities from LLM output: {Output}", llmOutput);
         return new List<PreliminaryEntity>();
        }
 }
}

public record EnrichedChunk
{
    public required Guid ChunkId { get; init; }
    public required string Summary { get; init; }
    public required List<string> Keywords { get; init; }
    public required List<PreliminaryEntity> PreliminaryEntities { get; init; }
    public required int TokensUsed { get; init; }
}

public record PreliminaryEntity
{
    public required string Name { get; init; }
    public required string Type { get; init; } // "Person", "Organization", etc.
    public double Confidence { get; init; }
}
```

### IPromptManager

```csharp
/// <summary>
/// Manages prompt templates with tenant-specific overrides.
/// </summary>
public interface IPromptManager
{
    /// <summary>
    /// Retrieves a prompt template by name, with tenant override support.
    /// </summary>
    Task<string> GetPromptAsync(
        string promptName,
        TenantContext tenant,
  CancellationToken cancellationToken = default);

/// <summary>
 /// Updates a tenant-specific prompt override.
    /// </summary>
    Task SetTenantPromptAsync(
        string promptName,
        string promptTemplate,
        TenantContext tenant,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists all available prompts.
 /// </summary>
    Task<IEnumerable<PromptMetadata>> ListPromptsAsync(
   CancellationToken cancellationToken = default);
}

public record PromptMetadata
{
    public required string Name { get; init; }
    public required string Description { get; init; }
    public required string DefaultTemplate { get; init; }
    public required string[] Variables { get; init; }
}
```

---

## Knowledge Graph Components

### Entity

**Phase**: 22  
**Namespace**: `PanoramicData.Chunker.KnowledgeGraph.Models`

```csharp
/// <summary>
/// Represents an entity in the knowledge graph.
/// </summary>
public record Entity
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public required string Name { get; init; }
    public required EntityType Type { get; init; }
    public Dictionary<string, object> Properties { get; init; } = new();
    public List<string> Aliases { get; init; } = new();
    public List<Guid> SourceChunks { get; init; } = new();
    public double Confidence { get; init; } = 1.0;
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; init; } = DateTimeOffset.UtcNow;
}

public enum EntityType
{
    Person,
    Organization,
    Location,
    Product,
    Event,
    Date,
    Vulnerability,
    Other
}
```

### Relationship

```csharp
/// <summary>
/// Represents a relationship between two entities.
/// </summary>
public record Relationship
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public required Guid SourceEntityId { get; init; }
    public required Guid TargetEntityId { get; init; }
    public required RelationshipType Type { get; init; }
    public Dictionary<string, object> Properties { get; init; } = new();
    public double Confidence { get; init; } = 1.0;
    public Guid? SourceChunkId { get; init; }
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
}

public enum RelationshipType
{
    WorksAt,
    LocatedIn,
    PartOf,
    Owns,
    Manages,
 Mentions,
    RelatesTo,
    HasVulnerability,
    Affects,
    DependsOn,
    Other
}
```

### IEntityExtractor

```csharp
/// <summary>
/// Extracts named entities from chunks using LLM-based NER.
/// </summary>
public interface IEntityExtractor
{
  /// <summary>
    /// Extracts entities from a chunk.
    /// </summary>
    Task<IEnumerable<Entity>> ExtractAsync(
      Chunk chunk,
        TenantContext tenant,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Batch extraction for efficiency.
    /// </summary>
    Task<IDictionary<Guid, IEnumerable<Entity>>> ExtractBatchAsync(
    IEnumerable<Chunk> chunks,
        TenantContext tenant,
        CancellationToken cancellationToken = default);
}
```

### IEntityDeduplicator

**Phase**: 22

```csharp
/// <summary>
/// Deduplicates entities within a tenant's knowledge graph.
/// </summary>
public interface IEntityDeduplicator
{
    /// <summary>
 /// Finds existing entities similar to the candidate.
    /// </summary>
    Task<IEnumerable<EntityMatch>> FindSimilarEntitiesAsync(
        Entity candidate,
        TenantContext tenant,
        double similarityThreshold = 0.85,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Merges a candidate entity with an existing entity.
    /// </summary>
    Task<Entity> MergeEntitiesAsync(
     Guid existingEntityId,
        Entity candidate,
        TenantContext tenant,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Calculates similarity between two entity names.
    /// </summary>
    double CalculateSimilarity(string name1, string name2);
}

public record EntityMatch
{
    public required Entity Entity { get; init; }
    public required double SimilarityScore { get; init; }
    public required SimilarityReason Reason { get; init; }
}

public enum SimilarityReason
{
    ExactMatch,
    FuzzyMatch,
    AliasMatch,
    EmbeddingSimilarity
}
```

### IRelationshipExtractor

**Phase**: 23

```csharp
/// <summary>
/// Extracts relationships between entities from text.
/// </summary>
public interface IRelationshipExtractor
{
    /// <summary>
    /// Extracts relationships from a chunk with known entities.
    /// </summary>
    Task<IEnumerable<Relationship>> ExtractAsync(
        Chunk chunk,
        IEnumerable<Entity> entities,
        TenantContext tenant,
    CancellationToken cancellationToken = default);

    /// <summary>
    /// Extracts relationships using dependency parsing.
    /// </summary>
    Task<IEnumerable<Relationship>> ExtractFromDependenciesAsync(
        Chunk chunk,
        IEnumerable<Entity> entities,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Applies domain-specific relationship extraction rules.
    /// </summary>
 Task<IEnumerable<Relationship>> ExtractDomainSpecificAsync(
        Chunk chunk,
   IEnumerable<Entity> entities,
 string domain,
        TenantContext tenant,
        CancellationToken cancellationToken = default);
}
```

---

## Query & Retrieval Components

### IGraphQueryBuilder

**Phase**: 24  
**Namespace**: `PanoramicData.Chunker.Query`

```csharp
/// <summary>
/// LINQ-style query builder for knowledge graphs.
/// </summary>
public interface IGraphQueryBuilder
{
    /// <summary>
    /// Starts a query for entities of a specific type.
    /// </summary>
    IEntityQuery<T> Entities<T>(TenantContext tenant) where T : Entity;

    /// <summary>
 /// Starts a query for relationships.
    /// </summary>
  IRelationshipQuery Relationships(TenantContext tenant);
}

public interface IEntityQuery<T> where T : Entity
{
    IEntityQuery<T> Where(Expression<Func<T, bool>> predicate);
    IEntityQuery<T> WithProperty(string key, object value);
    IEntityQuery<T> OrderBy(Expression<Func<T, object>> selector);
    IEntityQuery<T> Take(int count);
    IEntityQuery<T> Skip(int count);
    IEntityQuery<T> WithNeighbors(int maxDepth = 1);
    Task<IEnumerable<T>> ToListAsync(CancellationToken cancellationToken = default);
    Task<T?> FirstOrDefaultAsync(CancellationToken cancellationToken = default);
    Task<int> CountAsync(CancellationToken cancellationToken = default);
}

// Example usage:
// var people = await queryBuilder
//     .Entities<Person>(tenant)
//     .Where(p => p.Name.Contains("Smith"))
//     .WithProperty("role", "CEO")
//     .WithNeighbors(depth: 2)
// .ToListAsync();
```

### ICypherQueryEngine

```csharp
/// <summary>
/// Executes Cypher queries against the knowledge graph.
/// </summary>
public interface ICypherQueryEngine
{
    /// <summary>
    /// Executes a Cypher query and returns results.
    /// </summary>
    Task<CypherResult> ExecuteAsync(
        string cypherQuery,
        IDictionary<string, object> parameters,
        TenantContext tenant,
    CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates a Cypher query for security (no DROP, DELETE outside tenant).
    /// </summary>
    Task<ValidationResult> ValidateQueryAsync(
    string cypherQuery,
        TenantContext tenant,
        CancellationToken cancellationToken = default);
}

public record CypherResult
{
    public required IEnumerable<IReadOnlyDictionary<string, object>> Rows { get; init; }
    public required IEnumerable<string> Columns { get; init; }
    public required int RowCount { get; init; }
    public required TimeSpan ExecutionTime { get; init; }
}
```

### IHybridRetriever

**Phase**: 26

```csharp
/// <summary>
/// Hybrid retrieval combining vector search and graph traversal.
/// </summary>
public interface IHybridRetriever
{
    /// <summary>
    /// Retrieves results using both vector and graph search.
    /// </summary>
    Task<HybridSearchResult> RetrieveAsync(
        string query,
        TenantContext tenant,
        HybridSearchOptions options,
        CancellationToken cancellationToken = default);

  /// <summary>
    /// Expands context using graph traversal.
    /// </summary>
    Task<ExpandedContext> ExpandContextAsync(
        IEnumerable<Chunk> chunks,
        TenantContext tenant,
        int maxDepth = 2,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Reranks results using graph-aware scoring.
/// </summary>
    Task<IEnumerable<RankedResult>> RerankAsync(
 IEnumerable<SearchResult> vectorResults,
        IEnumerable<Entity> graphResults,
        string query,
        CancellationToken cancellationToken = default);
}

public record HybridSearchOptions
{
    public int VectorTopK { get; init; } = 20;
    public int GraphMaxDepth { get; init; } = 2;
    public double VectorWeight { get; init; } = 0.6;
    public double GraphWeight { get; init; } = 0.4;
    public bool ExpandContext { get; init; } = true;
}

public record HybridSearchResult
{
    public required IEnumerable<RankedResult> Results { get; init; }
    public required ExpandedContext? Context { get; init; }
    public required SearchMetrics Metrics { get; init; }
}

public record ExpandedContext
{
 public required IEnumerable<Entity> Entities { get; init; }
 public required IEnumerable<Relationship> Relationships { get; init; }
    public required IEnumerable<Chunk> RelatedChunks { get; init; }
}

public record RankedResult
{
    public required Chunk Chunk { get; init; }
    public required double Score { get; init; }
    public required double VectorScore { get; init; }
    public required double GraphScore { get; init; }
    public required IEnumerable<Entity> AssociatedEntities { get; init; }
}

public record SearchMetrics
{
    public required TimeSpan VectorSearchTime { get; init; }
    public required TimeSpan GraphSearchTime { get; init; }
    public required TimeSpan RerankingTime { get; init; }
    public required TimeSpan TotalTime { get; init; }
    public required int VectorResultsCount { get; init; }
    public required int GraphResultsCount { get; init; }
    public required int FinalResultsCount { get; init; }
}
```

---

## Security & Compliance Components

### IRBACService

**Phase**: 19  
**Namespace**: `PanoramicData.Chunker.Security`

```csharp
/// <summary>
/// Role-Based Access Control service for multi-tenant authorization.
/// </summary>
public interface IRBACService
{
    /// <summary>
    /// Validates if a user can access a tenant's resources.
    /// </summary>
    Task<AuthorizationResult> ValidateAccessAsync(
        string userId,
 TenantContext tenant,
     Permission permission,
    CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if an action is safe (no destructive operations).
    /// </summary>
    Task<ActionSafetyResult> ValidateActionSafetyAsync(
        string action,
        IDictionary<string, object> parameters,
        TenantContext tenant,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Assigns a role to a user within a tenant.
    /// </summary>
    Task AssignRoleAsync(
        string userId,
        TenantContext tenant,
        Role role,
     CancellationToken cancellationToken = default);
}

public enum Permission
{
    Read,
    Write,
    Delete,
    AdministerTenant,
    ManageUsers,
    ExportData
}

public enum Role
{
    Viewer,
    Editor,
    Admin,
    SuperAdmin
}

public record AuthorizationResult
{
    public required bool IsAuthorized { get; init; }
    public string? Reason { get; init; }
    public IEnumerable<Permission> GrantedPermissions { get; init; } = Array.Empty<Permission>();
}

public record ActionSafetyResult
{
    public required bool IsSafe { get; init; }
    public required ActionCategory Category { get; init; }
    public required bool RequiresConfirmation { get; init; }
    public string? Warning { get; init; }
}

public enum ActionCategory
{
    ReadOnly,
    Write,
    Destructive
}
```

### IAuditLogger

```csharp
/// <summary>
/// Audit logging service for compliance (GDPR, SOC 2, etc.).
/// </summary>
public interface IAuditLogger
{
    /// <summary>
    /// Logs an access attempt.
    /// </summary>
    Task LogAccessAttemptAsync(
        AuditEvent auditEvent,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Logs a data export (GDPR compliance).
    /// </summary>
    Task LogDataExportAsync(
  string userId,
        TenantContext tenant,
        int recordCount,
        long fileSizeBytes,
 CancellationToken cancellationToken = default);

    /// <summary>
    /// Queries audit logs (admin only).
    /// </summary>
    Task<IEnumerable<AuditEvent>> QueryLogsAsync(
        AuditQuery query,
CancellationToken cancellationToken = default);
}

public record AuditEvent
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public required string TenantId { get; init; }
    public required string UserId { get; init; }
    public required string Action { get; init; }
    public required string Resource { get; init; }
    public required DateTimeOffset Timestamp { get; init; }
    public required string Result { get; init; } // "Success", "Denied", "Error"
    public string? IpAddress { get; init; }
    public Dictionary<string, object> Metadata { get; init; } = new();
}

public record AuditQuery
{
    public string? TenantId { get; init; }
    public string? UserId { get; init; }
    public DateTimeOffset? StartDate { get; init; }
    public DateTimeOffset? EndDate { get; init; }
    public string? Action { get; init; }
    public int Limit { get; init; } = 100;
}
```

---

**Document Version**: 1.0  
**Last Updated**: January 2025  
**Status**: Draft - Component Specifications for Phase 10-26
