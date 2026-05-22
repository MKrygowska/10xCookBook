---
bootstrapped_at: 2026-05-21T17:40:00Z
starter_id: angular
starter_name: Angular
project_name: 10x-cookbook
language_family: js
package_manager: npm
cwd_strategy: subdir-then-move
bootstrapper_confidence: verified
phase_3_status: ok
audit_command: npm audit --json
---

## Hand-off

```yaml
starter_id: angular
package_manager: npm
project_name: 10x-cookbook
hints:
  language_family: js
  team_size: solo
  deployment_target: azure
  ci_provider: github-actions
  ci_default_flow: auto-deploy-on-merge
  bootstrapper_confidence: verified
  path_taken: custom
  quality_override: false
  self_check_answers:
    typed: true
    from_official_starter: true
    conventions: true
    docs_current: true
    can_judge_agent: true
  has_auth: true
  has_payments: false
  has_realtime: false
  has_ai: false
  has_background_jobs: false
```

A solo developer building the 10xCookBook web application under a 3-week after-hours timeline with strict user authentication needs selected a custom frontend architecture using Angular. Angular was chosen for its mature, convention-over-configuration structure and strong type-safety, clearing all four agent-friendly quality gates. The project will deploy to Azure via GitHub Actions with an automated deployment flow on merge to the main branch. The five-point readiness self-check is fully satisfied, giving high confidence for AI agent alignment and code-generation correctness.

## Pre-scaffold verification

| Signal             | Value                              | Severity | Notes                              |
| ------------------ | ---------------------------------- | -------- | ---------------------------------- |
| npm package        | @angular/cli v21.2.12 published 2026-05-20 | fresh    | resolved from cmd_template         |
| GitHub repo        | not run                            | fresh    | from card.docs_url (not a GitHub URL) |

## Scaffold log

**Resolved invocation**: `npx @angular/cli new bootstrap-scaffold --defaults --routing --style scss --skip-tests --ssr false`
**Strategy**: subdir-then-move (temp folder named `bootstrap-scaffold`)
**Exit code**: 0
**Files moved**: 21
**Conflicts (.scaffold siblings)**: none
**.gitignore handling**: moved silently
**.bootstrap-scaffold cleanup**: deleted

## Post-scaffold audit

**Tool**: npm audit --json
**Summary**: 0 CRITICAL, 26 HIGH, 12 MODERATE, 4 LOW
**Direct vs transitive**: 0 direct of total 42 vulnerabilities (transitive only)

#### CRITICAL findings

None.

#### HIGH findings

- **tar** (<=7.5.10): High severity path traversal and race condition advisories (GHSA-83g3-92jg-28cx, GHSA-qffp-2rhf-9h96, GHSA-9ppj-qmqm-q256, GHSA-r6q2-hw4h-h46w). Transitive dependency under pacote, cacache, and node-gyp.
- **tuf-js** (<=2.2.1): High severity vulnerability in tuf-js transitively imported by @sigstore/tuf and make-fetch-happen.

#### MODERATE findings

- **vite** (<=6.4.1): Moderate severity path traversal in optimized deps .map handling (GHSA-4w7w-66w2-5vf9). Transitive devDependency under @angular-devkit/build-angular.
- **webpack-dev-server** (<=5.2.3): Moderate severity cross-origin source code exposure advisories (GHSA-9jgg-88mc-972h, GHSA-4v9v-hfq4-rm2v, GHSA-79cf-xcqc-c78w). Transitive devDependency under @angular-devkit/build-angular and @angular-devkit/build-webpack.

#### LOW / INFO findings

- **tmp** (<=0.2.3): Low severity arbitrary temporary file/directory write via symlink (GHSA-52f5-9888-hmc6). Transitive devDependency under external-editor.
- **webpack** (>=5.49.0 <=5.104.0): Low severity SSRF bypass advisories (GHSA-8fgc-7cc6-rx7x, GHSA-38r7-794h-5758). Transitive devDependency under @angular-devkit/build-angular.

## Hints recorded but not acted on

| Hint                       | Value                              |
| -------------------------- | ---------------------------------- |
| bootstrapper_confidence    | verified                           |
| quality_override           | false                              |
| path_taken                 | custom                             |
| team_size                  | solo                               |
| deployment_target          | azure                              |
| ci_provider                | github-actions                     |
| ci_default_flow            | auto-deploy-on-merge               |
| self_check_answers         | {"typed":true,"from_official_starter":true,"conventions":true,"docs_current":true,"can_judge_agent":true} |
| has_auth                   | true                               |
| has_payments               | false                              |
| has_realtime               | false                              |
| has_ai                     | false                              |
| has_background_jobs        | false                              |

## Next steps

Next: a future skill will set up agent context (CLAUDE.md, AGENTS.md). For now, your project is scaffolded and verified — happy hacking.

Useful manual steps in the meantime:
- `git init` (if you have not already) to start your own repo history.
- Review any `.scaffold` siblings the conflict policy created and decide which version of each file to keep.
- Address audit findings per your project's risk tolerance — the full breakdown is in this log.
