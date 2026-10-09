# 5. Active world physics: sun, seasons, atmosphere, weather and the water cycle

## 5.1 A single world clock and actual solar conditions

One authoritative calendar and clock drive all environmental and human systems. **Default calendar:** 365 days, twelve recognizable display months, a stable axial/seasonal model and realistic day/night cycle appropriate to the generated latitude. The sun's local direction, elevation above horizon and day length are calculated from position and time, with terrain horizon obstruction where relevant. Night is not a filter added to a daylight battle; it is the time when local sunlight is absent or weak.

Day length changes with season and latitude; winter generally has weaker solar energy and shorter days in the relevant hemisphere. Cloud opacity, landscape shading, surface reflectivity and ground cover change the solar heating reaching the ground. Different slopes receive different exposure through the day and season; shaded valley floors can remain cooler and wetter than nearby exposed slopes. The model should support sunrise/sunset, twilight, shadows' gameplay implications and night visibility without making a full physically based rendering engine mandatory.

## 5.2 What climate determines, and what weather determines

**Climate** is a geographically distributed long-run set of seasonal tendencies and variability, informed by latitude, ocean proximity, topography, thermal inertia and prevailing circulation. It may slowly change if major modeled environmental factors change, but does not reroll independently each turn. It influences vegetation ranges, plausible crops, snowpack expectations and player planning.

**Weather** is the current evolving state of air masses and their moisture/heat. It contains spatially connected wind, temperature, humidity, clouds, precipitation and visibility-related conditions. Weather fronts move. A storm may cover multiple valleys as it travels, weaken near mountains, intensify where moisture converges or pass into a rain-shadow region. Adjacent weather cells should not produce unrelated daily dice rolls simply because they have different province owners.

The player may consult both seasonal norms and observed current conditions; only the simulation knows hidden and future actual weather. A forecast is an uncertain prediction, not access to deterministic future weather.

## 5.3 Atmospheric state: pressure, air temperature and humidity

The authoritative regional atmosphere maintains enough state for consistent outcomes: heat/temperature, moisture content, pressure/circulation tendencies, wind, cloud and precipitation. Pressure differences and large-scale circulation help produce connected wind systems; seasonal thermal gradients, terrain and roughness modify them. Air temperature reflects solar forcing, ground/sea thermal inertia, elevation and air-mass transport. Temperature is not simply `base biome + random daily offset`.

**Air humidity is not soil moisture.** Actual water vapor can be transported with wind, while relative humidity changes with temperature and influences condensation potential. Moist air brought from sea, evaporated from wet terrain or transpired by plants can contribute to cloud and rain under suitable cooling and lifting. The atmosphere has a finite moisture budget in the implemented approximation. Evaporation cannot continuously supply limitless precipitation without an upstream water/energy source.

Do not attempt exact global meteorology as the product goal. Use a stable physically inspired atmosphere that reproduces the *relations* critical to gameplay: ocean-to-land moisture transport, coherent storms, air-mass heat exchange, prevailing wind patterns, terrain channeling, seasonal variability and rain shadows.

## 5.4 Wind fields at every scale

Wind is a **vector field** of direction, speed, exposure and, when relevant, gustiness—not a single number applied to the entire continent. Broad prevailing flow influences weather-system movement, while local terrain creates shelter, acceleration along passes, deflection in valleys and increased exposure on ridges. Forests and structures can lower near-ground wind locally. Wind near the ground and aloft may differ; ranged combat uses wind conditions appropriate to a projectile's path.

Wind can turn or vary gradually as a weather system passes. Wind updates are correlated in time and space, with plausible gust variation. A local battle may resolve fine wind exposure and turbulence from its landscape while staying consistent with the regional atmosphere. A cloud bank cannot drift against the governing wind without a specific atmospheric explanation. Ships, fire spread, smoke, evaporation and projectile flight can consume this same wind state.

## 5.5 Clouds are active moisture-bearing weather features

Clouds are not an image overlay selected independently of rain. Condensation develops where moist air cools, rises or enters conditions favorable to cloud formation; advection transports cloud-bearing air with prevailing winds. Orographic uplift, frontal lifting, convective heating and maritime air can generate different cloud and precipitation patterns at the level of abstraction supported by the world model.

Cloud cover shades the surface, reducing incoming sunlight and affecting temperature. Low clouds/fog affect visibility. Condensed cloud water can be returned as precipitation when the local process supports it. When moisture is depleted or air warms/drys, clouds can dissipate. Rendered cloud appearance is a visualization of this state; the appearance never generates its own physically disconnected rain.

