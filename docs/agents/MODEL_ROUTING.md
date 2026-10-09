# Model routing for orchestrators and workers

Keep the user's selected front model as the orchestrator: GPT Sol/Astra or Claude Opus/Fable when available. It owns ambiguous decisions, contracts, delegation and integration. The front model also implements work directly when handoff would cost more than it saves. Repository instructions cannot change the active front model or make another provider available.

## Resolve availability, then choose the tier

At the first delegation in a session, use the client's available-model list and supported spawn options. Check official provider documentation when a model/version or capability is uncertain. Reuse that decision for the session; do not browse pricing/model catalogs before every worker. Explicit user model choices take precedence.

These are engineering defaults, not universal benchmark rankings:

| Assignment | Starting tier | Codex candidates | Claude Code candidates |
|---|---|---|---|
| Bounded search, summarization, exact mechanical edits | Fast, economical | Available current Luna | Available current Haiku |
| Routine implementation, debugging, consumer tracing, normal review | Balanced coding | Available current Sol | Available current Sonnet |
| Ambiguous architecture, difficult root cause, numerical reasoning, sensitive contracts | Frontier | Available current Astra or strong Sol configuration | Available current Opus/Fable |
| Independent review of a consequential change | Capable of the changed domain | Sol; Astra for harder risks | Sonnet; Opus/Fable for harder risks |

Before spawning, estimate ambiguity, coupling, failure cost, required tools and context. Choose the least expensive tier likely to succeed, including retries and integration cost. A short but subtle schema/security change deserves a stronger worker than a long mechanical edit. Do not assume Fable, Opus or Astra is automatically best for every task; use observed task results.

Use low/medium reasoning for clear work when supported; increase it for evidence-backed complexity. Do not inherit maximal front-model reasoning into every worker. If the client cannot override worker models, use its built-in agents or the current model with a narrow packet, and disclose any material efficiency limitation. Do not claim cheaper execution merely because a prompt names a model.

## Escalation and fallbacks

- A concrete failed approach merits diagnosis. Repeated failure, unresolved ambiguity or a larger contract risk merits narrowing or moving up a tier. Avoid sending the same unsuccessful prompt repeatedly.
- Resolve missing context before paying for a stronger model. Escalate decisions to the orchestrator when they span owners or change acceptance.
- If a model is unavailable, select another listed model in the appropriate tier, or use the front model. Never guess a provider ID or silently switch to an unsupported name.
- A useful worker return includes evidence and remaining uncertainty. Confidence without evidence is not grounds for accepting a change.
- Keep routing notes to a line per substantial assignment: role, model and reason. Persist them only when useful for long-task recovery or comparing actual failures/latency. No new telemetry service, model benchmark suite or API bridge is needed.

## Client adapters

**Codex:** `.codex/config.toml` enables delegation and caps it at three simultaneous workers. Its economical default applies to unspecified worker models; the orchestrator explicitly overrides model/effort for balanced or frontier assignments. `.codex/agents/` defines scout, implementer and reviewer roles without fixed models so invocation overrides remain effective. Scout/reviewer request read-only execution; workers inherit the active session's permissions. Use the native spawn tool's model/effort fields, with a focused context fork where supported. Hosts that restrict overrides to explicit instructions are authorized by this routing policy to make the task-appropriate selection, subject to higher-priority client constraints.

**Claude Code:** `CLAUDE.md` imports `AGENTS.md`; `.claude/agents/` provides a Haiku scout and Sonnet implementer/reviewer. Select the invocation model to override those defaults for the actual assignment. Scout/reviewer only read/search; provide routed paths and command evidence in their packets. Use the client model selector for the front model; workers do not recursively delegate.

Use each client's native model catalog. Codex does not gain Claude workers, nor Claude Code OpenAI workers, from these files. Cross-provider execution needs an explicitly configured and authorized bridge; none is installed by this repo.

## Verified reference snapshot — 2026-10-09

The snapshot supports bootstrap configuration; runtime availability wins. Refresh it when adopting a different model generation or client, not during ordinary coding.

- OpenAI currently documents `gpt-6.1-sol` for balanced work, `gpt-6-astra` for demanding work and `gpt-6-luna` for focused economical work. Availability and supported reasoning vary by client/account. Sources: [model selection](https://developers.openai.com/api/docs/guides/model-selection), [Codex subagents and configuration](https://learn.chatgpt.com/docs/agent-configuration/subagents).
- Claude Code documents the `haiku`, `sonnet`, `opus` and `fable` aliases. The current Anthropic API mappings are Haiku/Sonnet/Opus 5.5 and Fable 5.1; provider mappings can differ. Prefer supported aliases for latest worker releases; resolve an exact ID only when reproducibility requires it. Sources: [model configuration](https://code.claude.com/docs/en/model-config), [subagent overrides](https://code.claude.com/docs/en/sub-agents).
- Claude Code at preparation time is `2.1.196`. Official model guidance requires newer clients for those latest releases (Haiku ≥2.1.293, Sonnet ≥2.1.284, Opus ≥2.1.280, Fable ≥2.1.257). Update the client through its normal installation mechanism to use them; the repo's longstanding Haiku/Sonnet role aliases remain portable. No client upgrade or authenticated model execution was performed during preparation.
