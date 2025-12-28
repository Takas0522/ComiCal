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
