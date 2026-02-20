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
LIBRARIES="/Users/puya/Library/Application Support/McNeel/Rhinoceros/8.0/Plug-ins/Grasshopper (b45a29b1-4343-4035-989e-044e8580d9cf)/Libraries"

# Prefer dotnet from PATH; fall back to common Mac install locations
if ! command -v dotnet >/dev/null 2>&1; then
  for d in /usr/local/share/dotnet /opt/homebrew/share/dotnet; do
    if [ -x "$d/dotnet" ]; then
      export PATH="$d:$PATH"
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

echo "Installing to Grasshopper Libraries..."
mkdir -p "$LIBRARIES"
# Grasshopper loads add-ons by .gha extension only.
cp -f "$PROJECT_DIR/bin/GHDarkMode.dll" "$LIBRARIES/GHDarkMode.gha"
rm -f "$LIBRARIES/GHDarkMode.dll"

echo "Done. Restart Rhino/Grasshopper to load the updated plugin."
echo "Component: Params → Util → GH Dark Mode"
