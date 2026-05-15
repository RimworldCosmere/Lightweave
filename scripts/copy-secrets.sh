#!/usr/bin/env bash
# copy-secrets.sh - Copy GitHub Actions secrets from RimworldCosmere/RimworldCosmere
# to RimworldCosmere/Lightweave.
#
# Requires: gh CLI authenticated with admin:repo_hook + repo scope on both repos.
# Usage:    bash scripts/copy-secrets.sh
#
# Reads each secret value from a local source (you'll be prompted) and writes it
# to the new repo. GitHub's API does NOT allow reading existing secret values, so
# you must have them locally (in a .env, password manager, or by re-fetching).

set -euo pipefail

SOURCE_REPO="RimworldCosmere/RimworldCosmere"
TARGET_REPO="RimworldCosmere/Lightweave"

# Secrets needed by the Lightweave release workflow.
SECRETS=(
    "PAT"                  # GitHub Personal Access Token (for semantic-release)
    "UNITY_EMAIL"          # Unity license email (for AssetBundle build)
    "UNITY_PASSWORD"       # Unity license password
    "STEAM_USERNAME"       # Steam account username (workshop publisher)
    "STEAM_CONFIG_VDF_B64" # base64-encoded Steam login config.vdf (after MFA)
    "NUGET_API_KEY"        # NuGet.org API key (write scope)
)

echo "==> Verifying gh CLI auth..."
gh auth status >/dev/null

echo "==> Verifying source secrets exist on $SOURCE_REPO..."
gh secret list -R "$SOURCE_REPO" >/dev/null

echo "==> Verifying target repo $TARGET_REPO is reachable..."
gh repo view "$TARGET_REPO" >/dev/null

echo ""
echo "GitHub does not expose secret values via the API."
echo "For each secret below, paste the value (input is hidden)."
echo "Press Enter on an empty line to skip that secret."
echo ""

for name in "${SECRETS[@]}"; do
    echo -n "  $name: "
    IFS= read -r -s value
    echo ""
    if [ -z "$value" ]; then
        echo "    -> skipped"
        continue
    fi
    printf '%s' "$value" | gh secret set "$name" -R "$TARGET_REPO" --body -
    echo "    -> set on $TARGET_REPO"
done

echo ""
echo "Done. Verify with: gh secret list -R $TARGET_REPO"
