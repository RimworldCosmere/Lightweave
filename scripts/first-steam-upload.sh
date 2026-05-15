#!/usr/bin/env bash
# first-steam-upload.sh - One-time manual first publish to Steam Workshop.
#
# semantic-release-steam handles every subsequent publish, but it requires an
# existing workshop item ID. This script does the initial upload that creates
# the workshop entry. Run this ONCE per branch (stable, beta), then write the
# resulting publishedfileid into PublishedFileIds.json under "Lightweave":
#   { "Lightweave": { "stable": "<id>", "beta": "<id>" } }
#
# After that, push to main/beta and the GH Actions workflow will publish via
# semantic-release-steam.
#
# Requires:
#   - SteamCMD installed (https://developer.valvesoftware.com/wiki/SteamCMD)
#   - $STEAM_USERNAME exported with workshop publish permissions on app 294100
#   - The mod content already built locally (run `make build && make build-assets-all`)
#
# Usage:
#   STEAM_USERNAME=<user> bash scripts/first-steam-upload.sh stable

set -euo pipefail

BRANCH_TARGET="${1:-beta}"
APP_ID="294100"  # RimWorld

if [ -z "${STEAM_USERNAME:-}" ]; then
    echo "STEAM_USERNAME env var required."
    exit 1
fi

ROOT="$(cd "$(dirname "$0")/.." && pwd)"
TEMP_DIR="$(mktemp -d)"
CONTENT_DIR="$TEMP_DIR/content"
mkdir -p "$CONTENT_DIR"

echo "==> Staging mod content from $ROOT into $CONTENT_DIR (respecting .steamignore)..."
rsync -a \
    --exclude-from="$ROOT/.steamignore" \
    --exclude='.git' \
    --exclude='node_modules' \
    "$ROOT/" "$CONTENT_DIR/"

if [ ! -f "$CONTENT_DIR/Assemblies/Lightweave.dll" ]; then
    echo "Lightweave.dll not found in staged content."
    echo "Run 'make build' first."
    exit 1
fi

PREVIEW_PATH="$ROOT/About/preview.png"
[ -f "$PREVIEW_PATH" ] || PREVIEW_PATH=""

VDF_PATH="$TEMP_DIR/workshop.vdf"
cat >"$VDF_PATH" <<EOF
"workshopitem"
{
    "appid"           "$APP_ID"
    "contentfolder"   "$CONTENT_DIR"
    "previewfile"     "$PREVIEW_PATH"
    "visibility"      "0"
    "title"           "Lightweave"
    "description"     "A composable IMGUI framework for RimWorld mods. First-time upload — to be managed by semantic-release after this."
    "changenote"      "Initial publish"
}
EOF

echo "==> Workshop VDF written to $VDF_PATH"
echo ""
cat "$VDF_PATH"
echo ""
echo "==> Uploading via steamcmd (you may be prompted for Steam Guard)..."
steamcmd \
    +login "$STEAM_USERNAME" \
    +workshop_build_item "$VDF_PATH" \
    +quit

echo ""
echo "==> Done. Find the publishedfileid in the steamcmd output above."
echo "    Add it to PublishedFileIds.json:"
echo ""
echo "    {"
echo "      \"Lightweave\": { \"$BRANCH_TARGET\": \"<publishedfileid>\" }"
echo "    }"
echo ""
echo "    Then commit + push and semantic-release-steam will take over."
