-- ComiCal Database Initialization Script for PostgreSQL
-- This script creates the database schema for the ComiCal application

-- Create comic table (main table for comic information)
CREATE TABLE IF NOT EXISTS comic (
    isbn VARCHAR(13) NOT NULL PRIMARY KEY,
    title VARCHAR(255) NOT NULL,
    titlekana VARCHAR(255),
    seriesname VARCHAR(255),
    seriesnamekana VARCHAR(255),
    author VARCHAR(100) NOT NULL,
    authorkana VARCHAR(100),
    publishername VARCHAR(100) NOT NULL,
    salesdate TIMESTAMP WITHOUT TIME ZONE NOT NULL,
    schedulestatus SMALLINT NOT NULL  -- 0: Confirm, 1: UntilDay, 2: UntilMonth, 3: UntilYear, 9: Undecided
);

-- Create indexes for better search performance
CREATE INDEX IF NOT EXISTS ix_comic_titleandkana ON comic (title, titlekana);
CREATE INDEX IF NOT EXISTS ix_comic_seriesandkana ON comic (seriesname, seriesnamekana);
CREATE INDEX IF NOT EXISTS ix_comic_authorandkana ON comic (author, authorkana);

-- Create comicimage table (stores comic image information)
CREATE TABLE IF NOT EXISTS comicimage (
    isbn VARCHAR(13) NOT NULL PRIMARY KEY,
    imagebaseurl VARCHAR(255) NOT NULL,
    imagestorageurl TEXT
);

-- Create configmigration table (stores configuration migration data)
CREATE TABLE IF NOT EXISTS configmigration (
    id CHAR(10) NOT NULL PRIMARY KEY,
    value TEXT NOT NULL
);

-- Create batch_states table (stores batch execution state and control information)
CREATE TABLE IF NOT EXISTS batch_states (
    id SERIAL PRIMARY KEY,
    batch_date DATE NOT NULL UNIQUE,
    status VARCHAR(50) NOT NULL DEFAULT 'pending',  -- pending, running, completed, failed, delayed, manual_intervention
    total_pages INTEGER,
    processed_pages INTEGER DEFAULT 0,
    failed_pages INTEGER DEFAULT 0,
    registration_phase VARCHAR(50) DEFAULT 'pending',  -- pending, running, completed, failed
    image_download_phase VARCHAR(50) DEFAULT 'pending',  -- pending, running, completed, failed
    delayed_until TIMESTAMP WITHOUT TIME ZONE,
    retry_attempts INTEGER NOT NULL DEFAULT 0,
    manual_intervention_required BOOLEAN NOT NULL DEFAULT FALSE,
    auto_resume_enabled BOOLEAN NOT NULL DEFAULT TRUE,
    error_message TEXT,
    created_at TIMESTAMP WITHOUT TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITHOUT TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP
);

-- Create indexes for batch_states table
CREATE INDEX IF NOT EXISTS ix_batch_states_batch_date ON batch_states (batch_date);
CREATE INDEX IF NOT EXISTS ix_batch_states_status ON batch_states (status);
CREATE INDEX IF NOT EXISTS ix_batch_states_delayed_until ON batch_states (delayed_until) WHERE delayed_until IS NOT NULL;
CREATE INDEX IF NOT EXISTS ix_batch_states_manual_intervention ON batch_states (manual_intervention_required) WHERE manual_intervention_required = TRUE;

-- Create batch_page_errors table (stores page-level error records and retry information)
CREATE TABLE IF NOT EXISTS batch_page_errors (
    id SERIAL PRIMARY KEY,
    batch_id INTEGER NOT NULL REFERENCES batch_states(id) ON DELETE CASCADE,
    page_number INTEGER NOT NULL,
    phase VARCHAR(50) NOT NULL,  -- registration, image_download
    error_type VARCHAR(100) NOT NULL,
    error_message TEXT NOT NULL,
    retry_count INTEGER NOT NULL DEFAULT 0,
    last_retry_at TIMESTAMP WITHOUT TIME ZONE,
    resolved BOOLEAN NOT NULL DEFAULT FALSE,
    resolved_at TIMESTAMP WITHOUT TIME ZONE,
    created_at TIMESTAMP WITHOUT TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITHOUT TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT uq_batch_page_phase UNIQUE (batch_id, page_number, phase)
);

-- Create indexes for batch_page_errors table
CREATE INDEX IF NOT EXISTS ix_batch_page_errors_batch_id ON batch_page_errors (batch_id);
CREATE INDEX IF NOT EXISTS ix_batch_page_errors_page_number ON batch_page_errors (page_number);
CREATE INDEX IF NOT EXISTS ix_batch_page_errors_resolved ON batch_page_errors (resolved) WHERE resolved = FALSE;
CREATE INDEX IF NOT EXISTS ix_batch_page_errors_phase ON batch_page_errors (phase);
