# Tests and fixtures

All implemented behavior needs repeatable evidence. Never report tests that are not present or were not executed.

| Location | What it checks | Command |
|---|---|---|
| `Sovereigns.Simulation.Tests/` | Simulation and headless-runner behavior: clock, commands and replay, checksums, logging, content validation, failure reports, leak and architecture checks. No Godot needed. | `dotnet test Sovereigns.slnx` |
| `../game/tests/` | Godot client behavior, run headless | `godot --headless --path game res://tests/ClientTests.tscn` |
| `fixtures/` | [Versioned reference fixtures](fixtures/README.md) | `dotnet run --project tools/Sovereigns.Headless -- run --fixture F-EMPTY` |
| `../tools/tests/` | Repository validation and Python tooling | `python3 -m unittest discover -s tools/tests -v` |
