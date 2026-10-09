# 3. Physical world foundation: authoritative spatial data and causal simulation

## 3.1 The non-negotiable foundation

**Sovereigns is a physical-world simulation first and a medieval strategy game built upon it.** All terrain, climate, weather, hydrology, ecology, land productivity, settlement location, road performance, army movement and tactical environmental conditions originate from an interconnected world-state model. Geography is not a static picture behind a separate rules engine. It is the common physical substrate on which those rules act.

The intended fantasy is to open a point anywhere in the generated world and discover a meaningful set of physical conditions: exact terrain height; nearby landform and surface material; likely climate; current temperature, humidity, wind and precipitation; sunlight and time of day; soil texture and wetness; surface and subsurface water; vegetation; accessibility; and changes caused by nature or human activity. Those conditions must be able to produce further consequences through the simulation. **Every gameplay-relevant world coordinate is queryable**, at an appropriate resolution, whether or not the player is looking at it.

This promise does **not** mean allocating hundreds of variables to every render pixel or running a planetary computational-fluid-dynamics solver at 60 frames per second. A location resolves its world data from multiple authoritative layers and derived queries: stored grids, geological and hydrological features, sparse objects, historical changes, deterministic high-resolution refinements and current dynamic fields. Data can be queried at any location even when it is not materialized as an individually stored cell. Physical coherence, continuity and meaningful consequences are mandatory; wasteful per-pixel simulation is not.

**The universal chain:** geology and landforms constrain water, radiation and atmospheric flow; the atmosphere and seasons drive temperature, precipitation and snow; topography and material properties route water into rivers, lakes, soils and groundwater; energy and water balances determine ground conditions; long-term environmental conditions constrain vegetation, wildlife and food production; those affect population, markets, infrastructure and civilization; armies move and fight within that same state; battles and construction change it in return.

No subsystem is permitted to invent a parallel environment to get a desired gameplay result. No province receives a free-floating *fertile*, *muddy*, *cold*, *forested*, *river defense*, or *high-ground advantage* statistic divorced from physical conditions. User-facing labels are projections of the underlying state, not independent sources of truth.

## 3.2 The world is a spatial data atlas, not a terrain-type board

Each world position has one canonical geographic coordinate, horizontal location and elevation reference. The generated world defines its latitudinal placement, north direction, scale, geodesic/geographic simplifications and time/calendar orientation. Horizontal distance, height, area, flow, velocity, temperature, moisture and quantity have well-defined units. Political borders are later overlays; they never define the resolution or extent of physics.

World state is organized into **field families** rather than an enormous undifferentiated cell object. Different families have different spatial and temporal resolutions. The following catalog is normative: agents may choose compact representations but cannot omit the underlying behavior.

