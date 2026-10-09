# C. Cross-phase integration rails (always active)

> Historical process reference. Gameplay invariants and integrated acceptance scenarios remain relevant. Current agent execution, context loading, evidence scope and verification cadence are defined in `docs/agents/`; do not apply the archived per-task ceremony to unrelated maintenance or every small edit.

The following are *ongoing requirements*, not permission to skip a phase. Every integrated build must continue to satisfy them.

## C1. Five mandatory living invariants

1. **Coordinates are shared:** a river, road, ridge, fort and local slope occupy the same place in strategic and tactical space; local refinement cannot contradict parent data.
2. **Time and physical state are shared:** weather, sun, moisture, water and ground state come from one chronological world, even when the renderer is closed.
3. **Resources are conserved:** goods and people move through real time and routes and cannot be duplicated by trading, construction, recruitment or battle aftermath.
4. **Political legality and physical power differ:** access rights, occupation, influence, sovereignty and annexation remain separate states at every scale.
5. **An explanation can be produced:** major state changes expose upstream causes, timestamps and meaningful player actions.

## C2. Mandatory vertical demonstrations

| Demonstration | Earliest checkpoint | Required chain and observables |
|---|---|---|
| Mountain storm | Phases 07–11 | Moisture advection → clouds/rain → infiltration/runoff → river discharge → flooded ford; show field queries and water budget. |
| Winter pass | Phases 09–13, 27 | Short daylight + snow/freeze → pass/soil state → route or formation speed/visibility; preserve exact location. |
| Battle identity | Phases 12–13, 31–37 | Campaign location/time → tactical terrain/weather → formation impact → damage/casualties → same campaign location. |
| Failed harvest | Phases 16–20, 23 | Weather/soil → crop loss → inventories → market price/access → welfare and policy options. |
| Timber depletion | Phases 10, 18, 24 | Forest biomass → harvest → timber shipment → building completion → changed forest cover/recovery. |
| Road cutoff | Phases 19–20, 24, 27–30 | Bridge destruction/flood → trade/army route interruption → shortage/control shift → repair or reopening. |
| Allied passage | Phases 25, 27, 29 | Treaty → lawful army crossing → consumption/permission effects → no occupation or annexation. |
| Mountain encirclement | Phases 28–30, 38 | Control all accessible passes → delayed shortage → garrison decision → relief attempt; no Paper.io polygon rule. |
| Fortress siege | Phases 24, 28–38 | Real hill fortress → supply stores → blockade and works → assault or surrender → persistent structural damage. |
| Self-sustaining civilization | Phases 15–23, 39–45 | Terrain → food/resources → population/trade → institutions → kingdoms/history → playable opening state. |
| Full kingdom campaign | Phases 23–54 | Choose seed and polity → govern → develop → march → battle → occupy/peace → next year → save/replay. |

## C3. Per-task completion packet for AI agents

Before taking any `SOV-PNN-TNN`, an agent records these fields in an issue or task handoff (not necessarily in the master checklist):

- **Task ID, owner, status, GDB version and canonical sections.**
- **Dependencies:** upstream authoritative producers, required prior phase gate and related tasks.
- **Behavioral contract:** exact player-visible behavior; no made-up physical/economic outcomes.
- **Data contract:** source of truth, coordinates, physical units, spatial scale, update frequency, persistence, cache invalidation.
- **Affected consumers:** UI, economy, AI, logistics, territorial control, tactical combat, chronicles and saves as relevant.
- **Acceptance evidence:** named seed, reproducible steps, automated test assertions, debug overlay/replay as appropriate.
- **Failure handling:** extreme values, chunk boundaries, time skips, missing data, stale intelligence, interrupted transactions.
- **Performance evidence:** relevant cost/quality comparison, or reason no benchmark is appropriate yet.
- **Human verification:** reviewer, decision, known caveats and updated documentation.

**Task is Verified only after** automated checks pass, the required integrated demo works, failures are explained, and no earlier invariant regresses. Temporary debug rendering is fine; fake physics or placeholder authoritative state is not.

## C4. Phase execution protocol: focus until it is good

1. Identify the **lowest incomplete phase whose prerequisites are verified**.
2. Within that phase, select the **lowest incomplete unblocked task** by stable ID, except when a documented dependency requires a different order.
3. Create task-specific acceptance criteria and capture a failing test or reproducible missing behavior first where practical.
4. Implement the smallest complete behavior, keeping data producer/consumer contracts explicit.
5. Validate positive, negative, boundary, save/reload, alternate seeds and time-advance cases relevant to the task.
6. Re-run the living reference fixtures; examine causal diagnostics and performance budget impact.
7. Record evidence and obtain review; only then mark `[x]` in this roadmap.
8. When every item is Verified, execute the **phase exit gate** as a separate check and record the evidence.
9. Tag the stable integrated reference build and proceed to the next phase. Reopen prior phase work if a regression is discovered.

For expensive subsystems, a phase can be split into subphases without removing any existing requirement. Never treat the phase's checkbox count as proof of quality.

## C5. Explicitly non-goals for initial production release

- Multiplayer, live-service requirements and persistent shared multiplayer worlds.
- Tactical naval battles or individual sailor/ship-combat simulation; sea/river transport remains strategic.
- Full three-dimensional world graphics, isometric camera commitment or a mandatory 3D animation pipeline.
- Fully conscious individual agents for every civilian or soldier; only aggregated demographic/economic and regiment-level decisions are required.
- Planet-wide full-resolution fluid dynamics, geological epochs calculated frame-by-frame or per-pixel authoritative physics objects.
- Mythical creatures, magic spells, supernatural resource chains and hand-authored fixed Earth geography as the primary map.
- A province-capture board game, arbitrary fixed weather penalties or instant loop-based territorial annexation.

These do not prohibit later exploration through an explicit GDB change proposal; they are excluded from this release's implementation path.

## C6. Companion document index

| Document | Authority | State |
|---|---|---|
| `SOVEREIGNS_GAME_DESIGN_BIBLE.md` | What the game does; physical/gameplay truth | Existing, authoritative |
| `SOVEREIGNS_DEVELOPMENT_ROADMAP.md` | What to deliver and in which verified order | This file, living |
| `SOVEREIGNS_VISUAL_DIRECTION.md` | Approved visual identity, sprite, environment, UI art and audio visual treatment | To author before Phase 46 art production |
| `docs/architecture/` or equivalent | Engine, formats, numeric methods and implementation rationale | Created progressively; agent-owned |
| `docs/testing/` or equivalent | Fixture seeds, benchmark results, causal goldens and exit evidence | Created progressively; agent-owned |
| `docs/agents/` or equivalent | Task handoffs and review evidence | Created progressively; agent-owned |

**Rule:** The roadmap does not override the Game Design Bible. If an implementation reveals a true contradiction, stop and propose a focused GDB clarification with a test; do not quietly change mechanics in code.

## C7. When the project is ready to call itself production-complete

Production complete means more than every checkbox showing checked. A player can create an unfamiliar but geographically credible world, inspect and govern a historical kingdom, alter its economy and military, occupy and blockade realistic territory, personally command large regiment battles on that actual evolving terrain, and witness coherent results through many seasons and generations. On supported hardware this remains responsive, stable and reproducible, with trustworthy saves, usable controls, onboarding, coherent presentation, robust AI and a shippable supported desktop build. The physical world—not a set of disconnected bonuses—continues to power every strategic and tactical consequence.
