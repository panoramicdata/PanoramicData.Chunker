-- Script: 003_create_tenant_schema_function.sql
-- Description: Creates a function to provision tenant-specific schemas with graph support
-- This function creates isolated data partitions for each tenant

-- Function to create a complete tenant schema
CREATE OR REPLACE FUNCTION create_tenant_schema(tenant_id TEXT)
RETURNS VOID AS $$
DECLARE
    graph_name TEXT;
BEGIN
    -- Validate tenant_id
    IF tenant_id IS NULL OR LENGTH(TRIM(tenant_id)) = 0 THEN
      RAISE EXCEPTION 'Tenant ID cannot be null or empty';
  END IF;
    
    -- Sanitize tenant_id for schema name (lowercase, alphanumeric and underscores only)
    tenant_id := LOWER(REGEXP_REPLACE(tenant_id, '[^a-z0-9_]', '_', 'g'));
 
    RAISE NOTICE 'Creating schema for tenant: %', tenant_id;
    
    -- Create tenant schema
    EXECUTE format('CREATE SCHEMA IF NOT EXISTS %I', tenant_id);
    
    RAISE NOTICE 'Creating entities table for tenant: %', tenant_id;
    
    -- Create entities table
    EXECUTE format('
        CREATE TABLE IF NOT EXISTS %I.entities (
            entity_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
      tenant_id TEXT NOT NULL DEFAULT %L,
  entity_type_id INT REFERENCES central.entity_types(entity_type_id),
       name TEXT NOT NULL,
      normalized_name TEXT NOT NULL,
            properties JSONB DEFAULT ''{}''::jsonb,
    aliases TEXT[] DEFAULT ARRAY[]::TEXT[],
            source_chunks UUID[] DEFAULT ARRAY[]::UUID[],
            confidence FLOAT CHECK (confidence >= 0 AND confidence <= 1) DEFAULT 1.0,
            frequency INT DEFAULT 1,
 importance_score FLOAT,
            embedding VECTOR(1536),
  created_at TIMESTAMP DEFAULT NOW(),
            updated_at TIMESTAMP DEFAULT NOW(),
    created_by TEXT,
  CONSTRAINT chk_entity_name CHECK (LENGTH(name) > 0)
        )', tenant_id, tenant_id);
    
    RAISE NOTICE 'Creating relationships table for tenant: %', tenant_id;
    
    -- Create relationships table
    EXECUTE format('
        CREATE TABLE IF NOT EXISTS %I.relationships (
         relationship_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
   tenant_id TEXT NOT NULL DEFAULT %L,
            source_entity_id UUID REFERENCES %I.entities(entity_id) ON DELETE CASCADE,
         target_entity_id UUID REFERENCES %I.entities(entity_id) ON DELETE CASCADE,
            relationship_type_id INT REFERENCES central.relationship_types(relationship_type_id),
            properties JSONB DEFAULT ''{}''::jsonb,
        weight FLOAT CHECK (weight >= 0 AND weight <= 1) DEFAULT 1.0,
        confidence FLOAT CHECK (confidence >= 0 AND confidence <= 1) DEFAULT 1.0,
      bidirectional BOOLEAN DEFAULT FALSE,
            evidence JSONB[] DEFAULT ARRAY[]::JSONB[],
         created_at TIMESTAMP DEFAULT NOW(),
    created_by TEXT,
        CONSTRAINT chk_different_entities CHECK (source_entity_id != target_entity_id)
        )', tenant_id, tenant_id, tenant_id, tenant_id);
    
    RAISE NOTICE 'Creating chunks table for tenant: %', tenant_id;
    
    -- Create chunks table
    EXECUTE format('
        CREATE TABLE IF NOT EXISTS %I.chunks (
     chunk_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
       tenant_id TEXT NOT NULL DEFAULT %L,
     document_id UUID NOT NULL,
 chunk_type TEXT NOT NULL,
            content TEXT NOT NULL,
   metadata JSONB DEFAULT ''{}''::jsonb,
        embedding VECTOR(1536),
            token_count INT,
         parent_chunk_id UUID,
            sequence_number INT,
       created_at TIMESTAMP DEFAULT NOW(),
     created_by TEXT
        )', tenant_id, tenant_id);
    
    RAISE NOTICE 'Creating documents table for tenant: %', tenant_id;
    
    -- Create documents table
    EXECUTE format('
        CREATE TABLE IF NOT EXISTS %I.documents (
        document_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
         tenant_id TEXT NOT NULL DEFAULT %L,
  filename TEXT NOT NULL,
    file_path TEXT,
   file_size BIGINT,
            mime_type TEXT,
  document_type TEXT,
   metadata JSONB DEFAULT ''{}''::jsonb,
  chunk_count INT DEFAULT 0,
     entity_count INT DEFAULT 0,
   created_at TIMESTAMP DEFAULT NOW(),
            processed_at TIMESTAMP,
 created_by TEXT
        )', tenant_id, tenant_id);
    
    RAISE NOTICE 'Creating indexes for tenant: %', tenant_id;
  
    -- Create indexes for entities
    EXECUTE format('CREATE INDEX IF NOT EXISTS idx_%I_entities_type ON %I.entities(entity_type_id)', tenant_id, tenant_id);
    EXECUTE format('CREATE INDEX IF NOT EXISTS idx_%I_entities_name ON %I.entities USING GIN(name gin_trgm_ops)', tenant_id, tenant_id);
    EXECUTE format('CREATE INDEX IF NOT EXISTS idx_%I_entities_normalized ON %I.entities(normalized_name)', tenant_id, tenant_id);
    EXECUTE format('CREATE INDEX IF NOT EXISTS idx_%I_entities_confidence ON %I.entities(confidence)', tenant_id, tenant_id);
    EXECUTE format('CREATE INDEX IF NOT EXISTS idx_%I_entities_created ON %I.entities(created_at)', tenant_id, tenant_id);
    
    -- Create indexes for relationships
    EXECUTE format('CREATE INDEX IF NOT EXISTS idx_%I_relationships_source ON %I.relationships(source_entity_id)', tenant_id, tenant_id);
    EXECUTE format('CREATE INDEX IF NOT EXISTS idx_%I_relationships_target ON %I.relationships(target_entity_id)', tenant_id, tenant_id);
    EXECUTE format('CREATE INDEX IF NOT EXISTS idx_%I_relationships_type ON %I.relationships(relationship_type_id)', tenant_id, tenant_id);
    EXECUTE format('CREATE INDEX IF NOT EXISTS idx_%I_relationships_confidence ON %I.relationships(confidence)', tenant_id, tenant_id);
    
    -- Create indexes for chunks
    EXECUTE format('CREATE INDEX IF NOT EXISTS idx_%I_chunks_document ON %I.chunks(document_id)', tenant_id, tenant_id);
    EXECUTE format('CREATE INDEX IF NOT EXISTS idx_%I_chunks_type ON %I.chunks(chunk_type)', tenant_id, tenant_id);
    EXECUTE format('CREATE INDEX IF NOT EXISTS idx_%I_chunks_parent ON %I.chunks(parent_chunk_id)', tenant_id, tenant_id);
 
    -- Create indexes for documents
  EXECUTE format('CREATE INDEX IF NOT EXISTS idx_%I_documents_created ON %I.documents(created_at)', tenant_id, tenant_id);
    EXECUTE format('CREATE INDEX IF NOT EXISTS idx_%I_documents_type ON %I.documents(document_type)', tenant_id, tenant_id);
    
    RAISE NOTICE 'Enabling Row-Level Security for tenant: %', tenant_id;
    
    -- Enable Row-Level Security
    EXECUTE format('ALTER TABLE %I.entities ENABLE ROW LEVEL SECURITY', tenant_id);
    EXECUTE format('ALTER TABLE %I.relationships ENABLE ROW LEVEL SECURITY', tenant_id);
    EXECUTE format('ALTER TABLE %I.chunks ENABLE ROW LEVEL SECURITY', tenant_id);
    EXECUTE format('ALTER TABLE %I.documents ENABLE ROW LEVEL SECURITY', tenant_id);
    
    -- Create RLS policies for entities
    EXECUTE format('
   CREATE POLICY IF NOT EXISTS tenant_isolation_entities ON %I.entities
        USING (tenant_id = current_setting(''app.tenant_id'', true))
    ', tenant_id);
  
    -- Create RLS policies for relationships
    EXECUTE format('
     CREATE POLICY IF NOT EXISTS tenant_isolation_relationships ON %I.relationships
        USING (tenant_id = current_setting(''app.tenant_id'', true))
    ', tenant_id);
 
    -- Create RLS policies for chunks
    EXECUTE format('
      CREATE POLICY IF NOT EXISTS tenant_isolation_chunks ON %I.chunks
        USING (tenant_id = current_setting(''app.tenant_id'', true))
    ', tenant_id);
    
    -- Create RLS policies for documents
    EXECUTE format('
 CREATE POLICY IF NOT EXISTS tenant_isolation_documents ON %I.documents
        USING (tenant_id = current_setting(''app.tenant_id'', true))
    ', tenant_id);
    
    RAISE NOTICE 'Creating Apache AGE graph for tenant: %', tenant_id;
    
    -- Create Apache AGE graph
    graph_name := tenant_id || '_graph';
    PERFORM ag_catalog.create_graph(graph_name);
    
    RAISE NOTICE 'Tenant schema created successfully: %', tenant_id;
    
EXCEPTION
    WHEN OTHERS THEN
  RAISE EXCEPTION 'Failed to create tenant schema for %: %', tenant_id, SQLERRM;
END;
$$ LANGUAGE plpgsql;

COMMENT ON FUNCTION create_tenant_schema(TEXT) IS 'Creates a complete tenant-isolated schema with graph support';

-- Function to drop a tenant schema (use with caution!)
CREATE OR REPLACE FUNCTION drop_tenant_schema(tenant_id TEXT, confirm_deletion TEXT)
RETURNS VOID AS $$
DECLARE
    graph_name TEXT;
BEGIN
    -- Safety check
    IF confirm_deletion != 'DELETE_' || tenant_id THEN
        RAISE EXCEPTION 'Deletion not confirmed. Must provide: DELETE_%', tenant_id;
    END IF;
    
    -- Sanitize tenant_id
 tenant_id := LOWER(REGEXP_REPLACE(tenant_id, '[^a-z0-9_]', '_', 'g'));
    
  RAISE NOTICE 'Dropping schema for tenant: %', tenant_id;
 
    -- Drop Apache AGE graph
    graph_name := tenant_id || '_graph';
    BEGIN
        PERFORM ag_catalog.drop_graph(graph_name, true);
    EXCEPTION
        WHEN OTHERS THEN
            RAISE NOTICE 'Could not drop graph %: %', graph_name, SQLERRM;
    END;
    
    -- Drop schema
    EXECUTE format('DROP SCHEMA IF EXISTS %I CASCADE', tenant_id);
    
    RAISE NOTICE 'Tenant schema dropped: %', tenant_id;
END;
$$ LANGUAGE plpgsql;

COMMENT ON FUNCTION drop_tenant_schema(TEXT, TEXT) IS 'Drops a tenant schema and all data (requires confirmation)';

-- Record this migration
INSERT INTO public.schema_version (version_number, script_name)
VALUES ('1.0.0', '003_create_tenant_schema_function.sql');
