# PostgreSQL + EF Core + Apache AGE Knowledge Graph Storage

This directory contains the PostgreSQL storage implementation for Knowledge Graph persistence using **dual architecture**:

## Architecture Overview

### Two-Layer Approach

1. **EF Core Layer** (CRUD Operations)
   - Standard entity/relationship persistence
   - Loading and saving graphs
   - Type-safe queries with LINQ
   - Used by: `PostgresGraphStore`

2. **Apache AGE Layer** (Graph Queries)
   - Cypher query execution
   - Graph traversal (shortest path, neighbors)
   - Pattern matching
   - Advanced graph algorithms
   - Used by: `ApacheAgeCypherExecutor`

### Why Both?

- **EF Core**: Best for standard CRUD operations, maintains data integrity, type-safe
- **Apache AGE**: Best for graph-specific queries (shortest path, pattern matching, traversal)

## Implementation Components

The implementation uses:
- **Entity Framework Core 9.0** for ORM and CRUD operations
- **Npgsql** for PostgreSQL connectivity
- **Apache AGE** for Cypher queries and graph traversal
- **Testcontainers** for integration testing (spins up PostgreSQL in Docker)
- **xUnit v3 Fixture pattern** for test infrastructure
- **User Secrets** for local development database credentials

## Database Schema

The Knowledge Graph uses three main tables with **PascalCase naming** (company standard):

### `KgGraphs`
Stores graph metadata.

| Column | Type | Description |
|--------|------|-------------|
| Id | UUID | Primary key (graph ID) |
| Name | VARCHAR(200) | Graph name (unique) |
| Metadata | JSONB | Graph metadata |
| Schema | JSONB | Schema definitions |
| Statistics | JSONB | Computed graph statistics |

### `KgEntities`
Stores graph entities (nodes).

| Column | Type | Description |
|--------|------|-------------|
| Id | UUID | Primary key (entity ID) |
| Type | VARCHAR(50) | Entity type enum |
| Name | VARCHAR(500) | Entity name |
| NormalizedName | VARCHAR(500) | Normalized name for matching |
| Confidence | DOUBLE | Extraction confidence (0-1) |
| Frequency | INT | Occurrence frequency |
| Properties | JSONB | Custom properties |
| Aliases | JSONB | Alternative names |
| RelatedEntityIds | JSONB | Related entity IDs |
| Sources | JSONB | Source chunks |
| Metadata | JSONB | Entity metadata |

**Indexes**:
- `IX_KgEntities_Type` on `Type`
- `IX_KgEntities_NormalizedName` on `NormalizedName`
- `IX_KgEntities_Confidence` on `Confidence`

### `KgRelationships`
Stores relationships between entities (edges).

| Column | Type | Description |
|--------|------|-------------|
| Id | UUID | Primary key (relationship ID) |
| Type | VARCHAR(50) | Relationship type enum |
| FromEntityId | UUID | Source entity ID |
| ToEntityId | UUID | Target entity ID |
| Weight | DOUBLE | Relationship weight (0-1) |
| Confidence | DOUBLE | Extraction confidence (0-1) |
| Bidirectional | BOOLEAN | Is bidirectional? |
| Properties | JSONB | Custom properties |
| Evidence | JSONB | Supporting evidence |
| Metadata | JSONB | Relationship metadata |

**Indexes**:
- `IX_KgRelationships_Type` on `Type`
- `IX_KgRelationships_FromEntity` on `FromEntityId`
- `IX_KgRelationships_ToEntity` on `ToEntityId`
- `IX_KgRelationships_FromTo` on `(FromEntityId, ToEntityId)`

### Apache AGE Graph

In addition to the EF Core tables, Apache AGE maintains a separate graph structure:

- **Graph Name**: `knowledge_graph`
- **Vertex Label**: `Entity` (synced with `KgEntities`)
- **Edge Label**: `Relationship` (synced with `KgRelationships`)

**Synchronization**: When entities/relationships are saved via EF Core, they can be synchronized to Apache AGE for graph queries.

## Setup for Local Development

### Prerequisites

1. **PostgreSQL 11+** installed
2. **Apache AGE extension** installed (see below)
3. **Docker** (for Testcontainers) OR existing PostgreSQL instance

### Install Apache AGE

#### Option 1: Docker (Easiest)
```bash
docker pull apache/age
docker run --name age-postgres -e POSTGRES_PASSWORD=postgres -p 5432:5432 -d apache/age
```

