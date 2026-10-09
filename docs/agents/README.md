# Agent workflow map

Start with the root [AGENTS.md](../../AGENTS.md). Read further by need:

| Need | Guide |
|---|---|
| Split, delegate and integrate substantial work | [WORKFLOW.md](WORKFLOW.md) |
| Pick worker models and escalate | [MODEL_ROUTING.md](MODEL_ROUTING.md) |
| Choose sufficient checks and stop | [QUALITY_BAR.md](QUALITY_BAR.md) |
| Statuses, Done/Verified, regressions, evidence records, phase gates | [DEFINITION_OF_DONE.md](DEFINITION_OF_DONE.md) |
| Change a contract or resolve a spec conflict | [CHANGE_CONTROL.md](CHANGE_CONTROL.md) |
| Preserve review/recovery evidence | [HANDOFF_TEMPLATE.md](HANDOFF_TEMPLATE.md) |
| Dependencies, untrusted input or external actions | [SECURITY.md](SECURITY.md) |

Codex uses [project configuration](../../.codex/config.toml) and project-scoped worker definitions. Claude Code imports the same root instructions through [CLAUDE.md](../../CLAUDE.md) and provides equivalent workers under `.claude/agents/`. Model selection is conditional on the client and account; it is not a cross-provider execution service.
