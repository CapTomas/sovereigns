# 0. How to use and maintain this bible

## 0.1 Purpose

**Foundational dependency:** The authoritative physical-field atlas and coupled world simulation defined in Sections 3–6 underpin every other chapter. No economics, population, military, AI or battle agent may substitute independent weather, soil, elevation, river or terrain truths.

This file is the **single source of truth for intended game behavior and its underlying physical-world simulation**, not a technical implementation specification. It answers what the game is, what the player experiences, how systems interact, which decisions are settled, which abstractions are intentional, and what behavior is unacceptable. Agents should derive implementation tasks, tests, assets and interface designs from it without replacing its decisions with convenient shortcuts.

It specifies **systems and contracts**, not programming languages, code, function signatures, directory structure, class diagrams, database schemas or arbitrary engine APIs. Numeric examples are illustrations unless explicitly called **design defaults**. Design defaults are tunable in balancing; the governing principle must remain intact. A mechanic described as simulated may use an appropriate approximation if its observable behavior, causal relationships and invariants are preserved.

This is a **living document**, not a frozen implementation contract. Changes are welcome when supported by evidence from playtesting, performance, coherence or improved design. Changes must be recorded rather than made silently.

## 0.2 Authority and conflict resolution

1. **Player-facing promises and invariants take priority** over convenience, sample figures and implementation examples.
2. A specific subsystem rule takes priority over a broad introductory description unless that contradicts an explicit global invariant.
3. A design default may be tuned; its meaning and relationships may not be silently removed.
4. An agent should not invent a disconnected substitute for an explicitly connected system. For example, it must not turn real food stores into a free-floating army supply bar.
5. If a genuine contradiction appears, flag it in the decision log, propose the narrowest coherent amendment and continue on an explicitly marked assumption when possible. Do not quietly implement mutually incompatible behaviors.
6. Do not interpret the build-order chapters as permission to reduce the final design to an isolated small demonstration. They describe validation order, not product scope.
7. A request that clearly conflicts with this bible requires a documented design change, not an unnoticed exception.

## 0.3 Decision vocabulary

- **Committed:** Foundational decision; altering it changes the game's identity.
- **Default:** Explicit starting design; subject to balancing and playtest refinement.
- **Derived:** Emerges from underlying state; must not be maintained as an unrelated permanent bonus.
- **Abstracted:** Modeled at a coarser scale intentionally; still produces credible consequences.
- **Excluded:** Not part of the intended game; do not add without approval.

All mechanics below are part of the intended experience unless explicitly excluded or identified as an optional user setting. There are no undecided design questions left intentionally open in this edition. New edge cases should be resolved according to the invariants before changing the conceptual model.

## 0.4 Editing protocol

For each material revision: update version and date; amend the relevant canonical section; check upstream and downstream systems; add a short entry to the change log; and update affected acceptance scenarios. Do not keep contradictory older variants in the body. Record rejected alternatives only in the decision log when the rationale matters.

**Versioning:** major for changed product identity or fundamental simulation contract; minor for new or materially revised system; patch for clarification, corrections or tuning. **One gameplay document** remains authoritative for mechanics. The separate `SOVEREIGNS_VISUAL_DIRECTION.md` is authoritative for approved art direction, camera treatment, sprite production and audiovisual identity when created; it must never change simulation or gameplay contracts. Engineering plans, asset catalogs and balancing tables may live elsewhere and refer back to section IDs here.

## 0.5 Reading guide

Begin with Sections 1–2 for identity and player experience; **Sections 3–6 are the mandatory physical simulation foundation and should be read before any other gameplay subsystem is designed.** Sections 7–9 establish cultures and government; Sections 10–16 establish economic causality; Sections 17–24 cover diplomacy, armies, territorial control, combat, sieges and aftermath. The remaining sections cover AI, player information, presentation boundaries, simulation integrity, examples, acceptance checks and ongoing document governance.

---
