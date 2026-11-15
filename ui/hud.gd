extends CanvasLayer
## Presentation atom: the HUD. GDScript owns the scene tree and only ever
## talks to SimHost through the tick-boundary events_ready signal (§4).
## Values are text first — colour is never the only signal (§9).

const Palette = preload("res://ui/palette.gd")

const ACTIVITY_NAMES := ["idle", "sleep", "eat", "work"]

var _title: Label
var _status: Label
var _detail: Label
var _fps := 0.0
var _last_frame: Dictionary = {}


func _ready() -> void:
	_title = _make_label(Vector2(18, 14), 20)
	_title.text = "HAMLET  ·  M1 greybox"
	_status = _make_label(Vector2(18, 44), 16)
	_detail = _make_label(Vector2(18, 68), 15)
	_hint()


func set_fps(fps: float) -> void:
	_fps = fps
	_render()


func show_frame(frame: Dictionary, _grid_size: int) -> void:
	_last_frame = frame
	_render()


func _make_label(position: Vector2, font_size: int) -> Label:
	var label := Label.new()
	label.position = position
	label.add_theme_font_size_override("font_size", font_size)
	label.add_theme_color_override("font_color", Palette.BUILDING_WALL)
	label.add_theme_color_override("font_outline_color", Color(0.05, 0.09, 0.07, 0.85))
	label.add_theme_constant_override("outline_size", 5)
	add_child(label)
	return label


func _render() -> void:
	var tick := int(_last_frame.get("tick", 0))
	var minute_of_day := tick % 1440
	var clock_text := "%02d:%02d" % [minute_of_day / 60, minute_of_day % 60]
	var day := tick / 1440 + 1

	var citizens: Dictionary = _last_frame.get("citizens", {})
	var xs: PackedInt32Array = citizens.get("xs", PackedInt32Array())
	var activity: PackedByteArray = citizens.get("activity", PackedByteArray())
	var hunger: PackedByteArray = citizens.get("hunger", PackedByteArray())
	var energy: PackedByteArray = citizens.get("energy", PackedByteArray())

	_status.text = "day %d  ·  %s  ·  roads %d  ·  citizens %d  ·  %.0f fps" % [
		day,
		clock_text,
		int(_last_frame.get("roads", 0)),
		xs.size(),
		_fps,
	]

	if xs.is_empty():
		_detail.text = "waiting for the first settler…"
		return

	var leader := int(activity[0]) if not activity.is_empty() else 0
	var verb: String = ACTIVITY_NAMES[leader] if leader < ACTIVITY_NAMES.size() else "?"
	var households: Dictionary = _last_frame.get("households", {})
	var meals: PackedInt32Array = households.get("meals", PackedInt32Array())
	var pantry := int(meals[0]) if not meals.is_empty() else 0
	_detail.text = "citizen 1  ·  %s  ·  hunger %d  ·  energy %d  ·  pantry %d" % [
		verb,
		int(hunger[0]) if not hunger.is_empty() else 0,
		int(energy[0]) if not energy.is_empty() else 0,
		pantry,
	]


func _hint() -> void:
	var hint := _make_label(Vector2(18, 94), 13)
	hint.text = "WASD pan  ·  wheel zoom  ·  right-drag orbit"
