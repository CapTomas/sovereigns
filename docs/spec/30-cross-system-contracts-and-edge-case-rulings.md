# 30. Cross-system contracts and edge-case rulings

The following decisions resolve questions that would otherwise lead different agents to implement incompatible mechanics. They are **normative** unless a later documented revision supersedes them.

## 30.1 Terrain, river and transport edge cases

| Situation | Required behavior |
|---|---|
| Rain raises river level while army approaches ford | Ford availability recalculates from water depth/current. Existing route warns or reroutes; army never crosses a physically impassable ford automatically. |
| River is visible on campaign map but smaller than tactical cells | Preserve river geometry as a feature. Refine depth, banks and channel locally; do not erase it or shift it to an unrelated location. |
| Stream or small ravine only appears at tactical resolution | Allowed if derived from local drainage/erosion and does not contradict strategic passability. A major unforecastable obstacle affecting whole armies must be represented in strategic movement data. |
| Road and river cross | There must be a bridge, ford, ferry or explicit interrupted connection; the road cannot simply pass through deep water. |
| Army destroys bridge during battle | Mark route interrupted at actual coordinates immediately. Ongoing shipments and following armies re-evaluate their routes and arrival times. |
| Snow closes mountain pass after orders are queued | Orders stop, delay or reroute according to prior standing instructions. Report cause and revised estimate. |
| River is endorheic, ending in a lake/desert basin | This is valid geography. Do not force every river to the ocean. Water balance must account for lakes/infiltration/evaporation. |
| Generated mountain is steep enough to be impassable | Humans and carts take feasible passes or engineered roads. An impassable mountain is a meaningful barrier, not a generation bug by itself. |
| Battlefield repeated months later | Reuse persisted geography and infrastructure; apply new weather/vegetation and legitimate changes, not a new independent random seed. |
| Two battles happen near the same location | Their modification footprints must combine consistently. Damaged roads and structures remain damaged for both. |
| Forest is cleared for farming | Remove physical cover in later battles and adjust local production/soil effects. Do not retain an invisible forest-cover modifier. |
| Major flood covers farmland and road | Both strategic and tactical access/use reflect flooding. When water recedes, flood damage/soil changes persist appropriately. |

## 30.2 Time and event sequencing

| Situation | Required behavior |
|---|---|
| Player submits seven-day order; battle begins on day 3 | Advance shared world to exact engagement. Pause turn resolution; complete battle at tactical time; commit state; resolve remainder of turn chronologically. |
| More than one battle is scheduled during the same turn | Resolve in chronological order. Later encounter uses casualties, damage, weather and altered positions from earlier events. |
| Battle lasts through midnight into a new day | Calendar/daylight change normally; relevant daily systems catch up without skipping/double running. |
| Other battles are pending while player manually fights | They do not jump ahead beyond current authoritative world time. Process events consistently as time advances, with appropriate interruption ordering. |
| Siege and battle happen concurrently nearby | Shared location/inventories and reinforcements are booked once. No unit or supply can participate in two places at once. |
| A delayed shipment arrives during a battle | If it could physically reach a relevant access point within elapsed time, handle campaign/tactical interaction consistently; don't teleport its cargo into a regiment. |
| Time is sped up or auto-advanced | Every required state transition still occurs; results remain consistent with the chosen simulation discretization. |
| User reloads before an encounter | Same unchanged seed/state/orders produce equivalent location and terrain; replay may diverge if the player changes decisions. |

## 30.3 Economy and ownership edge cases

