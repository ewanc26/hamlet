extends Node3D
## Presentation root: the one place the tick-boundary packets are fanned out to
## the view atoms. It never reads sim state directly — everything arrives in
## `events_ready`, already batched by SimHost (§4, rules 1–2).

const DEFAULT_FOCUS := Vector3(16, 0, -16)

@onready var _terrain = $Terrain
@onready var _roads = $Roads
@onready var _buildings = $Buildings
@onready var _citizens = $Citizens
@onready var _camera = $Camera
@onready var _hud = $HUD

var _grid_size := 0
var _frames := 0
var _fps_clock := 0.0
var _fps := 0.0


func _ready() -> void:
	_camera.focus_at(DEFAULT_FOCUS)
	var sim_host := get_node("/root/SimHost")
	sim_host.connect(&"EventsReady", _on_events_ready)
	_apply_stress_option(sim_host)


func _process(delta: float) -> void:
	_frames += 1
	_fps_clock += delta
	if _fps_clock >= 0.5:
		_fps = _frames / _fps_clock
		_frames = 0
		_fps_clock = 0.0
		_hud.set_fps(_fps)


func _on_events_ready(events: Array) -> void:
	if events.is_empty():
		return
	var frame: Dictionary = events[0]
	var ground := Callable(_terrain, "ground_height")

	if frame.has("world"):
		var world: Dictionary = frame["world"]
		_terrain.build(world)
		_grid_size = int(world.get("size", 0))
		_camera.focus_at(Vector3(
			float(world.get("focus_x", 16)), 0.0, -float(world.get("focus_y", 16))))

	_roads.update(frame.get("road_tiles", PackedInt32Array()), _grid_size, ground)
	_buildings.update(frame.get("households", {}), ground)
	_citizens.update(frame.get("citizens", {}), ground)
	_hud.show_frame(frame, _grid_size)


## `--stress=N` (user args, after `--`) spawns N extra settlers on the greybox
## strip: the M0 in-engine budget check (§8), 1,000 dots at 60 fps.
func _apply_stress_option(sim_host: Node) -> void:
	for argument in OS.get_cmdline_user_args():
		if argument.begins_with("--stress="):
			var count := int(argument.trim_prefix("--stress="))
			if count > 0:
				sim_host.SpawnSettlers(count)
