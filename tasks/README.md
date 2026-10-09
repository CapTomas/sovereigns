# Task backlog

The task tracker is divided into 55 ordered phase documents; **phase files own checkbox status**. `meta/tasks.json` is a navigation index, not completion authority. A tracked task becomes **Verified** only after its required review accepts evidence against its acceptance criteria; a phase completes when its exit gate is satisfied. Do not treat a compiling scaffold or a successful document validator as gameplay completion.

For gameplay implementation, use [the agent workflow](../docs/agents/WORKFLOW.md) and [the proportional quality contract](../docs/agents/QUALITY_BAR.md). Route one relevant task with `python3 tools/task_context.py SOV-PNN-TNN --list`, then read its task/phase gate and affected authority. Phase 00 is the initial delivery phase; follow actual dependencies and resolve missing prerequisites rather than starting unrelated gameplay. Preserve existing IDs and checked history; append IDs only when creating real new tracked work. Statuses, the Definition of Done and evidence records (`tasks/evidence/PNN.md`) are defined in [the Definition of Done](../docs/agents/DEFINITION_OF_DONE.md).

Repository housekeeping, agent instructions, documentation navigation and tooling improvements can proceed directly under the user's objective without a gameplay task ID or completion of every earlier phase. Do not load all phase files or the [historical process archive](../docs/process/README.md) for a focused change. Current engineering workflow lives in `docs/agents/`; phase-specific acceptance requirements and gameplay invariants remain in force.

