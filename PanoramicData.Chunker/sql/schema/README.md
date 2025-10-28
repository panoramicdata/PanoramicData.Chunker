# PostgreSQL + Apache AGE Schema Setup

## Overview

This directory contains SQL migration scripts for setting up the Knowledge Graph infrastructure using PostgreSQL with Apache AGE extension.

## Prerequisites

1. **PostgreSQL 11+** installed and running
2. **Apache AGE Extension 1.5.0+** installed
3. **pg_trgm extension** for fuzzy text matching
4. **PostgreSQL user** with superuser or database creation privileges

## Installation Steps

### 1. Install Apache AGE

**On Ubuntu/Debian:**
```bash
sudo apt-get install postgresql-15-age
```

**On macOS (using Homebrew):**
```bash
brew install apache-age
```

**From Source:**
```bash
git clone https://github.com/apache/age.git
cd age
make install
```

### 2. Create Database

```bash
createdb panoramic_chunker
```

Or using psql:
```sql
CREATE DATABASE panoramic_chunker;
```

### 3. Enable Required Extensions

```bash
psql -d panoramic_chunker -c "CREATE EXTENSION IF NOT EXISTS pg_trgm;"
psql -d panoramic_chunker -c "CREATE EXTENSION IF NOT EXISTS vector;"  -- if using pgvector
```

### 4. Run Migration Scripts

Execute the scripts in order:

```bash
psql -d panoramic_chunker -f 001_create_extension.sql
psql -d panoramic_chunker -f 002_create_central_schema.sql
psql -d panoramic_chunker -f 003_create_tenant_schema_function.sql
psql -d panoramic_chunker -f 004_create_helper_functions.sql
```

Or run all at once:
```bash
for file in *.sql; do
    echo "Executing $file..."
    psql -d panoramic_chunker -f "$file"
done
```

### 5. Create Tenant Schema

```sql
-- For single tenant
SELECT create_tenant_schema('tenant_a');

-- For multiple tenants
SELECT create_tenant_schema('tenant_a');
SELECT create_tenant_schema('tenant_b');
SELECT create_tenant_schema('acme_corp');
```

## Schema Structure

### Central Schema (`central`)
Shared across all tenants:
- `entity_types` - 40+ predefined entity types
- `relationship_types` - 40+ predefined relationship types

### Tenant Schema (`<tenant_id>`)
Isolated per tenant:
- `entities` - Entity instances
- `relationships` - Relationship instances
- `chunks` - Document chunks
- `documents` - Document metadata
- Apache AGE graph: `<tenant_id>_graph`

## Usage Examples

### Check Installation

```sql
-- Verify AGE extension
SELECT * FROM pg_extension WHERE extname = 'age';

-- Check schema version
SELECT * FROM public.schema_version ORDER BY applied_at DESC;

-- List entity types
SELECT name, description, category FROM central.entity_types WHERE is_active = TRUE;
```

### Working with Tenants

```sql
-- Create a new tenant
SELECT create_tenant_schema('my_company');

-- Set tenant context
SET app.tenant_id = 'my_company';

-- Insert an entity
INSERT INTO my_company.entities (entity_type_id, name, normalized_name, confidence)
VALUES (
    get_entity_type_id('Organization'),
    'Microsoft',
'microsoft',
    0.95
);

-- Insert a relationship
INSERT INTO my_company.relationships (
    source_entity_id,
    target_entity_id,
    relationship_type_id,
    weight,
    confidence
)
VALUES (
    '...entity1_id...',
    '...entity2_id...',
    get_relationship_type_id('WorksAt'),
    0.8,
    0.9
);
```

### Query Graph Statistics

```sql
-- Get statistics for a tenant
SELECT * FROM calculate_graph_statistics('tenant_a');

-- Find entities by name
SELECT * FROM find_entities_by_name('tenant_a', 'Microsoft', 0.7, 10);

-- Get neighbors of an entity
SELECT * FROM get_entity_neighbors(
    'tenant_a',
    '...entity_id...',
    2,  -- max depth
    'WorksAt'  -- optional relationship type filter
);
```

### Apache AGE Cypher Queries

```sql
-- Load AGE extension
LOAD 'age';
SET search_path = ag_catalog, "$user", public;

-- Query using Cypher
SELECT * FROM ag_catalog.cypher('tenant_a_graph', $$
    MATCH (n:Entity)
    RETURN n
    LIMIT 10
$$) as (entity agtype);

-- Find relationships
SELECT * FROM ag_catalog.cypher('tenant_a_graph', $$
    MATCH (a:Entity)-[r:WorksAt]->(b:Entity)
 RETURN a.name, b.name, r.confidence
$$) as (person agtype, organization agtype, confidence agtype);

-- Path queries
SELECT * FROM ag_catalog.cypher('tenant_a_graph', $$
    MATCH path = (a:Entity)-[*1..3]-(b:Entity)
    WHERE a.name = 'John Doe'
    RETURN path
    LIMIT 5
$$) as (path agtype);
```

