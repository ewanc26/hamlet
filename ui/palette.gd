extends RefCounted
## Palette v1 "lush green" (docs/art-direction.md): the single source of the
## fifteen colours. Terrain bands straight from the quantised 0-100 layers at
## integer thresholds, so identical worlds render identically.


const DEEP_WATER := Color("3d6e8f")
const SHALLOW_WATER := Color("7fb2c8")
const SAND := Color("e7d8a8")
const LOWLAND_GRASS := Color("a8c97a")
const FERTILE_MEADOW := Color("7fae5e")
const FOREST_CANOPY := Color("4e8154")
const FOLIAGE := Color("5e8c61")
const HILL := Color("c2a87b")
const ROCK := Color("8e8e88")
const ROAD := Color("565b63")
const BUILDING_WALL := Color("f1ebdf")
const ROOF_SLATE := Color("64748b")
const ROOF_TERRACOTTA := Color("c97c5a")
const WOOD := Color("8b6f4e")
const ACCENT := Color("e2725b")

## Sky: desaturated-cool top over a warm horizon, per the art direction.
const SKY_TOP := Color("6e9bb5")
const SKY_HORIZON := Color("e7d8a8")
const GROUND_BOTTOM := Color("4e8154")
const SUN := Color("fff1d6")
const AMBIENT := Color("ffd9a8")


static func terrain_color(water: int, elevation: int) -> Color:
	if water >= 60:
		return DEEP_WATER
	if water > 0:
		return SHALLOW_WATER
	if elevation < 28:
		return SAND
	if elevation < 36:
		return LOWLAND_GRASS
	if elevation < 48:
		return FERTILE_MEADOW
	if elevation < 62:
		return FOREST_CANOPY
	if elevation < 76:
		return HILL
	return ROCK


## Citizens read as one warm accent; the activity only shifts the value, never
## the hue, so a crowd stays legible.
static func activity_color(activity: int) -> Color:
	match activity:
		1:
			return ROOF_SLATE.lightened(0.15)
		2:
			return SAND
		3:
			return ACCENT
		4:
			return SAND.darkened(0.2)
		_:
			return ACCENT.lightened(0.35)