| World data family | Authoritative or evolving values | Consequences and users |
|---|---|---|
| Geographic reference | World seed, physical scale, coordinates, latitude, sea-level datum, local time/season | Sun/day length, regional climate, distance, navigation, save identity |
| Bedrock and landform | Height above datum, lithology/parent material, erodibility, terrain structures, sediment stores | Slope, ridges, passes, minerals, valleys, stability, construction |
| Terrain derivatives | Slope, aspect, curvature, horizon obstruction, aspect exposure, elevation change, drainage direction | Roads, visibility, agriculture, building feasibility, tactical movement |
| Surface material | Soil/rock/sand/clay/gravel/peat, depth, roughness, bearing strength, erosion susceptibility | Mud, wheeled traffic, vegetation, excavation, infantry/cavalry traction |
| Atmospheric dynamics | Pressure anomaly, air mass, temperature, humidity, wind vector, turbulence/exposure | Cloud transport, evaporation, rain, fog, missiles, travel conditions |
| Atmospheric water | Water-vapor content, relative humidity, cloud condensate/cover, precipitation type/rate | Snow/rain, visibility, insolation, hydrological recharge |
| Radiative/thermal state | Sun angle, day length, incoming radiation, cloud shade, surface/air temperature, accumulated heat | Day/night, frost, crop development, heat/cold exposure, snowmelt |
| Surface water and catchment | Downhill routing, flow accumulation, watersheds, channels, discharge, water level, width/depth/current | Rivers, lakes, fords, floods, ships, irrigation, tactical crossing |
| Subsurface/soil water | Soil liquid water, saturation, drainage, infiltration, aquifer/groundwater proxy, springs | Water availability, runoff, plants, wells, mud, drought resilience |
| Snow and frozen state | Snow water equivalent, snow depth, melt, surface frost, soil freeze, river/lake ice | Pass closures, traction, river flows, logistics, field operations |
| Climate reference | Seasonal distributions and long-run means/variability, prevailing circulation and wet/dry patterns | Biomes, crop suitability, settlement expectations, forecast uncertainty |
| Ecosystems | Biome tendency, plant species pools, tree/grass cover, biomass, regrowth, ecological stress | Timber, forage, wildlife, concealment, habitat, erosion |
| Soil productivity | Soil fertility/nutrients, organic content, salinity where relevant, moisture history, damage | Harvest, forest growth, land value, desertification risk |
| Geological resources | Deposit location, geology, depth, quantity/grade/accessibility | Mines, metallurgy, settlement incentives, transport |
| Human landscape | Land parcels, land use, crops, roads, bridges, drainage works, buildings, fortifications, pollution/damage | Economy, water balance, movement, sieges, occupation |
| Transient local conditions | Standing water, puddling, mud depth/firmness, smoke/dust, fire, tracks, fallen obstacles | Battlefield and campaign movement, LOS, risk, aftermath |

Additional military, economic and political data are **not** a replacement for physical state. They consume these field families; human works and events can, in turn, mutate relevant fields.

## 3.3 Stored, computed, cached and perceived values

Every property must be clearly classified:

1. **Persistent source field:** Earth-like physical structure or slowly changing world state, such as elevation, bedrock, river geometry, a bridge or woodland biomass.
2. **Dynamic field:** State evolved over time by simulation, such as air temperature, vapor, precipitation, snowpack, soil water, river discharge and local flood state.
3. **Derived value:** Calculated from authoritative inputs, such as slope, crop suitability, ground bearing capacity, effective visibility or army movement cost. Derived values are never silently saved as competing independent truth.
4. **Cached derived value:** A performance optimization with declared dependencies and invalidation triggers. It must refresh or be correctly recomputed after changes to sources.
5. **Faction observation:** What a kingdom knows or estimates. Observed values may be old, incomplete or uncertain; this must never rewrite world truth.
6. **Visual representation:** 2D presentation of selected data; colors, decorative particles and graphical labels cannot modify physics by themselves.

For instance, *muddy ground* is not a hand-painted category. It is a condition derived from surface material, soil-water saturation, drainage, slope, compaction, vegetation, temperature, rainfall history and traffic. Its exact tactical resistance also depends on the type and load of moving unit.

Every important derived condition should be explainable as a short **cause chain**: `condition → contributing fields → originating events`. Players see a readable version; debugging and balancing tools expose deeper provenance.

## 3.4 Spatial resolution: huge world, precise local truth

The world is continuous in gameplay terms, represented with chunked multiresolution data. A single fixed grid for everything is explicitly rejected. **Reference resolutions for investigation, not guarantees:** planetary/continental climate fields operate coarsely; landscape elevation and hydrology may use hundreds-of-metres cells across large regions; strategic feature networks retain exact paths and junctions; regional terrain/land use may refine to tens of metres near meaningful sites; battles may refine passability and height to metres or finer where needed. Atmosphere, topography, water and vegetation need not share a cell size.

