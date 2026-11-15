# changelog

All notable changes to Hamlet are documented here. The format follows
[Keep a Changelog 1.1.0](https://keepachangelog.com/en/1.1.0/); versioning
is [Semantic Versioning](https://semver.org/spec/v2.0.0.html) — pre-1.0,
MINOR bumps per milestone.

## [0.3.0] — 2026-11-15

M2: A Living Street. A hundred citizens live on one street — jobs, shops, and
money as real entities — and the town runs itself for ten in-game days,
replaying bit-identically from the seed.

### Added

- Road-graph pathfinding: congestion-weighted A* over road tiles
  (`Pathfinder`, `Congestion`, `RoadPath`) with integer costs, id tie-breaks,
  and allocation-free stamped scratch arrays; citizens detour around jams and
  fall back to the direct walk when no road connects the ends.
- Shops as real counters with meal stock and their own ledger: households walk
  to their corner shop and buy meal bundles from a wallet (`Shop`/`ShopBook`,
  `FoundShop`). Shops restock wholesale from a supplier firm at the top of the
  morning restock hour; every coin is a ledger transfer.
- Firms as real employers paying each completed shift from a payroll ledger
  (`Firm`/`FirmBook`, `FoundFirm`); `household.CollectWage()` on shift end.
- The M2 exit gate (`TownStabilityTests`): 100 households, 10 shops, 10 firms
  on one street for 10 in-game days with no intervention — no hunger crisis,
  ≥90% employed in the last two days, every pantry stocked at the end, total
  ledger money unchanged, identical state hash across replays. Writing the
  gate exposed and fixed: a restock trigger that never fired, sleep/work
  outranking hunger crisis for low-hunger-weight citizens, night sleep
  carrying citizens into starvation, and shop errands pre-empting meals.
  Crisis now pre-empts continuously: hunger crisis zeroes sleep and work,
  work drive scales with satiety, and the night-sleep floor drops once hunger
  falls due.
- LOD tier 2 (§6.5): the camera focus square arrives as a `SetCameraFocus`
  command (1 Hz from the presentation, tick-boundary aligned); off-camera
  citizens keep full per-tick needs and staggered replanning but jump
  straight to the plan anchor instead of per-tick nav.
- Household bodies, first merge tier (§6.6): co-located household members
  with identical intent and target merge into one body at tick boundaries
  (family-sized, `MaxBodyMembers = 8`); the representative plans and moves,
  riders follow, and perform effects scale by mass. Crisis or divergence
  splits losslessly; merge → split → merge round-trips exactly (seeded
  property test).
- 85 sim tests, including the determinism gate.

## [0.2.0] — 2026-11-15

M1: the first citizen. One agent sleeps, eats, and walks to work unprompted
through a full day cycle, and the greybox shows it happening in-engine.

### Added

- Needs model with data-driven, trait-modulated integer decay rates: every
  need ticks on its own interval and nothing fractional persists.
- Utility scoring over the need vector and travel → perform plans for the
  first primitive activities — sleep, eat, and work — with a switch margin so
  a running plan is not pre-empted by noise.
- Households with a home, job site, and pantry, founded through the
  `FoundHousehold` command; the household drives a full day cycle.
- Day schedule atom (`sim/core/DaySchedule.cs`): wake, night, and shift hours
  live in one clock-keyed place.
- Presentation snapshots (`Citizen.Snapshot()`, `Household.Snapshot()`,
  `WorldGrid.CaptureTerrain()`) as plain tick-boundary values; the host never
  hands the scene tree a live sim reference.
- In-engine greybox against `docs/art-direction.md`: banded flat-shaded
  terrain from the 0–100 layers, road strip, home and workplace, one citizen
  body, warm sun + hemisphere ambient, gradient sky, and an orbit/pan/zoom
  camera. Every citizen renders through one MultiMesh.
- `SimHost` now marshals packed tick-boundary packets (terrain once, compact
  citizen/household/road arrays once a second) instead of per-agent
  dictionaries, and picks a deterministic dry, gently-elevated settlement row
  from the seed.
- Headless routine report: the smoke run prints the day's activity trace.
- M0 in-engine budget check: `--stress=1000` spawns a thousand real settlers
  on the road strip; verified at 1,001 rendered bodies holding 120 fps.

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