#!/usr/bin/env bash
#
# Build locally and attach GHDarkMode.gha to a GitHub Release (GitHub CLI).
#
# Why local: Grasshopper/Rhino SDK + NuGet graph do not reliably build on Linux CI.
#
# Prerequisites:
#   - Rhino 8 + .NET 7 on **macOS** (same as ./scripts/build.sh), or build **GHDarkMode.gha** on Windows and run only the `gh release` steps manually if you prefer.
#   - gh auth login  (https://cli.github.com/)
#
# Usage:
#   ./scripts/release-github.sh v1.0.1
#   ./scripts/release-github.sh     # uses Version from GHDarkModeInfo.cs → tag v1.0.x
#
# Bump GHDarkModeInfo.Version and packaging/manifest.yml before releasing if needed.
# After a successful run, push the tag if it only exists locally:
#   git push origin v1.0.1
#

set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"

if [ -n "${1:-}" ]; then
  TAG="$1"
else
  INFO="$REPO_ROOT/src/GHDarkMode/GHDarkModeInfo.cs"
  VER=$(sed -n 's/^[[:space:]]*public override string Version => "\([^"]*\)".*/\1/p' "$INFO" | head -1)
  if [ -z "$VER" ]; then
    echo "Usage: $0 <tag>   example: $0 v1.0.1"
    echo "Could not read Version from $INFO"
    exit 1
  fi
  TAG="v${VER}"
  echo "No tag passed; using $TAG from GHDarkModeInfo.Version"
fi

case "$TAG" in
  v*) ;;
  *)
    echo "Error: tag must start with v (e.g. v1.0.0)"
    exit 1
    ;;
esac

if ! command -v gh >/dev/null 2>&1; then
  echo "Error: gh (GitHub CLI) not found."
  exit 1
fi

"$REPO_ROOT/scripts/build.sh"

GHA="$REPO_ROOT/dist/GHDarkMode.gha"
if [ ! -f "$GHA" ]; then
  echo "Error: missing $GHA"
  exit 1
fi

if gh release view "$TAG" >/dev/null 2>&1; then
  echo "Release $TAG exists; uploading / replacing GHDarkMode.gha ..."
  gh release upload "$TAG" "$GHA" --clobber
else
  echo "Creating release $TAG ..."
  gh release create "$TAG" "$GHA" \
    --title "GH Dark Mode $TAG" \
    --generate-notes
fi

echo "Done. If this was a new release, push the tag: git push origin $TAG"
