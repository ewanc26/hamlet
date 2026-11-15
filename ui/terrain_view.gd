extends MeshInstance3D
## Terrain atom: builds the banded diorama patch from the one-time world packet
## and answers the tile heights everything else stands on. Colours come from
## Palette, terracing from the quantised elevation layer — no textures.

const Palette = preload("res://ui/palette.gd")

const WATER_LEVEL := 0.55
const HEIGHT_PER_ELEVATION := 0.07

var size := 0
var heights := PackedFloat32Array()


func build(world: Dictionary) -> void:
	size = int(world.get("size", 0))
	if size <= 0:
		return
	var elevation: PackedByteArray = world.get("elevation", PackedByteArray())
	var water: PackedByteArray = world.get("water", PackedByteArray())
	if elevation.size() != size * size or water.size() != size * size:
		push_error("terrain packet does not match its declared size")
		return

	heights = PackedFloat32Array()
	heights.resize(size * size)
	for i in heights.size():
		heights[i] = WATER_LEVEL if water[i] > 0 else float(elevation[i]) * HEIGHT_PER_ELEVATION

	var surface := SurfaceTool.new()
	surface.begin(Mesh.PRIMITIVE_TRIANGLES)
	surface.set_smooth_group(-1)
	for y in size:
		for x in size:
			var i := y * size + x
			var corners := [
				Vector3(x, _corner_height(x, y), -y),
				Vector3(x + 1, _corner_height(x + 1, y), -y),
				Vector3(x + 1, _corner_height(x + 1, y + 1), -(y + 1)),
				Vector3(x, _corner_height(x, y + 1), -(y + 1)),
			]
			var color := Palette.terrain_color(water[i], elevation[i])
			# Two triangles, hard per-tile colour, up normals: a flat-shaded
			# terrace that never blends into its neighbour.
			_tri(surface, corners[0], corners[2], corners[1], color)
			_tri(surface, corners[0], corners[3], corners[2], color)

	var material := StandardMaterial3D.new()
	material.vertex_color_use_as_albedo = true
	material.cull_mode = BaseMaterial3D.CULL_DISABLED
	material.roughness = 0.95
	surface.set_material(material)
	mesh = surface.commit()


func ground_height(x: int, y: int) -> float:
	if size <= 0:
		return 0.0
	var cx := clampi(x, 0, size - 1)
	var cy := clampi(y, 0, size - 1)
	return heights[cy * size + cx]


## Tile corners rise to the highest touching tile, so terraces meet without
## cracks instead of leaving vertical gaps at every step.
func _corner_height(corner_x: int, corner_y: int) -> float:
	var highest := 0.0
	for dy: int in [-1, 0]:
		for dx: int in [-1, 0]:
			var x: int = corner_x + dx
			var y: int = corner_y + dy
			if x < 0 or y < 0 or x >= size or y >= size:
				continue
			highest = maxf(highest, heights[y * size + x])
	return highest


func _tri(surface: SurfaceTool, a: Vector3, b: Vector3, c: Vector3, color: Color) -> void:
	for vertex in [a, b, c]:
		surface.set_normal(Vector3.UP)
		surface.set_color(color)
		surface.add_vertex(vertex)
