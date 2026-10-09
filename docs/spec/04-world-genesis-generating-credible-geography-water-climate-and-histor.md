# 4. World Genesis: generating credible geography, water, climate and history

## 4.1 What procedural generation must accomplish

Every new campaign begins in an unfamiliar **physically plausible Earth-like world**, not an assemblage of arbitrary terrain tiles. World generation produces an underlying physical planet/continent, whose soil, water, wind, climate, biomes, resources and accessibility can support plausible settlements and military geography. Generated societies then develop on that land through a historical simulation. Generation is seed-reproducible; diversity arises from different underlying geography and history, not simply randomized faction colors.

The generated world is allowed to be unfair, wild, isolated or environmentally difficult. Playability requires interpretable advantages and constraints, not equalized geology. The default playable geography is a large continent with surrounding seas, varied mountain belts, lakes, river basins, plains, coasts and multiple climatic regions. The game can generate subcontinental or larger setups under consistent physical principles. Importing real Earth topography is conceptually compatible with the physical-state interface but is not a core launch mode.

## 4.2 Stage A — Reference geometry, sea and solar setting

Establish world physical extent, latitude bands, cardinal directions, horizontal scale, sea level, coastline opportunity, climate baseline and solar/seasonal parameters before decorating terrain. A northern and southern region cannot share an identical day/night and seasonal signal by accident. Distance must mean real travel distance, not an arbitrary map step.

Define oceans, land area and approximate regional sea temperatures/circulation influence. The ocean is a heat and moisture reservoir with slower thermal changes than land. Do not require a computational ocean-circulation model in the initial authoritative design; parameterized regional sea temperatures, coastal moderation and prevailing moisture sources are acceptable if they yield coherent results.

## 4.3 Stage B — Geological architecture and landform skeleton

Construct broad geologic regions using plausible tectonic-like patterns: continental blocks, mountain belts, uplifts, basins, plateaus, coastal plains and volcanic/intrusive regions where appropriate. Noise can enrich structure but may not be the sole cause of disconnected peaks, arbitrary crater-like depressions or wildly oscillating valleys.

Rock types and parent materials influence erosion, soil development, slope stability, quarry resources and ore prospectivity. Geological resources should correlate with these provinces. A generator should produce ridges and massifs that look coherent across tens/hundreds of kilometres; mountain chains should contain sensible passes and associated catchments. Coastal plains, rift/depression systems and continental interiors need sufficiently different geography to create distinct strategic conditions.

## 4.4 Stage C — Ancient erosion, sediment and relief refinement

Apply physically inspired fluvial erosion and deposition during Genesis to convert rough geological relief into connected valley systems. Relatively hard rock resists erosion more than soft material; catchments collect flow; high-energy reaches incise; low-gradient depositional areas develop floodplains and alluvium. Terraces, alluvial fans and deltas may emerge from suitable geometry and long-run sediment routing.

The model need not simulate millions of literal years. Iterated landscape processes or constrained geomorphic generation are acceptable provided output exhibits believable ridge-valley networks and topographic continuity. A river does not cut a canyon through an unmotivated ridge because a random line desired a shortcut.

Local tactical refinement must later honor this geological character: an incised upland channel, a soft alluvial valley and a rocky ridge should create different microterrain. Battlefield microrelief may add physically plausible details but may not erase a major landform or reverse the drainage system.

## 4.5 Stage D — Drainage basins, channels, lakes and coastlines

From authoritative elevation and terrain depressions, establish watershed divides, downstream routing, catchment area and outlet types. Support water reaching open sea, retained water flowing through lakes, interior terminal basins, wetlands and seasonal closed basins. Resolve topographic sinks intentionally: some are legitimate lakes/endorheic basins; others are artifacts of terrain generation and must be connected or corrected rather than creating unexplained pooling.

Build a connected hierarchy of ephemeral streams, perennial streams and major rivers based on climate-water input and contributing watershed, not simply on visual width. Branches merge consistently; no river begins as an enormous blue line on an otherwise dry ridge. Meanders, braided reaches, deltas, channels through lakes and estuaries are governed by plausible valley slope, sediment and flow regime approximations. Aquifer-fed springs may support water even during dry periods.

Rivers are represented by persistent geographical centerlines, bank and bed morphology, water-surface connectivity and hydrological metadata. **Discharge, water level, active width and depth are dynamic** during play; the static drainage graph alone is not a river simulation. Narrow channels, floodplains and overflow/storage features are generated where water would realistically spread.

## 4.6 Stage E — Regional energy, atmosphere and moisture circulation

Establish seasonal solar forcing, prevailing regional winds and air-mass pathways over ocean, coast, plains and mountains. Represent likely pressure patterns and temperature gradients; account for oceanic thermal moderation, continentality, latitude, elevation and mountain barriers. Moist air transported over oceans can become clouds and precipitation where cooled or lifted. Windward slopes tend to receive more precipitation under appropriate conditions; leeward rain-shadow regions can remain dry.

Clouds, storms, temperature distributions and rainfall patterns originate from the **same atmosphere**, not independently randomized maps. Winter/summer contrasts should emerge from sun, geography, air masses and moisture transport. Validate wet coasts, dry interiors, alpine snow, monsoon-like seasonal patterns where world geography supports them, and dry downwind basins without requiring every map to include every climate.

