#!/usr/bin/env bash
#
# Build GH Dark Mode and produce a distributable artifact in dist/.
# Run from repo root: ./scripts/build.sh
#
# Output:
#   dist/GHDarkMode.gha
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

echo "Done: $DIST_DIR/GHDarkMode.gha"
