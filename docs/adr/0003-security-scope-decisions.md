# 0003. Security Scope Decisions for Fuzzing and CII Best Practices

- **Status**: Accepted
- **Date**: 2026-08-01
- **Deciders**: Repository maintainer, Copilot coding agent

## Context and Problem Statement

OpenSSF Scorecard reports governance alerts for Fuzzing (#20) and CII-Best-Practices (#19). ComiCal is a web application that aggregates manga release information from Rakuten Books API and exposes bounded user workflows such as search, subscriptions, and purchase tracking. It is not a reusable parser, compiler, protocol implementation, cryptography library, or native memory component that would naturally fit OSS-Fuzz onboarding.

The CII Best Practices badge is useful for public security posture, but it is a self-enrollment and evidence collection process outside the code changes required for this workstream. It should be tracked explicitly without blocking the governance documentation PR.

## Decision Drivers

- Focus security engineering effort on the highest-risk input surfaces for this application.
- Avoid introducing an OSS-Fuzz integration that would have low signal and high maintenance cost for a bounded web app.
- Keep Scorecard dismissals auditable by documenting rationale and residual risk.
- Track the CII badge as a separate maintainer-owned governance task.

## Considered Options

- **Option A: Risk-accept fuzzing for now and track CII badge separately** — Use existing xUnit boundary tests and Testcontainers-backed integration tests for the bounded input surface; open a follow-up issue for CII badge enrollment.
- **Option B: Add OSS-Fuzz immediately** — Attempt to build fuzz harnesses and OSS-Fuzz project configuration in this PR.
- **Option C (Status quo)** — Leave both Scorecard alerts open without documented rationale or follow-up.

## Decision Outcome

採用案: **Option A: Risk-accept fuzzing for now and track CII badge separately**

### Rationale

ComiCal's main untrusted inputs are HTTP API request bodies, authentication claims from Entra External ID, and Rakuten Books API JSON responses. These are better protected by validators, schema/contract tests, boundary tests, and integration tests than by OSS-Fuzz-style native fuzz harnesses. OSS-Fuzz remains out of scope until the repository introduces a parser/protocol component or a high-risk transformation layer with a stable fuzz target.

The CII Best Practices badge is not a code dependency or runtime control. It should be pursued as a separate governance checklist so the maintainer can provide accurate project metadata, policy evidence, and process attestations.

### Scope decisions

#### Fuzzing (#20)

- Decision: Do not onboard OSS-Fuzz for this workstream.
- Justification: The application is content-aggregation focused with limited untrusted input surface; existing .NET xUnit boundary tests and frontend/backend validation cover the relevant request/response boundaries.
- Residual risk: Rakuten Books API JSON parsing or future feed transformations may accept malformed or unexpected data.
- Mitigation: Keep schema/DTO validation and boundary tests around Rakuten API responses; add targeted property/fuzz-style tests if a new parser or complex normalizer is introduced.

#### CII Best Practices (#19)

- Decision: Do not block this PR on badge enrollment.
- Justification: CII badge completion is self-enrollment requiring maintainer verification of project practices and public metadata.
- Follow-up: Track issue #337 for the maintainer to complete the badge questionnaire and attach evidence.

### Consequences

- ✅ Positive: Governance risk acceptance is explicit and reviewable instead of being an unexplained Scorecard dismissal.
- ✅ Positive: Testing investment remains aligned with the actual web application input surface.
- ⚠️ Negative / Trade-off: Scorecard Fuzzing remains a risk-accepted/dismissal item rather than a technical implementation.
- ⚠️ Negative / Trade-off: CII badge score improvement is deferred until maintainer self-enrollment is complete.

## Validation

- Confirm this ADR is linked when dismissing Scorecard Fuzzing alert #20 as `tolerable_risk` or equivalent maintainer-approved rationale.
- Confirm the follow-up CII badge issue exists and is linked from alert #19 before dismissing or resolving the alert.
- During future parser/feed-normalization changes, require tests for malformed Rakuten API JSON and reconsider property-based or fuzz-style tests.

## Links

- OpenSSF Scorecard Fuzzing alert: https://github.com/Takas0522/ComiCal/security/code-scanning/20
- OpenSSF Scorecard CII-Best-Practices alert: https://github.com/Takas0522/ComiCal/security/code-scanning/19
- Follow-up CII badge issue: https://github.com/Takas0522/ComiCal/issues/337
- CII Best Practices Badge: https://www.bestpractices.dev/
