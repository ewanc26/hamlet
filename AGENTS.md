# AGENTS.md

Guidance for AI coding agents working in **hamlet**.

Mandatory instructions for AI coding agents (and human contributors) in this
repository. Every rule below is standing and admits no exception unless a
later section of `docs/project-brief.md` explicitly supersedes it.

## Read before writing any code

- `docs/project-brief.md` — the design document. Section references below
  (§4, §5.1, …) point into it.
- `CONTRIBUTING.md` — expands the rules here with rationale.
- `README.md` — the project layout table.

## The project in one paragraph

Hamlet is a city-to-country builder: **Godot 4.7** (.NET build) with a
**deterministic C#/.NET 8 simulation core** (`sim/`, engine-agnostic) and
**GDScript presentation** (`ui/`, `scenes/`), joined by exactly one C# Node
(`host/SimHost.cs`). Licensed **AGPL-3.0** — new files need no header, but
never remove or weaken the license.

## File architecture — mandatory on every change

Every file is an **atom**: `<scope>/[subscope/]<atom>.<ext>`.

- One concept per file. A tightly-bound family (e.g. the ledger pair, one
  enum plus the struct it parameterises) is one atom; unrelated concepts
  never share a file.
- Scopes and subscopes are lowercase: `sim/core`, `tests/sim`,
  `data/samples`. At most one subscope level — `sim/agent/needs/` is a smell.
- C# atoms are named exactly for their class: `SimHost.cs` (Godot enforces
  the file-class match). GDScript atoms are snake_case: `hud.gd`. JSON
  content atoms: one file per entity: `data/samples/bakery.json`.
- Platform-enforced paths (`.github/workflows/ci.yml`, `project.godot`)
  are the only exceptions.
- When adding a concept, create a new atom; do not append it to an existing
  atom that already owns a different concept.

## Language split — never cross the boundary casually

- `sim/` and `headless/` are plain C#: **no `Godot` namespaces, no Godot
  types, ever**. If a `sim/` file needs `using Godot;` to compile, the
  design is wrong — move that code to `host/` or `ui/`.
- `host/` contains the **only** C# Node. Nothing else in C# may derive from
  a Godot type.
- GDScript owns the scene tree. It reaches the simulation only through
  `SimHost.enqueue_command()` and the `events_ready` signal.
- Cross-language calls happen **only at tick boundaries, in batch** —
  never per-frame per-agent calls across the boundary.
- The sim thread never touches the scene tree or the frame clock.

## Determinism — the core promise

- The only randomness source is `sim/core/DeterministicRandom.cs`
  (seeded from the world seed). **No `System.Random`, no `GD.RandRange`,
  no `randi()`, no time-seeded anything.**
- Nothing floats persist: compute in floats if needed, quantise on write
  to the integer scale. Saves and state never contain floats.
- Tick loops allocate nothing: pre-size or pool. No LINQ in hot paths.
- Identical (seed, command stream) must produce a bit-identical state hash
  on every machine — `tests/sim/DeterminismTests.cs` enforces this and a
  red gate blocks the merge, no exceptions, no skips.

## The 0–100 convention

Every scalar measure (needs, happiness, opinions, land value, pollution,
coverage, relationship strength, …) is an integer on the shared 0–100
scale, clamped on write. Exempt: counts, money, IDs (natural units) and
categorical fields (stay categorical). Never introduce a second scale.

## Commits — the atomic pattern applies here too

- One commit = one atom or one coherent concept. A new atom and its tests
  are one commit; a fix to one atom is one commit; a rename series is one
  commit per atom. Never mix unrelated changes — split the work instead.
- Subject line: imperative, and it names the atom or scope it touches.
  The body explains why the change is needed.
- Every commit builds and leaves the test suite green on its own. No
  "fix the last commit" follow-ups — amend or rebase before pushing.

## Versioning — SemVer, one atom of truth

- The version lives in one place: `Directory.Build.props` (`<Version>`).
  Every project inherits it; no project sets its own version.
- Tags are `vX.Y.Z` (e.g. `v0.1.0`). Pre-1.0: MINOR bumps per milestone
  (M0 → 0.1.0, M1 → 0.2.0, …), PATCH for fixes within a milestone;
  breaking changes also bump MINOR until 1.0.
- A version bump and its `CHANGELOG.md` entry ship in the same commit.
  No bump without an entry; no entry without a bump.
- Releasing is automated: the `release` workflow tests, then tags and
  publishes a GitHub release whenever the version atom changes on `main`.
  Do not tag by hand.
- Once saves exist, their headers and content schemas record the sim
  version that wrote them; a bump that changes either is breaking.

## Workflow — what you must do

```sh
dotnet test tests/Hamlet.Sim.Tests.csproj        # must be fully green
dotnet run --project headless -- 12345 10000 128 # smoke run must complete
```

- Every change adds or updates a test in `tests/sim/`. A PR that only
  moves code around still updates the affected tests.
- Build with zero warnings (`TreatWarningsAsErrors` is on in `sim/`) —
  do not work around a warning by suppressing it; fix the cause.
- Keep the design document in sync: if a decision changes, update
  `docs/project-brief.md` (and the canvas it mirrors) in the same PR.
- Never reformat, rename, or "clean up" atoms you were not asked to touch.

## What not to do

- No new Godot-facing C# types outside `host/`.
- No scripted outcome thresholds ("if happiness < 40 then riot") —
  outcomes emerge from agent behaviour (§3).
- No economy shortcuts that mint or destroy money — transfers only, via
  `LedgerBook`; conservation is asserted in tests (§3.2).
- No per-frame allocations or signal traffic in the tick loop (§7.1).
- Do not delete or weaken tests, CI steps, or the determinism gate.
- Do not tag releases by hand or version projects individually — the
  version atom and the release workflow own both.

<!-- allowed-agents -->
## Allowed agents

Only **Claude** (Anthropic) and **Mistral** (Mistral AI) may be used as coding agents in this repository. This rule supersedes any earlier agent-attribution guidance in this file.

- Agent-assisted commits must credit only one of those agents:
  - `Co-Authored-By: Claude <noreply@anthropic.com>`
  - `Co-Authored-By: Mistral Vibe <vibe@mistral.ai>`
- Allowed agent footers are `🤖 Generated with [Claude Code](https://claude.com/claude-code)` and `🤖 Generated with [Mistral Vibe](https://mistral.ai)`.
- Do not add `Co-Authored-By:` trailers, `Generated with` footers, author names, or committer identities for any other agent or AI tool.
- Do not credit another agent in commit metadata.