The initial climate equilibrium is a **long-run state and variability distribution**, not a script guaranteeing that precisely the same rain falls on the same date each year. The runtime weather solver draws coherent evolving daily/hourly conditions consistent with these climatic tendencies and its current state.

## 4.7 Stage F — Soil, groundwater and cryosphere equilibrium

Derive soil parent material from rock and sediment, then develop plausible texture, depth, drainage, fertility and local water-holding capacity using topography, rainfall, vegetation and geomorphic history. Wet floodplains, shallow rocky slopes, sandy dry lowlands and deep loess-like agricultural zones must feel physically different.

Create groundwater/aquifer-storage proxies and recharge/discharge connections that can support springs, wells, wetland conditions and river baseflow. These are regional/hydrologic aggregates, not necessarily full subterranean fluid mechanics. High latitude and high elevation may develop winter snowpack, persistent snow patches, ice or perennially cold soil according to long-term energy/moisture conditions. Stored snow water must be released through thaw and contribute to river response.

## 4.8 Stage G — Ecosystems, land cover and ecological succession

Generate candidate plant/species pools and biome tendencies from climate, growing season, soil, water, elevation and barriers to dispersal. Allow ecological succession to establish forests, open grassland, steppe, wetland, scrub, desert and alpine vegetation based on actual constraints. Dense rainforest-like forests belong where climate and moisture support them; desert vegetation and bare surfaces belong where persistent dryness dominates. Vegetation coverage and dominant species are not randomly assigned visual biomes.

Seed wildlife and fisheries at aggregate ecological productivity levels. Let vegetation, biomass and soil adjust until the world reaches a plausible beginning state. Thereafter those quantities become slow-lived runtime state that can be cleared, burned, grazed, degraded and regenerated.

## 4.9 Stage H — Minerals, suitability and natural strategic locations

Geological origin sets mineral presence/quality, with additional constraints for depth, access, drainage, extraction technology and transport. Timber, crop potential, forage, fish, quarry rock, water power and navigable rivers derive from physical geography. Settlement and fortification site potential is calculated from defensibility, water, productive land, transport access, raw materials and political context.

A high hill overlooking a crossing is a *potential* fortress site; it is not guaranteed to receive a castle. River cities tend to be valuable when routes and productive hinterlands support them, but can also fail through wars, flooding or institutional accidents. Geography generates incentives, never an infallible optimal civilization layout.

## 4.10 Stage I — Peoples, settlement, trade and history

Initial human groups occupy regions where food, water and resource networks plausibly support life. Over compressed centuries, population grows and migrates, crops and technologies diffuse, settlements appear, roads follow trade and military incentives, ports develop, and cultures/religions/political institutions diverge and intermingle. Rulers build fortifications in response to threats; wars, conquest and surrender change effective control and recognized sovereignty through the same conceptual rules used in campaign play.

The generator must not infer a culture's character or inherent combat strength from biome. Geography constrains what is easy or costly; people, institutions, historical events and choices produce cultural and political variation. Centuries of history are simulated at coarse economic/demographic/environmental resolution; there is no need to replay millions of hours of detailed atmosphere or tactical battles. Historical narratives report **actual simulated state transitions** rather than fabricating backstories to explain arbitrary starting positions.

## 4.11 Stage J — Physical spin-up and playable starting snapshot

Before opening the campaign, initialize coherent current weather, soil water, snowpack, river flows, lake storage, ecological biomass, crops and food reserves consistent with the generated climate and historical land use. An otherwise rainy biome cannot start with every river dry simply because dynamic hydrology hasn't been initialized. A snowbound pass cannot start with summer-level soil temperatures because the atmospheric model has never run.

Resolve basic inconsistencies: ocean/coast alignment, drainage connectivity, water balances, snow persistence, impossible crops, food supply, settlement access, trade network continuity and fortress feasibility. Generate a campaign chronicle, physical atlas and kingdom briefs from the coherent starting world. The seed and version reproduce these outputs for matching settings.

## 4.12 World-quality audits and procedural diversity

Run automatic geography audits across many seeds. Check that rivers reach outlets or named inland sinks, lakes stay in basins, major watersheds do not jump divides, slopes/valleys remain coherent across chunks, wet windward regions occur when winds and terrain support them, leeward dryness and continental patterns are possible, mountains stay cooler on comparable air-mass conditions, crops respect growing seasons, and settlements have explainable resource/transport bases.

Measure gameplay diversity as well as appearance: number and distribution of defensible crossings, isolated valleys, navigable routes, food-surplus basins, industrial areas, climate barriers, fertile frontiers, culturally mixed regions and different strategic shapes. Different seeds must produce distinct military and economic problems. The generator may reject a truly broken seed or repair incoherent output; it must not secretly inject universally fertile land or mandatory equal-sized neighboring powers.

## 4.13 Generation reproducibility and future expansion

World generation must be reproducible from seed, settings and rule version. Any stochastic local detail uses location-stable procedural randomness so requesting the same valley twice yields the same unmodified landscape. Generator updates require explicit version compatibility or migration rather than changing an existing save's geography.

The interface between Genesis and runtime is **the authoritative physical-state model** from Section 3. A future imported Earth heightmap would still need physically compatible soil, hydrology, dynamic weather and ecological initial conditions. It would not receive unique battle or economy rules. The main procedural game stands on its own and is not gated on any Earth-data support.

---