Important linear and topological features—coastlines, channels, ridge crests, mountain passes, roads, bridges, walls and parcel boundaries—have persistent geometry. Coarse averaging cannot accidentally erase a river, reverse a slope, move a bridge or straighten a defensible ridgeline. Local detail generation is constrained by parent geometry, upstream/downstream connectivity, landform type, geology, erosion character and persistent human changes.

**Every point queries the same hierarchy.** A place not presently loaded still has stable generated properties and correctly advanced slow-state summaries. On zoom-in, finer sampling reveals additional detail, not a different world. On zoom-out, maps aggregate the true fine-scale phenomena rather than substituting arbitrary province values.

The battle view is **not a separate random-map generator**. It is a higher-resolution local physical simulation attached to the shared world coordinates and exact campaign time. All military environmental effects consume the same field identities and update lineage. Battlefield and campaign are two representations of one physical place.

## 3.5 Units, coordinates and environmental meaning

Treat units and coordinate conventions as global design contracts so separate agents cannot accidentally make contradictory systems. Height is measured relative to a stable sea datum; slopes are calculated from actual world distances, not from the color of a heightmap; water quantities and flows are physically dimensioned or explicitly normalized with consistent conversion; atmospheric temperatures are in a consistent internal scale and shown to players in their display preference; wind is a direction-and-speed vector at defined reference height/exposure; humidity must distinguish relative humidity from actual vapor content; precipitation is an amount delivered over time, not a cloud icon; snow has both depth and stored water content; soil moisture is a stored water condition, not synonymous with relative air humidity.

*Climate* describes long-run local statistical conditions, *weather* describes evolving realized conditions, *biome* describes ecological patterns, *ground condition* describes immediate surface state, and *weather forecast* is a faction's uncertain inference. These terms must not be used interchangeably in data, UI, AI or tests.

Design agents may choose simplified units internally when justified, but conversion, reference conventions, interpolation and numerical ranges must be documented in implementation specifications. Zero wind, no precipitation, dry soil and freezing temperature must have explicit unambiguous meanings.

## 3.6 Coupling is the central gameplay contract

Each major physical subsystem is simultaneously **a consumer of upstream inputs and a producer of downstream state**. It must not simply set visual decorations. The minimum causal dependency graph is:

| Physical change | Immediate physical response | Slower physical response | Strategic/tactical outcome |
|---|---|---|---|
| Mountain ridge | Forces terrain slope, air lifting and drainage divide | Rain shadow, seasonal snow, distinct ecosystems | Passes, routes, defensive elevations, farms |
| Warm moist wind | Moisture transport; cloud formation under appropriate lifting/cooling | Rain or snow, groundwater and river response | Travel disruption, visibility, crop water, archery drift |
| Winter solar cycle | Shorter days, lower solar heating | Cold soil, snow accumulation, ice, reduced growth | Seasonal logistics, food stores, daylight combat |
| Multi-day rainfall | Saturation, runoff, cloud cover and reduced radiation | Raised rivers, floods, erosion, altered habitat | Dangerous fords, damaged bridges, mud, trade bottlenecks |
| Drought | Water deficit and declining discharge | Crop/forest stress, lower yields, dry fuels | Food shortage, migration, fire exposure, political tension |
| Deforestation | Biomass loss and lower canopy interception | Changed soil cover/runoff/erosion; slow regrowth | Timber supply, land clearance, battlefield cover |
| New road or bridge | Surface/route geometry and flow access change | Trade, settlement incentives, environmental wear | Army routes, supply capacity, strategic chokepoints |
| Battle or siege | Soldiers/objects physically move; smoke, fire, surface damage | Damaged structures, land recovery, depletion of stores | Control, rebuilding, disease and livelihood impacts |

The arrows are causal, not merely correlated art effects. A biome should never be chosen first and then forced to manufacture suitable rainfall retroactively. Conversely, do not pretend feedback flows only downward: vegetation changes surface roughness, soil and moisture behavior; water and human activity erode or alter landforms; settlements modify land use and drainage. The simulation must support these **feedback loops** at believable, numerically stable rates.

