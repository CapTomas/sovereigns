# Simulation implementation scope

No Godot dependency or reliance on the client frame rate. Shared world state has explicit ownership. Every physical field or entity must declare units, spatial/temporal resolution, authoritative write owner, downstream consumers, invalidation rules, save/replay behavior, diagnostics and numerical/tolerance tests. No placeholder algorithms that superficially mimic required physics. Coordinate with other modules before changing authoritative contracts.
