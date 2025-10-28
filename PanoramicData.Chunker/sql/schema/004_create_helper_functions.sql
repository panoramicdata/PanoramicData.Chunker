-- Script: 004_create_helper_functions.sql
-- Description: Creates helper functions for graph operations and queries

-- Function to get entity type ID by name
CREATE OR REPLACE FUNCTION get_entity_type_id(type_name TEXT)
RETURNS INT AS $$
DECLARE
    type_id INT;
BEGIN
    SELECT entity_type_id INTO type_id
    FROM central.entity_types
    WHERE name = type_name AND is_active = TRUE;
    
    IF type_id IS NULL THEN
        RAISE EXCEPTION 'Entity type not found: %', type_name;
    END IF;
    
    RETURN type_id;
END;
$$ LANGUAGE plpgsql STABLE;

-- Function to get relationship type ID by name
CREATE OR REPLACE FUNCTION get_relationship_type_id(type_name TEXT)
RETURNS INT AS $$
DECLARE
    type_id INT;
BEGIN
    SELECT relationship_type_id INTO type_id
    FROM central.relationship_types
    WHERE name = type_name AND is_active = TRUE;
    
    IF type_id IS NULL THEN
        RAISE EXCEPTION 'Relationship type not found: %', type_name;
    END IF;
    
    RETURN type_id;
END;
$$ LANGUAGE plpgsql STABLE;