- [Phase 00 — Project authority, requirements and delivery discipline](phases/P00-project-authority-requirements-and-delivery-discipline.md) · 30 tasks
- [Phase 01 — Repository, toolchain, executable shell and continuous verification](phases/P01-repository-toolchain-executable-shell-and-continuous-verification.md) · 16 tasks
- [Phase 02 — Authoritative world identity, clock and deterministic simulation kernel](phases/P02-authoritative-world-identity-clock-and-deterministic-simulation-kern.md) · 17 tasks
- [Phase 03 — Queryable multiresolution physical-field atlas](phases/P03-queryable-multiresolution-physical-field-atlas.md) · 17 tasks
- [Phase 04 — Procedural geology, coastlines, mountains and elevation](phases/P04-procedural-geology-coastlines-mountains-and-elevation.md) · 17 tasks
- [Phase 05 — Drainage topology, rivers, lakes and base groundwater structure](phases/P05-drainage-topology-rivers-lakes-and-base-groundwater-structure.md) · 17 tasks
- [Phase 06 — Solar geometry, day/night, seasons and thermal foundation](phases/P06-solar-geometry-day-night-seasons-and-thermal-foundation.md) · 15 tasks
- [Phase 07 — Atmosphere, pressure, humidity, wind, clouds and precipitation](phases/P07-atmosphere-pressure-humidity-wind-clouds-and-precipitation.md) · 20 tasks
- [Phase 08 — Infiltration, groundwater, runoff and water-budget coupling](phases/P08-infiltration-groundwater-runoff-and-water-budget-coupling.md) · 20 tasks
- [Phase 09 — Live rivers, lakes, floods, snow, ice and seasonal thaw](phases/P09-live-rivers-lakes-floods-snow-ice-and-seasonal-thaw.md) · 21 tasks
- [Phase 10 — Soils, biomes, vegetation, ecology and changing ground](phases/P10-soils-biomes-vegetation-ecology-and-changing-ground.md) · 20 tasks
- [Phase 11 — Environmental laboratory, stable fixtures and causality proof](phases/P11-environmental-laboratory-stable-fixtures-and-causality-proof.md) · 20 tasks
- [Phase 12 — Tactical terrain refinement and two views of one location](phases/P12-tactical-terrain-refinement-and-two-views-of-one-location.md) · 19 tasks
- [Phase 13 — First tactical physics experiment: one commanded formation](phases/P13-first-tactical-physics-experiment-one-commanded-formation.md) · 19 tasks
- [Phase 14 — World generation complete pass, diversity and seed quality](phases/P14-world-generation-complete-pass-diversity-and-seed-quality.md) · 19 tasks
- [Phase 15 — Settlement geography, habitation and useful sites](phases/P15-settlement-geography-habitation-and-useful-sites.md) · 19 tasks
- [Phase 16 — Population cohorts, demographics, migration and households](phases/P16-population-cohorts-demographics-migration-and-households.md) · 19 tasks
- [Phase 17 — Fields, crop seasons, livestock and food storage](phases/P17-fields-crop-seasons-livestock-and-food-storage.md) · 19 tasks
- [Phase 18 — Natural resources, extraction and sustainable land use](phases/P18-natural-resources-extraction-and-sustainable-land-use.md) · 19 tasks
- [Phase 19 — Regional transport graph, shipments and infrastructure constraints](phases/P19-regional-transport-graph-shipments-and-infrastructure-constraints.md) · 19 tasks
- [Phase 20 — Local markets, autonomous trade and economic price feedback](phases/P20-local-markets-autonomous-trade-and-economic-price-feedback.md) · 19 tasks
- [Phase 21 — Rights, holdings, taxes and crown-versus-private wealth](phases/P21-rights-holdings-taxes-and-crown-versus-private-wealth.md) · 19 tasks
- [Phase 22 — Civilizations, cultures, religions and generated historical institutions](phases/P22-civilizations-cultures-religions-and-generated-historical-institutio.md) · 21 tasks
- [Phase 23 — Playable campaign shell, seven-day orders and sovereign player identity](phases/P23-playable-campaign-shell-seven-day-orders-and-sovereign-player-identi.md) · 19 tasks
- [Phase 24 — Construction, land conversion and engineering consequences](phases/P24-construction-land-conversion-and-engineering-consequences.md) · 20 tasks
- [Phase 25 — Diplomacy, access treaties, intelligence sources and state relations](phases/P25-diplomacy-access-treaties-intelligence-sources-and-state-relations.md) · 19 tasks
- [Phase 26 — Recruitment, regiment identity, equipment and army organizations](phases/P26-recruitment-regiment-identity-equipment-and-army-organizations.md) · 20 tasks
- [Phase 27 — Physical army marching, routes, posture and fatigue](phases/P27-physical-army-marching-routes-posture-and-fatigue.md) · 20 tasks
- [Phase 28 — Supply depots, army consumption, foraging and economic disruption](phases/P28-supply-depots-army-consumption-foraging-and-economic-disruption.md) · 20 tasks
- [Phase 29 — Military presence, influence, occupation and territorial control](phases/P29-military-presence-influence-occupation-and-territorial-control.md) · 20 tasks
- [Phase 30 — Encirclement, frontlines, blockades and relief operations](phases/P30-encirclement-frontlines-blockades-and-relief-operations.md) · 20 tasks
- [Phase 31 — Operational contact, battle invitations and tactical transition](phases/P31-operational-contact-battle-invitations-and-tactical-transition.md) · 19 tasks
- [Phase 32 — Battle command foundation and full regiment formation geometry](phases/P32-battle-command-foundation-and-full-regiment-formation-geometry.md) · 20 tasks
- [Phase 33 — Melee contact, damage and meaningful battlefield interaction](phases/P33-melee-contact-damage-and-meaningful-battlefield-interaction.md) · 19 tasks
- [Phase 34 — Ranged combat, visibility, wind and evolving battlefield weather](phases/P34-ranged-combat-visibility-wind-and-evolving-battlefield-weather.md) · 19 tasks
- [Phase 35 — Battle morale, fatigue, cohesion, routing and victory](phases/P35-battle-morale-fatigue-cohesion-routing-and-victory.md) · 19 tasks
- [Phase 36 — Tactical opponent AI, battle automation and reinforcement tactics](phases/P36-tactical-opponent-ai-battle-automation-and-reinforcement-tactics.md) · 20 tasks
- [Phase 37 — Battle aftermath, casualties, terrain persistence and autoresolve](phases/P37-battle-aftermath-casualties-terrain-persistence-and-autoresolve.md) · 20 tasks
- [Phase 38 — Fortresses, sieges, siege assaults and strategic maritime support](phases/P38-fortresses-sieges-siege-assaults-and-strategic-maritime-support.md) · 21 tasks
- [Phase 39 — Governance depth: dynasties, reforms, law, legitimacy and rebellion](phases/P39-governance-depth-dynasties-reforms-law-legitimacy-and-rebellion.md) · 19 tasks
- [Phase 40 — Knowledge diffusion, institutions and economic development over generations](phases/P40-knowledge-diffusion-institutions-and-economic-development-over-gener.md) · 18 tasks
- [Phase 41 — Full strategic AI: autonomous polities, governors and military planning](phases/P41-full-strategic-ai-autonomous-polities-governors-and-military-plannin.md) · 21 tasks
- [Phase 42 — Campaign completeness: objectives, losses, chronicles and strategic replayability](phases/P42-campaign-completeness-objectives-losses-chronicles-and-strategic-rep.md) · 19 tasks
- [Phase 43 — Production campaign UI, overlays, planning and information design](phases/P43-production-campaign-ui-overlays-planning-and-information-design.md) · 21 tasks
- [Phase 44 — Production tactical UX, battle control and readable combat feedback](phases/P44-production-tactical-ux-battle-control-and-readable-combat-feedback.md) · 19 tasks
- [Phase 45 — Procedural balance, long-run ecology and world diversity polishing](phases/P45-procedural-balance-long-run-ecology-and-world-diversity-polishing.md) · 19 tasks
- [Phase 46 — Visual-direction document, asset production and 2D rendering polish](phases/P46-visual-direction-document-asset-production-and-2d-rendering-polish.md) · 20 tasks
- [Phase 47 — Soundscape, music, localization source and atmosphere](phases/P47-soundscape-music-localization-source-and-atmosphere.md) · 16 tasks
- [Phase 48 — Tutorials, player assistance, accessibility and ease-of-use](phases/P48-tutorials-player-assistance-accessibility-and-ease-of-use.md) · 19 tasks
- [Phase 49 — Long-term saves, replays, migration and persistent world integrity](phases/P49-long-term-saves-replays-migration-and-persistent-world-integrity.md) · 20 tasks
- [Phase 50 — Modding, data authoring, diagnostics and agent-friendly content pipeline](phases/P50-modding-data-authoring-diagnostics-and-agent-friendly-content-pipeli.md) · 19 tasks
- [Phase 51 — Performance, memory, determinism and large-world scaling](phases/P51-performance-memory-determinism-and-large-world-scaling.md) · 22 tasks
- [Phase 52 — Full-game balance, AI robustness, reliability and QA campaigns](phases/P52-full-game-balance-ai-robustness-reliability-and-qa-campaigns.md) · 23 tasks
- [Phase 53 — Production build, distribution, legal review and release candidate](phases/P53-production-build-distribution-legal-review-and-release-candidate.md) · 21 tasks
- [Phase 54 — Launch readiness, release verification and post-launch stewardship](phases/P54-launch-readiness-release-verification-and-post-launch-stewardship.md) · 20 tasks
