# 32. Validation: design invariants and acceptance scenarios

Agents should use these as concrete outcomes to verify, not merely as aspirational slogans.

## 32.1 Global invariants

1. **One world:** campaign, economy, politics and battle resolve against authoritative shared geographic and chronological state.
2. **No free matter:** food, wood, metal, equipment, animals and transported goods are sourced, produced, moved, consumed, damaged or lost explicitly.
3. **No free people:** recruits come from populations, casualties persist, and workforce changes have economic consequences.
4. **No free information:** faction knowledge depends on observation, reporting and uncertainty.
5. **No free movement:** armies, shipments and reinforcements need a viable route, travel time and carrying capacity.
6. **No scene contradiction:** major terrain, infrastructure and persistent changes agree at every view scale.
7. **One time:** battle hours and campaign days share a clock; events resolve in correct chronology.
8. **No omnipotent crown:** rights, institutions, loyalty and logistics limit player authority.
9. **No arbitrary culture determinism:** cultural and military practices can change through history and learning.
10. **No gratuitous micromanagement:** player directs strategy; local routine economic activity operates autonomously.
11. **Explain causes:** meaningful losses, shortages, price changes, movement delays and tactical modifiers have inspectable causal sources.
12. **Consistent autoresolve:** automatic and manual battle models share situational premises and evaluate the same relevant capabilities.

## 32.2 World generation tests

- On generated map seeds, large rivers follow connected drainage and do not climb terrain or intersect impossible mountain divides.
- River widths/discharges broadly increase with accumulated watershed contributions, with sensible seasonal variation and local exceptions accounted for.
- Rain shadows, elevation temperature gradients, continental drylands and wet coastal areas occur where generated winds/geography justify them.
- Important ecological zones respond to climate/soil rather than decoratively painted random biome masks.
- Important mineral distributions relate to geological setting; agriculture relates to land suitability and knowledge.
- Settlements cluster around plausible economic/access advantages without making every river mouth identically urbanized.
- Fortifications and strategic roads correspond to plausible passes, crossings, frontier and market constraints.
- Multiple seeds generate substantially different military geographies, trade patterns and political shapes.
- Generated history and existing world entities agree on dates, locations and current state.
- Same seed/settings/version reproduce the same starting world.

## 32.3 Economy and population tests

- Decreasing available agricultural labor at planting/harvest can reduce output under controlled conditions.
- Shifting precipitation changes soil moisture and yield forecasts, with seasonal lag and plausible dependence.
- Low household income can cause underconsumption even when market-wide food stocks are adequate.
- A blocked trade edge delays/reduces supply and can change destination market price without creating or deleting cargo.
- A state project cannot advance past material/worker constraints even if treasury is full.
- Raising a tax increases government receipts only through actual collection and reduces another holder's wealth/goods.
- High tax burdens can produce specific local political grievances; collecting from more prosperous areas differs from starving areas.
- Recruiting troops reduces civilian labor and later demobilization returns only survivors.
- Migrating cohorts decrease source population and increase destination population with feasible transit.
- Market prices move with changing supply/demand without unstable daily oscillations under ordinary conditions.
- Voluntary private investment can grow a settlement under good conditions without the player placing each building.

## 32.4 Campaign operational tests

- A heavy army cannot take a path restricted to foot travel without changes to baggage or route.
- Snow, rain and collapsing bridges change route feasibility and ETA appropriately.
- Two armies do not automatically engage when separated by an impassable river despite being geographically nearby.
- Scouting an enemy improves available information and realistic interception choices.
- Withdrawing army suffers plausible pursuit costs rather than mandatory instant destruction or costless escape.
- Nearby reinforcements enter battle based on real arrival time and direction.
- A hungry army performs and moves differently through connected conditions, without a magic universal stat rewrite.
- Rival AI respects route, supply and intelligence limits.

## 32.5 Territorial control, access and encirclement tests

