# Contributing to Hamlet

*A city-to-country builder* — every citizen a persistent, autonomous agent. Hamlet grows from a handful of settlers on empty land to a nation of specialised, trading settlements, one continuous simulation at every scale. Godot 4.7, C#/.NET simulation core, GDScript presentation, AGPL-3.0. Thanks for wanting to help!

The full design lives in [`docs/project-brief.md`](docs/project-brief.md); the milestone ladder is in [`ROADMAP.md`](ROADMAP.md).

## The split (the rules that make the project work)

| Side | Language | Lives in |
| --- | --- | --- |
| Simulation core (engine-agnostic, deterministic, headless) | C# | `sim/` |
| The single Godot boundary Node (thread + signals) | C# | `host/` |
| Presentation, UI, tools | GDScript | `ui/`, `scenes/` |
| Headless console harness | C# | `headless/` |
| Tests incl. the determinism gate | C# | `tests/` |

**One C# Node only (`SimHost`); cross-language calls happen only at tick boundaries; the sim never touches the scene tree.** The full contract is in the brief, §4 — read it before your first PR.

## File architecture

Every file is an atom — one concept per file — at `<scope>/[subscope/]<atom>.<ext>`: lowercase scopes, at most one subscope level, C# atoms named exactly for their class (Godot requires it), snake_case GDScript atoms, one JSON file per content entity.

## Build & test

The .NET 8 SDK is all you need for the sim and its tests; the Godot 4.7 .NET editor is only required to run the game itself. The **determinism gate** in `tests/` is the gate that must not regress: same seed, same behaviour, every time.

## Before opening a PR

- `dotnet test` (sim + tests) passes, determinism intact; the headless harness still runs.
- Conventional commits; one-concept PRs that match the atom structure; say how you verified.
- AI-assisted PRs welcome; stand behind every line and run the gate yourself.

## Licence

AGPL-3.0 (see [LICENSE](LICENSE)). Contributions accepted under the same terms.