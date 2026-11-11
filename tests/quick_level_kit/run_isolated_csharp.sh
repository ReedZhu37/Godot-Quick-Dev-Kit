#!/usr/bin/env bash
# Proves that the C# package builds/runs without any GDScript or another module.
set -euo pipefail
ROOT="$(cd "$(dirname "$0")/../.." && pwd)"
: "${GODOT_BIN:=godot}"
: "${DOTNET_BIN:=dotnet}"
STATE="$(mktemp -d)"
trap 'rm -rf "$STATE"' EXIT
PROJECT="$STATE/project"
mkdir -p "$PROJECT/addons/quick_level_kit"
cp -R "$ROOT/addons/quick_level_kit/csharp" "$PROJECT/addons/quick_level_kit/"
cat > "$PROJECT/project.godot" <<'CFG'
config_version=5
[application]
config/name="Quick Level isolated C#"
config/features=PackedStringArray("4.7", "C#", "GL Compatibility")
[dotnet]
project/assembly_name="QuickLevelStandalone"
[rendering]
renderer/rendering_method="gl_compatibility"
CFG
cat > "$PROJECT/QuickLevelStandalone.csproj" <<'XML'
<Project Sdk="Godot.NET.Sdk/4.7.2">
  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <EnableDynamicLoading>true</EnableDynamicLoading>
    <Nullable>enable</Nullable>
  </PropertyGroup>
</Project>
XML
export HOME="$STATE/home" XDG_DATA_HOME="$STATE/data" XDG_CONFIG_HOME="$STATE/config" XDG_CACHE_HOME="$STATE/cache"
mkdir -p "$HOME" "$XDG_DATA_HOME" "$XDG_CONFIG_HOME" "$XDG_CACHE_HOME"
RESTORE=()
if [[ -n "${NUGET_CONFIG:-}" ]]; then RESTORE=(--configfile "$NUGET_CONFIG"); fi
"$DOTNET_BIN" restore "$PROJECT/QuickLevelStandalone.csproj" "${RESTORE[@]}"
"$DOTNET_BIN" build "$PROJECT/QuickLevelStandalone.csproj" --no-restore -warnaserror
"$DOTNET_BIN" build "$PROJECT/QuickLevelStandalone.csproj" --no-restore -c ExportRelease -warnaserror
"$GODOT_BIN" --headless --editor --path "$PROJECT" --import
"$GODOT_BIN" --headless --path "$PROJECT" res://addons/quick_level_kit/csharp/tests/QuickLevelKitTests.tscn -- suite
# Exercise the sample with its actual C# autoload. Source repository remains unchanged.
cat >> "$PROJECT/project.godot" <<'CFG'
[autoload]
QuickLevelKit="*res://addons/quick_level_kit/csharp/runtime/QuickLevelKitManager.cs"
CFG
"$GODOT_BIN" --headless --path "$PROJECT" res://addons/quick_level_kit/csharp/examples/BasicUsage.tscn --quit-after 30
printf 'Quick Level isolated C# validation passed.\n'
