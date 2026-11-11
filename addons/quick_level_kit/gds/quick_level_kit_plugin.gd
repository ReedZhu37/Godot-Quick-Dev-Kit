@tool
extends EditorPlugin

const AUTOLOAD_NAME := "QuickLevelKit"
const AUTOLOAD_PATH := "res://addons/quick_level_kit/gds/runtime/quick_level_kit_manager.gd"
const DOCK_SCRIPT := preload("res://addons/quick_level_kit/gds/editor/quick_level_kit_dock.gd")

var _dock: Control


func _enter_tree() -> void:
	_dock = DOCK_SCRIPT.new()
	add_control_to_dock(DOCK_SLOT_RIGHT_UL, _dock)


func _exit_tree() -> void:
	if _dock != null:
		remove_control_from_docks(_dock)
		_dock.queue_free()
		_dock = null
	# Editor shutdown/reload must preserve the project's autoload registration.


func _enable_plugin() -> void:
	if ProjectSettings.has_setting("autoload/" + AUTOLOAD_NAME):
		if not _owns_autoload():
			push_warning("Quick Level Kit GDScript: QuickLevelKit is already registered by another implementation. Disable the other language plugin first, then re-enable this plugin. The existing autoload was preserved.")
		return
	add_autoload_singleton(AUTOLOAD_NAME, AUTOLOAD_PATH)


func _disable_plugin() -> void:
	if _owns_autoload():
		remove_autoload_singleton(AUTOLOAD_NAME)


func _owns_autoload() -> bool:
	var setting := "autoload/" + AUTOLOAD_NAME
	if not ProjectSettings.has_setting(setting):
		return false
	var path := str(ProjectSettings.get_setting(setting)).trim_prefix("*")
	# Godot 4.7 stores new autoloads as UIDs; older projects may retain paths.
	if path.begins_with("uid://"):
		var uid := ResourceUID.text_to_id(path)
		if not ResourceUID.has_id(uid):
			return false
		path = ResourceUID.get_id_path(uid)
	return path == AUTOLOAD_PATH
