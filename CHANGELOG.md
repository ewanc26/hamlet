# changelog

All notable changes to Hamlet are documented here. The format follows
[Keep a Changelog 1.1.0](https://keepachangelog.com/en/1.1.0/); versioning
is [Semantic Versioning](https://semver.org/spec/v2.0.0.html) — pre-1.0,
MINOR bumps per milestone.

## [unreleased]

## [0.1.0] — 2026-11-15

M0: the simulation core scaffold.

### Added

- Deterministic, engine-agnostic C# sim core: seeded PRNG, fixed-timestep
  clock, state hash, 0-100 needs, citizens, stage-dependent agent bodies,
  world grid with deterministic terrain generation, road graph, and the
  conserved-ledger economy.
- `SimHost`, the single C# boundary Node: background sim thread with
  batched tick-boundary signals.
- Headless CLI harness and the xunit suite with the determinism gate
  (21 tests).
- CI: tests + headless smoke run. Godot 4.7 project shell and HUD atom.
- Sample content atom (`data/samples/bakery.json`) and the project brief
  mirrored under `docs/`.
- AGPL-3.0 license and `AGENTS.md` codifying the repo's standing patterns.