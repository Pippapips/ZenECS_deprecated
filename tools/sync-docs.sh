#!/usr/bin/env bash
set -euo pipefail

SRC="Docs"
DST="Packages/com.zenecs.core/Documentation~"
mkdir -p "$DST"

# Map of source:destination (relative to $SRC and $DST respectively)
declare -a FILES=(
  "overview/faq.md:faq.md"
  "getting-started/quickstart-basic.md:quickstart.md"
)

# Build package index with external full-docs link
cat > "$DST/index.md" << 'EOF'
# ZenECS Core — Documentation (Package)

Start here:
- Quickstart: `quickstart.md`
- FAQ: `faq.md`

**Full docs:** https://your-docs-site.example

> This package ships a minimal doc set. The website is the source of truth.
EOF

for pair in "${FILES[@]}"; do
  IFS=":" read -r from to <<< "$pair"
  mkdir -p "$(dirname "$DST/$to")"
  cp "$SRC/$from" "$DST/$to"
done

echo "Synced ${#FILES[@]} files to $DST"
