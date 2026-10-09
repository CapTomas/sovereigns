# 36. Fast answers to frequent design-agent questions

**Q: What is the true foundation of the game?**  
**A:** A single coupled, queryable physical world whose terrain, atmosphere, water, heat, seasons, soils and ecology generate data consumed by all strategic and tactical systems. See Sections 3–6 before building any subsystem.  

**Q: Does every pixel need all physical variables stored and updated every frame?**  
**A:** No. Every gameplay-relevant coordinate must produce consistent values through multi-resolution authoritative fields, derived queries and deterministic local refinement. Accuracy, continuity and causality matter, not brute-force per-pixel physics.  

**Q: What causes a rainy mountain valley and dry leeward plain?**  
**A:** Moisture-bearing winds, topography, air lifting/cooling, condensation, precipitation, atmospheric transport and the resulting rain shadow—not a biome label chosen in advance.  

**Q: Is precipitation the same as soil moisture or river level?**  
**A:** No. Rain enters land/water stores subject to infiltration, runoff and evaporation; rivers route upstream water with lag and storage; soil saturation governs local mud and root water.  

**Q: Are night, winter and local temperature only rendering changes?**  
**A:** No. Latitude/time/solar exposure and thermal conditions affect snow, evaporation, crop growth, visibility, ground freezing, water and military operations.  

**Q: If it rains during a battle, what changes?**  
**A:** Precipitation adds water according to the same environmental rules as on the campaign map. Consequences depend on soil, drainage, terrain, wind, temperature and time; archery uses actual wind/visibility and projectile flight; mud and river levels evolve through water stores rather than instant blanket modifiers.  

**Q: Do climate, weather and physics stop in unseen parts of the map?**  
**A:** No. Distant places use appropriately aggregated, time-consistent evolving state; watershed connections and long-run environmental history remain active.  

**Q: Can I just paint a forest biome tile to populate the world?**  
**A:** No. Use underlying climate, soils, water, species and ecological progression to determine tree cover. A forest may be depicted artistically, but must exist as gameplay-relevant vegetation state.

**Q: Can I use noise to invent the fine battlefield from scratch?**  
**A:** Noise can add detail, but rivers, ridges, roads, slopes and structures must remain anchored to the campaign's actual location and constraints.

**Q: Does 2D mean flat gameplay?**  
**A:** No. Elevation, slope, water depth, line of sight and projectile arcs are modeled physically. The presentation is fully 2D orthographic; the companion visual-direction document determines exact framing.

**Q: Are every tree and soldier fully simulated globally?**  
**A:** No. Trees are aggregates/derived instances; soldiers are mostly formation-level tactical state with efficient visual representation. Precision is spent where it changes gameplay.

**Q: Can I make it easier by giving each province a fixed food number?**  
**A:** No. Regions can aggregate field output, but crop/weather/soil/labor must generate output. Provinces are not the physics substrate.

**Q: Does player ownership of territory mean all its industries belong to the player?**  
**A:** No. Governance, lordly authority, private property, charters and institutional ownership remain distinct.

**Q: Who decides which farms plant wheat?**  
**A:** Households and estate managers react to physical suitability, knowledge, markets and needs. Player influences through rights, investment, mandates and incentives.

**Q: How does the player raise a unit?**  
**A:** Recruit available people through legitimate institutions, equip them with actual gear, train/muster them and assemble them physically into an army.

**Q: What if there are no iron weapons in a region?**  
**A:** Find local sources, trade/import, use available substitute equipment where credible, or change military composition. Do not materialize weapons for balance.

**Q: Can the player build on any hill?**  
**A:** Only where terrain, engineering, resources, access and land rights permit. Suitability and costs are shown before authorization.

**Q: Can a battle happen anywhere?**  
**A:** Wherever hostile operational forces can physically meet and fight. Practical battlefield extent and accessible terrain are derived from location and scenario.

**Q: Can a defending army set up anywhere on the tactical map?**  
**A:** No. Deployment is constrained by approach, actual time held, knowledge, obstacles and control.

