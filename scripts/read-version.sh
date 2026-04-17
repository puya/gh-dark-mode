#!/usr/bin/env bash
#
# Print the project's release version (MSBuild <Version> from Directory.Build.props).
# Used by release-github.sh and for quick checks. No build required.
#
# Usage: ./scripts/read-version.sh
#

set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"

cd "$REPO_ROOT/src/GHDarkMode"
dotnet msbuild -getProperty:Version -nologo -verbosity:quiet
