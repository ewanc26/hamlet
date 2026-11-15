# roadmap

The milestone ladder from the design document ([docs/project-brief.md](docs/project-brief.md) §10), kept current. Versioning is SemVer, pre-1.0: every milestone bumps MINOR — M0 → 0.1.0, M1 → 0.2.0, and so on; **1.0.0 is reserved for the launch** after M6 exits.

## where we are

**M1 complete — v0.2.0.** One citizen lives a full day unattended: needs decay on
trait-modulated intervals, utility scoring picks sleep/eat/work, and the greybox
renders the terrain, road strip, home, workplace, and citizen from tick-boundary
packets. The in-engine budget check also clears M0's last criterion: 1,001
road-walking bodies at 120 fps. **M2 — A Living Street** is in progress: citizens
route over the road graph with congestion-weighted A*, detouring around
jams instead of walking the straight line. Jobs, shops, and money are real
entities now — firms pay shifts from payroll ledgers, households buy meal
bundles from shop counters, and shops restock wholesale from a supplier firm
each morning — and the stability gate is green: 100 citizens on one street
stay fed and employed for 10 in-game days with no intervention, replaying
bit-identically from the seed. LOD tier 2 and the first household body are in:
the camera focus square rides the command stream (off-camera citizens move
coarse while their needs stay full), and compatible family trips merge into one
body with mass, splitting losslessly on crisis or divergence.

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

- **Jobs and shops as real entities** — **landed**: shops are real counters with
  meal stock and their own ledgers — households walk to the shop and buy
  groceries from a wallet; shops order wholesale stock from a supplier firm at
  the top of each morning's restock hour. Firms are real employers that pay
  each completed shift from a real payroll ledger. Money is conserved across
  all ledgers (§7.3).
- **Pathfinding on the road graph** — **landed** (tile-level first cut; the
  junction/flow-field hierarchy follows): A* with congestion-weighted edges
  replaces the M1 straight-line walk; citizens route around jams.
- **LOD tier 2** — **landed**: the camera focus square travels as a
  `SetCameraFocus` command (1 Hz from the presentation, so it lands only on
  tick boundaries); inside it citizens run full LOD (one tile per tick),
  outside they run standard — needs still decay every tick, replanning stays
  on the staggered 60-tick bucket, and movement jumps straight to the plan's
  anchor instead of per-tick nav.
- **Household bodies** — **landed (first merge tier)**: at each tick boundary a
  household's co-located members with the same intent and target merge into one
  body (§6.6); the lowest-id member plans and moves, riders follow, and perform
  effects scale by mass (every member eats and is paid, shopping buys mass ×
  bundle). A finished trip, a divergent member, or a member crisis splits the
  body losslessly. Family-sized only (`MaxBodyMembers = 8`); the merge → split →
  merge round trip is a seeded property test.
- **100 citizens, 10 in-game days, no intervention** — **landed as the
  stability gate** (`TownStabilityTests`): no hunger crisis, ≥ 90% employed in
  the last two days, every pantry stocked at the end, total money unchanged,
  and the decade replays bit-identically from the seed.

## quality gates (every milestone)

- **Determinism CI** — every PR runs the sim twice from the same seed and byte-compares; any divergence blocks the merge.
- **Property tests** — needs stay within 0–100 under fuzzed activity streams; money is conserved across all ledgers; merge → split → merge round-trips losslessly.
- **Golden replay fixtures** — the headless runner replays canonical cities nightly and diffs outcomes.
- **Performance gates** — tick ≤ 4 ms at 15 Hz on a canned 10k-agent city; a regression fails the build.