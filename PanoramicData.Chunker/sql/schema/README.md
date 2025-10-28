# Database Schema Management

**Important**: This project now uses **Entity Framework Core Migrations** for database schema management.

## EF Core Migrations

All database schema changes are managed through EF Core migrations located in:
```
PanoramicData.Chunker/KnowledgeGraph/Storage/Migrations/
```

The standalone SQL scripts in this directory are for **reference only** and are **not used** in the actual application.

## Initial Migration

The `InitialCreateWithApacheAge` migration includes:

1. **Apache AGE Extension Installation**
   - Creates the AGE extension
   - Sets up the search path
   - Creates the `knowledge_graph` graph

2. **EF Core Tables** (PascalCase naming)
   - `KgGraphs` - Graph metadata
   - `KgEntities` - Entity nodes
   - `KgRelationships` - Relationship edges

3. **Apache AGE Labels**
   - Vertex label: `Entity`
   - Edge label: `Relationship`

4. **Indexes** for performance
5. **Permissions** for application user (if exists)

## Running Migrations

### Development

```bash
# Apply all migrations
dotnet ef database update --context KnowledgeGraphDbContext --project PanoramicData.Chunker

# Create new migration
dotnet ef migrations add MigrationName --context KnowledgeGraphDbContext --project PanoramicData.Chunker --output-dir KnowledgeGraph/Storage/Migrations

# Remove last migration (if not applied)
dotnet ef migrations remove --context KnowledgeGraphDbContext --project PanoramicData.Chunker

# View migration SQL (without applying)
dotnet ef migrations script --context KnowledgeGraphDbContext --project PanoramicData.Chunker
```

### Production

```bash
# Generate SQL script for DBA review
dotnet ef migrations script --context KnowledgeGraphDbContext --project PanoramicData.Chunker --output migration.sql

# Apply with specific connection string
dotnet ef database update --context KnowledgeGraphDbContext --project PanoramicData.Chunker --connection "Host=prod;Database=kg;Username=app_user;Password=..."
```

### Testing

The test infrastructure automatically applies migrations via:
```csharp
await context.Database.EnsureCreatedAsync();
```

This is handled in `PostgresKnowledgeGraphFixture` during test setup.

## Prerequisites

1. **PostgreSQL 11+** installed
2. **Superuser privileges** (for initial Apache AGE extension installation)
3. **dotnet-ef tool**:
   ```bash
   dotnet tool install --global dotnet-ef
   dotnet tool update --global dotnet-ef
   ```

## Apache AGE Installation

The migration **requires superuser privileges** to install the AGE extension.

### Docker (Easiest)

```bash
docker run --name age-postgres \
  -e POSTGRES_PASSWORD=postgres \
  -p 5432:5432 \
  -d apache/age:latest
```

### Ubuntu/Debian

```bash
sudo apt-get install postgresql-15-age
```

### From Source

```bash
git clone https://github.com/apache/age.git
cd age
make install
```

## Manual AGE Setup (If Migration Fails)

If the migration fails due to permissions, manually run as superuser:

```sql
-- Connect as postgres superuser
\c your_database

-- Install AGE
CREATE EXTENSION IF NOT EXISTS age;
SET search_path = ag_catalog, "$user", public;
SELECT ag_catalog.create_graph('knowledge_graph');

-- Grant permissions to application user
GRANT USAGE ON SCHEMA ag_catalog TO your_app_user;
GRANT SELECT ON ALL TABLES IN SCHEMA ag_catalog TO your_app_user;
GRANT EXECUTE ON ALL FUNCTIONS IN SCHEMA ag_catalog TO your_app_user;
```

Then run EF migrations as the application user:
```bash
dotnet ef database update --context KnowledgeGraphDbContext --project PanoramicData.Chunker --connection "Host=localhost;Database=your_database;Username=your_app_user;..."
```

## Troubleshooting

### "Extension 'age' does not exist"

Install Apache AGE extension first (see above).

### "Permission denied to create extension"

Run the initial migration as PostgreSQL superuser:
```bash
dotnet ef database update --context KnowledgeGraphDbContext --project PanoramicData.Chunker --connection "Host=localhost;Database=your_database;Username=postgres;Password=..."
```

### "Graph 'knowledge_graph' already exists"

The migration is idempotent. Existing graphs won't cause errors.

### View Migration History

```bash
dotnet ef migrations list --context KnowledgeGraphDbContext --project PanoramicData.Chunker
```

## Architecture

### Dual-Layer Approach

1. **EF Core** (via `PostgresGraphStore`)
   - CRUD operations
   - Type-safe LINQ queries
   - Automatic schema management

2. **Apache AGE** (via `ApacheAgeCypherExecutor`)
   - Cypher queries
   - Graph traversal (shortest path, neighbors)
   - Pattern matching
   - Advanced graph algorithms

### Database Schema

**EF Core Tables** (relational storage):
- `KgGraphs` - Graph metadata
- `KgEntities` - Entity nodes with JSONB properties
- `KgRelationships` - Relationship edges with JSONB evidence

**Apache AGE Graph** (graph storage):
- Graph name: `knowledge_graph`
- Vertex label: `Entity` (synced with `KgEntities`)
- Edge label: `Relationship` (synced with `KgRelationships`)

## Best Practices

1. ? **Always review migrations** before applying to production
2. ? **Test migrations** in development environment first
3. ? **Generate SQL scripts** for DBA review in production
4. ? **Backup databases** before running migrations
5. ? **Never modify applied migrations** - create new ones instead
6. ? **Use transactions** (EF Core does this automatically)

## See Also

- **Main Documentation**: `PanoramicData.Chunker/KnowledgeGraph/Storage/README.md`
- **Phase 11 Documentation**: `docs/phases/Phase-11.md`
- [EF Core Migrations](https://learn.microsoft.com/en-us/ef/core/managing-schemas/migrations/)
- [Apache AGE Documentation](https://age.apache.org/)
- [Npgsql Documentation](https://www.npgsql.org/doc/)
