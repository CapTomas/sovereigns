# Content data

Versioned, read-only definitions that the simulation and client load at startup: scenarios now; crops, commodities, equipment, buildings, institutions and asset references as their phases arrive. Content is data, not code. Behavior belongs in `src/`.

## Layout and IDs

One JSON object per file at `content/<namespace>/<kind>/<name>.json`. Its `id` must be `<namespace>:<kind>/<name>` ([ADR-0004 §5](../docs/architecture/ADR-0004-units-coordinates-time-identifiers.md)). The base game uses namespace `core`, and each mod uses its own namespace. Members use `snake_case`. Physical quantities use SI units, and the unit is part of the member name where it is not obvious (`extent_m`, `step_ms`).

| Kind | Definition | Purpose |
|---|---|---|
| `scenario` | [`ScenarioDefinition`](../src/Sovereigns.Simulation/Scenarios/ScenarioDefinition.cs) | World setup selected by name together with a seed |

## Validation

```sh
dotnet run --project tools/Sovereigns.Headless -- validate-content
```

The loader rejects unknown, missing, mistyped and null members, IDs that do not match the file location, unknown kinds, stray files, semantic violations and references to missing or wrong-kind definitions. Each error names the file, line or JSON path and the fix, for example:

```text
content/core/scenario/empty_world.json:8: $.stepms: The JSON property 'stepms' could not be mapped to any .NET member contained in type 'Sovereigns.Simulation.Scenarios.ScenarioDefinition'. Fix: ScenarioDefinition members are: id, description, extent_m, step_ms, duration_ms
```

The game and the headless runner refuse to start with invalid content. To add a kind, write its record implementing `IContentDefinition` (schema, `Validate`, `References`) and register it in [`ContentKinds`](../src/Sovereigns.Simulation/Content/ContentKinds.cs). Renaming an ID requires a migration entry once saves exist.

Media and data files (images, audio, fonts, datasets) need an entry in the [third-party and asset registry](../docs/legal/README.md).