## Security

### Row-Level Security (RLS)

All tenant tables have RLS enabled. Access is controlled by the `app.tenant_id` session variable:

```sql
-- Set tenant context before querying
SET app.tenant_id = 'tenant_a';

-- Now queries will only see tenant_a data
SELECT * FROM tenant_a.entities;
```

### Dropping a Tenant (DANGEROUS!)

```sql
-- Requires confirmation to prevent accidents
SELECT drop_tenant_schema('tenant_a', 'DELETE_tenant_a');
```

This will:
1. Drop the Apache AGE graph
2. Drop the schema and all tables
3. Delete all tenant data permanently

## Maintenance

### Backup

```bash
# Backup entire database
pg_dump -d panoramic_chunker > backup.sql

# Backup specific tenant
pg_dump -d panoramic_chunker -n tenant_a > tenant_a_backup.sql

# Backup with Apache AGE graphs
pg_dump -d panoramic_chunker -n tenant_a -n ag_catalog > tenant_a_full_backup.sql
```

### Restore

```bash
psql -d panoramic_chunker < backup.sql
```

### Performance Tuning

```sql
-- Analyze tables
ANALYZE tenant_a.entities;
ANALYZE tenant_a.relationships;

-- Reindex
REINDEX SCHEMA tenant_a;

-- Vacuum
VACUUM ANALYZE tenant_a.entities;
VACUUM ANALYZE tenant_a.relationships;
```

### Monitor Graph Size

```sql
-- Table sizes
SELECT 
    schemaname,
    tablename,
    pg_size_pretty(pg_total_relation_size(schemaname||'.'||tablename)) AS size
FROM pg_tables
WHERE schemaname LIKE 'tenant%'
ORDER BY pg_total_relation_size(schemaname||'.'||tablename) DESC;

-- Graph statistics by tenant
SELECT 
    table_schema,
    (SELECT COUNT(*) FROM entities e WHERE e.entity_id IS NOT NULL) as entity_count,
    (SELECT COUNT(*) FROM relationships r WHERE r.relationship_id IS NOT NULL) as relationship_count
FROM information_schema.tables
WHERE table_schema LIKE 'tenant%'
AND table_name = 'entities';
```

## Troubleshooting

### AGE Extension Not Found

```bash
# Check if AGE is installed
pg_config --sharedir
ls $(pg_config --sharedir)/extension/ | grep age

# If not found, install AGE extension
# See Prerequisites section
```

### Permission Denied

```sql
-- Grant necessary permissions
GRANT USAGE ON SCHEMA central TO your_user;
GRANT SELECT ON ALL TABLES IN SCHEMA central TO your_user;
GRANT USAGE ON SCHEMA tenant_a TO your_user;
GRANT ALL ON ALL TABLES IN SCHEMA tenant_a TO your_user;
```

### RLS Blocking Access

```sql
-- Check current tenant context
SHOW app.tenant_id;

-- Set correct tenant context
SET app.tenant_id = 'tenant_a';

-- Or temporarily disable RLS for debugging (superuser only)
ALTER TABLE tenant_a.entities DISABLE ROW LEVEL SECURITY;
-- Don't forget to re-enable!
ALTER TABLE tenant_a.entities ENABLE ROW LEVEL SECURITY;
```

## References

- [Apache AGE Documentation](https://age.apache.org/age-manual/master/index.html)
- [PostgreSQL Row Level Security](https://www.postgresql.org/docs/current/ddl-rowsecurity.html)
- [Cypher Query Language](https://neo4j.com/docs/cypher-manual/current/)
- [pgvector Extension](https://github.com/pgvector/pgvector)

## Migration Tracking

All executed migrations are tracked in the `public.schema_version` table:

```sql
SELECT * FROM public.schema_version ORDER BY applied_at DESC;
```

| version_number | script_name | applied_at | applied_by |
|---|---|---|---|
| 1.0.0 | 004_create_helper_functions.sql | 2025-01-15 10:30:00 | postgres |
| 1.0.0 | 003_create_tenant_schema_function.sql | 2025-01-15 10:29:00 | postgres |
| 1.0.0 | 002_create_central_schema.sql | 2025-01-15 10:28:00 | postgres |
| 1.0.0 | 001_create_extension.sql | 2025-01-15 10:27:00 | postgres |
