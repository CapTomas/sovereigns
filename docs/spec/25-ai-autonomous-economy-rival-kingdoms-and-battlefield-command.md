# 25. AI: autonomous economy, rival kingdoms and battlefield command

## 25.1 Three distinct AI jobs

Do not conflate market autonomy, political decision-making and tactical control. They operate at different scales and should follow different rules:

1. **Economic actors** pursue livelihood, profitable opportunities and institutional obligations within local constraints.
2. **Political actors** pursue security, prosperity, prestige, legitimacy and survival through diplomacy, policy and warfare.
3. **Military actors** interpret objectives, scout, march, deploy, maneuver, fight and retreat using their available knowledge.

These should share the same authoritative world and be able to explain major actions. A state should not decide to invade over winter with a huge force while its supply planning assumes a nonexistent road.

## 25.2 Local autonomous economy

Households seek food, income and security. Farms choose plausible crops and labor allocation. Enterprises react to demand and input supply. Merchants compare trade opportunities accounting for cost and risk. Governors allocate budgets under directives, law and political incentives. Individual agents aren't necessary to simulate mundane behavior; aggregated actor groups can generate the same meaningful outcomes.

Economic AI must conserve labor, goods and money. It may misjudge price trends or overinvest, but never gain infinite funds or stock because a heuristic failed. Where markets are illiquid, rationing and failure are acceptable outcomes.

## 25.3 Political AI hierarchy

Rival states have stable strategic objectives (survival, border security, prestige, trade access, dynastic claims), situation assessment, long-term plans, operating priorities and tactical reactions. Objectives change when rulers, economies, alliances or geography change. Personality affects preferences and risk tolerance without eliminating basic rationality.

A state facing famine may import grain, request aid, relax tolls, raid for supplies or reduce military activity depending on relationships and authority. Two states with similar inputs may make different plausible decisions. Diplomatic overtures should explain the strategic value they seek.

## 25.4 Campaign military AI

Before war, examine actual routes, likely seasons, supply distances, forts, vulnerable crossings, allied obligations and enemy intelligence. During war, maintain objectives and revise them after losses or opportunities. Build depots, besiege, screen, retreat, defend a chokepoint or march to relieve a settlement when justified. AI force assessment must consider readiness, terrain and supplies, not only troop count.

Opponents must **not** access unrevealed player positions, player-only UI data or perfect future weather. They can make reasonable inferences and use their own scouts. A scouting investment should noticeably improve decisions.

## 25.5 Tactical battle AI

The tactical AI operates at command-group and regiment level: form lines, screen flanks, position ranged troops, exploit terrain, protect cavalry routes, use reserves, react to disorder and disengage if hopeless. It should avoid suicidal hill charges and impossible river fords when alternatives exist, but its choices are bounded by commander ability, visibility, fatigue and scenario. It uses the same movement and combat physics as the player.

Formation-group tactics should be spatial, using objectives such as hold ridge, defend bridge, refuse flank, occupy forest edge and attack exposed line, rather than perfectly choreographed individual soldiers. Respond to changing rain, visibility and movement conditions during a battle.

## 25.6 AI fairness and diagnostic behavior

There are no invisible AI supplies, no teleporting reinforcements, no free empire-wide recruitment and no permanent arbitrary combat buffs for difficulty. The game may abstract ordinary AI micromanagement at scale, but outputs must obey the same quantity and time contracts as the player. Provide developer views that reveal intentions, perceived intelligence, chosen routes, market bottlenecks and reason codes so failures can be diagnosed.

## 25.7 Automation for the player

Optional advisers may propose construction, suggest improvements, queue routine military recruitment, handle local economic development and manage governors under budgets. The player must be able to inspect projected and actual consequences and revoke orders. Default automation should reduce repetition rather than silently override major decisions.

## 25.8 AI decisions are grounded in the same physical environment

AI kingdom and army planning must consume true environmental observations and permitted forecasts with the same knowledge limitations as the player. Rivals may recognize that winter snow closes a pass, that rains increase ford risk or that a forest offers ambush cover, but may not read future storm events or teleport supplies through an impassable bridge. Autonomous farms and merchants observe the same climate, wet soil, crop history and transport constraints and adapt production, storage and routes accordingly.

AI evaluation can use aggregated approximations but may not invent alternate physical facts. A strategic AI may predict that a river will rise after storms; the actual crossing result is still determined by the world simulation.

---
