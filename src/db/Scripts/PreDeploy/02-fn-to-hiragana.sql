-- Idempotent: drop fnToHiragana only when it exists and DACPAC will recreate it.
-- Computed columns that reference this function prevent a simple DROP, so we skip
-- the drop here and let DACPAC handle ALTER FUNCTION via its deployment script.
-- This script intentionally left as a no-op; definition is owned by the DACPAC model.
PRINT N'fnToHiragana: managed by DACPAC model (Functions/dbo.fnToHiragana.sql).';
