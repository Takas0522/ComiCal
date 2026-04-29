# WireMock fixtures for Rakuten Books API

ComiCal's batch worker (`src/backend/batch/`) calls the [Rakuten Books Total Search API](https://webservice.rakuten.co.jp/documentation/books-total-search) to discover new manga releases. To keep dev loops and CI deterministic — and to stay within Rakuten's published rate limits (1 req/sec) — every test and local run hits a **WireMock** stub backed by the JSON mappings in this directory instead of the live API.

> ⚠️ Fixtures here must be **idempotent and deterministic**. No random data, no `${{random …}}` templating, no clock-dependent fields. Tests rely on stable counts, ISBNs, and titles for assertions.

## Layout

```
tools/wiremock/
├── mappings/                          # WireMock stub definitions
│   ├── rakuten-books-search.json          # default success — 30 manga items
│   ├── rakuten-books-search-no-results.json   # keyword=__NORESULTS__  → 200 + empty Items
│   ├── rakuten-books-search-empty-isbn.json   # keyword=__EMPTYISBN__  → 200 + malformed ISBNs
│   ├── rakuten-books-rate-limit.json          # keyword=__RATELIMIT__  → 429 + Retry-After: 1
│   └── rakuten-books-server-error.json        # keyword=__500__        → 500
├── scripts/
│   ├── run-wiremock.sh                # docker-based standalone server
│   └── healthcheck.sh                 # probes /__admin/health
└── README.md (this file)
```

### Sentinel keywords

The default mapping (`priority: 10`) matches **any** `keyword=…` so most tests get the 30-item success payload. The four error/edge mappings use `priority: 1` or `3` so they take precedence when the request carries the matching sentinel keyword:

| Sentinel keyword | HTTP | Notes |
| --- | --- | --- |
| `__NORESULTS__`  | 200 | Empty `Items: []`, `count: 0`. |
| `__EMPTYISBN__`  | 200 | One item with blank `isbn`, one with non-13-digit `isbn`. |
| `__RATELIMIT__`  | 429 | Sets `Retry-After: 1` for Polly retry. |
| `__500__`        | 500 | Generic upstream failure. |

In WireMock, **lower `priority` values win**, so the sentinel mappings always beat the default.

### Required request fields

The default success mapping requires three query parameters:

- `keyword=<anything>`
- `bookGenreId=001001`  (Rakuten's "Books > Comics & Anime" root genre)
- `applicationId=<anything>`

If any of these are missing the request returns 404 from WireMock — that's intentional: the Rakuten client must always send them.

### URL path

All mappings target:

```
GET /services/api/BooksTotal/Search/20170404
```

This is the same path as the live Rakuten API, which lets the client point its `BaseAddress` at either the WireMock URL (dev/test) or the production endpoint (prod) without changing routes.

## Running locally

```bash
./tools/wiremock/scripts/run-wiremock.sh           # default port 9090
WIREMOCK_PORT=9091 ./tools/wiremock/scripts/run-wiremock.sh

# In another terminal, smoke-test:
./tools/wiremock/scripts/healthcheck.sh
curl 'http://localhost:9090/services/api/BooksTotal/Search/20170404?keyword=ワンピース&bookGenreId=001001&applicationId=test'
```

Admin UI: <http://localhost:9090/__admin>

## How tests use these fixtures

xUnit integration tests boot **`WireMock.Net`** in-process and load this very directory of mappings, so the standalone Docker server is *not* required during `dotnet test`. See [`src/tests/backend/ComiCal.Tests.Integration/Fixtures/WireMockFixture.cs`](../../src/tests/backend/ComiCal.Tests.Integration/Fixtures/WireMockFixture.cs).

```csharp
[Collection("WireMock")]
public sealed class MyRakutenTests(WireMockFixture mock)
{
    [Fact]
    public async Task Search_returns_30_items()
    {
        using var http = new HttpClient { BaseAddress = new Uri(mock.BaseUrl) };
        var res = await http.GetAsync("/services/api/BooksTotal/Search/20170404?keyword=k&bookGenreId=001001&applicationId=test");
        res.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
```

Each fixture instance picks a random free port, so xUnit collections can run in parallel.

## Adding a new mapping

1. Drop a new `*.json` file into `mappings/` following the [WireMock stub schema](https://wiremock.org/docs/stubbing/) (the same JSON shape works in standalone WireMock and in `WireMock.Net`).
2. Choose a `priority`:
   - `10` — default/catch-all responses
   - `3`  — keyword-scoped happy variations (no-results, malformed)
   - `1`  — error injection (4xx / 5xx)
3. Use `queryParameters` matchers (`equalTo`, `matches`) to scope the stub to a specific sentinel keyword if needed.
4. Re-run `dotnet test --filter Category=Integration` to verify the in-process fixture still loads.
5. Keep the response body deterministic — no timestamps, no random IDs.

## See also

- Rakuten Books Total Search API: <https://webservice.rakuten.co.jp/documentation/books-total-search>
- WireMock stubbing reference: <https://wiremock.org/docs/stubbing/>
- `WireMock.Net`: <https://github.com/WireMock-Net/WireMock.Net>
- ComiCal docs: [`docs/specs/oo-init/06-architecture.md`](../../docs/specs/oo-init/06-architecture.md), [`docs/specs/oo-init/09-batch.md`](../../docs/specs/oo-init/09-batch.md)