**Q: Does all rain make archers inaccurate?**  
**A:** Not by universal magic penalty. Visibility, ground, bow/crossbow handling and wind are modeled separately; actual projectile performance is calibrated.

**Q: What changes when army morale breaks?**  
**A:** Ordered formations lose effectiveness, may retreat, rout or surrender; movement and pursuit occur physically; after-battle survivors and equipment are reconciled.

**Q: Do units heal/replenish automatically?**  
**A:** No. Healing, replacement recruits, equipment and supplies require real time and resources. Rest can improve fatigue and morale, not reverse fatalities.

**Q: Can I use separate economic and battle weather?**  
**A:** No. Local battlefield refinement must agree with the shared atmosphere and clock; fine local variation is permissible when derived consistently.

**Q: Can merchant goods teleport to a faraway construction site?**  
**A:** No. Goods must be owned, routed, transported and delivered before they can be consumed at the site.

**Q: What happens if I delete a bridge during tactical combat?**  
**A:** The persistent bridge structure and its transport links change, queued movement and shipments respond, and later maps show the damage until repaired.

**Q: Does a king dying end the campaign?**  
**A:** No, unless the sovereign state actually ceases to be playable under the political rules. Succession and claims matter but player identity is the polity.

**Q: Can AI opponents spawn reinforcements off-camera?**  
**A:** Only armies that physically exist and can arrive along feasible routes at the correct time. The battlefield edge is not a spawn resource.

**Q: Should the player see all information because the simulation knows it?**  
**A:** No. Faction intelligence, reliable reports and observation determine what can be shown or estimated.

**Q: Is there a scripted victory ending?**  
**A:** Open-ended sandbox by default; optional ambitions celebrate milestones. Defeat requires loss of genuine sovereign capacity.

**Q: Should agents create implementation pseudocode and interfaces in this file?**  
**A:** No. Keep this as a game design bible. Implementation-specific work belongs in linked engineering specifications and task files that comply with the design.

**Q: If I run an army around a loop, do I conquer what is inside?**  
**A:** No. Only held places, actual patrol/security reach and negotiated submissions change effective control; legal title changes through appropriate political settlement. Geometry by itself has no conquest effect.

**Q: Can my army pass through an ally on its way to a different enemy?**  
**A:** Yes when the alliance or a separate treaty explicitly grants the relevant transit rights. Passage alone neither annexes nor occupies the ally's territory. Supply purchases, basing and requisitions need their own rights.

**Q: What if I cross a neutral kingdom without permission?**  
**A:** The move is possible only if terrain and opposition allow it, and counts as a diplomatic breach. The neutral may demand withdrawal, stop the column or declare war. Mere crossing never makes the land yours.

**Q: How do I conquer undefended countryside?**  
**A:** Secure its settlements, crossings and roads with credible presence, obtain local submission or station supported patrols. Control is reversible if you do not maintain it, and is separate from legal sovereignty.

**Q: Does encircling a fortress starve it immediately?**  
**A:** No. Actual viable supply paths and their carrying capacities determine isolation; finite stocks, water, local production, smuggling and relief determine endurance. Attackers must also remain supplied.

**Q: Can a battle win an entire province by default?**  
**A:** No. It may give decisive military leverage, cause local surrender or open access to key sites, but each place and legal settlement follows coherent occupation rules.

**Q: Which document defines the art style and soldier look?**  
**A:** The separate `SOVEREIGNS_VISUAL_DIRECTION.md`. This bible fixes fully 2D orthographic presentation and gameplay information requirements, not color, texture, sprite or animation style.

**Q: What if an unmentioned detail must be decided immediately?**  
**A:** Apply the foundational principles: continuity, causality, player agency, autonomy, no free matter, physically credible approximation, clarity and feasibility. Choose the smallest coherent rule, document it and update affected tests.

**Q: Is this an excuse to implement only a small demo?**  
**A:** No. Integrated reference scenarios validate the complete design in slices; they don't redefine or shrink the intended game.

---
