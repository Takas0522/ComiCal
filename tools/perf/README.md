# ComiCal Performance Test Harness (planned)

This directory will host load / soak / smoke scripts that drive the deployed
`dev` environment. **Not yet implemented** — Phase 3 (`P3-10`) deferred actual
load testing until Stage Z provides a real Azure target. The targets and
budgets below are the source of truth that the eventual scripts must verify.

## Frontend (Lighthouse / WebPageTest)

| Metric | Target (mobile, 4G throttle) |
| --- | --- |
| Performance score | ≥ 90 |
| LCP | ≤ 2.5 s |
| INP | ≤ 200 ms |
| CLS | ≤ 0.1 |
| TBT | ≤ 200 ms |

Hard bundle budgets are enforced at build time via `angular.json` `budgets`:
`initial` warning 350 kB / error 500 kB, `anyComponentStyle` 4 kB / 8 kB.

## API (k6 — to be authored)

Planned scripts under `tools/perf/k6/`:

- `series-search.js` — `/api/v1/series?q=…` mixed cache-hit/miss, target P95 ≤ 300 ms.
- `calendar-window.js` — `/api/v1/calendar?monthFrom=…&monthCount=3`, P95 ≤ 400 ms.
- `series-detail.js` — `/api/v1/series/{id}`, P95 ≤ 250 ms.
- `me-subscriptions.js` — authenticated read/write mix, P95 ≤ 350 ms.

Run shape:

```bash
k6 run --vus 50 --duration 5m tools/perf/k6/series-search.js
```

Acceptance gate (CI, post-Stage-Z): all P95 latencies under target and HTTP
error rate < 0.5 %. Until then this README is the placeholder noted by Phase 3
performance hardening.

## Batch (Durable Functions SLOs)

- Daily 03:00 JST `DailyFetchOrchestrator` end-to-end ≤ 30 min.
- Rakuten Books client respects 1 req/sec rate limit (verified via unit test
  in `src/tests/backend`); load tests are not appropriate against the real API.

## See Also

- `docs/specs/oo-init/14-observability-sre.md` — alert thresholds
- `docs/specs/oo-init/16-test-strategy.md` — overall test pyramid
- `angular.json` — frontend build budgets
- `src/backend/api/Common/CacheControlPolicies.cs` — anonymous-read cache TTL
