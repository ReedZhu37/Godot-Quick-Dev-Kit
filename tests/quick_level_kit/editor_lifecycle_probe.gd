@tool
extends EditorPlugin

# Installed only into an isolated temporary project by editor_lifecycle_tests.sh.
const SETTING := "autoload/QuickLevelKit"
const FOREIGN := "*res://addons/editor_lifecycle_probe/foreign.gd"
var _failures := 0

func _enter_tree() -> void:
	call_deferred("_run")

func _run() -> void:
	# Let startup import and dock layout settle before testing user-driven
	# enable/disable operations.
	await get_tree().create_timer(1.0).timeout
	while EditorInterface.get_resource_filesystem().is_scanning():
		await get_tree().process_frame
	await get_tree().process_frame
	var language := OS.get_environment("LEVEL_EDITOR_LANGUAGE")
	var phase := OS.get_environment("LEVEL_EDITOR_PHASE")
	var plugin := "quick_level_kit/" + language
	var expected := "*res://addons/quick_level_kit/" + language + "/runtime/"
	expected += "QuickLevelKitManager.cs" if language == "csharp" else "quick_level_kit_manager.gd"
	if phase == "enable":
		_check(not ProjectSettings.has_setting(SETTING), "enable starts without an autoload")
		EditorInterface.set_plugin_enabled(plugin, true)
		await get_tree().process_frame
		_check(EditorInterface.is_plugin_enabled(plugin), "plugin enables")
		_check(_registration() == expected, "enable registers its own manager")
		if language == "csharp":
			await _check_catalog_editing()
		# Leave enabled and quit. The next process checks the persisted registration.
	elif phase == "restart":
		_check(EditorInterface.is_plugin_enabled(plugin), "plugin is restored on editor restart")
		_check(_registration() == expected, "editor shutdown/restart preserves manager")
		EditorInterface.set_plugin_enabled(plugin, false)
		await get_tree().process_frame
		_check(not ProjectSettings.has_setting(SETTING), "disable removes owned manager")
		# Repeated cycles must leave neither a duplicate dock nor a stale autoload.
		for cycle in range(2):
			EditorInterface.set_plugin_enabled(plugin, true)
			await get_tree().process_frame
			_check(_registration() == expected, "re-enable registers manager")
			EditorInterface.set_plugin_enabled(plugin, false)
			await get_tree().process_frame
			_check(not ProjectSettings.has_setting(SETTING), "re-disable removes manager")
	elif phase == "conflict":
		# A user's unrelated autoload must survive both enable and disable.
		ProjectSettings.set_setting(SETTING, FOREIGN)
		EditorInterface.set_plugin_enabled(plugin, true)
		await get_tree().process_frame
		_check(_registration() == FOREIGN, "enable preserves foreign autoload")
		EditorInterface.set_plugin_enabled(plugin, false)
		await get_tree().process_frame
		_check(_registration() == FOREIGN, "disable preserves foreign autoload")
		ProjectSettings.clear(SETTING)
	elif phase == "languages":
		var other := "quick_level_kit/gds" if language == "csharp" else "quick_level_kit/csharp"
		EditorInterface.set_plugin_enabled(plugin, true)
		await get_tree().process_frame
		_check(_registration() == expected, "first language registers manager")
		EditorInterface.set_plugin_enabled(other, true)
		await get_tree().process_frame
		_check(_registration() == expected, "second language cannot replace manager")
		EditorInterface.set_plugin_enabled(other, false)
		await get_tree().process_frame
		_check(_registration() == expected, "second language cannot remove first manager")
		EditorInterface.set_plugin_enabled(plugin, false)
		await get_tree().process_frame
		_check(not ProjectSettings.has_setting(SETTING), "owner removes manager")
	else:
		_check(false, "unknown test phase: " + phase)
	ProjectSettings.save()
	if _failures == 0:
		print("EDITOR_LIFECYCLE_PASS ", language, " ", phase)
	get_tree().quit(_failures)

func _registration() -> String:
	var registration := str(ProjectSettings.get_setting(SETTING, ""))
	var path := registration.trim_prefix("*")
	if path.begins_with("uid://"):
		var uid := ResourceUID.text_to_id(path)
		if ResourceUID.has_id(uid):
			return ("*" if registration.begins_with("*") else "") + ResourceUID.get_id_path(uid)
	return registration

func _check(passed: bool, message: String) -> void:
	if not passed:
		_failures += 1
		push_error("EDITOR_LIFECYCLE_FAIL: " + message)


func _check_catalog_editing() -> void:
	var catalog_script := load("res://addons/quick_level_kit/csharp/runtime/definitions/QuickLevelKitCatalog.cs") as Script
	var catalog: Resource = catalog_script.new()
	var path := "res://editor_catalog_probe.tres"
	_check(ResourceSaver.save(catalog, path) == OK, "catalog saves as a real .tres")
	catalog = ResourceLoader.load(path, "", ResourceLoader.CACHE_MODE_IGNORE)
	EditorInterface.edit_resource(catalog)
	await get_tree().process_frame
	await get_tree().process_frame
	var dock := _find_script_node(get_tree().root, "res://addons/quick_level_kit/csharp/editor/QuickLevelKitDock.cs")
	_check(dock != null, "enabled plugin creates its actual catalog dock")
	if dock == null:
		return
	_check(dock.call("GetCatalog") == catalog, "selecting a catalog .tres assigns it to the plugin dock")
	var scene := PackedScene.new()
	var level_node := Node.new()
	scene.pack(level_node)
	level_node.free()
	_check(dock.call("AddLevel", &"editor_added", scene), "actual plugin dock edits selected catalog")
	_check(catalog.call("GetLevelCount") == 1, "dock edits reach the inspected catalog")
	_check(ResourceSaver.save(catalog, path) == OK, "edited catalog saves")
	var restored: Resource = ResourceLoader.load(path, "", ResourceLoader.CACHE_MODE_IGNORE)
	_check(restored.call("GetLevelCount") == 1, "dock changes survive resource reload")
	EditorInterface.edit_resource(restored)
	await get_tree().process_frame
	await get_tree().process_frame
	_check(dock.call("GetCatalog") == restored, "switching catalog selection updates the plugin dock")


func _find_script_node(node: Node, path: String) -> Node:
	var script: Script = node.get_script()
	if script != null and script.resource_path == path:
		return node
	for child in node.get_children(true):
		var found := _find_script_node(child, path)
		if found != null:
			return found
	return null
