#!/usr/bin/env bash
#
# Single entry point: build the Yak package, push to the Rhino package server, then GitHub Release.
# Run from repo root after bumping <Version> in Directory.Build.props (commit/push source as you prefer).
#
# Usage:
#   ./scripts/publish.sh
#   ./scripts/publish.sh v1.2.3
#
# Prerequisites: same as yak-pack.sh + release-github.sh (Rhino 8 Yak CLI, gh auth login).
#

set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"
DIST_DIR="$REPO_ROOT/dist"

"$SCRIPT_DIR/yak-pack.sh"

VER=$("$SCRIPT_DIR/read-version.sh")
shopt -s nullglob
yaks=( "$DIST_DIR"/gh-dark-mode-"$VER"-*-any.yak )
if [ "${#yaks[@]}" -eq 0 ]; then
  echo "Error: expected dist/gh-dark-mode-${VER}-*-any.yak after yak-pack.sh"
  exit 1
fi
if [ "${#yaks[@]}" -gt 1 ]; then
  echo "Error: multiple matching .yak files for version $VER; clean dist/ and retry."
  printf '  %s\n' "${yaks[@]}"
  exit 1
fi

if [ -x "/Applications/Rhino 8.app/Contents/Resources/bin/yak" ]; then
  DEFAULT_YAK="/Applications/Rhino 8.app/Contents/Resources/bin/yak"
else
  DEFAULT_YAK=""
fi
YAK="${YAK:-$DEFAULT_YAK}"
if [ -z "$YAK" ] || [ ! -x "$YAK" ]; then
  echo "Error: Yak CLI not found. Set YAK to the yak executable path."
  exit 1
fi

echo "Publishing to Yak: ${yaks[0]}"
"$YAK" push "${yaks[0]}"

"$SCRIPT_DIR/release-github.sh" "${1:-}"

echo "Publish finished (Yak + GitHub Release). If needed: git fetch origin --tags"
