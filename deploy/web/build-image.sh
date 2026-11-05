#!/usr/bin/env bash
set -euo pipefail
image="${1:?image tag required}"
public="${2:?WebGL build directory required}"
shift 2
test -s "$public/index.html"
test -d "$public/Build"
context="$(mktemp -d)"
trap 'rm -rf "$context"' EXIT
cp deploy/web/Dockerfile deploy/web/nginx.conf "$context/"
cp -a "$public" "$context/public"
args=(--label "org.opencontainers.image.source=https://github.com/Apptive-Game-Team/ArcaneCastersClient" -t "$image")
for tag in "$@"; do args+=(-t "$tag"); done
if [ "${PUSH_IMAGE:-false}" = true ]; then
    docker buildx build --platform linux/amd64,linux/arm64 --push "${args[@]}" "$context"
else
    docker build "${args[@]}" "$context"
fi
