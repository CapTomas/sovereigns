# Claude Code adapter

The root `CLAUDE.md` imports `AGENTS.md`, so the same repository policy serves Claude Code and Codex. Read scoped `AGENTS.md` files explicitly before working in their directories; a root `CLAUDE.md` prevents automatic `AGENTS.md` discovery under Claude Code's default instruction mode. Keep policy in `AGENTS.md` and `docs/agents/`, not copied into this adapter.

The front model stays the user's choice. These project agents provide reusable worker roles:

| Agent | Default model | Use |
| --- | --- | --- |
| `repo-scout` | `haiku` | Targeted file, task, and dependency discovery; read-only |
| `repo-implementer` | `sonnet` | A bounded implementation with explicit file ownership |
| `repo-reviewer` | `sonnet` | Independent review of a meaningful change and its evidence; read-only |

Apply `docs/agents/MODEL_ROUTING.md` before dispatch. Do small, tightly coupled work directly when delegation adds more overhead than value. A scout locates authority; the front model resolves ambiguous requirements. A review is a focused decision, not another implementation or automatic rerun of every test. Tool lists omit `Agent`, so these workers return to the front model instead of building recursive agent trees.

Claude Code supports per-invocation model overrides ahead of an agent's frontmatter default. Prefer the installed runtime's current family aliases after confirming they resolve to an available model. Provider defaults and environment overrides can differ; use a supported full model ID when a particular release is required. Do not change the user's front model, account settings, or permissions to make a routing choice.

After adding this adapter, start a new Claude Code session if the agents directory was created during an existing session. Use `/context` to inspect loaded context. On clients that support it, `/tasks` shows the actual worker model (2.1.242+) and `claude plugin validate .claude/agents` checks frontmatter without starting a worker (2.1.233+). Current invocation/environment model precedence requires 2.1.251+; older clients may let `CLAUDE_CODE_SUBAGENT_MODEL` override invocation choices. The installed 2.1.196 predates these features and the latest model requirements recorded in the shared routing guide. These are setup checks, not mandatory steps on each task.

Configuration references, checked 2026-10-09: [memory and shared imports](https://code.claude.com/docs/en/memory), [custom subagents and model precedence](https://code.claude.com/docs/en/sub-agents), [model aliases and provider differences](https://code.claude.com/docs/en/model-config). Model release details live in `docs/agents/MODEL_ROUTING.md`.