#### Option 2: From Source
```bash
# Clone Apache AGE
git clone https://github.com/apache/age.git
cd age

# Build and install
make install
```

#### Option 3: Package Manager (Ubuntu/Debian)
```bash
sudo apt-get install postgresql-15-age
```

### Setup Database

#### Step 1: Run AGE Setup Script

```bash
psql -U postgres -d panoramicdata_chunker_test -f sql/schema/001_apache_age_setup.sql
```

Or manually:
```sql
CREATE EXTENSION IF NOT EXISTS age;
SET search_path = ag_catalog, "$user", public;
SELECT create_graph('knowledge_graph');
```

#### Step 2: Create Database User

```sql
CREATE DATABASE panoramicdata_chunker_test;
CREATE USER test_user WITH PASSWORD 'your_secure_password';
GRANT ALL PRIVILEGES ON DATABASE panoramicdata_chunker_test TO test_user;
GRANT USAGE ON SCHEMA ag_catalog TO test_user;
GRANT EXECUTE ON ALL FUNCTIONS IN SCHEMA ag_catalog TO test_user;
```

#### Step 3: Configure User Secrets

```bash
# Navigate to the test project
cd PanoramicData.Chunker.Tests

# Initialize user secrets
dotnet user-secrets init

# Set the connection string
dotnet user-secrets set "ConnectionStrings:KnowledgeGraph" "Host=localhost;Port=5432;Database=panoramicdata_chunker_test;Username=test_user;Password=your_secure_password"

# Set the graph name
dotnet user-secrets set "ApacheAge:GraphName" "knowledge_graph"

# Enable using existing database
dotnet user-secrets set "UseExistingDatabase" "true"
```

#### Step 4: Run Tests

```bash
dotnet test --filter "FullyQualifiedName~PostgresGraphStoreTests"
```

## Usage Examples

### Using EF Core for CRUD Operations

```csharp
// Inject IGraphStore
public class MyService
{
    private readonly IGraphStore _graphStore;

    public MyService(IGraphStore graphStore)
    {
  _graphStore = graphStore;
    }

    public async Task SaveGraphAsync()
    {
        var graph = new Graph("My Graph");
      var entity = new Entity(EntityType.Person, "John Doe");
        graph.AddEntity(entity);

        // Uses EF Core under the hood
        await _graphStore.SaveGraphAsync(graph);
    }
}
```

### Using Apache AGE for Cypher Queries

```csharp
// Inject ICypherQueryExecutor
public class GraphQueryService
{
    private readonly ICypherQueryExecutor _cypherExecutor;

    public GraphQueryService(ICypherQueryExecutor cypherExecutor)
    {
   _cypherExecutor = cypherExecutor;
    }

    public async Task<List<Guid>> FindShortestPathAsync(Guid fromId, Guid toId)
    {
        // Uses Apache AGE for graph traversal
        return await _cypherExecutor.FindShortestPathAsync(fromId, toId);
    }

    public async Task<List<Entity>> GetNeighborsAsync(Guid entityId)
    {
 // Uses Cypher query
        return await _cypherExecutor.GetNeighborsAsync(entityId, depth: 2);
    }

    public async Task<CypherMatchResult> FindPeopleWorkingForCompaniesAsync()
    {
// Pattern matching with Cypher
        var pattern = "(person:Entity {Type: 'Person'})-[:WorksFor]->(company:Entity {Type: 'Organization'})";
        return await _cypherExecutor.ExecutePatternMatchAsync(pattern);
    }
}
```

### Custom Cypher Queries

```csharp
// Execute raw Cypher
var cypherQuery = @"
    MATCH (person:Entity {Type: 'Person'})
    WHERE person.Confidence > 0.8
    RETURN person
    ORDER BY person.Frequency DESC
    LIMIT 10
";

var results = await _cypherExecutor.ExecuteQueryAsync<Entity>(cypherQuery);
```

## User Secrets Configuration

The test infrastructure supports the following user secret keys:

```json
{
  "ConnectionStrings": {
    "KnowledgeGraph": "Host=localhost;Port=5432;Database=panoramicdata_chunker_test;Username=test_user;Password=your_password"
  },
  "UseExistingDatabase": true,
  "ApacheAge": {
    "GraphName": "knowledge_graph",
    "EnableAgeExtension": true
  },
  "PostgresDocker": {
    "Image": "apache/age:latest",
    "Username": "test_user",
    "Password": "test_password",
    "Database": "panoramicdata_chunker_test"
  }
}
```

