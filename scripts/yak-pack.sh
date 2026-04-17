#!/usr/bin/env bash
#
# Build dist/ (GHDarkMode.gha + manifest.yml + Package Manager icon) and run `yak build`.
# Writes a .yak file under dist/ for publishing to the Rhino package server.
#
# From repo root:
#   ./scripts/yak-pack.sh
#   ./scripts/yak-pack.sh --platform mac
#   ./scripts/yak-pack.sh --platform win
#   YAK=/path/to/yak ./scripts/yak-pack.sh
#
# Mac (Rhino 8): /Applications/Rhino 8.app/Contents/Resources/bin/yak
# Windows: "C:\Program Files\Rhino 8\System\Yak.exe"
#

set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"
DIST_DIR="$REPO_ROOT/dist"

"$REPO_ROOT/scripts/build.sh"

if [ -x "/Applications/Rhino 8.app/Contents/Resources/bin/yak" ]; then
  DEFAULT_YAK="/Applications/Rhino 8.app/Contents/Resources/bin/yak"
else
  DEFAULT_YAK=""
fi

YAK="${YAK:-$DEFAULT_YAK}"
if [ -z "$YAK" ] || [ ! -x "$YAK" ]; then
  echo "Error: Yak CLI not found. Install Rhino 8 or set YAK to the yak executable path."
  echo "  Example: YAK=\"/Applications/Rhino 8.app/Contents/Resources/bin/yak\" $0"
  exit 1
fi

echo "Running Yak from: $YAK"
cd "$DIST_DIR"
"$YAK" build "$@"

echo "Done. Look for *.yak in: $DIST_DIR"
