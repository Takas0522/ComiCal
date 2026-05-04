-- ============================================================================
-- One-time data fix: strip stale "covers/" prefix from ThumbnailAssets.BlobKey.
--
-- Background:
--   Earlier versions of BlobStorageService stored BlobKey as "covers/<hash>.jpg"
--   while BlobBaseUrl already contains the container path
--   (e.g. https://<account>.blob.core.windows.net/covers). The mapper concatenates
--   "<BlobBaseUrl>/<BlobKey>", which produced ".../covers/covers/<hash>.jpg" → 404.
--
--   The code has been fixed to write BlobKey as "<hash>.jpg" only. This script
--   normalizes any rows that were persisted with the legacy prefix.
--
-- Idempotency:
--   The WHERE clause limits the UPDATE to rows still carrying the legacy prefix,
--   so re-running the script is a no-op.
-- ============================================================================

UPDATE dbo.ThumbnailAssets
SET BlobKey = SUBSTRING(BlobKey, LEN(N'covers/') + 1, 4000)
WHERE BlobKey LIKE N'covers/%';
