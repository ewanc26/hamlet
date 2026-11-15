extends MultiMeshInstance3D
## Citizen atom: one chunky body per citizen, rendered through a single
## MultiMesh — the documented pattern for thousands of instances (§5).
## Bodies are colour-coded by activity and lifted to the terrain height.

const Palette = preload("res://ui/palette.gd")

const MAX_BODIES := 4096


func _ready() -> void:
	var body := CapsuleMesh.new()
	body.radius = 0.16
	body.height = 0.55
	var multimesh := MultiMesh.new()
	multimesh.transform_format = MultiMesh.TRANSFORM_3D
	multimesh.use_colors = true
	multimesh.mesh = body
	multimesh.instance_count = MAX_BODIES
	multimesh.visible_instance_count = 0
	self.multimesh = multimesh

	var material := StandardMaterial3D.new()
	material.vertex_color_use_as_albedo = true
	material.roughness = 0.7
	material_override = material


func update(citizens: Dictionary, ground: Callable) -> void:
	if multimesh == null:
		return
	var xs: PackedInt32Array = citizens.get("xs", PackedInt32Array())
	var ys: PackedInt32Array = citizens.get("ys", PackedInt32Array())
	var activity: PackedByteArray = citizens.get("activity", PackedByteArray())
	var count := mini(xs.size(), MAX_BODIES)
	for i in count:
		var origin := Vector3(xs[i] + 0.5, ground.call(xs[i], ys[i]) + 0.3, -(ys[i] + 0.5))
		multimesh.set_instance_transform(i, Transform3D(Basis(), origin))
		multimesh.set_instance_color(i, Palette.activity_color(activity[i]))
	multimesh.visible_instance_count = count
