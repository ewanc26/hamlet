extends Node3D
## Building atom: one greybox house and one workplace per household the sim
## reports — walls, a pitched roof, and nothing else until the bespoke kit
## lands. Building variants become content atoms in M2.

const Palette = preload("res://ui/palette.gd")


var _signature := ""


func update(households: Dictionary, ground: Callable) -> void:
	var home_x: PackedInt32Array = households.get("home_x", PackedInt32Array())
	var home_y: PackedInt32Array = households.get("home_y", PackedInt32Array())
	var work_x: PackedInt32Array = households.get("work_x", PackedInt32Array())
	var work_y: PackedInt32Array = households.get("work_y", PackedInt32Array())
	# Households change rarely; only rebuild the boxes when the layout does.
	var signature := "%s|%s|%s|%s" % [home_x, home_y, work_x, work_y]
	if signature == _signature:
		return
	_signature = signature
	for child in get_children():
		remove_child(child)
		child.queue_free()
	for i in home_x.size():
		_add_building(ground.call(home_x[i], home_y[i]), home_x[i], home_y[i], Palette.ROOF_TERRACOTTA)
		_add_building(ground.call(work_x[i], work_y[i]), work_x[i], work_y[i], Palette.ROOF_SLATE)


func _add_building(ground_y: float, x: int, y: int, roof_color: Color) -> void:
	var origin := Vector3(x + 0.5, ground_y, -(y + 0.5))

	var walls := MeshInstance3D.new()
	var wall_mesh := BoxMesh.new()
	wall_mesh.size = Vector3(1.8, 1.5, 1.8)
	walls.mesh = wall_mesh
	walls.position = origin + Vector3(0, 0.75, 0)
	walls.material_override = _material(Palette.BUILDING_WALL)
	add_child(walls)

	var roof := MeshInstance3D.new()
	var roof_mesh := PrismMesh.new()
	roof_mesh.size = Vector3(1.0, 1.0, 1.4)
	roof.mesh = roof_mesh
	roof.position = origin + Vector3(0, 1.5, 0)
	roof.scale = Vector3(2.0, 0.7, 1.5)
	roof.material_override = _material(roof_color)
	add_child(roof)


func _material(color: Color) -> StandardMaterial3D:
	var material := StandardMaterial3D.new()
	material.albedo_color = color
	material.roughness = 0.9
	return material
