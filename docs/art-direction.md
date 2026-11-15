# Art direction

Locked 15 November 2026. The full research lives in the project canvas
(*Hamlet Art Direction*); the visual reference is the *Hamlet Style Target*
mockup; the design document is `docs/project-brief.md` §9.

## Direction: diorama minimalism

- **Flat-shaded 3D low-poly** with hard normals; every surface carries
  2–3 facet tones, nothing smoother.
- **No textures.** All colour comes from palette materials or vertex
  colours; buildings re-skin by palette swap.
- **One directional sun (NW) + warm hemisphere ambient**, gradient sky.
  Fake bounce light with the ambient — do not build real GI.
- **Terrain colours band directly from the quantised 0–100 layers** at
  integer thresholds: identical worlds render identically.
- **Merged agent bodies render honestly**: one chunky agent shape for a
  merged household, never five fakes.
- Soft ellipse contact shadows; no dynamic shadow work beyond the sun's.

## Palette v1 — "lush green" (15 colours)

| Role | Hex |
| --- | --- |
| Deep water | `#3D6E8F` |
| Shallow water | `#7FB2C8` |
| Sand (thin coastal band) | `#E7D8A8` |
| Lowland grass (warm yellow-green) | `#A8C97A` |
| Fertile meadow (mid green) | `#7FAE5E` |
| Forest canopy (deep blue-green) | `#4E8154` |
| Foliage / crops | `#5E8C61` |
| Hill (dry bronze) | `#C2A87B` |
| Rock / ore | `#8E8E88` |
| Road | `#565B63` |
| Building wall | `#F1EBDF` |
| Roof (slate) | `#64748B` |
| Roof (terracotta accent) | `#C97C5A` |
| Trunk / wood | `#8B6F4E` |
| Citizen / UI accent | `#E2725B` |

## Rules

- Adjacent green bands differ in **value**, not just hue — elevation bands
  must stay crisp at distance.
- Water and roads stay desaturated-cool to frame the greens.
- Terracotta and coral are the only hot accents; nothing else saturates.
- Greys carry a hint of blue (`#64748B`, `#565B63`) — no pure greys.
- Seasonal tint swaps (spring / autumn) are allowed later because terrain
  colour is data-driven banded — a tint swap is a palette change, not an
  asset change.

## Pipeline

- **Greybox:** the M1 in-engine greybox is built from procedural boxes in
  GDScript (`ui/terrain_view.gd`, `ui/building_view.gd`) so the presentation
  plumbing could land without an asset dependency. Kenney's CC0 city-builder
  kit — Godot-native — replaces the boxes once M2 adds shops and firms.
- **Bespoke:** a small Blender kit — walls, pitched roofs, chimneys, trees,
  props — exported as glTF with flat shading; building variants are content
  atoms (JSON) + material/prop permutations.
- **Instancing:** every repeatable mesh (trees, crops, props, statistical
  citizens) renders through MultiMesh per district.