| Situation | Required behavior |
|---|---|
| Royal treasury is rich but no stone can reach a fortress | Construction cannot proceed normally; funding may hire transport or open a route but cannot create stone. |
| Poor households cannot afford food while market stores are full | Hunger can occur due to affordability. Player can intervene through relief, wage/market policy or in-kind transfers. |
| A region grows food, but its bridge is blocked | Local surplus need not solve an isolated distant shortage until a feasible shipment arrives. |
| A tax demand exceeds actual surplus | Collection falls short or harms household stocks, livelihoods and legitimacy; unpaid obligations or evasion become visible. |
| Merchant ships are captured at sea | Cargo and ships leave former owner's accessible inventory and change control or are lost; never counted at destination. |
| Crown seizes a noble mine | Political/property transition and enforcement must occur. Production may stop due to dispute; title color change alone isn't sufficient. |
| Mine deposit becomes exhausted | Output declines or shifts to less attractive workings; no free replenishment. Alternative industry and migration can follow. |
| Local crop fails but neighboring market has grain | Merchants seek profitable shipment if security, capacity and affordability allow. Imports are not guaranteed or instantaneous. |
| Army requisitions grain immediately before harvest | It can take existing stores or available crops under realistic conditions; cannot collect a harvest that hasn't grown. |
| Troops are mobilized out of rural cohorts | Available labor falls. Crop work, income and household needs reflect missing workers; disbandment restores survivors, not casualties. |
| A building is destroyed but remains in ownership ledger | Its usable capacity and ongoing costs change to damaged/ruined state; its property rights may persist, but it does not keep producing. |
| A new settlement forms at a mining site | It must obtain people, food access, building materials, legal rights and infrastructure. Do not create fully occupied housing from nowhere. |
| Trade is blocked by a treaty breach | Actual route access and diplomatic relationship change; shipments already moving resolve according to interception, return or alternate routing. |

## 30.4 Army and battle edge cases

| Situation | Required behavior |
|---|---|
| One army sees another across a river | Contact does not force immediate pitched battle if crossing is inaccessible and neither can engage. They may shadow, defend or maneuver. |
| Defender reaches ridge first | Deployment and prebuilt works reflect available time, materials and actual local position. |
| Attacker arrives exhausted | Tactical units inherit fatigue and supply state. No fresh-spawn reset. |
| Enemy is hidden behind forest ridge | It remains hidden without scouting/line of sight, even though battlefield generator knows its position. |
| Archers fire in crosswind | Projectiles experience direction- and flight-dependent effects, within tuned physical plausibility. Not all shots simply suffer same hit-rate modifier. |
| Ground is wet and heavy cavalry charges | Acceleration/turning/cohesion can suffer according to soil and slope; no free “charge successful” outcome from unit label alone. |
| Archer regiment runs out of ammunition | It can cease firing, switch to available secondary weapons, resupply if feasible or withdraw. No unlimited arrows. |
| A regiment attacks across a narrow bridge | Formation compresses, frontage limits local combatants, queues form and losses depend on conditions. |
| A large regiment is hit in the flank | Facing, support, frontage, command, morale and cohesion determine result. Attacks don't ignore geometry. |
| Commander is killed | Command capability/relationships/morale change; units don't vanish or become uncontrollable forever without cause. |
| Army is routed but not destroyed | Survivors flee along viable routes; some regroup, become prisoners or scatter. Casualty accounting matches persistence. |
| Enemy refuses battle | If physically possible it withdraws; pursuing army must detect/catch it rather than trigger a teleported encounter. |
| Siege garrison runs out of food | Hunger/negotiation/desertion/health respond; walls don't lose hit points because bread is missing. |
| Autosolve says bridge defense is impossible despite huge chokepoint | This is an inconsistency to fix in autoresolve calibration, not a reason to remove chokepoint rules in manual battle. |
| Large battle exceeds rendering budget | Reduce visual soldier density/effects and optimize tactical aggregation before changing unit numbers or ignoring terrain mechanics. |

## 30.5 Political and social edge cases

