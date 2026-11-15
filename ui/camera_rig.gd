extends Camera3D
## Camera atom: orbit/pan/zoom rig for the greybox. Presentation only — it
## never reads simulation state and never feeds anything back across the
## boundary.

const ZOOM_STEP := 1.12
const MIN_DISTANCE := 6.0
const MAX_DISTANCE := 70.0
const ORBIT_SPEED := 0.25
const PAN_SPEED := 14.0

var target := Vector3(16, 0, -16)
var distance := 26.0
var yaw_degrees := 28.0
var pitch_degrees := -42.0

var _orbiting := false


func _ready() -> void:
	_apply()


func focus_at(point: Vector3) -> void:
	target = point
	_apply()


func _process(delta: float) -> void:
	var pan := Vector3.ZERO
	if Input.is_key_pressed(KEY_W) or Input.is_key_pressed(KEY_UP):
		pan.z -= 1.0
	if Input.is_key_pressed(KEY_S) or Input.is_key_pressed(KEY_DOWN):
		pan.z += 1.0
	if Input.is_key_pressed(KEY_A) or Input.is_key_pressed(KEY_LEFT):
		pan.x -= 1.0
	if Input.is_key_pressed(KEY_D) or Input.is_key_pressed(KEY_RIGHT):
		pan.x += 1.0
	if pan != Vector3.ZERO:
		var basis := Basis(Vector3.UP, deg_to_rad(yaw_degrees))
		target += basis * pan.normalized() * PAN_SPEED * delta * (distance / 22.0)
		_apply()


func _unhandled_input(event: InputEvent) -> void:
	if event is InputEventMouseButton:
		match event.button_index:
			MOUSE_BUTTON_WHEEL_UP:
				if event.pressed:
					distance = clampf(distance / ZOOM_STEP, MIN_DISTANCE, MAX_DISTANCE)
					_apply()
			MOUSE_BUTTON_WHEEL_DOWN:
				if event.pressed:
					distance = clampf(distance * ZOOM_STEP, MIN_DISTANCE, MAX_DISTANCE)
					_apply()
			MOUSE_BUTTON_MIDDLE, MOUSE_BUTTON_RIGHT:
				_orbiting = event.pressed
	elif event is InputEventMouseMotion and _orbiting:
		yaw_degrees -= event.relative.x * ORBIT_SPEED
		pitch_degrees = clampf(pitch_degrees - event.relative.y * ORBIT_SPEED, -85.0, -18.0)
		_apply()


func _apply() -> void:
	var rotation_basis := Basis.from_euler(
		Vector3(deg_to_rad(pitch_degrees), deg_to_rad(yaw_degrees), 0.0))
	global_position = target + rotation_basis * Vector3(0.0, 0.0, distance)
	look_at(target, Vector3.UP)
