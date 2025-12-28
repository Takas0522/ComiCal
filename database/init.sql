-- ComiCal Database Initialization Script for PostgreSQL
-- This script creates the database schema for the ComiCal application

-- Enable pg_trgm extension for partial match searching
CREATE EXTENSION IF NOT EXISTS pg_trgm;

-- Create comics table (compatible with Cosmos DB model)
CREATE TABLE IF NOT EXISTS comics (
    isbn VARCHAR(13) NOT NULL PRIMARY KEY,
    type VARCHAR(50) DEFAULT 'comic',
    title TEXT NOT NULL,
    title_kana TEXT,
    series_name TEXT,
    series_name_kana TEXT,
    author TEXT NOT NULL,
    author_kana TEXT,
    publisher_name TEXT NOT NULL,
    sales_date DATE NOT NULL,
    schedule_status INTEGER NOT NULL
);

-- Create GIN indexes with trgm_ops for partial match searching on title and author
CREATE INDEX IF NOT EXISTS idx_comics_title_trgm ON comics USING GIN (title gin_trgm_ops);
CREATE INDEX IF NOT EXISTS idx_comics_author_trgm ON comics USING GIN (author gin_trgm_ops);

-- Create additional B-tree indexes for exact match and sorting
CREATE INDEX IF NOT EXISTS idx_comics_sales_date ON comics (sales_date);
CREATE INDEX IF NOT EXISTS idx_comics_type ON comics (type);

-- Create config_migrations table
CREATE TABLE IF NOT EXISTS config_migrations (
    id VARCHAR(255) NOT NULL PRIMARY KEY,
    value TEXT NOT NULL
);

-- Grant permissions (optional, for development only)
-- For production, grant only specific permissions needed:
-- GRANT SELECT, INSERT, UPDATE, DELETE ON ALL TABLES IN SCHEMA public TO comical;