**Configuration Priority**:
1. User Secrets (highest priority - for local dev)
2. Environment Variables (for CI/CD)
3. `appsettings.Test.json` (defaults, not committed to Git)
4. Testcontainers (fallback - spins up PostgreSQL container)

## Environment Variables (CI/CD)

For CI/CD pipelines, set these environment variables:

```bash
export ConnectionStrings__KnowledgeGraph="Host=postgres;Port=5432;Database=test;Username=postgres;Password=postgres"
export UseExistingDatabase=true
```

## Running Migrations

The tests automatically run `EnsureCreatedAsync()` to create the schema. For production use:

```bash
# Install EF Core tools
dotnet tool install --global dotnet-ef

# Navigate to main project
cd PanoramicData.Chunker

# Create initial migration
dotnet ef migrations add InitialCreate --context KnowledgeGraphDbContext --output-dir KnowledgeGraph/Storage/Migrations

# Apply migration
dotnet ef database update --context KnowledgeGraphDbContext
```

## PostgreSQL Configuration Recommendations

### For Local Development
```
# postgresql.conf
max_connections = 100
shared_buffers = 256MB
effective_cache_size = 1GB
work_mem = 4MB
```

### For Production
```
# postgresql.conf
max_connections = 200
shared_buffers = 2GB
effective_cache_size = 6GB
work_mem = 16MB
random_page_cost = 1.1  # For SSD storage
```

## Performance Considerations

1. **JSONB Indexing**: PostgreSQL's JSONB type supports GIN indexes for fast queries:
   ```sql
   CREATE INDEX "IX_KgEntities_Properties" ON "KgEntities" USING GIN ("Properties");
   ```

2. **Connection Pooling**: Npgsql automatically pools connections. Configure in connection string:
   ```
   Host=localhost;Database=kg;Username=user;Password=pass;Minimum Pool Size=10;Maximum Pool Size=100
   ```

3. **Batch Operations**: Use `SaveChangesAsync()` sparingly - batch multiple entities/relationships before saving.

## Troubleshooting

### "Npgsql.PostgresException: role 'test_user' does not exist"

Create the user:
```sql
CREATE USER test_user WITH PASSWORD 'your_password';
GRANT ALL PRIVILEGES ON DATABASE panoramicdata_chunker_test TO test_user;
```

### "Cannot connect to Docker daemon"

Make sure Docker Desktop is running, then retry the tests.

### "Password authentication failed"

Check your user secrets or environment variables for correct credentials.

### "Database 'panoramicdata_chunker_test' does not exist"

The tests will create it automatically. If using an existing instance, create it manually:
```sql
CREATE DATABASE panoramicdata_chunker_test;
```

## Naming Conventions

This implementation follows **PanoramicData company standards**:
- **Tables**: PascalCase (e.g., `KgEntities`, `KgRelationships`)
- **Columns**: PascalCase (e.g., `FromEntityId`, `NormalizedName`)
- **Indexes**: PascalCase with IX_ prefix (e.g., `IX_KgEntities_Type`)

## Security Notes

**?? Never commit database credentials to Git!**

- Use **User Secrets** for local development
- Use **Environment Variables** for CI/CD pipelines
- Use **Azure Key Vault** or similar for production deployments

The `appsettings.Test.json` file contains only default values for Testcontainers - no real credentials.

## Next Steps

- [ ] Implement Cypher query support (Phase 24)
- [ ] Add graph traversal algorithms (Phase 24)
- [ ] Implement bulk import/export (Phase 15)
- [ ] Add graph versioning (Phase 15)
- [ ] Integrate with Apache AGE for advanced graph queries (Phase 13)

## References

- [Entity Framework Core Documentation](https://learn.microsoft.com/en-us/ef/core/)
- [Npgsql Documentation](https://www.npgsql.org/doc/)
- [PostgreSQL JSONB Documentation](https://www.postgresql.org/docs/current/datatype-json.html)
- [Testcontainers for .NET](https://dotnet.testcontainers.org/)
- [.NET User Secrets](https://learn.microsoft.com/en-us/aspnet/core/security/app-secrets)
