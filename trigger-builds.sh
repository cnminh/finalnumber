#!/bin/bash
# Build trigger script for Final Number
# This script triggers GitHub Actions workflow_dispatch for iOS and Android builds

set -e

REPO="finalnumber/finalnumber"
WORKFLOW="build.yml"

echo "=== Final Number Build Trigger ==="
echo ""
echo "This script will trigger GitHub Actions builds for both iOS and Android."
echo ""

# Check if gh is authenticated
if ! gh auth status &>/dev/null; then
    echo "❌ GitHub CLI not authenticated. Please run:"
    echo "   gh auth login"
    echo ""
    echo "Then re-run this script."
    exit 1
fi

echo "✅ GitHub CLI authenticated"
echo ""

# Check if repo exists
if ! gh repo view "$REPO" &>/dev/null; then
    echo "❌ Repository $REPO not found."
    echo ""
    echo "To create and push the repo:"
    echo "   gh repo create finalnumber/finalnumber --public --source=. --push"
    echo ""
    exit 1
fi

echo "✅ Repository found: $REPO"
echo ""

# Trigger Android build
echo "🚀 Triggering Android build..."
gh workflow run "$WORKFLOW" \
    --repo "$REPO" \
    --ref main \
    --field buildTarget=Android \
    --field buildType=Development

# Trigger iOS build
echo "🚀 Triggering iOS build..."
gh workflow run "$WORKFLOW" \
    --repo "$REPO" \
    --ref main \
    --field buildTarget=iOS \
    --field buildType=Development

echo ""
echo "✅ Build workflows triggered!"
echo ""
echo "Monitor progress at:"
echo "   https://github.com/$REPO/actions"
echo ""
echo "Builds will be available as artifacts in ~10-15 minutes."
