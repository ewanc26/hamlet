# contributing to hamlet

## File architecture (standing rule)

Every file is an atom: `<scope>/[subscope/]<atom>.<ext>`.

- One concept per file; a tightly-bound family (all event kinds, the ledger pair)
  is one atom, but unrelated concepts never share a file.
- Lowercase scopes and subscopes: `sim/core`, `tests/sim`, `data/samples`.
- At most one subscope level — `sim/agent/needs` is a smell, `sim/agent/Needs.cs` is not.
- C# atom names match their class exactly (`SimHost.cs`) — Godot enforces this.
- GDScript atoms are snake_case (`hud.gd`); JSON atoms are kebab/snake-case.
- Platform-enforced paths (`.github/workflows/ci.yml`, `project.godot`) are the
  only exceptions.

## The language split

- `sim/` and `headless/` are plain C#: no `Godot` namespaces, ever. If a type
  compiles in `sim/` because of a `using Godot;`, the PR is wrong.
- `host/` contains the only C# Node. Nothing else in C# may derive from a Godot type.
- GDScript owns the scene tree. It reaches the sim via `SimHost.enqueue_command()`
  and the `events_ready` signal — never per-frame per-agent calls across the boundary.
- Calls cross the boundary only at tick boundaries, in batch.

## Determinism rules

- The only randomness source is `DeterministicRandom` — no `System.Random`,
  no `GD.RandRange`, no `randi()`.
- Nothing floats persist: the 0-100 scale quantises on write.
- Tick loops allocate nothing: pool or pre-size.
- Every PR adds or updates a test; CI byte-compares runs from the same seed.

## Tests

```sh
dotnet test tests/Hamlet.Sim.Tests.csproj
```
