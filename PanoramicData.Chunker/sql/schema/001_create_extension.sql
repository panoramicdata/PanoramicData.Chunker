-- Script: 001_create_extension.sql
-- Description: Install Apache AGE extension for graph database support
-- PostgreSQL Version: 11+
-- Apache AGE Version: 1.5.0+

-- Enable Apache AGE extension
CREATE EXTENSION IF NOT EXISTS age;

-- Load the AGE extension into the search path
LOAD 'age';

-- Set search path to include ag_catalog
SET search_path = ag_catalog, "$user", public;

-- Verify AGE installation
DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM pg_extension WHERE extname = 'age'
    ) THEN
RAISE EXCEPTION 'Apache AGE extension installation failed';
    END IF;
    
    RAISE NOTICE 'Apache AGE extension installed successfully';
END $$;

-- Create schema version tracking table
CREATE TABLE IF NOT EXISTS public.schema_version (
    version_id SERIAL PRIMARY KEY,
    version_number VARCHAR(20) NOT NULL,
    script_name VARCHAR(100) NOT NULL,
    applied_at TIMESTAMP DEFAULT NOW(),
    applied_by VARCHAR(100) DEFAULT CURRENT_USER
);

-- Record this migration
INSERT INTO public.schema_version (version_number, script_name)
VALUES ('1.0.0', '001_create_extension.sql');

COMMENT ON TABLE public.schema_version IS 'Tracks applied database schema migrations';
