#!/usr/bin/env bash
#
# Build GH Dark Mode and install to Grasshopper Libraries.
# Run from repo root: ./scripts/build-and-install.sh
# Requires: .NET 7 SDK (dotnet in PATH). After running, restart Rhino/Grasshopper to load the updated plugin.
# Builds against the Grasshopper SDK from your installed Rhino 8 app (see docs/SDK_VERSION_AND_COMPATIBILITY.md).
#

set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"
PROJECT_DIR="$REPO_ROOT/src/GHDarkMode"
BUILD_DIR="$REPO_ROOT/build"
DIST_DIR="$REPO_ROOT/dist"

# Grasshopper Libraries folder.
# Why it looks weird:
# - On macOS, the Grasshopper plugin data lives under the Rhino user folder.
# - The "Grasshopper (...GUID...)" part is Grasshopper's plugin id folder name.
#
# Override if needed:
#   GH_LIBRARIES="/custom/path/to/Libraries" ./scripts/build-and-install.sh
#
# Auto-detect (default):
#   ~/Library/Application Support/McNeel/Rhinoceros/8.0/Plug-ins/Grasshopper*/Libraries
if [ -n "${GH_LIBRARIES:-}" ]; then
  LIBRARIES="$GH_LIBRARIES"
else
  # Pick the first matching Libraries folder if multiple are present.
  # shellcheck disable=SC2206
  CANDIDATES=( "$HOME/Library/Application Support/McNeel/Rhinoceros/8.0/Plug-ins/Grasshopper"*/Libraries )
  if [ "${#CANDIDATES[@]}" -gt 0 ] && [ -d "${CANDIDATES[0]}" ]; then
    LIBRARIES="${CANDIDATES[0]}"
  else
    echo "Error: Could not find a Grasshopper Libraries folder."
    echo "Looked for: $HOME/Library/Application Support/McNeel/Rhinoceros/8.0/Plug-ins/Grasshopper*/Libraries"
    echo "Set it explicitly, e.g.:"
    echo "  GH_LIBRARIES=\"/path/to/Libraries\" ./scripts/build-and-install.sh"
    exit 1
  fi
fi

# Prefer dotnet from PATH; fall back to common Mac install locations
if ! command -v dotnet >/dev/null 2>&1; then
  for candidate in \
    "/usr/local/share/dotnet/dotnet" \
    "/opt/homebrew/share/dotnet/dotnet" \
    "/usr/local/bin/dotnet" \
    "/opt/homebrew/bin/dotnet" \
    "$HOME/.dotnet/dotnet"
  do
    if [ -x "$candidate" ]; then
      export PATH="$(dirname "$candidate"):$PATH"
      break
    fi
  done
fi
if ! command -v dotnet >/dev/null 2>&1; then
  echo "Error: 'dotnet' not found. Install the .NET 7 SDK (or later) and ensure it is in your PATH, then run this script again."
  exit 1
fi

echo "Building GH Dark Mode (against your Rhino 8 app SDK)..."
cd "$PROJECT_DIR"
dotnet build -c Release

echo "Preparing dist/ artifact..."
mkdir -p "$DIST_DIR"
# Grasshopper loads add-ons by .gha extension only; .gha is just a .NET assembly.
cp -f "$BUILD_DIR/GHDarkMode.dll" "$DIST_DIR/GHDarkMode.gha"
cp -f "$REPO_ROOT/packaging/manifest.yml" "$DIST_DIR/manifest.yml"
cp -f "$REPO_ROOT/icons/gh-darkmode-main-a.png" "$DIST_DIR/gh-darkmode-main-a.png"

echo "Installing to Grasshopper Libraries..."
mkdir -p "$LIBRARIES"
# Grasshopper loads add-ons by .gha extension only.
cp -f "$DIST_DIR/GHDarkMode.gha" "$LIBRARIES/GHDarkMode.gha"
rm -f "$LIBRARIES/GHDarkMode.dll"

echo "Done. Restart Rhino/Grasshopper to load the updated plugin."
echo "Component: Params → Util → GH Dark Mode"
