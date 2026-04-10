#!/bin/bash
# Install git pre-commit hook for game-team build verification.
# Run once after clone: ./game-team/build-check/install-hooks.sh
set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(git -C "$SCRIPT_DIR" rev-parse --show-toplevel)"
HOOK_FILE="$REPO_ROOT/.git/hooks/pre-commit"

if [ -f "$HOOK_FILE" ] && ! grep -q "game-team build check" "$HOOK_FILE" 2>/dev/null; then
    echo "WARNING: pre-commit hook already exists and is not ours."
    echo "Location: $HOOK_FILE"
    echo "Backup as $HOOK_FILE.backup and retry, or merge manually."
    exit 1
fi

cat > "$HOOK_FILE" << 'HOOKEOF'
#!/bin/bash
# Pre-commit hook: game-team build check
# Installed by game-team/build-check/install-hooks.sh

# Only run if game-team C# files are staged
STAGED_CS=$(git diff --cached --name-only --diff-filter=ACMR -- \
    'game-team/scripts/*.cs' \
    'game-team/scripts/**/*.cs' \
    'game-team/unity-scripts/*.cs' \
    'game-team/unity-scripts/**/*.cs' \
    'game-team/build-check/Stubs/*.cs')

if [ -z "$STAGED_CS" ]; then
    exit 0
fi

echo "=== Pre-commit: game-team build check ==="

REPO_ROOT="$(git rev-parse --show-toplevel)"
CSPROJ="$REPO_ROOT/game-team/build-check/BogatyrskayaZastava.csproj"

if [ ! -f "$CSPROJ" ]; then
    echo "WARNING: $CSPROJ not found, skipping build check"
    exit 0
fi

if ! dotnet build "$CSPROJ" --nologo -v quiet 2>&1; then
    echo ""
    echo "BUILD FAILED. Fix errors before committing."
    echo "Run: ./game-team/build-check/build.sh"
    exit 1
fi

echo "=== Build OK ==="
HOOKEOF

chmod +x "$HOOK_FILE"
echo "Pre-commit hook installed: $HOOK_FILE"
