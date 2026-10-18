#!/usr/bin/env bash
set -euo pipefail
ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
ROOT="$ROOT/implementations/csharp"
GODOT="${GODOT:-godot}"
DOTNET="${DOTNET:-dotnet}"
if [[ "$("$GODOT" --version)" != *mono* ]]; then
  echo 'Use Godot 4.7.2 .NET/Mono: set GODOT to its executable.' >&2; exit 2
fi
# Manual suite: first cover enabled-before-build editor startup.
GODOT="$GODOT" DOTNET="$DOTNET" bash "$ROOT/tests/quick_vfx_manager/run_cold_load.sh"
# Recreate a standalone project using ONLY this plugin. No symlinks or other Quick modules.
WORK="$(mktemp -d "${TMPDIR:-/tmp}/quick-vfx-test.XXXXXX")"
trap 'rm -rf "$WORK"' EXIT
mkdir -p "$WORK/addons"
cp -R "$ROOT/addons/quick_vfx_manager" "$WORK/addons/"
cp -R "$ROOT/demos" "$ROOT/tests" "$WORK/"
cp "$ROOT/project.godot" "$ROOT/QuickVfxDemo.csproj" "$WORK/"
RESTORE=()
if [[ -n "${NUGET_CONFIG:-}" ]]; then RESTORE=(--configfile "$NUGET_CONFIG"); fi
"$DOTNET" build "$WORK/QuickVfxDemo.csproj" -c Debug "${RESTORE[@]}"
"$DOTNET" build "$WORK/QuickVfxDemo.csproj" -c Release "${RESTORE[@]}"
"$GODOT" --headless --path "$WORK" --editor --quit
"$GODOT" --headless --path "$WORK" res://tests/quick_vfx_manager/VfxTests.tscn