## 5.6 Precipitation is an actual water input

Precipitation has position, duration, intensity, total amount, type and storm provenance. Whether it falls as rain, snow, mixed precipitation or freezes at the surface depends on thermal conditions and relevant approximations. Very intense rainfall may run off before infiltrating; gentle rain on permeable soils may recharge the soil more effectively. Snow can accumulate instead of instantly entering the river.

Windward slopes can receive increased precipitation as air rises and cools; downwind regions can become drier. River valleys can experience different rainfall than nearby uplands. Weather and landform fields together produce this spatial variability rather than assigning a random permanent wetness category to each region.

## 5.7 Atmospheric/land thermal cycle

The day/night heat cycle depends on incoming solar radiation, cloud cover, land material, wetness, ground cover and recent temperatures. Land generally warms and cools faster than deep water. Snow-covered ground tends to reflect more sunlight; vegetation and terrain shade can moderate surface conditions. Wind and humidity influence sensible/latent heat exchange, evaporation and perceived exposure.

The model distinguishes at least **air temperature, ground/surface thermal condition and growing-season heat accumulation**, because they answer different questions. An autumn valley can have cold air and unfrozen soil; a warm afternoon need not instantly melt a deep winter snowpack; a crop responds to accumulated conditions over weeks, not just one hot hour. Where justified, local frost pockets or cold-air drainage in valleys may be represented through a terrain-informed approximation.

## 5.8 Evaporation, transpiration and drying

Exposed water, wet soil and plants return moisture to the air at rates constrained by available water, energy, air humidity, wind and vegetative cover. Dry soil has less water to evaporate. Wetland and forest vegetation can contribute evapotranspiration; drought-stressed plants may reduce transpiration. Surface drying after rain therefore depends on weather, material and cover, not only the number of elapsed turns.

Evaporation reduces liquid water in its source reservoir and increases atmospheric moisture or its aggregated transport accounting. Transpiration draws from accessible soil/plant water. Rain, groundwater recharge and irrigation replenish these stores; exhaustion or prolonged cold limits these exchanges.

## 5.9 Soil infiltration, retention, saturation and surface runoff

Different soils store and transmit water differently. Sandy/porous ground drains rapidly, while dense clayey or compacted surfaces can support ponding and slower infiltration. Organic cover and vegetation influence infiltration and erosion; slope and surface roughness change runoff pathways. Water entering the soil fills a finite storage capacity, may percolate toward deeper stores or be taken up by roots.

A rainfall event first interacts with current conditions. A dry field can absorb some rain with limited surface flow; a saturated field may shed water quickly even under similar rain intensity. Roadbeds, trampled army camps and exposed slopes can drain and erode differently from undisturbed vegetation. Irrigation channels, drainage ditches and embankments can change local water paths and therefore must be persistent world features.

Surface wetness, puddles, mud, traction, wheel rutting and field workability follow from these stored conditions. **Mud is an outcome**, not a guaranteed rain penalty: exposed rock may remain firm in heavy rain; a compacted clay track may become impassable long after the sky clears.

## 5.10 Groundwater, springs and stored drought resilience

Groundwater is represented through one or more physically credible stores/proxies with recharge, depletion and discharge mechanisms. Rain/snow infiltration can recharge them gradually. Springs, wells, wetlands and river baseflow can draw on groundwater. Aquifers can therefore sustain flow or water supply during dry weather, but not indefinitely without replenishment. Terrain and geology constrain where water is accessible.

A deep groundwater model is not required everywhere. Regional aquifer nodes/fields with meaningful hydraulic connections are sufficient, provided water is conserved at the chosen abstraction level and does not appear anywhere the gameplay needs a well.

## 5.11 Watersheds and living river discharge

A river's geography is established during Genesis, but its **discharge changes continuously** as water enters, moves through, is stored by, or leaves the watershed. Precipitation, upstream inflow, snowmelt, groundwater discharge, local runoff and human withdrawals contribute. Channel size, slope, roughness and valley shape determine how much flow it can contain at a given water level. River stage, width, depth and current are dynamic consequences.

Not all streams flow permanently. Seasonal streams can dry, reappear or become dangerous torrents. A mountain stream may react quickly to a storm; a large river or lake may respond with substantial delay as upstream water travels downstream. Water from an upstream catchment must not reach its mouth instantaneously. Broad routing/hydrographs may be approximated, but their delays and stored volumes remain coherent.

