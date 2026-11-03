#!/usr/bin/env bash
set -euo pipefail
container="$(docker run -d --rm -p 127.0.0.1::8080 "${1:?image tag required}")"
trap 'docker logs "$container"; docker stop "$container" >/dev/null' EXIT
port="$(docker port "$container" 8080/tcp)"
python3 deploy/web/check-http.py "http://$port" "${2:?WebGL build directory required}"
