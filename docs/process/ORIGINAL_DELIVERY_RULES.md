# A. How to execute this roadmap

> Historical process reference. Use `docs/agents/WORKFLOW.md` and `QUALITY_BAR.md` for current execution, delegation and proportional checks. Retain the gameplay invariants, fixture definitions and explicit task/phase acceptance requirements below; the historical per-edit packets and blanket regression instructions are superseded by the current working policy.

## A1. Order, priority and working state

The phase numbers are the **default dependency order**, not suggestions to implement subsystems in isolation. Complete each task and its local checks, then satisfy the phase exit gate before moving to the next phase. When later work reveals a genuine missing prerequisite, add it to the appropriate earlier phase with an ID and test; do not quietly invent a parallel solution. The final game scope is the Game Design Bible, not only the early integrated reference builds.

Tasks are checkboxes. A checked box means the behavior, automated tests, manual inspection where appropriate, documentation, save/load implications and cross-system effects are complete and reviewed. An implementation that merely looks correct is **not done**. Each task has a stable ID `SOV-PNN-TNN` so agents, issues and commits can refer to it. Never renumber existing tasks; append new IDs at the end of the relevant phase. Maintain **one active integration phase** and preferably one focused implementation task at a time; bounded adjacent work may be parallelized only with explicit integration contracts.

The six statuses for agent work items are **Not started → In progress → Ready for review → Verified → Blocked → Superseded**. Leave the master checkbox empty until **Verified**. Blockers require a reason, owner, prerequisite and next action. Superseded tasks remain in history with a link to the replacement; don't silently erase work.

**Before implementing a task:** Read relevant GDB sections, record upstream data dependencies and downstream consumers, define observable success, and identify tests and save/load implications. **After implementing:** run the existing regression suite, demonstrate expected behavior in a stable seed, confirm no duplicate authoritative data, record the verification evidence, and mark the checkbox only after review.

## A2. Non-negotiable game contracts

- **One source of truth:** no unrelated campaign/battle weather, river, terrain, water, road, resources, ownership or time. Local detail must refine authoritative data.
- **Physics informs gameplay:** wind affects actual projectile paths; moisture accumulates and changes soil, crops, roads and river levels; snow is a stored water reservoir, not just a sprite; river flow follows its watershed.
- **No per-pixel brute force:** every relevant coordinate is queryable; multiresolution fields, caching, bounded updates and aggregation are explicitly allowed and necessary.
- **No teleporting goods or people:** material flows, inventories, distance, ownership, transport capacity, consumption, losses and elapsed time have real consequences.
- **Separate sovereignty from force:** political ownership, access rights, military pressure, enforceable control, occupation and annexation are distinct.
- **No instant Paper.io conquest:** an army route or polygon does not transfer legal title; strategic nodes, supply, resistance, garrisons and institutions determine control.
- **Two views, one place:** tactical combat is a higher-detail view of the actual encounter location, with its current environment, not a newly rolled battle map.
- **Player rules a kingdom:** local economic activity is generally autonomous; the player sets policies, spends crown resources and commands armies. Decentralized medieval rights matter.
- **Visual style is undecided here:** only fully **2D orthographic** is canonical; final palette, sprites, animation, texture, camera framing and audio belong in the separate visual-direction document.
- **Stable causality:** a reproducible seed and ordered actions must produce traceable outcomes; deterministic replay is a development requirement within documented numerical tolerances.

## A3. Required evidence for every completed task

Each task's associated issue/agent handoff must record: `GDB references | authoritative inputs | outputs/consumers | update cadence | spatial level | player-facing behavior | AI impact | persistence | failure/edge cases | tests | performance evidence if relevant | reviewer`. Source-code organization and numerical algorithms are agent-owned, but the observable rules are not.

For each phase, retain a named **stable reference seed**, an automated validation fixture, a brief capture of the expected behavior (screenshot, debug overlay, metrics or replay as applicable), and a before/after regression record. Add a new fixture whenever an important physical, social or combat interaction appears.

## A4. Reference worlds and cumulative demonstrations

Maintain these test fixtures across the project:

