#!/usr/bin/env bash
# Crawls the site to flat HTML for a static host (Vercel, Pages, any CDN).
#
# Every component renders statically and search/feedback/copy are plain
# JavaScript, so the crawled output is the whole application — there is no
# server left to miss.
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
OUT="${1:-$ROOT/dist}"
PUBLISH="$(mktemp -d)"
PORT="${PRERENDER_PORT:-5399}"
BASE="http://127.0.0.1:$PORT"

cleanup() {
  [[ -n "${APP_PID:-}" ]] && kill "$APP_PID" 2>/dev/null || true
  rm -rf "$PUBLISH"
}
trap cleanup EXIT

echo "==> publishing"
dotnet publish "$ROOT/dotnet-start.csproj" -c Release -o "$PUBLISH" -nologo -v q

echo "==> booting"
# Run from the publish directory: started inside the source tree, static web
# assets resolve back to source paths and the precompressed variants go missing.
(cd "$PUBLISH" && ASPNETCORE_ENVIRONMENT=Production ASPNETCORE_URLS="$BASE" dotnet "$PUBLISH/dotnet-start.dll" >"$PUBLISH/server.log" 2>&1) &
APP_PID=$!

for _ in $(seq 1 120); do
  curl -sf -o /dev/null "$BASE/healthz" && break
  sleep 0.5
done
curl -sf -o /dev/null "$BASE/healthz" || { echo "server did not start"; cat "$PUBLISH/server.log"; exit 1; }

echo "==> crawling"
rm -rf "$OUT"
mkdir -p "$OUT"

# Static assets first: everything the pages reference lives in wwwroot.
cp -R "$PUBLISH/wwwroot/." "$OUT/"
# The precompressed companions are for a .NET host to negotiate; a CDN does its
# own compression and would otherwise serve these as opaque files.
find "$OUT" -name '*.br' -delete
find "$OUT" -name '*.gz' -delete

count=0
while IFS= read -r route; do
  [[ -z "$route" ]] && continue
  if [[ "$route" == "/" ]]; then
    target="$OUT/index.html"
  else
    mkdir -p "$OUT${route}"
    target="$OUT${route}/index.html"
  fi
  curl -sf "$BASE$route" -o "$target"
  count=$((count + 1))
done < <(curl -sf "$BASE/sitemap.txt")

# MapStaticAssets serves fingerprinted URLs (app.<hash>.css) from virtual routes
# rather than real files, so the crawled HTML would 404 on a plain file server.
# Materialise each one next to its source, keeping the immutable-cache filename.
echo "==> materialising fingerprinted assets"
grep -rhoE '(href|src)="[^"]*\.[a-z0-9]{10}\.(css|js)"' "$OUT" \
  | sed 's/.*="//;s/"//' | sort -u \
  | while IFS= read -r asset; do
      source_file="$(echo "$asset" | sed -E 's/\.[a-z0-9]{10}\.(css|js)$/.\1/')"
      if [[ -f "$OUT/$source_file" ]]; then
        cp "$OUT/$source_file" "$OUT/$asset"
        echo "    $source_file -> $asset"
      else
        echo "    WARNING: no source for $asset" >&2
      fi
    done

# Data the client-side search fetches at runtime.
curl -sf "$BASE/search-index.json" -o "$OUT/search-index.json"
curl -sf "$BASE/sitemap.txt" -o "$OUT/sitemap.txt"

# A page that does not exist should still look like the site.
curl -sf "$BASE/docs/this-route-does-not-exist" -o "$OUT/404.html"

echo "==> $count pages -> $OUT"
du -sh "$OUT"
