# Selectors audit — Phase 1 E2E

This document records gaps between the **registry keys** requested by the
Phase 1 E2E test plan and the **actual `data-testid` attributes** rendered
by the Angular v21 frontend (`src/frontend/src/app/`).

The frontend is **not modified** by the E2E task. Each row below documents
either a rename (the registry key was bound to a differently-named but
semantically equivalent existing testid) or a true gap (no equivalent
testid exists; specs must use `test.fixme()` or `test.skip()` until the
frontend exposes one).

Audit method: `grep -r "data-testid" src/frontend/src/`.

## Renames (registry key → actual frontend testid)

| Registry key (selectors/) | Requested name | Actual frontend testid |
| ------------------------- | -------------- | ---------------------- |
| `HOME.navLinkHome`        | `nav-link-home`        | `nav-home`             |
| `HOME.navLinkSearch`      | `nav-link-search`      | `nav-search`           |
| `SEARCH.paginationCursor` | `pagination-cursor`    | `pagination-load-more` |
| `SERIES_DETAIL.seriesTitle` | `series-title`     | `series-detail-title`  |
| `VOLUME_BY_ISBN.volumeDetail`     | `volume-detail`        | `volume-by-isbn-card`        |
| `VOLUME_BY_ISBN.volumeIsbn`       | `volume-isbn`          | `volume-by-isbn-isbn`        |
| `VOLUME_BY_ISBN.volumeReleaseDate`| `volume-release-date`  | `volume-by-isbn-release`     |
| `VOLUME_BY_ISBN.volumeSeriesLink` | `volume-series-link`   | `volume-by-isbn-series-link` |
| `LEGAL.footerPrivacyLink` | `footer-privacy-link`  | `footer-link-privacy`  |
| `LEGAL.footerTermsLink`   | `footer-terms-link`    | `footer-link-terms`    |
| `LEGAL.footerOssLink`     | `footer-oss-link`      | `footer-link-oss`      |

## Gaps (no equivalent testid in frontend)

| Registry key | Requested testid | Status | Notes |
| ------------ | ---------------- | ------ | ----- |
| `SERIES_DETAIL.seriesAuthor`    | `series-author`    | MISSING | The series-detail page only renders title + status + volume list. Author is not surfaced. Spec uses `test.fixme()` if asserting. |
| `SERIES_DETAIL.seriesPublisher` | `series-publisher` | MISSING | Same as above — publisher is not in the Phase 1 detail layout. |
| Toast container — global        | `toast` / `toast-container` | MISSING | `ToastService` exists (`core/services/toast.service.ts`) but no DOM element with a stable testid is rendered yet. `ToastComponent` PO is provided as a placeholder using `[role="status"]`/`[role="alert"]` ARIA fallback so error-path UX assertions can be wired once the component lands. |

## Frontend-side follow-up (NOT in scope of this task)

When the frontend adds the missing attributes, only `_audit.md` and the
relevant `selectors/*.ts` constants need to be updated — POMs and specs
already consume them via the registry.
