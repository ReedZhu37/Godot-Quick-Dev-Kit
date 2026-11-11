#!/usr/bin/env bash
# Build once, import, then launch five processes sharing an isolated user://.
set -euo pipefail
ROOT="$(cd "$(dirname "$0")/../.." && pwd)"
: "${GODOT_BIN:=godot}"
: "${DOTNET_BIN:=dotnet}"
# Resolve executable paths before replacing HOME (SDK shims may be home-relative).
GODOT_BIN="$(command -v "$GODOT_BIN")"
DOTNET_BIN="$(command -v "$DOTNET_BIN")"
STATE="$(mktemp -d)"
trap 'rm -rf "$STATE"' EXIT
export HOME="$STATE/home" XDG_DATA_HOME="$STATE/data" XDG_CONFIG_HOME="$STATE/config" XDG_CACHE_HOME="$STATE/cache"
mkdir -p "$HOME" "$XDG_DATA_HOME" "$XDG_CONFIG_HOME" "$XDG_CACHE_HOME"
cd "$ROOT"
restore_args=()
if [[ -n "${NUGET_CONFIG:-}" ]]; then restore_args+=(--configfile "$NUGET_CONFIG"); fi
"$DOTNET_BIN" restore "${restore_args[@]}"
"$DOTNET_BIN" build --no-restore --nologo -warnaserror
"$GODOT_BIN" --headless --path "$ROOT" --editor --import
SCENE=res://addons/quick_level_kit/csharp/tests/QuickLevelKitTests.tscn
for mode in suite save-write; do
  "$GODOT_BIN" --headless --path "$ROOT" --scene "$SCENE" -- "$mode"
done
for mode in interop-read interop-write; do
  "$GODOT_BIN" --headless --path "$ROOT" --script res://tests/quick_level_kit/progress_interop.gd -- "$mode"
done
"$GODOT_BIN" --headless --path "$ROOT" --scene "$SCENE" -- save-read
# Retained GDScript behavior is checked after path migration as well.
for test in addons/quick_level_kit/gds/tests/test_*.gd; do
  "$GODOT_BIN" --headless --path "$ROOT" --script "$test"
done