| Fixture | Purpose |
|---|---|
| `F-VALLEY` | Mountain watershed, tributary, river, ford, bridge, road, wet valley, forest, hill, quarry, farms, market town, fortress, two states, enemy enclave and allied passage corridor. Main end-to-end demonstration. |
| `F-RAIN-SNOW` | Wet maritime mountain, rain shadow, falling precipitation phase, snowpack, frozen ground, thaw and delayed downstream flood. |
| `F-TRADE` | Farm basin, isolated mining town, coastal market, alternative roads/river shipping and weather-blocked link. |
| `F-BORDER` | Friendly transit, neutral access denied, hostile occupation, bypassed fortress, cut supply, relief route and treaty boundary. |
| `F-BATTLE` | Dry/wet/sloped/wooded/river crossings, wind, day/night and repeat battle at a permanently modified location. |
| `F-MANY-SEEDS` | Procedural-world batch across diverse geography, size and difficulty settings, with anomaly reports. |
| `F-LONG-RUN` | Autonomous multi-century simulation/extended campaign with conservation, stability, memory and save/load diagnostics. |

## A5. Phase exit rule

A phase is complete only when all its checkboxes are verified, its **Exit gate** passes, the reference game build still runs, known regressions are resolved, and relevant design/engineering documentation is updated. A remaining defect may be accepted only through an explicit, documented exception that does not violate a GDB invariant. Performance targets are measured and agreed based on actual representative hardware and benchmark scenes, never guessed into the roadmap.

## A6. Delivery checkpoints

- **Checkpoint A — A world that exists:** a seeded, inspectable, coherent geography with persistent spatial fields and connected waterways.
- **Checkpoint B — A world that behaves:** atmosphere, seasons, precipitation, soil, snow, rivers and ecology evolve together; an unloaded valley remains correct.
- **Checkpoint C — A place you can fight in:** the exact same valley opens in an orthographic battle view with matched terrain, conditions and a controlled formation.
- **Checkpoint D — A world with people:** settlements, societies, food, resources and generated history originate from geography.
- **Checkpoint E — A kingdom worth ruling:** markets, infrastructure, rights, taxation, government and diplomacy generate meaningful choices.
- **Checkpoint F — A war that belongs to the world:** armies, military passage, supply, control, encirclement, battles, sieges and aftermath all agree.
- **Checkpoint G — A complete playable campaign:** all AI, interaction, save systems, content, teaching, controls and victory/defeat flow work together.
- **Checkpoint H — Production-ready release:** representative hardware performance, extensive playtests, accessibility, reliability, packaging, legal/commercial readiness and release QA pass.


## A7. Phase navigator: the intended construction order

| Step | Phases | Build focus | What exists when you leave this step |
|---|---|---|---|
| 1 | 00–03 | Design authority, repository, deterministic clock and queryable atlas | A reproducible simulation shell, tested and inspectable |
| 2 | 04–11 | Geology, watersheds, sunlight, wind, rain, water, snow, soil and ecology | A living physical world with proven causal water and weather cycles |
| 3 | 12–14 | Tactical terrain continuity, initial formation experiment, world seed quality | A believable landscape you can enter and maneuver upon |
| 4 | 15–22 | Settlements, people, agriculture, resources, transport, markets, rights and generated history | A historically developed, populated, trading procedural world |
| 5 | 23–25 | Kingdom campaign turn, buildings, roads, diplomacy and access | First governable kingdom in an evolving world |
| 6 | 26–31 | Regiment recruitment, marching, supply, occupation, encirclement and encounters | An operational war that arises from real geography and political rights |
| 7 | 32–38 | Tactical formations, melee, arrows, morale, battle AI, aftermath and sieges | Full manual medieval battles with persistent consequences |
| 8 | 39–45 | Political depth, knowledge, strategic AI, campaign completeness, full controls and generated-world balance | A complete single-player grand-strategy campaign |
| 9 | 46–50 | Separate art direction, sound, onboarding, save resilience and extensibility | A coherent, teachable and maintainable product |
| 10 | 51–54 | Optimization, end-to-end QA, release candidate, launch and maintenance | A production-ready and supportable desktop release |

**The first implementation action** is `SOV-P00-T01`. Do not begin implementation by jumping to a map renderer or complete army AI. However, phase exits require a growing *integrated* build, so isolated subsystems cannot be considered perfected until their real consumers demonstrate the promised behavior.

**Scope rule:** The phase ordering is a recommended dependency schedule. If measured evidence requires a prerequisite to move earlier, update the roadmap transparently with unchanged task IDs and explanatory dependency changes; never invent gameplay rules absent from the bible.

---
