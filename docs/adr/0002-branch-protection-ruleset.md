# 0002. Branch Protection Ruleset for main

- **Status**: Accepted
- **Date**: 2026-08-01
- **Deciders**: Repository maintainer, Copilot coding agent

## Context and Problem Statement

OpenSSF Scorecard reports governance alerts for Branch-Protection (#10) and Code-Review (#17). The repository follows trunk-based development and requires PR-based changes, but the policy must be represented as an enforceable GitHub Ruleset so direct pushes and unreviewed merges cannot silently bypass the documented process.

ComiCal is currently a single-maintainer repository. The maintainer needs an auditable emergency bypass for repository recovery, security hotfixes, and misconfigured CI/ruleset remediation; therefore the ruleset allows repository administrators to bypass with an audit trail.

SAST (#22) was also investigated while defining the required checks. CodeQL is enabled in `.github/workflows/codeql.yml` for `push` to `main`, `pull_request` to `main`, and a weekly Monday 03:00 UTC schedule. `gh run list --workflow=codeql.yml -L 30` showed recent `main` pushes on 2026-08-01 completed successfully. The Scorecard alert text says “25 commits out of 27 are checked with a SAST tool”; comparing the current latest 27 `main` commits with `/code-scanning/analyses?tool_name=CodeQL` found all 27 now have CodeQL analyses. Older missing analyses are historical 2026-05-02 dependency/workflow commits from before the current CodeQL coverage was consistently uploaded. No trigger or `paths-ignore` gap was found, so no workflow change is made in this ADR.

## Decision Drivers

- Enforce PR-only changes to `main` and prevent direct push/force-push drift.
- Require at least one approving review for Scorecard Code-Review expectations while preserving single-maintainer recovery.
- Require CI, infrastructure validation, and CodeQL/SAST before merging.
- Keep merge history linear and align the final `main` commit with Conventional Commits via squash merge.
- Make the maintainer-only administrative action reproducible because this agent lacks GitHub admin API access.

## Considered Options

- **Option A: Repository Ruleset on `main` with admin bypass** — Require PRs, one approval, status checks, CodeQL/code scanning, linear history, and squash-only PR merge method; allow repository admins to bypass when necessary.
- **Option B: Legacy branch protection rule** — Use classic branch protection for reviews and status checks.
- **Option C (Status quo)** — Keep the written branching policy without GitHub enforcement.

## Decision Outcome

採用案: **Option A: Repository Ruleset on `main` with admin bypass**

### Rationale

GitHub Rulesets are the current policy-as-configuration mechanism and can cover pull request requirements, required checks, non-fast-forward protection, linear history, CodeQL code scanning, and bypass actors in one auditable object. Admin bypass is intentionally limited to repository administrators (`RepositoryRole` actor id `5`) because the repository has a single maintainer and must remain recoverable if a required check or ruleset is misconfigured.

### Required policy

- PR-only for `refs/heads/main`; no direct push to `main` for non-bypass actors.
- One approving review before merge.
- Stale approvals are dismissed on new reviewable pushes.
- Review conversations must be resolved.
- Force pushes are blocked.
- Linear history is required.
- Allowed pull request merge method is squash only.
- Required logical checks: `lint`, `test-frontend`, `test-backend`, `build-db`, `bicep-what-if`, and `codeql`.
- CodeQL code scanning must report results with no `errors` and no `high_or_higher` security alerts before `main` is updated.
- Repository administrators may bypass for emergency recovery and single-maintainer continuity; bypasses must be noted in the PR/issue timeline.

### Maintainer command

This agent cannot apply repository rulesets because it does not have GitHub admin API access. The maintainer must run the following from an authenticated `gh` session with admin access. If the ruleset does not exist yet, create an empty `main-protection` ruleset in the UI or by changing `--method PUT /repos/Takas0522/ComiCal/rulesets/${RULESET_ID}` to `--method POST /repos/Takas0522/ComiCal/rulesets` and removing the `RULESET_ID` lookup.

```bash
RULESET_ID=$(gh api /repos/Takas0522/ComiCal/rulesets \
  --jq '.[] | select(.name == "main-protection") | .id')

gh api --method PUT "/repos/Takas0522/ComiCal/rulesets/${RULESET_ID}" \
  -H 'Accept: application/vnd.github+json' \
  -H 'X-GitHub-Api-Version: 2026-03-10' \
  --input - <<'JSON'
{
  "name": "main-protection",
  "target": "branch",
  "enforcement": "active",
  "bypass_actors": [
    {
      "actor_id": 5,
      "actor_type": "RepositoryRole",
      "bypass_mode": "always"
    }
  ],
  "conditions": {
    "ref_name": {
      "include": ["refs/heads/main"],
      "exclude": []
    }
  },
  "rules": [
    {
      "type": "pull_request",
      "parameters": {
        "allowed_merge_methods": ["squash"],
        "dismiss_stale_reviews_on_push": true,
        "require_code_owner_review": false,
        "require_last_push_approval": false,
        "required_approving_review_count": 1,
        "required_review_thread_resolution": true
      }
    },
    { "type": "required_linear_history" },
    { "type": "non_fast_forward" },
    {
      "type": "required_status_checks",
      "parameters": {
        "strict_required_status_checks_policy": true,
        "required_status_checks": [
          { "context": "lint" },
          { "context": "test-frontend" },
          { "context": "test-backend" },
          { "context": "build-db" },
          { "context": "bicep-what-if" },
          { "context": "codeql" }
        ]
      }
    },
    {
      "type": "code_scanning",
      "parameters": {
        "code_scanning_tools": [
          {
            "tool": "CodeQL",
            "alerts_threshold": "errors",
            "security_alerts_threshold": "high_or_higher"
          }
        ]
      }
    }
  ]
}
JSON
```

Because GitHub required status check names must match exact check-run contexts, the maintainer should verify the check names after the next PR run. If current workflows expose different context labels, either rename/add aggregate jobs to `lint`, `test-frontend`, `test-backend`, `build-db`, `bicep-what-if`, and `codeql`, or update the ruleset contexts to the observed names before setting `enforcement` to `active`.

### Consequences

- ✅ Positive: Scorecard Branch-Protection and Code-Review expectations become enforceable, auditable repository settings.
- ✅ Positive: Required checks and CodeQL are explicit merge gates for `main`.
- ⚠️ Negative / Trade-off: Admin bypass can reduce strictness, but is documented and limited to repository administrators for single-maintainer recovery.
- ⚠️ Negative / Trade-off: Incorrect status check context names can block all PR merges; verification is required before or immediately after activation.

## Validation

- Run `gh api /repos/Takas0522/ComiCal/rulesets --jq '.[] | select(.name == "main-protection")'` and confirm `enforcement` is `active`.
- Run `gh api /repos/Takas0522/ComiCal/rules/branches/main --jq '.[].type'` and confirm `pull_request`, `required_status_checks`, `required_linear_history`, `non_fast_forward`, and `code_scanning` apply to `main`.
- Open a test PR and confirm direct push is blocked, one approval is required, and required checks gate merging.
- Re-run OpenSSF Scorecard after ruleset activation and confirm alerts #10 and #17 close or are dismissible with this ADR.
- Re-run CodeQL or wait for the weekly schedule and confirm SAST alert #22 no longer reports unchecked recent commits.

## Links

- OpenSSF Scorecard Branch-Protection alert: https://github.com/Takas0522/ComiCal/security/code-scanning/10
- OpenSSF Scorecard Code-Review alert: https://github.com/Takas0522/ComiCal/security/code-scanning/17
- OpenSSF Scorecard SAST alert: https://github.com/Takas0522/ComiCal/security/code-scanning/22
- GitHub Rulesets REST API: https://docs.github.com/en/rest/repos/rules