Where flood stage exceeds bank capacity, water spreads onto compatible low-lying floodplain areas, fills depressions, infiltrates or returns to channels as conditions recede. Floodwater may damage roads, crops and structures, alter river-crossing feasibility, and leave wet ground after visible floodwater withdraws. Do not turn an entire political province into water merely because a river level threshold is exceeded; inundation follows local elevation and connectivity.

## 5.12 Lakes, inland basins, wetlands, estuaries and sea

Lakes receive direct precipitation, river inflow and groundwater exchange, and lose water through evaporation, outlet discharge, withdrawals and leakage where modeled. Lake level responds to this balance and topographic storage; high water may overflow through a defined outlet. Closed inland basins may shrink or expand seasonally without violating drainage logic. Wetlands are hydrologically connected saturated landscapes, not an arbitrary forest variant.

Sea level supplies a consistent global reference for coastlines. Coastal and estuarine behavior may include simplified tides, storm surges and river backwater effects where tactically relevant, but ocean circulation is not simulated at droplet level. Coastal maps, harbors, ports and estuary crossings must still be consistent with terrain and water state.

## 5.13 Snow, frost, ice and seasonal thaw

Precipitation accumulated as snow stores real water mass. Snow depth and water equivalent respond to snowfall, compaction, melt, sublimation and wind redistribution at the appropriate abstraction. Temperature, sunlight, exposure and snow cover determine how quickly it melts. Meltwater runs through soil and streams, potentially creating spring floods below apparently dry mountain ridges.

Ground freezing changes infiltration, traction and workability. Ice formation on lakes and rivers depends on sustained local thermal conditions and water behavior, with safe passage never inferred solely from one below-freezing night. A snow-covered pass may close gradually, reopen during thaw and then become muddy as melt saturates the ground.

## 5.14 Wind, rain, soil and day/night combine into local microclimate

Regional atmosphere is not sufficient to describe every battlefield or farm. At detailed locations, derive microclimate from elevation relative to surrounding land, slope aspect, terrain horizon, ridges/valleys, forest canopy, proximity to water, ground wetness, surface material and nearby structures.

Examples: exposed ridge winds can differ from sheltered forest-edge winds; a shaded north-facing slope can retain snow longer than a sunlit opposite slope; a valley bottom can accumulate cool air or fog; open wet fields may remain soft while a drained stone roadway is usable. This refinement must conserve consistency with regional weather and local energy/water inputs. It cannot spontaneously create tropical rain in a dry mountain basin without supporting moisture supply.

## 5.15 Physical movement properties of ground and water

At any location, the environment supplies **slope, material, roughness, bearing capacity, wetness, vegetation/obstacle cover, ice/snow, standing water and current/depth where applicable**. Movement resistance is derived together with the moving entity's weight, footwear/hooves/wheels, formation width, load, training and orders.

A wagon can become stuck in mud that infantry can cross; cavalry may lose charging cohesion in uneven boggy terrain; a rocky slope may be dry but unsuitable for wheeled artillery or siege machinery. A bridge's load and width limits are properties of an actual structure. A ford is usable only if river depth, current, banks, ground and unit type make crossing possible at that time.

These physical properties affect both operational routing and tactical movement. Coarse campaign movement may aggregate traversal costs, while tactical movement resolves them at finer scale. The two models must be calibrated so an army cannot conveniently cross impassable ground in one view but not the other.

## 5.16 Physics of ranged weapons and line of sight

A battlefield's geometry, sunlight, air and ground conditions are shared with all other systems. Projectile trajectories use launch direction/elevation, initial speed, gravity, flight time and plausible drag/wind influence. Wind matters through direction and exposure across the flight path: a crosswind can cause lateral drift, a headwind may alter range, and gusts increase uncertainty. Dense forest and walls physically obstruct paths; terrain ridges create blind zones; formation elevation changes sight lines and engagement geometry.

Weather impacts multiple distinct factors. Low light, fog and driving rain affect detection and aiming; muddy footing affects archer stability and infantry movement; strong winds affect trajectories; wet equipment may affect handling according to historically sensible equipment/material rules. **No universal arbitrary rule that rain makes all arrows ineffective.** The player should be able to understand the effective consequences and adapt tactics.

Projectile physics are accurate enough for believable tactical outcomes at game scale; individual air molecule simulation and full material deformation are excluded. The engine may use appropriate computational shortcuts that preserve directional drift, arc, collision, range and formation-level results.

## 5.17 Fire, smoke, dust and battlefield disturbances

