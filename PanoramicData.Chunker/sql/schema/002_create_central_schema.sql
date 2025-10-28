-- Script: 002_create_central_schema.sql
-- Description: Create central shared schema for entity and relationship type definitions
-- This schema is shared across all tenants

-- Create central schema
CREATE SCHEMA IF NOT EXISTS central;

COMMENT ON SCHEMA central IS 'Central shared schema for entity/relationship type definitions';

-- Entity types table (shared across tenants)
CREATE TABLE central.entity_types (
    entity_type_id SERIAL PRIMARY KEY,
    name VARCHAR(100) NOT NULL UNIQUE,
    description TEXT,
    category VARCHAR(50),
    schema JSONB,
    is_active BOOLEAN DEFAULT TRUE,
    created_at TIMESTAMP DEFAULT NOW(),
    updated_at TIMESTAMP DEFAULT NOW()
);

COMMENT ON TABLE central.entity_types IS 'Defines available entity types for knowledge graphs';

-- Insert standard entity types
INSERT INTO central.entity_types (name, description, category) VALUES
 ('Person', 'Individual human entity', 'Universal'),
    ('Organization', 'Company, institution, or group', 'Universal'),
    ('Location', 'Geographic location or place', 'Universal'),
    ('Date', 'Temporal reference or date', 'Universal'),
    ('Time', 'Time reference', 'Universal'),
    ('Money', 'Monetary amount', 'Universal'),
    ('Percentage', 'Percentage value', 'Universal'),
 ('Concept', 'Abstract idea or topic', 'Universal'),
    ('Keyword', 'Important term or keyword', 'Universal'),
    ('Event', 'Temporal event', 'Universal'),
    
    -- Technical types
    ('Class', 'Software class', 'Technical'),
    ('Method', 'Software method or function', 'Technical'),
    ('Function', 'Function definition', 'Technical'),
    ('API', 'API definition', 'Technical'),
    ('Endpoint', 'API endpoint', 'Technical'),
    ('Parameter', 'Function or method parameter', 'Technical'),
    ('Variable', 'Variable definition', 'Technical'),
    ('Technology', 'Technology or platform', 'Technical'),
    ('Framework', 'Software framework', 'Technical'),
    ('Library', 'Software library', 'Technical'),
  
    -- Business types
    ('Product', 'Product or service', 'Business'),
    ('Service', 'Service offering', 'Business'),
    ('Metric', 'Performance metric', 'Business'),
    ('KPI', 'Key Performance Indicator', 'Business'),
    ('Process', 'Business process', 'Business'),
    ('Department', 'Organizational department', 'Business'),
    ('Role', 'Job role or position', 'Business'),
    
  -- Legal types
    ('Party', 'Legal party', 'Legal'),
    ('Clause', 'Contract clause', 'Legal'),
    ('Obligation', 'Legal obligation', 'Legal'),
    ('Right', 'Legal right', 'Legal'),
    ('Jurisdiction', 'Legal jurisdiction', 'Legal'),
    ('LegalConcept', 'Legal concept', 'Legal'),
 
    -- Document types
    ('Document', 'Document reference', 'Document'),
    ('Section', 'Document section', 'Document'),
    ('Chapter', 'Document chapter', 'Document'),
    
-- Security types
('Vulnerability', 'Security vulnerability', 'Security'),
    ('CVE', 'Common Vulnerabilities and Exposures', 'Security'),
    ('ThreatActor', 'Threat actor or attacker', 'Security'),
    
    -- Custom
    ('Custom', 'Custom entity type', 'Custom');

-- Relationship types table (shared across tenants)
CREATE TABLE central.relationship_types (
  relationship_type_id SERIAL PRIMARY KEY,
    name VARCHAR(100) NOT NULL UNIQUE,
    description TEXT,
    category VARCHAR(50),
 is_directed BOOLEAN DEFAULT TRUE,
    source_entity_type_id INT REFERENCES central.entity_types(entity_type_id),
    target_entity_type_id INT REFERENCES central.entity_types(entity_type_id),
    constraints JSONB,
    is_active BOOLEAN DEFAULT TRUE,
    created_at TIMESTAMP DEFAULT NOW(),
    updated_at TIMESTAMP DEFAULT NOW()
);

