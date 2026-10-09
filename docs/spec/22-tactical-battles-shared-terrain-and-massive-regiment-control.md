# 22. Tactical battles: shared terrain and massive regiment control

## 22.1 Battlefield identity

The battle is **a temporary high-detail simulation of an actual world location**, not a pre-authored arena. This is a core promise. The geometry preserves real river direction, banks, elevation, ridges, existing roads, bridges, walls, forests, fields, settlements and recent environmental conditions. Added fine detail must respect the larger hydrology and terrain.

**Default battlefield extent:** roughly 3–6 km across for major field engagements, chosen to accommodate army size, approach and natural geography. Smaller clashes can use smaller extents. The battlefield may have irregular accessible boundaries shaped by terrain and scenario, but invisible hard borders should be disclosed with a believable operational explanation (for example, tactical command zone rather than the world ending).

## 22.2 Starting a battle

The campaign supplies timestamp, coordinate, geographic feature identity, environmental state, involved armies/regiments, approach route, fatigue, morale, equipment, available ammunition, carried supplies, preparation/entrenchment history, scouting/intelligence, nearby reinforcements and control of structures. Tactical detail is refined deterministically; units are placed based on real approaches rather than identical north/south default spawns.

All important generated tactical features that can affect future operations are tied to persistent world positions. No arbitrary ford, hill or castle may appear only in the battle scene while contradicting campaign geography.

## 22.3 Pre-battle phase

Allow terrain inspection and deployment within spaces legitimately controlled or reached by the force. Display visible obstacles, known enemy contact, predicted weather uncertainty and access points. Defenders who had operational preparation time can have trenches, stakes, barricades or earthworks where equipment, workers, material and terrain permitted. Attackers may deploy siege equipment only if brought along and assembled feasibly.

Information is imperfect. Hidden enemy units remain hidden behind real cover/terrain until revealed. Pre-battle does not allow omniscient map-edge repositioning, magically added structures or free resupply.

## 22.4 Player orders and responsiveness

Core commands: select regiment/group; move; move in formation; rotate/facing; advance; hold position; hold fire/fire at will; target area/unit; charge; withdraw; retreat; defend position; maintain spacing; form line/column/wedge/loose order/shielded or anti-cavalry stance where equipment/doctrine permit; assign group/hotkeys; queue coordinated actions and set rally points.

Orders should give rapid, comprehensible feedback and projected paths/formation ghosts. Units use local motion/collision and command mechanics to execute orders. Show when terrain, panic, congestion or commander separation prevent faithful execution. Avoid units walking through each other or teleporting into formations. A badly disrupted regiment cannot instantly reform into a perfect line in melee.

## 22.5 Combat representation hierarchy

**Regiment/formation** is the primary command and tactical simulation entity. Sub-formations/frontage groups may resolve local contacts at finer detail; visible soldiers are predominantly efficient illustrative agents. We aim for large-scale visual density without requiring independent heavyweight pathfinding, decision-making and full rigid-body physics for every soldier.

The simulation must still account for local frontage, pressure, gaps, collision, casualties and morale. A regiment cannot fight equally with all its soldiers when only a narrow front physically contacts the enemy. A large formation constrained by a bridge compresses, lengthens and risks disorder. Tactical mechanics cannot be a simple random unit-versus-unit damage exchange ignoring spatial contact.

## 22.6 Formation geometry and movement

Each regiment has coherent shape, frontage, depth, spacing, facing and current target formation. Terrain and obstacles constrain width, path and cohesion. Turning costs time depending on size and discipline. Moving uphill, through mud, across shallow water or into woods changes pace and organization. Cavalry charges require enough space, momentum, favorable footing and suitable frontage; they don't work equally well through forests or up steep saturated slopes.

Friendly units may pass through each other where physically reasonable and with disorder/time cost; they cannot phase through dense formed lines effortlessly. Formation crowding, road width and movement queues are visible. Pathfinding prioritizes obstacle avoidance and coherent regimental behavior over individually optimal soldier shortcuts.

## 22.7 Melee mechanics

Resolve local contact through manpower at the front, equipment/reach, formation geometry, armor, training, morale, fatigue, height/slope, momentum, support and flank/rear exposure. Casualties and cohesion damage depend on actual contact and tactical context. Flanking is effective because units cannot face and maintain formations equally in every direction. Encirclement strains morale, retreat paths and cohesion. Avoid hardcoded rock-paper-scissors outcomes that ignore battlefield circumstances; equipment counters can inform but not predetermine outcomes.