-- Function to calculate graph statistics for a tenant
CREATE OR REPLACE FUNCTION calculate_graph_statistics(schema_name TEXT)
RETURNS TABLE (
    entity_count BIGINT,
  relationship_count BIGINT,
    avg_relationships_per_entity NUMERIC,
    entity_types JSONB,
    relationship_types JSONB,
    most_connected_entities JSONB
) AS $$
BEGIN
RETURN QUERY EXECUTE format('
  WITH entity_stats AS (
       SELECT COUNT(*) as entity_count
       FROM %I.entities
 ),
        relationship_stats AS (
   SELECT COUNT(*) as relationship_count
  FROM %I.relationships
        ),
   entity_type_dist AS (
   SELECT 
    et.name,
       COUNT(*) as count
    FROM %I.entities e
   JOIN central.entity_types et ON e.entity_type_id = et.entity_type_id
         GROUP BY et.name
        ),
    relationship_type_dist AS (
        SELECT 
      rt.name,
  COUNT(*) as count
   FROM %I.relationships r
JOIN central.relationship_types rt ON r.relationship_type_id = rt.relationship_type_id
            GROUP BY rt.name
        ),
        entity_degrees AS (
        SELECT 
    e.entity_id,
       e.name,
                COUNT(r.relationship_id) as degree
            FROM %I.entities e
            LEFT JOIN %I.relationships r ON e.entity_id = r.source_entity_id OR e.entity_id = r.target_entity_id
            GROUP BY e.entity_id, e.name
 ORDER BY degree DESC
            LIMIT 10
        )
        SELECT 
     es.entity_count,
   rs.relationship_count,
         CASE 
 WHEN es.entity_count > 0 THEN ROUND((rs.relationship_count::NUMERIC / es.entity_count), 2)
                ELSE 0
END as avg_relationships_per_entity,
     (SELECT jsonb_object_agg(name, count) FROM entity_type_dist) as entity_types,
      (SELECT jsonb_object_agg(name, count) FROM relationship_type_dist) as relationship_types,
     (SELECT jsonb_agg(jsonb_build_object(''name'', name, ''degree'', degree)) FROM entity_degrees) as most_connected_entities
        FROM entity_stats es, relationship_stats rs
    ', schema_name, schema_name, schema_name, schema_name, schema_name, schema_name);
END;
$$ LANGUAGE plpgsql;

COMMENT ON FUNCTION calculate_graph_statistics(TEXT) IS 'Calculates comprehensive statistics for a tenant knowledge graph';

-- Function to find entities by name with fuzzy matching
CREATE OR REPLACE FUNCTION find_entities_by_name(
    schema_name TEXT,
    search_name TEXT,
    similarity_threshold FLOAT DEFAULT 0.3,
    max_results INT DEFAULT 10
)
RETURNS TABLE (
    entity_id UUID,
    name TEXT,
    normalized_name TEXT,
    entity_type TEXT,
    similarity_score FLOAT
) AS $$
BEGIN
    RETURN QUERY EXECUTE format('
        SELECT 
  e.entity_id,
      e.name,
     e.normalized_name,
     et.name as entity_type,
   similarity(e.name, %L) as similarity_score
        FROM %I.entities e
        JOIN central.entity_types et ON e.entity_type_id = et.entity_type_id
        WHERE similarity(e.name, %L) > %L
        ORDER BY similarity_score DESC
        LIMIT %L
    ', search_name, schema_name, search_name, similarity_threshold, max_results);
END;
$$ LANGUAGE plpgsql;

COMMENT ON FUNCTION find_entities_by_name(TEXT, TEXT, FLOAT, INT) IS 'Finds entities using fuzzy name matching';

-- Function to get entity neighbors (graph traversal)
CREATE OR REPLACE FUNCTION get_entity_neighbors(
    schema_name TEXT,
    entity_id UUID,
    max_depth INT DEFAULT 1,
    relationship_type TEXT DEFAULT NULL
)
RETURNS TABLE (
    entity_id UUID,
    name TEXT,
    entity_type TEXT,
    relationship_type TEXT,
    distance INT
) AS $$
BEGIN
    IF relationship_type IS NULL THEN
    RETURN QUERY EXECUTE format('
     WITH RECURSIVE neighbors AS (
          -- Base case: the entity itself
     SELECT 
            e.entity_id,
        e.name,
       et.name as entity_type,
           NULL::TEXT as relationship_type,
        0 as distance
      FROM %I.entities e
  JOIN central.entity_types et ON e.entity_type_id = et.entity_type_id
       WHERE e.entity_id = %L
         
          UNION
     
  -- Recursive case: neighbors
  SELECT 
     e.entity_id,
        e.name,
        et.name as entity_type,
              rt.name as relationship_type,
          n.distance + 1 as distance
   FROM neighbors n
    JOIN %I.relationships r ON (
        r.source_entity_id = n.entity_id OR
   r.target_entity_id = n.entity_id
  )
              JOIN %I.entities e ON (
CASE 
       WHEN r.source_entity_id = n.entity_id THEN r.target_entity_id
            ELSE r.source_entity_id
           END = e.entity_id
    )
    JOIN central.entity_types et ON e.entity_type_id = et.entity_type_id
       JOIN central.relationship_types rt ON r.relationship_type_id = rt.relationship_type_id
        WHERE n.distance < %L
       AND e.entity_id != %L  -- Avoid cycles
      )
     SELECT DISTINCT ON (entity_id) 
      entity_id, 
    name, 
     entity_type, 
            relationship_type, 
    distance
    FROM neighbors
            WHERE distance > 0
        ORDER BY entity_id, distance
   ', schema_name, entity_id, schema_name, schema_name, max_depth, entity_id);
    ELSE
        RETURN QUERY EXECUTE format('
      WITH RECURSIVE neighbors AS (
      SELECT 
             e.entity_id,
    e.name,
        et.name as entity_type,
           NULL::TEXT as relationship_type,
0 as distance
                FROM %I.entities e
                JOIN central.entity_types et ON e.entity_type_id = et.entity_type_id
                WHERE e.entity_id = %L
        
     UNION
            
     SELECT 
         e.entity_id,
       e.name,
          et.name as entity_type,
        rt.name as relationship_type,
        n.distance + 1 as distance
         FROM neighbors n
        JOIN %I.relationships r ON (
              r.source_entity_id = n.entity_id OR
 r.target_entity_id = n.entity_id
             )
    JOIN %I.entities e ON (
           CASE 
            WHEN r.source_entity_id = n.entity_id THEN r.target_entity_id
     ELSE r.source_entity_id
        END = e.entity_id
           )
       JOIN central.entity_types et ON e.entity_type_id = et.entity_type_id
    JOIN central.relationship_types rt ON r.relationship_type_id = rt.relationship_type_id
     WHERE n.distance < %L
     AND rt.name = %L
      AND e.entity_id != %L
            )
 SELECT DISTINCT ON (entity_id) 
        entity_id, 
  name, 
             entity_type, 
    relationship_type, 
    distance
     FROM neighbors
            WHERE distance > 0
            ORDER BY entity_id, distance
        ', schema_name, entity_id, schema_name, schema_name, max_depth, relationship_type, entity_id);
    END IF;
END;
$$ LANGUAGE plpgsql;

COMMENT ON FUNCTION get_entity_neighbors(TEXT, UUID, INT, TEXT) IS 'Retrieves neighbors of an entity within specified graph distance';

-- Record this migration
INSERT INTO public.schema_version (version_number, script_name)
VALUES ('1.0.0', '004_create_helper_functions.sql');
