# 34. Canonical decisions register

This table is a convenient summary; the detailed sections remain normative.

| Question | Decision | Status | Canonical section |
|---|---|---|---|
| Is the game fundamentally a physical-data simulation? | Yes: an evolving, queryable world atlas drives every environmental/economic/military outcome | Committed | 3–6 |
| Does every map point have physical data? | Yes, queryable through multiresolution fields and derived values; not necessarily individually stored or fully solved per pixel | Committed | 3 |
| How do clouds and rain work? | Atmospheric wind transports moisture; cooling/lifting and condensation create coherent cloud and precipitation | Committed | 4, 5 |
| How are temperature, day/night and seasons determined? | One clock and geographic solar model plus atmospheric/land/water thermal response | Committed | 3, 5 |
| Are humidity, soil moisture and river levels separate? | Yes, physically distinct coupled stores/fields with causal exchange and time lags | Committed | 3, 5 |
| How do rivers/lakes change? | Connected watersheds, rain/snow/groundwater input, storage, discharge, inundation and evaporation | Committed | 3–5 |
| Does weather directly control farming, roads and battle? | Through shared physical consequences and crop/growth integration, not disconnected roll-based modifiers | Committed | 5, 6, 11, 20, 22 |
| Is local high-resolution physics a separate world? | No; deterministic constrained refinement shares physical coordinates and evolving parent state | Committed | 3, 22 |
| Is physics simulated full fidelity everywhere? | No; conservation-aware multiscale approximation with persistent state and meaningful time consistency | Committed | 3, 29 |
| What is Sovereigns? | Serious medieval grand strategy with shared-world real-time field battles | Committed | 1 |
| Historical Earth or procedural? | Procedural Earth-like geography and history, every campaign | Committed | 1, 4 |
| Are cultures hand-placed? | No; develop from generated history, migration and institutions | Committed | 4, 7 |
| Are characters the player's identity? | No; player commands continuing sovereign polity | Committed | 2 |
| Campaign mode? | Turn-based orders; chronological world resolution | Committed | 2, 5 |
| Duration of strategic turn? | Seven days | Default | 2 |
| Battles? | Real-time regimental command, manual or calibrated autoresolve | Committed | 22, 24 |
| Presentation medium? | Fully 2D orthographic | Committed | 27 |
| Final art style? | Specified by separate visual-direction document, not this bible | Companion document | 27 |
| Soldier sprites and animation? | Specific treatment belongs to separate visual-direction document | Companion document | 27 |
| Provinces as terrain tiles? | No; continuous physical geography with administrative overlays | Committed | 3 |
| One world or separate battle map? | Same geographic reality, deterministic tactical refinement | Committed | 3, 22 |
| Terrain changing? | Yes; persistent construction, clearing, battle and disaster effects | Committed | 3, 24 |
| Are physical laws simulated everywhere at equal detail? | No; multiscale physically inspired/consistent simulation | Committed | 3, 29 |
| Weather? | Spatially connected changing atmosphere and local ground effects | Committed | 5 |
| Agriculture? | Seasonal crop/soil/weather simulation; mostly autonomous farmers | Committed | 11 |
| Population? | Cohorts/households with real labor, wealth, migration, need | Committed | 8 |
| Resources? | Geographically located conserved inventories and productive inputs | Committed | 6, 12 |
| Economic control? | Crown priorities and intervention over autonomous households/markets | Committed | 10–16 |
| Land ownership? | Streamlined decentralized estates, nobles, towns and institutions | Committed | 9 |
| Trade? | Physical routes, capacity, shipments and local prices | Committed | 13 |
| Taxes? | Transfers from real economic actors, not gold generators | Committed | 14 |
| Construction? | Geographically constrained real projects needing materials/labor/time | Committed | 15 |
| Technology? | Gradual knowledge/adoption/diffusion; no magic instant unlock | Committed | 18 |
| Armies? | Large formations with actual manpower, equipment, food, morale | Committed | 19 |
| Battle target size? | Roughly 10k–40k total visible troops in major engagements | Target, tune | 19 |
| Army movement? | Physical routes, seasonal conditions, no province teleports | Committed | 20 |
| Can army movement capture territory? | Military threat or occupied sites may change through secured presence; movement alone does not create legal ownership | Committed | 20, 21 |
| Closing an army loop? | No automatic polygon conquest; control depends on garrisons, routes, settlements and local compliance | Committed | 21 |
| Territorial layers? | Separate claims, sovereignty, property/jurisdiction, military control, access rights and knowledge | Committed | 3, 9, 21 |
| Passage through allied land? | Only according to explicit transit rights; no annexation from passage | Committed | 17, 21 |
| Trespassing on neutral land? | Diplomatic violation with warnings and possible interception/escalation, never automatic conquest | Committed | 17, 21 |
| Encirclement? | Block actual usable supply routes/capacities; no geometry-only fill or instant starvation | Committed | 21, 23 |
| Occupation and annexation? | Maintain military control through real capacity; political ownership changes by negotiated or legal acts | Committed | 21, 24 |
| Are meetings always battles? | No; detection, initiative, avoidance and contact matter | Committed | 20 |
| Field combat approach? | Formation geometry, morale, fatigue, frontage and local physics | Committed | 22 |
| Wind/rain effects? | Calibrated causal physics and ground conditions, not blanket penalties | Committed | 5, 22 |
| Sieges? | Persistent physical fortifications, supply, blockade and assaults | Committed | 23 |
| Naval play? | Maritime logistics/warfare strategic; no dedicated real-time ship combat | Committed | 23 |
| Death of ruler? | Political consequence, not automatic game over | Committed | 2, 7 |
| Win condition? | Open sandbox with optional ambitions and legacy milestones | Committed | 2 |
| AI cheats? | No hidden omniscience or free resources | Committed | 25 |
| Player platform? | Desktop mouse/keyboard-first | Committed | 26 |
| Simulation architecture? | Separate authoritative model from rendering, shared states | Committed | 29 |
| Specific engine/language? | Godot 4 suggested for client, language details engineering-owned | Recommendation | 29 |
| Multiplayer? | Not a primary product requirement | Excluded from committed scope | 1 |
| Fantasy and magic? | No | Excluded | 1 |

---