| Situation | Required behavior |
|---|---|
| King dies | Succession rules and political factions resolve; player continues as state if it survives. |
| Noble disobeys mobilization | Missing troops do not spawn. Loyalty/contract and diplomacy determine compensation or punishment. |
| Ruler conquers ethnically different town | Local population/culture does not change instantly; authority and economic activity respond to occupation and negotiated institutions. |
| Civil war splits realm | Break physical control, tax rights, market access, commands and armies according to actual allegiance; avoid two governments receiving the same revenue. |
| Capital is captured | Government may relocate only where viable; treasury, administration, legitimacy and control suffer according to actual losses. Not always instant defeat. |
| Kingdom becomes a vassal | Player retains powers guaranteed by vassal arrangement; objective can become restoration of sovereignty. |
| State ceases to exist entirely | Campaign ends unless valid documented recovery channel exists (claim in exile, political restoration path with real support). |
| Long peace lasts decades | Settlements, ecosystems, knowledge, market networks and rival AI development continue. History isn't frozen waiting for war. |

## 30.6 Sovereignty, access and occupation edge cases

| Situation | Required behavior |
|---|---|
| Army traverses treaty-authorized ally | Passage without ownership change, forceful occupation, taxation or automatic war. |
| Army enters neutral land without access | Mark a breach, provide neutral responses and interception; no free annexation. |
| Fast cavalry makes geometric loop | No polygon capture. Only real holdings, threatened routes, damage or secured locations change. |
| Captured crossing is the only road | Change supported route capacity and local security, but not legal borders by fiat. |
| Hostile fort remains behind the advance | Its supplied defenders can oppose movement and supplies through feasible adjacent routes. |
| Defended valley seemingly encircled but has river port | Calculate remaining water access and actual interdiction instead of declaring full isolation. |
| Two kingdoms claim same village, third occupies it | Keep independent claim/sovereignty/occupation layers; collect no resource twice. |
| Patrol zone exists but unit loses supplies | Security degrades or fails according to time, local aid and effective reach. |
| Occupied town signs separate submission | Apply agreed local rights and obligations; surrounding land remains a separately evaluated space. |
| War ends with delayed withdrawal | Legal settlement and military presence remain distinct until real forces depart or new conflict occurs. |

## 30.7 When simple abstraction is acceptable

An abstraction is permitted if it conserves the right quantities, respects geography and time, feeds connected systems, remains inspectable and produces credible outcomes under stress. For example: treat a region's 2,000 farmers as employment cohorts; treat trade across a major road as aggregated shipments; simulate a 20,000-soldier army's collision mostly by formations. What is forbidden is an abstraction that severs core causality, such as “enemy in same province → random generic battlefield with fixed sunny weather.”

## 30.8 Mandatory environmental and physical-data contracts

1. **Elevation → everything:** all slope, ridge, horizon, drainage and construction suitability derives from one geographic height reference; no tactical-only relief contradicting strategic elevation.
2. **Atmosphere → precipitation:** wind, humidity, temperature and cloud/rain processes share state; no separately randomized combat rain, crop rain or cloud direction.
3. **Precipitation → water stores:** rain/snow, interception, soil water, groundwater, runoff, channels, lakes and evaporation honor finite quantities and routing; no infinitely full river without a source.
4. **Calendar → energy:** latitude, day/night and seasons drive local sunlight/thermal forcing; night battles and crop dormancy use the same clock and world location.
5. **Hydrology → routes:** bridges, streams, current, fords, flooding and soil saturation are the same facts for merchants, armies and battlefield units.
6. **Land surface → movement:** soil material, saturation, slope, cover, compaction, snow/ice and load are inputs to movement; there is no unrelated universal terrain penalty table overriding them.
7. **Physical ecology → production:** vegetation and field yields draw from viable soils, water, thermal history and land use; biome tiles do not output resources by fiat.
8. **Human change → physics:** clearing, ditching, farming, burning, building and destroying modify authoritative landscape features and relevant stores.
9. **Simulation → information:** UI/AI observations are views of physical truth filtered by knowledge, with explicit uncertainty; render effects cannot independently mutate conditions.
10. **Battle → campaign → battle:** physical history and persistent deltas survive tactical exits, saves, turns, unloads and subsequent visits.

**Tie-breaker:** when a downstream mechanic conflicts with the field/data provenance defined in Sections 3–6, the physical foundation wins unless the bible is explicitly amended with a physically credible alternative.

---
