#!/usr/bin/env bash
set -euo pipefail

PKG_JSON="com.zenecs.core/package.json"
TAG="${GITHUB_REF_NAME:-}"
if [[ -z "$TAG" ]]; then
  echo "❌ GITHUB_REF_NAME is empty (are you running on tag push?)"
  exit 1
fi

TAG_VER="${TAG#v}"

if ! command -v jq >/dev/null 2>&1; then
  echo "Installing jq..."
  sudo apt-get update -y && sudo apt-get install -y jq
fi

PKG_VER=$(jq -r '.version' "$PKG_JSON")
echo "Tag: $TAG (ver=$TAG_VER)"
echo "package.json version: $PKG_VER"

if [[ "$PKG_VER" != "$TAG_VER" ]]; then
  echo "❌ Version mismatch: package.json=$PKG_VER vs tag=$TAG_VER"
  exit 2
fi
echo "✅ Version verified"
