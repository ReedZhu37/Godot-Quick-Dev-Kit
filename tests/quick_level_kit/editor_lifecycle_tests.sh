#!/usr/bin/env bash
# Godot 4.7.2 .NET editor lifecycle integration, without changing the source project.
# Run after the centralized build, or omit LEVEL_EDITOR_SKIP_BUILD to build here.
set -euo pipefail
ROOT="$(cd "$(dirname "$0")/../.." && pwd)"
: "${GODOT_BIN:=godot}"
: "${DOTNET_BIN:=dotnet}"
if [[ "${LEVEL_EDITOR_SKIP_BUILD:-0}" != 1 ]]; then
  (cd "$ROOT" && "$DOTNET_BIN" build --nologo -warnaserror)
fi
STATE="$(mktemp -d)"
trap 'rm -rf "$STATE"' EXIT
PROJECT="$STATE/project"
mkdir -p "$PROJECT/addons/editor_lifecycle_probe" "$PROJECT/.godot"
cp -a "$ROOT/addons/quick_level_kit" "$PROJECT/addons/"
cp -a "$ROOT/.godot/mono" "$PROJECT/.godot/"
cp "$ROOT/tests/quick_level_kit/editor_lifecycle_probe.gd" "$PROJECT/addons/editor_lifecycle_probe/plugin.gd"
printf 'extends Node\n' > "$PROJECT/addons/editor_lifecycle_probe/foreign.gd"
cat > "$PROJECT/addons/editor_lifecycle_probe/plugin.cfg" <<'CFG'
[plugin]
name="Editor lifecycle test probe"
description="Temporary integration test driver"
author="Quick Dev Kit"
version="1.0"
script="plugin.gd"
CFG
cat > "$PROJECT/project.godot" <<'CFG'
config_version=5
[application]
config/name="Quick Level Kit editor lifecycle tests"
config/features=PackedStringArray("4.7", "C#", "GL Compatibility")
[dotnet]
project/assembly_name="QuickVfxDemo"
[rendering]
renderer/rendering_method="gl_compatibility"
CFG
export HOME="$STATE/home" XDG_DATA_HOME="$STATE/data" XDG_CONFIG_HOME="$STATE/config" XDG_CACHE_HOME="$STATE/cache"
mkdir -p "$HOME" "$XDG_DATA_HOME" "$XDG_CONFIG_HOME" "$XDG_CACHE_HOME"
# First import without the driver so all GDS global types are registered.
"$GODOT_BIN" --headless --editor --path "$PROJECT" --import
cat >> "$PROJECT/project.godot" <<'CFG'
[editor_plugins]
enabled=PackedStringArray("res://addons/editor_lifecycle_probe/plugin.cfg")
CFG
for language in csharp gds; do
  for phase in enable restart conflict languages; do
    LOG="$STATE/$language-$phase.log"
    LEVEL_EDITOR_LANGUAGE="$language" LEVEL_EDITOR_PHASE="$phase" \
      timeout 90 "$GODOT_BIN" --headless --editor --path "$PROJECT" > "$LOG" 2>&1 || { cat "$LOG"; exit 1; }
    cat "$LOG"
    grep -q "EDITOR_LIFECYCLE_PASS $language $phase" "$LOG"
    if grep -E 'SCRIPT ERROR|EDITOR_LIFECYCLE_FAIL|ERROR:' "$LOG"; then exit 1; fi
  done
done
echo 'Quick Level Kit editor lifecycle tests passed.'