## 3.7 A shared simulation clock with independent update cadences

The game has one authoritative physical time. Different fields may update on different clocks: rapidly varying local combat and wind at short substeps, region-scale atmosphere on suitable intervals, hydrology and ground conditions on hourly/daily intervals as needed, crops on daily intervals, ecological cover seasonally or yearly, and geological baseline almost never during gameplay. The strategic turn remains seven days by default, but is a decision and scheduling envelope, **not a seven-day physical time jump**.

All update dependencies have a consistent order and timestamps. Wind/radiation/air mass state determine precipitation and heat flux; precipitation and evaporation update snow/soil/water stores; ground and channel states feed movement, crops and navigation; consumers read a coherent time-slice rather than mixing tomorrow's rain with yesterday's discharge. Substeps may be staggered, but state transitions must preserve causal chronology.

An ordinary campaign week can simulate its weather and hydrology at cheaper, larger intervals while maintaining accumulated effects. A local live battle can refine terrain and winds without allowing distant regions to jump ahead in world time. The event scheduler must reconcile them into one chronology.

## 3.8 Mass, water, heat and numerical credibility

The system is **physically inspired and conservation-aware**, not a full scientific Earth-system research solver. It still needs strict accounting where quantities matter:

- Water arriving as rain or snow must go into interception, surface/soil storage, runoff, river/lake stores, subsurface recharge or subsequent evaporation/transport. River water is not created from an unexplained flow animation.
- Groundwater, snow and lakes may release previously stored water over time. Dry periods lower their contributions unless upstream conditions support continued flow.
- Sediment and vegetation biomass do not materialize without processes. Roads or castles cannot be built from zero transported material.
- Solar radiation, land/sea heat capacity, cloud influence, air-mass transport and atmospheric exchange govern **plausible** temperature changes. Temperature is not required to obey a globally exact numerical energy solver, but it must not jump wildly without an event.
- Local advection and coarse-grid corrections need bounded, conservative treatment as appropriate; no persistent unexplained increase of water, energy proxy or goods due to grid refinement or chunk boundaries.
- Random variability affects plausible processes and parameters, never violates physical constraints or overwrites authoritative state.

Engineering may use simplified model physics and mass-balancing correction steps, but if a shortcut makes lakes fill forever, creates rivers on ridges or makes rainfall independent of atmospheric moisture supply, it violates design.

## 3.9 Topology matters more than cosmetic precision

There are hard world invariants: rivers follow connected drainage toward an outlet or inland terminal basin; lakes occupy topographically credible depressions with coherent inflows and outflows; uphill channels require a specific engineered cause or exceptional local hydraulic condition, not noise; a mountain barrier cannot be erased by camera zoom; roads and bridges connect actual reachable networks; sea-level coastlines obey the terrain; caves and groundwater shortcuts cannot implicitly make arbitrary large streams disappear. Some real-world complexities may be abstracted, but the visible system must remain consistent.

Natural rivers are allowed to meander, braid or form deltas where valley shape and flow conditions support them. A river need not follow the steepest adjacent tactical cell at every bend if its larger channel path and local hydraulic slope remain physically credible. Endorheic lakes and deserts are legitimate outcomes. Wetlands, estuaries, floodplains and seasonal streams belong to the same drainage system rather than being independent static paint.

## 3.10 The live physical-world query contract

At any gameplay-relevant coordinate and time, agents should be able conceptually to obtain a coherent **environmental snapshot** that answers:

- *Where am I?* Position, height, local relief, slope, aspect, exposure, material and landform.
- *What is the atmosphere doing?* Air temperature, humidity, wind vector, cloud/precipitation, visibility-relevant conditions and thermal trend.
- *Where is the water?* River/lake/sea membership, depth or level where applicable, soil wetness, groundwater access, surface flooding, snow/ice and runoff connections.
- *What grows or survives here?* Long-term climate, soil, species/vegetation, biomass, crop/pasture potential and ecological stress.
- *What has humanity changed?* Fields, deforestation, drains, roads, walls, bridges, irrigation, buildings and damage.
- *What happens if I act?* Construction suitability, route and traffic feasibility, crop response, projectile/weather exposure, unit movement conditions and potential environmental impacts.