Fire depends on fuel, current moisture, temperature and wind, and consumes vegetation or constructed material. It spreads under suitable conditions, creates smoke, alters visibility and may persist as burn damage. Wet forests are harder to ignite than dry brush; spreading flames follow local conditions rather than scripted radius expansion. Smoke and dust are transported by wind at a suitable approximation and affect sight and possibly cohesion/exposure. Trampling, digging, collapsed structures, felled trees and battlefield traffic can alter local access and surface condition.

These disturbances are **local state with persistence where strategically meaningful**. Smoke dissipates; damaged woodland or a destroyed bridge can remain. The simulation need not retain every footprint or ash particle forever, but it must preserve consequences that change later movement, production or battles.

## 5.18 Weather forecasts, observations and uncertainty

The physical simulation owns the true evolving atmosphere. Factions know only weather they can observe, reports they receive, seasonal expectations and fallible forecasts. A scout in a mountain pass may report heavy snow; the capital may receive it later. Maps can explain climate normals and recent weather without granting omniscience regarding tomorrow's exact storm.

Predictions become less reliable at longer horizons. UI and AI use the same permitted information boundaries. A planned winter invasion can estimate historical pass closure risk without secretly reading the actual weather seed. The player should see why a forecast changed when new observations become available.

## 5.19 Environmental disasters are state transitions, not independent event dice

Flooding emerges from rainfall/snowmelt, upstream flows, terrain storage and drainage. Drought arises from accumulated moisture deficit, atmospheric conditions and water reserves. Wildfire risk comes from dry biomass and ignition exposure; frost harms crops only when susceptible growth stages encounter relevant cold. Blizzards combine wind, snow and exposure; landslides require plausible unstable terrain and triggers. The engine may use stochastic ignition or hazard triggers, but severity and consequences must respect the actual physical state.

These events can affect food, migration, construction, transport, military readiness and political stability. Their causal chains should be visible in the atlas and chronicle. Avoid high-frequency disasters introduced purely for difficulty; plausibility and player response matter more than spectacle.

## 5.20 Climate, ecosystems and human feedback

Vegetation and land use affect roughness, albedo proxies, evapotranspiration, surface runoff, soil retention and local thermal/moisture behavior. Forest clearing and farming can influence local conditions over time. Irrigation withdrawals reduce water available downstream; reservoirs and drainage works alter stored water and flood response; large urban expansion changes land cover.

Feedback must be **scaled realistically**. Cutting ten trees does not instantly transform the climate of a kingdom. Clearing a watershed may plausibly change runoff and erosion without rewriting a continental prevailing-wind system. Forest recovery takes time and depends on remaining seed sources, grazing, soils and climate. Where a feedback is too weak or computationally expensive to model explicitly, record the chosen approximation; do not silently replace physical causality with fixed arbitrary buffs.

## 5.21 Simulation schedules and consistent event ordering

For investigation, regional atmosphere may step on hourly-or-coarser intervals appropriate to system scale; local rain and catchment response may integrate substeps during severe events; surface/soil/snow stores update often enough to capture important transitions; vegetation/crops accumulate daily conditions; climate summaries update from long-term histories; rivers and transport are re-evaluated when thresholds change; tactical projectiles and formation movement run on battle-appropriate substeps.

The engine may interpolate between stored atmosphere frames, but the **integrated precipitation and thermal history** must drive soil and crop changes, not just the most recent display frame. Crossing midnight or a strategic-turn boundary must not reset weather, cloud trajectories, river stores or snow depth. A major flood or environmental battle change generates an event that forces the relevant consumers to update coherently.

## 5.22 Canonical chain: from mountain weather to a battle

A prevailing moist wind reaches a cold mountain barrier. Air is lifted and cooled; moisture condenses and precipitation develops on an exposed flank. At high elevation the precipitation accumulates as snow, while lower slopes receive rain. Cloud cover lowers daytime solar heating. The rain infiltrates where ground capacity allows; excess becomes runoff, which moves through connected tributaries. River discharge rises after plausible travel delay. Some low-lying land floods; adjacent clay-rich soils remain saturated after the river recedes. An army traveling on a maintained raised road moves relatively well, while cavalry leaving the road loses cohesion and speed on the wet field. When the army fights, the local wind field changes the flight of long-range arrows and current cloud/fog conditions affect visibility. If the bridge is destroyed during battle, the world transport graph changes and a downstream supply route fails. Later, the damaged crossing and disturbed fields still exist.

**Every step is an interaction of the same stored or derived physical data.** There is no independent campaign river, battle river, economic precipitation roll, farm-weather bonus or projectile-weather setting. That causal chain is a defining acceptance case for Sovereigns.

---
