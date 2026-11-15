extends CanvasLayer
## Presentation atom: the HUD. GDScript owns the scene tree and only ever
## talks to SimHost through the tick-boundary events_ready signal (§4).

var _label: Label


func _ready() -> void:
	_label = Label.new()
	_label.position = Vector2(16, 12)
	add_child(_label)
	var sim_host := get_node("/root/SimHost")
	sim_host.events_ready.connect(_on_events_ready)


func _on_events_ready(events: Array) -> void:
	if events.is_empty():
		return
	var frame: Dictionary = events[0]
	_label.text = "tick %d  ·  roads %d" % [frame.get("tick", 0), frame.get("roads", 0)]
