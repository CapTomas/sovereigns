# Required repository-level enforcement

Policy documents do not enforce themselves. Phase 00 is not Verified until the items below are live on `CapTomas/sovereigns` and recorded in `tasks/evidence/P00.md`.

**Status, 2026-10-09:** the repository is public, and every item under "Enforcement to apply once public" is live. Evidence is under SOV-P00-T24 and T29 in `tasks/evidence/P00.md`. Re-run the readback commands there after any settings change.

## Decided policy (2026-10-09)

- **Sole maintainer.** GitHub does not let an author approve their own PR, so the ruleset requires **0 approvals**. Review comes from an independent `repo-reviewer` report in the evidence entry. The maintainer's merge is the acceptance ([Definition of Done](../agents/DEFINITION_OF_DONE.md)). This includes PRs that touch cross-domain contracts, save schemas or normative spec text. Raise the approval count when a second maintainer joins.
- **Free plan.** Rulesets on private repositories need GitHub Pro/Team, so the repository is made **public** before enforcement is applied.

## Before making the repository public (maintainer)

1. Accept that history becomes public: commit author names and emails, and every past revision. A 2026-10-09 scan of history found no credentials or keys.
2. Decide on a license. Without a `LICENSE` file, public code stays "all rights reserved".
3. Settings → General → Danger zone → Change visibility → Public.

## Enforcement to apply once public

1. **Protect `main`** with [`.github/rulesets/protect-main.json`](../../.github/rulesets/protect-main.json): PR required, required status checks from GitHub Actions (`docs`; since Phase 01 also `simulation (ubuntu-24.04)`, `simulation (windows-2025)`, `simulation (macos-15)`, `determinism`, `client` and `package`) and up to date with `main`, resolved conversations, no force push, no deletion, no bypass actors.
   `gh api --method POST repos/CapTomas/sovereigns/rulesets --input .github/rulesets/protect-main.json`
2. **Actions hardening:** in Settings → Actions → General, allow only GitHub-owned actions and require SHA-pinned actions. Set default workflow permissions to read-only, stop workflows from approving PRs, and require approval for workflows from outside contributors' fork PRs. The workflow pins actions by commit SHA and requests `contents: read`.
3. **Security:** enable secret scanning and push protection (free for public repositories).
4. **Ownership:** [`.github/CODEOWNERS`](../../.github/CODEOWNERS) names the real maintainer account. Code-owner review is not required while there is one maintainer. Never list fabricated or inactive accounts.
5. **PR evidence:** [the PR template](../../.github/pull_request_template.md) asks for task IDs, evidence entry and reviewer verdict.

## Evidence required for the Phase 00 gate

- Output of `gh api repos/CapTomas/sovereigns/rulesets`, plus the ruleset detail, showing it is active on the default branch.
- A merged PR whose required `docs` check ran on GitHub.
- A throwaway PR with a deliberately failing check, shown as blocked from merging and then closed unmerged.
- A clean-clone run of the repository checks.

## Later phases

- Phase 01 (done): `.github/workflows/build.yml` adds the .NET build and test, cross-OS determinism, headless Godot and packaging checks, and the ruleset requires them. Evidence is in `tasks/evidence/P01.md`. Re-apply the ruleset after renaming or adding a required job: `gh api --method PUT repos/CapTomas/sovereigns/rulesets/<id> --input .github/rulesets/protect-main.json`.
- Review platform and licensing constraints before accepting any dependency (SOV-P01-T14).
