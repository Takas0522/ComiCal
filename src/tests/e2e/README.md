# ComiCal E2E (Playwright + Page Object Model)

This package holds end-to-end tests that exercise the deployed (or locally running)
ComiCal stack — Angular v21 SSR via SWA + .NET 10 Functions API, with the
Rakuten Books API stubbed by the WireMock fixtures under `tools/wiremock/`.

## Run locally

```bash
# Terminal 1 — Rakuten Books mock
bash tools/wiremock/scripts/run-wiremock.sh &

# Terminal 1 (cont.) — Functions API
func start --script-root src/backend/api/bin/Debug/net10.0   # placeholder path

# Terminal 2 — Frontend (Angular SSR dev server, port 4200)
pnpm --filter frontend dev

# Terminal 3 — E2E
cd src/tests/e2e
pnpm exec playwright test                 # all browsers
pnpm exec playwright test --project=chromium
pnpm exec playwright test --headed        # debug
pnpm exec playwright show-report          # open last HTML report
```

`E2E_BASE_URL` overrides the default `http://localhost:4200` (e.g. when
running against an SWA Preview Environment).

### Optional: let Playwright start the dev server

`playwright.config.ts` ships with a commented-out `webServer` block. Uncomment
it to have Playwright launch `pnpm --filter frontend dev` automatically. It
honours `reuseExistingServer: !process.env.CI`, so a dev server you already
started in another terminal is preferred over a new one.

## Layout (Page Object Model)

```
src/tests/e2e/
├── playwright.config.ts
├── fixtures/      # Custom `test` extending Playwright with POM injection + axe
├── pages/         # 1 screen = 1 *.page.ts (extends base.page.ts)
├── components/    # Cross-cutting POs (Header / Footer / Toast)
├── specs/         # Thin specs — call PO methods only
├── selectors/     # Centralised data-testid constants + _audit.md
└── seeds/         # Seed data helpers (Testcontainers)
```

## Authoring rules (see `.github/instructions/e2e.instructions.md`)

1. Specs **only** call Page Object methods — no `page.click`, no CSS, no XPath, no text matchers.
2. Selectors live in `selectors/` as `as const` objects keyed by screen.
   Any divergence between requested registry keys and the actual frontend
   `data-testid` values is documented in `selectors/_audit.md`.
3. **`waitForTimeout` is BANNED.** Use Playwright auto-waiting or
   `expect(...).toBeVisible() / .toHaveText() / expect.poll(...)` instead.
   Fixed sleeps make the suite flaky and hide real timing bugs.
4. Page Object methods are named after **user intent** (`searchFor`, `openOssDialogFromFooter`),
   never after DOM operations (`clickSubscribeButton`).
5. Locators on Page Objects are `private readonly` — never expose them to specs.
6. Accessibility assertions go through the `axeBuilder` fixture
   (`@axe-core/playwright`). Specs call `axeBuilder().analyze()` and assert
   on `violations`. The cross-page sweep in `specs/accessibility.spec.ts`
   only fails on `serious`/`critical` impact tags.

## Status (Phase 1)

Every spec wraps assertions in `test.skip(true, 'Requires running app — Stage Z will enable')`
(or `test.fixme()` for assertions blocked on a frontend `data-testid` gap —
see `selectors/_audit.md`). This keeps the suite GREEN locally while
Playwright still validates spec syntax via `pnpm exec playwright test --list`.
Stage Z will flip the skip flag once the SWA + Functions + WireMock stack
is wired into CI.
