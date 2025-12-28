-- ComiCal Database Initialization Script for PostgreSQL
-- This script creates the database schema for the ComiCal application

-- Create Comic table
CREATE TABLE IF NOT EXISTS Comic (
    Isbn VARCHAR(13) NOT NULL PRIMARY KEY,
    Title VARCHAR(255) NOT NULL,
    TitleKana VARCHAR(255),
    SeriesName VARCHAR(255),
    SeriesNameKana VARCHAR(255),
    Author VARCHAR(100) NOT NULL,
    AuthorKana VARCHAR(100),
    PublisherName VARCHAR(100) NOT NULL,
    SalesDate TIMESTAMP NOT NULL,
    ScheduleStatus SMALLINT NOT NULL
);

-- Create indexes for Comic table
CREATE INDEX IF NOT EXISTS IX_Comic_TitleAndKana ON Comic (Title, TitleKana);
CREATE INDEX IF NOT EXISTS IX_Comic_SeriesAndKana ON Comic (SeriesName, SeriesNameKana);
CREATE INDEX IF NOT EXISTS IX_Comic_AuthorAndKana ON Comic (Author, AuthorKana);

-- Create ComicImage table
CREATE TABLE IF NOT EXISTS ComicImage (
    Isbn VARCHAR(13) NOT NULL PRIMARY KEY,
    ImageBaseUrl VARCHAR(255) NOT NULL,
    ImageStorageUrl TEXT
);

-- Create ConfigMigration table
CREATE TABLE IF NOT EXISTS ConfigMigration (
    Id CHAR(10) NOT NULL PRIMARY KEY,
    Value TEXT NOT NULL
);

-- Grant permissions (optional, for development only)
-- For production, grant only specific permissions needed:
-- GRANT SELECT, INSERT, UPDATE, DELETE ON ALL TABLES IN SCHEMA public TO comical;
