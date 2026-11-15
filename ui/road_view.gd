extends MultiMeshInstance3D
## Road atom: one MultiMesh of thin grey slabs over the road tiles the sim
## reports. Traffic stays a colour on these slabs until M2 congestion lands.

const Palette = preload("res://ui/palette.gd")

const MAX_TILES := 4096


func _ready() -> void:
	var slab := BoxMesh.new()
	slab.size = Vector3(1.0, 0.08, 1.0)
	var multimesh := MultiMesh.new()
	multimesh.transform_format = MultiMesh.TRANSFORM_3D
	multimesh.mesh = slab
	multimesh.instance_count = MAX_TILES
	multimesh.visible_instance_count = 0
	self.multimesh = multimesh

	var material := StandardMaterial3D.new()
	material.albedo_color = Palette.ROAD
	material.roughness = 0.85
	material.metallic = 0.05
	material_override = material


## `tiles` are row-major tile indices (y × size + x), as packed by SimHost.
func update(tiles: PackedInt32Array, grid_size: int, ground: Callable) -> void:
	if multimesh == null or grid_size <= 0:
		return
	var count := mini(tiles.size(), MAX_TILES)
	for i in count:
		var tile := tiles[i]
		var x := tile % grid_size
		var y := tile / grid_size
		var origin := Vector3(x + 0.5, ground.call(x, y) + 0.04, -(y + 0.5))
		multimesh.set_instance_transform(i, Transform3D(Basis(), origin))
	multimesh.visible_instance_count = count
