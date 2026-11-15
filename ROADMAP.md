# roadmap

The milestone ladder from the design document ([docs/project-brief.md](docs/project-brief.md) §10), kept current. Versioning is SemVer, pre-1.0: every milestone bumps MINOR — M0 → 0.1.0, M1 → 0.2.0, and so on; **1.0.0 is reserved for the launch** after M6 exits.

## where we are

**M1 complete — v0.2.0.** One citizen lives a full day unattended: needs decay on
trait-modulated intervals, utility scoring picks sleep/eat/work, and the greybox
renders the terrain, road strip, home, workplace, and citizen from tick-boundary
packets. The in-engine budget check also clears M0's last criterion: 1,001
road-walking bodies at 120 fps. **M2 — A Living Street** is in progress: citizens
now route over the road graph with congestion-weighted A*, detouring around
jams instead of walking the straight line. Jobs, shops, money, LOD tier 2, and
the first household body remain.

## milestones

| Milestone | Version | Deliverable | Exit criteria | Status |
| --- | --- | --- | --- | --- |
| **M0 — Foundations** | 0.1.0 | Godot + C# sim skeleton (`Hamlet.Sim` + `SimHost` thread boundary), grid, road placement, tick loop, save/replay harness | 1,000 road-walking dots at 60 fps; determinism verified by replay; zero cross-language calls outside tick boundaries | complete — 1,001 bodies at 120 fps in-engine; determinism gate green |
| **M1 — First Citizen** | 0.2.0 | Needs model, utility scoring, primitive activities, single household | One agent sleeps, eats, walks to work unprompted for a full day cycle | complete |
| **M2 — A Living Street** | 0.3.0 | 100 citizens, jobs, shops, money, pathfinding + congestion, LOD tier 2, household bodies (first merge tier) | Stable town of 100 with no player intervention for 10 in-game days; a household body round-trips merge → split → merge losslessly | next |
| **M3 — The Economy** | 0.4.0 | Rents, land value, taxes, services, immigration/emigration loop | City grows or shrinks believably in response to policy changes | planned |
| **M4 — The Society** | 0.5.0 | Relationship graph, memory, life stages, news storyteller, inspector panel | Anecdotes: emergent friendships, commutes, evictions reported on ticker | planned |
| **M5 — Scale & Polish** | 0.6.0 | 10k citizens, LOD tier 3, cohort bodies (city-stage merge), overlays, balance passes, mod data extraction | Performance budgets met on min-spec machine; closed beta | planned |
| **M6 — The Nation** | 0.7.0 | Multi-settlement maps, inter-city infrastructure, migration & trade flows, national policy layer, settlement bodies | Two specialised settlements trading along a shared highway while households migrate between them as real, inspectable agents | planned |

## M2 detail — what lands next

- **Jobs and shops as real entities**: shops are real counters with meal stock
  and their own ledgers — households walk to the shop and buy groceries from a
  wallet; firms are real employers that pay each completed shift from a real
  payroll ledger.
- **Pathfinding on the road graph**: A* with congestion-weighted edges,
  replacing the M1 straight-line walk — **landed** (tile-level first cut; the
  junction/flow-field hierarchy follows); citizens route around jams.
- **LOD tier 2**: off-camera citizens replan on the staggered 60-tick bucket and
  move coarsely between anchors.
- **Household bodies**: compatible family trips merge into one body with mass,
  and a merge → split → merge round trip must be lossless (property test).
- **100 citizens, 10 in-game days, no intervention**: the stability gate.

## quality gates (every milestone)

- **Determinism CI** — every PR runs the sim twice from the same seed and byte-compares; any divergence blocks the merge.
- **Property tests** — needs stay within 0–100 under fuzzed activity streams; money is conserved across all ledgers; merge → split → merge round-trips losslessly.
- **Golden replay fixtures** — the headless runner replays canonical cities nightly and diffs outcomes.
- **Performance gates** — tick ≤ 4 ms at 15 Hz on a canned 10k-agent city; a regression fails the build.