Units may push, yield ground, disengage with difficulty, rout or surrender under conditions. Fighting should produce credible rates of losses; armies shouldn't annihilate each other in a few seconds unless trapped/catastrophically routed.

## 22.8 Ranged combat

Arrows, bolts, javelins and other missiles use projectile flight approximations informed by muzzle/launch speed, mass/type, arc, gravity, wind vector, flight time, target movement, elevation difference, visibility, training and formation density. Crosswinds affect drift; headwinds/tailwinds affect range and time of flight where material. Rain mainly affects visibility, footing, equipment use and projectile behavior according to credible limits; it must **not** make every bow automatically useless.

Projectiles respect collision with terrain, obstacles, cover and formations. Friendly fire is possible under appropriate trajectories and conditions, clearly communicated. Ammunition is finite and replenishment requires carried reserves or physical supply. Archers need valid arcs and line of sight; dense forest and steep reverse slopes change effectiveness. Unit labels may summarize accuracy, but the observed effect should stem from situational mechanics.

## 22.9 Morale, fatigue, cohesion and discipline

Morale responds to casualties, perceived odds, commander presence, allies routing, flank/rear threats, isolation, exhaustion, hunger, recent victories and terrain confidence. Cohesion responds to movement difficulty, formation deformation, chaotic contact and command. Fatigue builds with movement, weather exposure and combat and recovers with rest at plausible rates. Experienced troops maintain control better but are not fearless.

Rout is a physical event. Soldiers/regiments seek routes away from threat and can be pursued, trapped, captured or reorganized if plausible. Capture, casualties and flight resolve consistently with after-battle manpower. Units may voluntarily withdraw without being routed; withdrawal under pressure is dangerous but not automatically impossible.

## 22.10 Local environment and visibility

Elevation affects line of sight and projectile geometry. Forests obscure visibility and disrupt formations, with actual tree patches. Fog, snow, clouds and light alter observation. Sun position/daylight affect concealment and visibility; night battles are allowed where contact occurs or planned action demands it, but order execution and identification become more difficult. Surface resistance reflects soil, water, gradient and traffic. Rivers use dynamic depth/current/ford accessibility. Hazards such as ice, floodwater and wildfire require physical preconditions.

## 22.11 Dynamic weather during combat

The battle keeps consuming live weather from the authoritative clock. Rain can start or stop; winds shift; wet ground gradually deteriorates under traffic; temperatures and daylight change. Do not suddenly toggle all terrain to a uniform “mud map.” Track local wetness and surfaces at a resolution proportionate to mechanics.

## 22.12 Unit visuals and targeting

Each regiment remains recognizable at several zoom levels through banner, facing, outline, formation geometry, weapon silhouettes, readable colors/patterns and optional soldier-level depictions. Unit status indicators are restrained but accessible. Target selection prioritizes clear interaction even in dense crowds; do not require pixel-perfect clicking on tiny sprites.

## 22.13 Victory and battle ending

A battle ends when tactical objectives or military outcome make continued confrontation unnecessary: retreat, rout, surrender, decisive field control, exhaustion/standoff, or scenario-specific siege outcome. Not every skirmish must end in complete destruction. Winners may hold ground yet suffer severe losses; a disciplined retreat can preserve an army. Battlefield control and pursuit affect prisoners, abandoned material and post-battle positioning.

## 22.14 Authoritative environmental battle contract

The tactical simulation receives a **physical snapshot and evolving regional boundary conditions** for the encounter's actual coordinates and timestamp: authoritative high-resolution elevation and material, parent geology and drainage, active surface and soil water, connected channels with actual stage/current, snow/frost/ice, vegetation/obstacles, built structures, atmospheric temperature, humidity, wind vector and gust/exposure, precipitation, cloud/fog potential, sun angle and light. Tactical subgrid refinement may add constrained shelter, puddles, local wind, microrelief and surface wear without violating the parent fields or mass/geometry continuity.

All significant interaction reads these conditions: formation slope and traction; river crossing based on dynamic depth/current; concealment from physically present canopy/fog; visibility from actual topography and solar/light state; projectile arcs from gravity, launch parameters and wind along the flight path; fatigue/exposure from temperature/weather; fire/smoke from fuel/moisture/wind. Rain is not an independent global `accuracy penalty`, and the same ford cannot be waist-deep on the campaign map but ankle-deep in battle at the same time.

Battle starts and finishes on the authoritative shared clock. Locally generated mud, trench damage, fallen trees, burned fields and destroyed structures become world deltas where persistent effects matter; local short-lived states such as smoke dissipate under physical rules. This contract is higher priority than any convenience-based tactical special case.

---
