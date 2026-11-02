#!/usr/bin/env bash
set -euo pipefail
image="${1:?image tag required}"
public="${2:?WebGL build directory required}"
test -s "$public/index.html"
test -d "$public/Build"
context="$(mktemp -d)"
trap 'rm -rf "$context"' EXIT
cp deploy/web/Dockerfile deploy/web/nginx.conf "$context/"
cp -a "$public" "$context/public"
docker build --label "org.opencontainers.image.source=https://github.com/Apptive-Game-Team/ArcaneCastersClient" -t "$image" "$context"