- An allied army with transit rights crosses a region and exits; host title, local administration, civilian inventories and control remain unchanged unless separately negotiated.
- An ally without passage permission cannot be represented as authorized by mere alliance; unauthorized crossing exposes diplomatic violation, interception and escalation.
- A neutral treaty grants a specified road corridor; off-corridor basing and requisition are not silently permitted by that right.
- An army passes through empty hostile countryside at high speed, leaving threat/damage but no automatic long-term occupation and no change of legal ownership.
- A small cavalry detachment completes a circle around an enemy town; the area inside stays unannexed, and the town's defenders retain their garrison and stores.
- A large force secures a real crossroads, posts supported patrols and gains credible control over reachable approaches; hostile strongholds beyond the patrol network remain outside control.
- A secured district loses its garrison and patrols; its military security changes over time according to local cooperation and hostile re-entry rather than remaining permanently player-colored.
- A fortified castle stays resistant within otherwise held countryside; it can interdict the occupier's supplies when its actual garrison, terrain and range support doing so.
- Blocking three mountain passes isolates a valley without drawing a closed geometric polygon.
- A viable river passage, port, ford or off-road path prevents an airtight blockade unless it too can be realistically interdicted.
- Closing supply access reduces actual deliveries rather than erasing stored provisions; garrison survival depends on finite food/water inventories, local output, rationing and relief.
- An encircling force with inadequate personnel or supplies loses blockade coverage and can be attacked in separated detachments.
- A captured bridge changes route availability, logistics and tactical terrain without transferring sovereignty over adjacent fields.
- A manual or automatic battle at a guarded crossing changes control according to actual survivors, positions, held sites and after-battle orders; it never gifts unrelated settlements.
- A peace treaty transferring a town changes the legal government and initiates enforceable administration/withdrawal actions; armies and inventories remain where they physically are until moved.
- Contested government and occupation do not duplicate rent, tax, grain, market stock, military personnel or local landholding.
- A player with insufficient reconnaissance cannot see enemy control strengths as if the control overlay were omniscient; the AI has the same restriction.
- Save/reload and inactive-chunk updates preserve garrisons, legal rights, permissions, territorial security, isolation and related timers.

## 32.6 Tactical tests

- A battle generated twice from unchanged state/seed has matching significant terrain/features.
- Campaign river, road, bridge, ridge, forest and castle are recognizable at corresponding battle coordinates.
- A combat formation compresses at a bridge and only physically exposed frontage participates effectively in contact.
- Wind direction can measurably alter comparable long-range projectile flights in different ways by firing orientation.
- Rain affects actual soil/wetness over time; dry rocky and wet clay ground behave differently.
- Formation facing and rear/flank attacks influence outcomes beyond simple unit strength.
- A regiment can't instantaneously reorient/reform at zero cost during severe contact.
- A hidden unit behind terrain remains unobserved until legitimate detection.
- Routed survivors, captives and casualties reconcile with post-battle totals.
- Walled assault uses persistent fortification geometry and damage state.
- Different weather on a return visit changes battle conditions without changing the mountain's position.

## 32.7 Persistence and chronology tests

- Destroying a bridge in battle changes later trade, army routes, project deliveries and subsequent battle scenes.
- Cutting trees changes both forest resource state and battlefield cover in that location.
- Combat losses flow to regiments and underlying population/equipment accounting without duplication.
- Resolving two adjacent battles in the same turn uses chronological, nonduplicated entity state.
- Saving/loading during active campaigns and battles reproduces structures, inventories, date, weather and orders accurately.
- Unloaded regions continue crop, population and market evolution at their valid aggregate schedule.
- A later campaign with the same seed is reproducible only if rules/inputs also match; past played choices legitimately create divergence.

## 32.8 UX tests

- A player can explain why a settlement suffers food shortage by inspecting no more than a few related screens.
- An impossible build or march order explains its missing right, route, input or condition before consumption of resources.
- Unit selection is reliable in dense formations; facing, morale and cohesion are recognizable at expected zoom levels.
- Important decisions interrupt auto-advance; unimportant fluctuations do not create notification floods.
- Map overlays and text convey terrain and hazards to players who cannot distinguish faction colors.
- The selected visuals remain 2D orthographic and communicate the same terrain, control, cover, weather and unit states as the simulation; detailed style compliance is validated against the separate visual-direction document.

## 32.9 Performance acceptance philosophy

No unmeasured invented hard hardware requirement is treated as a fact. The intended experience must support large-world simulation and visually huge tactical battles on ordinary contemporary desktop gaming hardware. Define budgets empirically for representative generated seeds, long-running campaigns, dense battles, active sieges, autosave and load. Track not only frame rate but turn-resolve latency, memory pressure, disk save size, render/physics costs and simulation consistency. Optimize without severing foundational causality. If a target is infeasible, propose a documented design trade-off instead of silently simplifying core features.

