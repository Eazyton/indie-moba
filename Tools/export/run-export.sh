#!/bin/sh
# Usage: sh Tools/export/run-export.sh   (from the repository root)
set -e
HERE="$(cd "$(dirname "$0")" && pwd)"
DEMO="$HERE/../../Reference/mossbound/mossbound/nextjs_space"
CHROME="${CHROME:-C:/Program Files/Google/Chrome/Application/chrome.exe}"
mkdir -p "$HERE/dist"
node "$DEMO/node_modules/esbuild/bin/esbuild" "$HERE/export-entry.ts" --bundle --format=iife --target=es2020 \
  --alias:phaser="$HERE/phaser-stub.ts" --outfile="$HERE/dist/export.js" --log-level=warning
"$CHROME" --headless=new --disable-gpu --no-first-run --user-data-dir="$HERE/dist/chrome-profile" \
  --allow-file-access-from-files --virtual-time-budget=60000 --dump-dom "file:///$HERE/export.html" > "$HERE/dist/dump.html"
node "$HERE/extract.mjs"
