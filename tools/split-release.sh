#!/usr/bin/env bash
set -e

CORE_VER=${1:-}
ADAPTER_VER=${2:-}

if [ -n "$CORE_VER" ]; then
  git subtree split --prefix=Packages/com.zenecs.core -b split/core
  git tag -f upm-core-$CORE_VER split/core
  git push origin split/core --force
  git push origin upm-core-$CORE_VER --force
fi

if [ -n "$ADAPTER_VER" ]; then
  git subtree split --prefix=Packages/com.zenecs.adapter.unity -b split/adapter
  git tag -f upm-adapter-$ADAPTER_VER split/adapter
  git push origin split/adapter --force
  git push origin upm-adapter-$ADAPTER_VER --force
fi
