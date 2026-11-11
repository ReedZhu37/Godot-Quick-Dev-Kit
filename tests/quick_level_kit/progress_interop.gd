extends SceneTree
## Checks the shared ConfigFile progress schema, not cross-language Resource scripts.
const Store = preload("res://addons/quick_level_kit/gds/runtime/progress/quick_level_kit_config_file_progress_store.gd")
const Progress = preload("res://addons/quick_level_kit/gds/runtime/progress/quick_level_kit_progress.gd")
const SAVE_PATH := "user://quick_level_kit_csharp_tests.cfg"

func _init() -> void:
	var args := OS.get_cmdline_user_args()
	var store = Store.new(SAVE_PATH)
	if "interop-read" in args:
		var progress = store.load_progress()
		var state: Dictionary = progress.get_level_state(&"level_001")
		if progress.current_level_id != &"level_002" or progress.highest_unlocked_level_id != &"level_003":
			push_error("GDScript could not read C# progress identifiers")
			quit(1)
			return
		if state.get("deaths", -1) != 7 or not state.get("nested") is Dictionary or state["nested"].get("stars", -1) != 3:
			push_error("GDScript could not read C# nested level state")
			quit(1)
			return
		print("Quick Level Kit GDScript read C# progress passed.")
		quit(0)
	elif "interop-write" in args:
		var progress = Progress.new()
		progress.current_level_id = &"level_002"
		progress.highest_unlocked_level_id = &"level_003"
		progress.set_level_state(&"level_001", {&"deaths": 7, &"nested": {&"stars": 3}})
		if store.save_progress(progress) != OK:
			push_error("GDScript progress save failed")
			quit(1)
			return
		print("Quick Level Kit GDScript wrote progress for C# reader.")
		quit(0)
	else:
		push_error("Expected interop-read or interop-write")
		quit(1)
