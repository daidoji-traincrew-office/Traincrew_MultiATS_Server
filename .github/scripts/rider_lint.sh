#!/usr/bin/env bash
# ReSharper CLI (jb inspectcode / cleanupcode) で、変更した .cs の変更行だけを検査する。
# 使い方: rider_lint.sh --sln <sln> [--exclude <dir>]... [--base <ref>] [--fix] [--format text|github]
#  - dotnet jb は「カレントのgitルート」の .config/dotnet-tools.json で解決される
#    (このスクリプトが置かれたリポジトリではなく、呼び出し側リポジトリ)。
#  - 指摘があれば exit 1、なければ 0。
set -uo pipefail

SLN=""
BASE="HEAD"
FIX=0
FORMAT="text"
EXCLUDES=()

while [ $# -gt 0 ]; do
  case "$1" in
    --sln) SLN="$2"; shift 2 ;;
    --exclude) EXCLUDES+=("$2"); shift 2 ;;
    --base) BASE="$2"; shift 2 ;;
    --fix) FIX=1; shift ;;
    --format) FORMAT="$2"; shift 2 ;;
    *) echo "unknown option: $1" >&2; exit 64 ;;
  esac
done
[ -n "$SLN" ] || { echo "--sln is required" >&2; exit 64; }

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
ROOT="$(git rev-parse --show-toplevel)" || exit 64
cd "$ROOT" || exit 64

# --- 対象ファイル: 変更 + 未追跡の *.cs (exclude除外) ---
mapfile -t TRACKED < <(git diff --name-only --diff-filter=d "$BASE" -- '*.cs')
mapfile -t UNTRACKED < <(git ls-files --others --exclude-standard -- '*.cs')
FILES=()
for f in "${TRACKED[@]}" "${UNTRACKED[@]}"; do
  [ -n "$f" ] || continue
  [ -f "$f" ] || continue
  skip=0
  for ex in "${EXCLUDES[@]+"${EXCLUDES[@]}"}"; do
    case "$f" in "$ex"*) skip=1; break ;; esac
  done
  [ "$skip" -eq 0 ] && FILES+=("$f")
done
[ "${#FILES[@]}" -gt 0 ] || exit 0

INCLUDE="$(IFS=';'; echo "${FILES[*]}")"
# キャッシュは設定ファイルの内容ハッシュでキー付けする(設定変更後に古い結果を返さないため)。
CACHE_ROOT="${RIDER_LINT_CACHES:-$HOME/.cache/rider-lint}"
KEY="$({ cat .editorconfig ./*.sln.DotSettings .config/dotnet-tools.json 2>/dev/null; echo "$SLN"; } | sha256sum | cut -c1-12)"
CACHES="$CACHE_ROOT/$KEY"
mkdir -p "$CACHES"
# 使用中キーのmtimeを更新し、14日より古いキーのディレクトリだけ削除(他リポジトリ/worktreeの並行実行を壊さない)
touch "$CACHES"
find "$CACHE_ROOT" -mindepth 1 -maxdepth 1 -type d -mtime +14 ! -name "$KEY" -exec rm -rf {} + 2>/dev/null || true
TMP="$(mktemp -d)"
trap 'rm -rf "$TMP"' EXIT

if [ "$FIX" -eq 1 ]; then
  dotnet jb cleanupcode "$SLN" --profile=ClaudeCleanup --no-build --include="$INCLUDE" \
    --caches-home="$CACHES" >"$TMP/cleanup.log" 2>&1 \
    || { echo "cleanupcode failed:" >&2; tail -20 "$TMP/cleanup.log" >&2; exit 2; }
fi

dotnet jb inspectcode "$SLN" --no-build --include="$INCLUDE" --severity=SUGGESTION \
  --format=Sarif --output="$TMP/result.sarif" --caches-home="$CACHES" >"$TMP/inspect.log" 2>&1 \
  || { echo "inspectcode failed:" >&2; tail -20 "$TMP/inspect.log" >&2; exit 2; }

python3 -I "$SCRIPT_DIR/rider_lint_filter.py" --sarif "$TMP/result.sarif" --base "$BASE" \
  --root "$ROOT" --format "$FORMAT" --untracked "${UNTRACKED[@]+"${UNTRACKED[@]}"}"
