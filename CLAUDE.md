@AGENTS.md

# Claude Code adapter

- Keep `AGENTS.md` and `docs/agents/` as the shared policy for Claude Code and Codex. Put only Claude-specific wiring here.
- Before working in a subtree, read its applicable `AGENTS.md` files from the repository root to the target directory. This import loads the root policy; nested `AGENTS.md` loading varies with Claude Code version and settings.
- The user's selected front model orchestrates. Apply `docs/agents/MODEL_ROUTING.md` when delegation is useful; use the project `repo-scout`, `repo-implementer`, and `repo-reviewer` agents as starting roles, with an explicit per-invocation model override when warranted.
- Give workers a bounded outcome, relevant paths or task ID, file ownership, acceptance criteria, and a proportionate verification plan. Return to the front model for ambiguity, cross-boundary decisions, and integration.
- For setup and model resolution details, read `.claude/README.md` only when needed.