COMMENT ON TABLE central.relationship_types IS 'Defines available relationship types for knowledge graphs';

-- Insert standard relationship types
INSERT INTO central.relationship_types (name, description, category, is_directed) VALUES
    -- Structural
    ('Contains', 'Entity contains another entity', 'Structural', TRUE),
    ('AppearsIn', 'Entity appears in a chunk', 'Structural', TRUE),
    ('ChildOf', 'Hierarchical child relationship', 'Structural', TRUE),
    ('PartOf', 'Compositional relationship', 'Structural', TRUE),
    ('Follows', 'Sequential relationship', 'Structural', TRUE),
    
    -- Semantic
 ('Mentions', 'Simple mention or co-occurrence', 'Semantic', FALSE),
    ('References', 'Explicit reference', 'Semantic', TRUE),
    ('RelatedTo', 'General semantic relationship', 'Semantic', FALSE),
    ('SynonymOf', 'Synonymous entities', 'Semantic', FALSE),
    ('AntonymOf', 'Opposite meaning', 'Semantic', FALSE),
    ('CooccursWith', 'Co-occurs with another entity', 'Semantic', FALSE),
    
    -- Technical
    ('Implements', 'Implementation relationship', 'Technical', TRUE),
    ('InheritsFrom', 'Inheritance relationship', 'Technical', TRUE),
    ('Calls', 'Method/function call', 'Technical', TRUE),
    ('DependsOn', 'Dependency relationship', 'Technical', TRUE),
    ('Uses', 'Usage relationship', 'Technical', TRUE),
    ('Returns', 'Return type relationship', 'Technical', TRUE),
    ('HasParameter', 'Parameter relationship', 'Technical', TRUE),
    ('ThrowsException', 'Exception relationship', 'Technical', TRUE),
    
    -- Social/Organizational
    ('AuthoredBy', 'Authorship relationship', 'Social', TRUE),
    ('EmployedBy', 'Employment relationship', 'Social', TRUE),
    ('WorksAt', 'Work location relationship', 'Social', TRUE),
    ('ReportsTo', 'Reporting relationship', 'Social', TRUE),
    ('MemberOf', 'Membership relationship', 'Social', TRUE),
    ('LeaderOf', 'Leadership relationship', 'Social', TRUE),
    
    -- Temporal
    ('Before', 'Temporal precedence', 'Temporal', TRUE),
    ('After', 'Temporal succession', 'Temporal', TRUE),
    ('During', 'Temporal containment', 'Temporal', TRUE),
    ('OccurredOn', 'Event occurrence', 'Temporal', TRUE),
    
    -- Spatial
  ('LocatedIn', 'Location relationship', 'Spatial', TRUE),
    ('Near', 'Proximity relationship', 'Spatial', FALSE),
    
    -- Legal
    ('PartyTo', 'Legal party relationship', 'Legal', TRUE),
('Governs', 'Governance relationship', 'Legal', TRUE),
 ('Supersedes', 'Supersession relationship', 'Legal', TRUE),
    ('Obligates', 'Obligation relationship', 'Legal', TRUE),
    
    -- Comparison
    ('SimilarTo', 'Similarity relationship', 'Comparison', FALSE),
    ('DifferentFrom', 'Difference relationship', 'Comparison', FALSE),
    
    -- Security
    ('HasVulnerability', 'Vulnerability relationship', 'Security', TRUE),
    ('Affects', 'Impact relationship', 'Security', TRUE),
    ('Exploits', 'Exploitation relationship', 'Security', TRUE),
    ('Mitigates', 'Mitigation relationship', 'Security', TRUE),
    
    -- Custom
    ('Custom', 'Custom relationship type', 'Custom', TRUE);

-- Create indexes
CREATE INDEX idx_entity_types_category ON central.entity_types(category);
CREATE INDEX idx_entity_types_active ON central.entity_types(is_active);
CREATE INDEX idx_relationship_types_category ON central.relationship_types(category);
CREATE INDEX idx_relationship_types_directed ON central.relationship_types(is_directed);

-- Record this migration
INSERT INTO public.schema_version (version_number, script_name)
VALUES ('1.0.0', '002_create_central_schema.sql');
