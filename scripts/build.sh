#!/usr/bin/env bash
#
# Build GH Dark Mode and produce a distributable artifact in dist/.
# Run from repo root: ./scripts/build.sh
#
# Output:
#   dist/GHDarkMode.gha
#   dist/manifest.yml, dist/gh-darkmode-main-a.png (Yak; see scripts/yak-pack.sh)
#

set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"
PROJECT_DIR="$REPO_ROOT/src/GHDarkMode"
BUILD_DIR="$REPO_ROOT/build"
DIST_DIR="$REPO_ROOT/dist"

echo "Building GH Dark Mode..."
cd "$PROJECT_DIR"
dotnet build -c Release

echo "Creating dist/ artifact..."
mkdir -p "$DIST_DIR"

# Grasshopper loads add-ons by .gha extension only; .gha is just a .NET assembly.
cp -f "$BUILD_DIR/GHDarkMode.dll" "$DIST_DIR/GHDarkMode.gha"

# Yak / Package Manager (canonical manifest in packaging/)
cp -f "$REPO_ROOT/packaging/manifest.yml" "$DIST_DIR/manifest.yml"
cp -f "$REPO_ROOT/icons/gh-darkmode-main-a.png" "$DIST_DIR/gh-darkmode-main-a.png"

echo "Done: $DIST_DIR/GHDarkMode.gha (+ manifest and icon for yak build)"
