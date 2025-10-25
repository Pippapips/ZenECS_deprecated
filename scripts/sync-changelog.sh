#!/usr/bin/env bash
set -euo pipefail

SOT="Docs/release/changelog.md"
PKG="com.zenecs.core/CHANGELOG.md"

if [[ ! -f "$SOT" ]]; then
  echo "❌ Missing $SOT"
  exit 1
fi

mkdir -p "$(dirname "$PKG")"
cp "$SOT" "$PKG"
echo "✅ Synced $SOT → $PKG"