Each requested attribute identifies whether it is exact stored state, derived state, estimated state, or unavailable by deliberate abstraction. Different subsystem queries at the **same place/time** must not disagree about wind direction, water depth, local height, snowfall or whether a bridge exists.

## 3.11 Persistence and the changing landscape

Save geological baseline, authoritative field state at a consistent simulation timestamp, long-lived ecological/soil/groundwater state, spatial feature definitions, human works, affected high-resolution patches and event-delta history necessary to reconstruct the current world. The unchanged detail of a distant hillside can be deterministically regenerated; its logged forest clearing, newly built road, erosion scar or destroyed bridge cannot be forgotten.

If the landscape changes under an active or dormant region, dependent routes, cached slopes, line-of-sight structures, crop zones, hydrological connections and AI travel plans must be revalidated according to declared dependencies. Do not require recalculating the entire planet when one bridge collapses; do not allow stale regional caches to contradict that collapse.

Determinism is defined by world seed, settings, generator/simulation rules version, recorded inputs, event order and saved authoritative state. Existing saves must not silently change coastlines, rainfall or bridge positions after a procedural algorithm update.

## 3.12 Map generation versus continuous runtime physics

Separate three phases: **(1) Genesis constructs plausible initial structure**, **(2) a spin-up or prehistory period establishes coherent climate/water/ecology and civilizations**, and **(3) live campaign simulation advances changing state**. Tectonics and ancient erosion are largely Genesis operations. Daily wind, rain, temperature, snow, runoff, river height and soil condition are runtime systems. Forest succession, human land use, soil degradation, river adjustments and infrastructure evolve slowly while playing.

Do not rerun geological formation every turn. Do not freeze rivers and weather just because their geography was generated procedurally. Generation and simulation are different stages of the same world model, not separate contradictory games.

## 3.13 Physical detail on demand, not physical rules on demand

Use level of detail to save performance, but avoid making environmental reality dependent on the camera. Whether watched or not, every region must maintain a **time-consistent physical state or conservative summary** able to reproduce its important accumulated effects. Distant rain changes watershed storage and downstream flows. Distant drought changes agriculture. A river upstream of the current battle cannot be frozen in time while the battle downstream floods.

Close to battles, settlements and construction, locally refine geometry, vegetation, currents, ground conditions, smoke and wind exposure. Aggregating distant state is acceptable when mass/flow and strategically relevant results remain coherent. Full-resolution simulated droplets, air vortices, individual plant cells and grains of mud across the world are excluded.

**Invariant:** simulation quality may vary by resolution and activity; fundamental laws and accumulated consequences may not vary by whether the player can see a location.

## 3.14 Design criteria and failure modes

A valid world can answer why a river occupies this valley, why the leeward slope is dry, why this farm grows rye instead of rice, why the bridge floods after rain, why snow remains on a high pass, why an archer's projectile drifted and why cavalry bogged down near the riverbank. Explanations should refer to shared, inspectable input data and processes.

Invalid implementations include: disconnected random river splines; independent daily weather dice for every province; rain clouds that ignore wind and moisture; elevation that influences only visual shading; globally identical day length; snow that does not store water and melt into streams; sea level that moves arbitrarily between terrain resolutions; tactical fog chosen independently of weather; hard-coded road speed unrelated to slope/material; arrows receiving unexplained fixed wet-weather misses; infinite crop yield without rainfall/irrigation; armies traversing mud that physically exists only in the battle renderer; forests painted as background sprites without canopy/biomass state.

**Above all: the simulation is not a checklist of visual effects. It is a coherent producer of facts and consequences for every other game system.**

---
