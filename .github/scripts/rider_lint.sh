#!/usr/bin/env bash
# ReSharper CLI (jb inspectcode / cleanupcode) で、変更した .cs の変更行だけを検査する。
# 使い方:
#   rider_lint.sh --sln <sln> [--exclude <dir>]... [--base <ref>] [--fix]
#                 [--no-fix-file <path>]... [--no-fix-list <file>] [--format text|github]
#  - dotnet jb は「カレントのgitルート」の .config/dotnet-tools.json で解決される
#    (このスクリプトが置かれたリポジトリではなく、呼び出し側リポジトリ)。
#  - sln は gitルート直下にある前提。SARIFのuri(slnからの相対パス)とgitルートからの相対パスを
#    同じ基準として突き合わせている。
#  - --no-fix-file / --no-fix-list(1行1パス)のファイルは --fix の対象から外す(検査は行う)。
#  - 指摘があれば exit 1、なければ 0、ツール自体の失敗は exit 2。
set -uo pipefail

SLN=""
BASE="HEAD"
FIX=0
FORMAT="text"
EXCLUDES=()
NOFIX=()

while [ $# -gt 0 ]; do
  case "$1" in
    --sln) SLN="$2"; shift 2 ;;
    --exclude) EXCLUDES+=("$2"); shift 2 ;;
    --base) BASE="$2"; shift 2 ;;
    --fix) FIX=1; shift ;;
    --no-fix-file) NOFIX+=("$2"); shift 2 ;;
    --no-fix-list)
      if [ -f "$2" ]; then
        while IFS= read -r l; do [ -n "$l" ] && NOFIX+=("$l"); done < "$2"
      fi
      shift 2 ;;
    --format) FORMAT="$2"; shift 2 ;;
    *) echo "unknown option: $1" >&2; exit 64 ;;
  esac
done
[ -n "$SLN" ] || { echo "--sln is required" >&2; exit 64; }

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
ROOT="$(git rev-parse --show-toplevel)" || exit 64
cd "$ROOT" || exit 64

G=(git -c core.quotePath=false)

# 日本語・スペース入りのパスを扱うため -z で受け取る
mapfile -d '' -t TRACKED < <("${G[@]}" diff -z --name-only --diff-filter=d "$BASE" -- '*.cs')
mapfile -d '' -t UNTRACKED < <("${G[@]}" ls-files -z --others --exclude-standard -- '*.cs')

# exclude は末尾スラッシュを正規化
NORM_EX=()
for ex in "${EXCLUDES[@]+"${EXCLUDES[@]}"}"; do
  ex="${ex%/}"
  [ -n "$ex" ] && NORM_EX+=("$ex")
done

FILES=()
FIXFILES=()
for f in "${TRACKED[@]+"${TRACKED[@]}"}" "${UNTRACKED[@]+"${UNTRACKED[@]}"}"; do
  [ -n "$f" ] || continue
  [ -f "$f" ] || continue
  skip=0
  for ex in "${NORM_EX[@]+"${NORM_EX[@]}"}"; do
    case "$f" in "$ex"/*) skip=1; break ;; esac
  done
  [ "$skip" -eq 0 ] || continue
  FILES+=("$f")
  nofix=0
  for n in "${NOFIX[@]+"${NOFIX[@]}"}"; do
    [ "$f" = "$n" ] && { nofix=1; break; }
  done
  [ "$nofix" -eq 0 ] && FIXFILES+=("$f")
done
[ "${#FILES[@]}" -gt 0 ] || exit 0

join_semicolon() { local IFS=';'; echo "$*"; }
INCLUDE="$(join_semicolon "${FILES[@]}")"

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

tail_log() { grep -v '^[[:space:]]*at ' "$1" | tail -20; }

if [ "$FIX" -eq 1 ] && [ "${#FIXFILES[@]}" -gt 0 ]; then
  dotnet jb cleanupcode "$SLN" --profile=ClaudeCleanup --no-build --include="$(join_semicolon "${FIXFILES[@]}")" \
    --caches-home="$CACHES" >"$TMP/cleanup.log" 2>&1 \
    || { echo "cleanupcode failed:" >&2; tail_log "$TMP/cleanup.log" >&2; exit 2; }
fi

dotnet jb inspectcode "$SLN" --no-build --include="$INCLUDE" --severity=SUGGESTION \
  --format=Sarif --output="$TMP/result.sarif" --caches-home="$CACHES" >"$TMP/inspect.log" 2>&1 \
  || { echo "inspectcode failed:" >&2; tail_log "$TMP/inspect.log" >&2; exit 2; }

python3 -I "$SCRIPT_DIR/rider_lint_filter.py" --sarif "$TMP/result.sarif" --base "$BASE" \
  --root "$ROOT" --format "$FORMAT" --untracked "${UNTRACKED[@]+"${UNTRACKED[@]}"}"