## 32.10 Physical-world simulation acceptance suite

These are mandatory reproducible design behaviors; test values and tolerances belong to balancing/engineering validation rather than being invented here.

1. **Every point query:** sample arbitrary map coordinates before and after refinement; elevation, soil, climate, water, wind and feature identity are coherent and yield explainable provenance.
2. **Scale agreement:** opening the same river valley on strategic/local/battle maps preserves channel direction, bank geometry, height relationships, structures and dynamic stage within defined resolution tolerances.
3. **Drainage audit:** trace generated catchments to sea, lake or explicit endorheic sink; no unexplained infinite water sources or isolated large river beginnings.
4. **Lake balance:** precipitation, evaporation, inflow, outflow and storage account for observed lake-level changes.
5. **Orographic climate:** appropriate prevailing moist flow produces coherent windward precipitation, leeward dryness and mountain-associated temperature/snow patterns.
6. **Wind/cloud coupling:** atmospheric cloud movement follows coherent advective wind and does not reroll direction per visual tile; rain responds to actual moisture/condensation state.
7. **Seasonal sun:** day length and solar elevation change with latitude and season; surface thermal conditions respond plausibly to night/cloud/shade.
8. **Soil-water memory:** identical rain over a dry permeable soil and a previously saturated compacted soil yields different infiltration, runoff and mud trajectories.
9. **Snow reservoir:** mountain winter snowfall accumulates and later melt creates delayed spring river flow; snow cannot disappear without storage accounting.
10. **Delayed watershed response:** an upstream storm affects downstream river stage after a plausible delay even while the upstream chunk is unloaded.
11. **Floodplain localization:** excess channel water covers connected low land according to terrain, not a uniformly flooded administrative region.
12. **Drought chain:** sustained moisture deficits lower crop water, forage and suitable stream flow, influencing food and logistics without arbitrary independent drought penalties.
13. **Forest ecology:** species cover and biomass follow long-term conditions; clearing/harvest persists, regrowth takes time, and tactical LOS tracks current vegetation.
14. **Geology/resources:** mineral deposits correlate with generated geology and do not teleport to satisfy local faction balance.
15. **Shared movement:** a wet road, ford or steep slope produces compatible feasibility on campaign, merchant transport and battlefield layers, respecting distinct vehicle/formation loads.
16. **Projectile wind:** flipping crosswind direction reverses expected lateral drift under otherwise matching conditions; shelter and exposure change local effect credibly.
17. **Night battle:** an engagement after sunset has matching campaign local time, solar state, light-dependent sight and ongoing thermal/weather conditions.
18. **No instant weather toggle:** a new rain event increases wetness and runoff through physical accumulation; not all surfaces become mud at once.
19. **Human world delta:** constructing and later destroying a bridge changes travel, trade, water/obstacles where appropriate, local battle terrain and saved world state.
20. **Save/load fidelity:** weather phase, river and lake storage, soil moisture, snow, vegetation, damage, seeds and timestamps reproduce coherent state on reload.
21. **Visual-independence:** leave a watershed unloaded for a simulated season and compare outputs to the equivalent active-run integration; strategic hydrology and crops agree within documented bounds.
22. **Conservation audit:** water/accounted sources/stores/sinks and tracked production/inventories stay within declared error tolerances across long simulations.
23. **Many-seed audit:** procedural geography repeatedly creates meaningful, connected basins and credible landforms without hidden manually placed fixes.
24. **Cause tracing:** from a failed crop, muddy charge or closed pass, diagnostics can identify the actual upstream physical fields and events.
25. **Battle-to-economy loop:** a flood-fed river changes an encounter, bridge damage persists, shipment rerouting changes markets, and no layer invents parallel conditions.

## 32.11 Minimum end-to-end physical causality demonstration

One integrated test region must demonstrate, without manually scripted result modifiers: **seasonal sun/temperature → wind/humidity/cloud formation → rainfall or snow → soil infiltration/runoff/snowmelt → watershed and changing river stage → local ground traction and road/ford viability → crop growth/harvest and trade → army movement, wind-affected archery and battle → lasting landscape and economic consequences**. Any missing link must be explicitly identified as unsupported, not disguised with decorative VFX or flat bonuses.

---
