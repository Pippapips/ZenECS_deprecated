#!/usr/bin/env bash
set -euo pipefail
# Usage: scripts/cut-release.sh 1.0.1
VER="${1:-}"
if [[ -z "$VER" ]];
then echo "Usage: $0 <version>"; exit 1; fi

FILE="Docs/release/changelog.md"
DATE=$(date +%F)

awk -v ver="$VER" -v date="$DATE" '
  BEGIN{done=0}
  /^## \[Unreleased\]/ && done==0 {
    print $0 RS "";
    print "## [" ver "] - " date;
    done=1; next
  }
  { print }
' "$FILE" > "$FILE.tmp" && mv "$FILE.tmp" "$FILE"

echo "✂️  Cut Unreleased → ["$VER"] - "$DATE
