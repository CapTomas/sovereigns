## Phase 19 — Regional transport graph, shipments and infrastructure constraints
**Depends on:** 15 and 18.  
**Outcome:** Goods and travelers flow between locations with time, capacity, cost and access constraints.  
**GDB:** §§13.2, 13.5–13.8, 15.3, 19.7.

- [ ] **SOV-P19-T01** — Create a transport graph from actual roads, tracks, river reaches, ports, crossings and sea links.
- [ ] **SOV-P19-T02** — Derive traversability from slopes, river state, snow, bridge load and weather effects.
- [ ] **SOV-P19-T03** — Represent route time, seasonal reliability, carrying capacity and economic cost.
- [ ] **SOV-P19-T04** — Distinguish walking, pack animal, cart, riverboat and coastal shipping capabilities.
- [ ] **SOV-P19-T05** — Model bridges, ferries, fords and passes as real nodes with capacity and access constraints.
- [ ] **SOV-P19-T06** — Represent route closures and alternative paths without rerouting through impassable cells.
- [ ] **SOV-P19-T07** — Track physical shipments with commodity, quantity, origin, destination and elapsed travel.
- [ ] **SOV-P19-T08** — Separate aggregated civilian flows from visible strategic convoys with equivalent accounting.
- [ ] **SOV-P19-T09** — Deduct goods at dispatch and add them at confirmed arrival, accounting for losses.
- [ ] **SOV-P19-T10** — Reserve route capacity and prevent simultaneous shipments exceeding modeled throughput.
- [ ] **SOV-P19-T11** — Represent storage depots, warehouses, granaries and port transshipment costs.
- [ ] **SOV-P19-T12** — Connect border permissions and dangerous routes to shipment feasibility.
- [ ] **SOV-P19-T13** — Allow army operations to obstruct routes through a future political-access interface.
- [ ] **SOV-P19-T14** — Model road wear, flooding, snow and infrastructure condition as transport modifiers.
- [ ] **SOV-P19-T15** — Show transport heatmap, bottlenecks, blocked nodes, travel times and capacity use.
- [ ] **SOV-P19-T16** — Test a valley cutoff, bridge collapse and recovery after alternate road construction.
- [ ] **SOV-P19-T17** — Test cold-season river closure with working overland substitution when possible.
- [ ] **SOV-P19-T18** — Test cargo preservation across save/load and fast-forward travel.
- [ ] **SOV-P19-T19** — Test trade cannot deliver food to an inland town when every route is legitimately blocked.

**Exit gate 19:** Local inventories move through physically possible edges with travel time and capacity; a washed-out bridge can isolate a settlement without altering political ownership.
