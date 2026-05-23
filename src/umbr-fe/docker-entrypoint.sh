#!/bin/sh
set -e
# Reduce "too many open files" from file watchers in constrained containers.
ulimit -n 65536 2>/dev/null || true
exec node server.js
