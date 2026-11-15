# roadmap

The milestone ladder from the design document ([docs/project-brief.md](docs/project-brief.md) §10), kept current. Versioning is SemVer, pre-1.0: every milestone bumps MINOR — M0 → 0.1.0, M1 → 0.2.0, and so on; **1.0.0 is reserved for the launch** after M6 exits.

## where we are

**M0 core complete — v0.1.0.** Deterministic C# sim core, the single `SimHost` boundary, headless harness, 21-test suite with the determinism gate, CI, AGPL-3.0. Art direction locked: diorama minimalism, flat-shaded, green-forward palette ([docs/art-direction.md](docs/art-direction.md)). **M1 — First Citizen is next.**

## milestones

| Milestone | Version | Deliverable | Exit criteria | Status |
| --- | --- | --- | --- | --- |
| **M0 — Foundations** | 0.1.0 | Godot + C# sim skeleton (`Hamlet.Sim` + `SimHost` thread boundary), grid, road placement, tick loop, save/replay harness | 1,000 road-walking dots at 60 fps; determinism verified by replay; zero cross-language calls outside tick boundaries | headless complete · in-engine check rides with the M1 greybox |
| **M1 — First Citizen** | 0.2.0 | Needs model, utility scoring, primitive activities, single household | One agent sleeps, eats, walks to work unprompted for a full day cycle | next |
| **M2 — A Living Street** | 0.3.0 | 100 citizens, jobs, shops, money, pathfinding + congestion, LOD tier 2, household bodies (first merge tier) | Stable town of 100 with no player intervention for 10 in-game days; a household body round-trips merge → split → merge losslessly | planned |
| **M3 — The Economy** | 0.4.0 | Rents, land value, taxes, services, immigration/emigration loop | City grows or shrinks believably in response to policy changes | planned |
| **M4 — The Society** | 0.5.0 | Relationship graph, memory, life stages, news storyteller, inspector panel | Anecdotes: emergent friendships, commutes, evictions reported on ticker | planned |
| **M5 — Scale & Polish** | 0.6.0 | 10k citizens, LOD tier 3, cohort bodies (city-stage merge), overlays, balance passes, mod data extraction | Performance budgets met on min-spec machine; closed beta | planned |
| **M6 — The Nation** | 0.7.0 | Multi-settlement maps, inter-city infrastructure, migration & trade flows, national policy layer, settlement bodies | Two specialised settlements trading along a shared highway while households migrate between them as real, inspectable agents | planned |

## M1 detail — what lands next

- Needs atoms wired to data-driven decay rates; utility scoring over the need vector.
- Primitive activities: sleep, eat, walk, work — the first real GOAP-lite plans.
- One household of citizens, end-to-end: sim → `SimHost` → scene tree.
- **In-engine style greybox**: generated terrain patch, road strip, three buildings, one citizen, rendered against the style target; tune the ambient before any bespoke assets. This run also checks off M0's 1,000-dots-at-60-fps criterion.

## quality gates (every milestone)

- **Determinism CI** — every PR runs the sim twice from the same seed and byte-compares; any divergence blocks the merge.
- **Property tests** — needs stay within 0–100 under fuzzed activity streams; money is conserved across all ledgers; merge → split → merge round-trips losslessly.
- **Golden replay fixtures** — the headless runner replays canonical cities nightly and diffs outcomes.
- **Performance gates** — tick ≤ 4 ms at 15 Hz on a canned 10k-agent city; a regression fails the build.