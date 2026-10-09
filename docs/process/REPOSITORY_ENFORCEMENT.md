# Required repository-level enforcement

Policy documents do not enforce themselves. Configure the actual GitHub repository (or equivalent) before declaring Phase 00 complete:

1. Protect `main`: require PR, successful docs CI, reviewers, conversation resolution and current base checks; disallow force push.
2. Require reviews for cross-domain simulation contracts, save schemas and normative game-rule edits; name actual maintainers in an appropriate CODEOWNERS once their accounts are known. **No fabricated usernames or inactive CODEOWNERS.**
3. Require structured PR templates and evidence for tracked tasks.
4. Restrict token/secret exposure and third-party actions; action versions must be reviewed and pinned.
5. Verify the CI workflow on a clean fork/clone and actual PR, and capture a link/screenshot to prove protection is effective.
6. Once .NET/Godot sources arrive in Phase 01, extend required CI to compile, test and headlessly exercise the game. A docs-only workflow is insufficient for a code project.
7. Review platform and licensing constraints for distribution before accepting a new dependency.

This is a task for the real repository host; this exported filesystem package cannot configure remote branch protections or prove live CI behavior.
