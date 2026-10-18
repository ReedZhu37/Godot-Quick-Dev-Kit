#!/usr/bin/env bash
set -euo pipefail
ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
GODOT="${GODOT:-godot}"
WORK="$(mktemp -d "${TMPDIR:-/tmp}/quick-gdscript.XXXXXX")"
trap 'rm -rf "$WORK"' EXIT
cp -R "$ROOT/implementations/gdscript/." "$WORK/"
rm -rf "$WORK/.godot"
"$GODOT" --headless --path "$WORK" --editor --quit 2>&1 | tee "$WORK/import.log"
! grep -qE "SCRIPT ERROR:|^ERROR:" "$WORK/import.log"
for test in "$WORK"/tests/*/test_*.gd; do
  echo "Testing ${test#"$WORK/"}"
  timeout 90 "$GODOT" --headless --path "$WORK" --script "res://${test#"$WORK/"}" 2>&1 | tee "$WORK/test.log"
  grep -q "tests passed\." "$WORK/test.log"
  ! grep -q "SCRIPT ERROR:" "$WORK/test.log"
done
