# Codex project adapter

The shared [AGENTS.md](../AGENTS.md) is the entrypoint. [MODEL_ROUTING.md](../docs/agents/MODEL_ROUTING.md) defines tier selection, escalation and unavailable-model fallback.

The project [config](config.toml) enables agents, caps parallel workers at three and supplies a Luna/medium default for bounded work. It leaves the user's front model and session permissions intact. The orchestrator must explicitly select Sol/frontier models for more demanding assignments.

Worker files in `agents/` deliberately omit `model` and `model_reasoning_effort`: current Codex custom-file values otherwise override invocation choices. The roles are `repo_scout`, `repo_implementer` and `repo_reviewer`; select model/effort at invocation. Read-only roles request a read-only sandbox, subject to the host's effective permission rules.

Use a current client that supports standalone project agent TOML and the documented `[agents]` fields. Project config loading is subject to Codex's workspace trust rules; accept the normal project trust prompt when appropriate, then start a new session to discover the files. Git worktrees, pull and commit use this checkout's actual repository; remote protections require configuration at the Git host.

Source: [official Codex subagent configuration](https://learn.chatgpt.com/docs/agent-configuration/subagents). The front model must use the supported native agent tools; these definitions do not launch another provider or override account limits.
