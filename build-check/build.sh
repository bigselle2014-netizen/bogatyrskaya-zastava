#!/bin/bash
# Build verification script.
# Run after every agent commit: ./build-check/build.sh
# 0 errors = code is type-safe. 0 warnings (ideally) = clean.
# Replace with Unity batch-mode build in Week 1-2 when Unity project exists.

set -e
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
echo "=== BogatyrskayaZastava build check ==="
dotnet build "$SCRIPT_DIR/BogatyrskayaZastava.csproj" --nologo -v minimal
echo "=== Done ==="
