#!/usr/bin/env bash
# Claude Code の Stop / SubagentStop フック。変更した .cs を Rider(ReSharper CLI) で検査する。
# 引数はそのまま rider_lint.sh に渡す (例: --sln X.sln --exclude public/)。
# exit 2 + stderr で Claude に差し戻す。同一sessionでの差し戻しは MAX_BLOCKS 回まで。
set -uo pipefail

MAX_BLOCKS=2
HOOK_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
LINT="$HOOK_DIR/../../.github/scripts/rider_lint.sh"
[ -x "$LINT" ] || exit 0

INPUT="$(cat)"
SESSION="$(printf '%s' "$INPUT" | python3 -I -c 'import sys,json
try: print(json.load(sys.stdin).get("session_id") or "nosession")
except Exception: print("nosession")' 2>/dev/null)"
SESSION="${SESSION//[^A-Za-z0-9_-]/_}"

ROOT="$(git rev-parse --show-toplevel 2>/dev/null)" || exit 0
cd "$ROOT" || exit 0

CACHE="${XDG_CACHE_HOME:-$HOME/.cache}/rider-lint"
mkdir -p "$CACHE"
HASH_FILE="$CACHE/$SESSION.hash"
BLOCK_FILE="$CACHE/$SESSION.blocks"

diff_hash() {
  {
    git diff HEAD -- '*.cs'
    git ls-files --others --exclude-standard -z -- '*.cs' | xargs -0 -r cat
  } 2>/dev/null | sha256sum | cut -d' ' -f1
}

BEFORE="$(diff_hash)"
EMPTY="$(printf '' | sha256sum | cut -d' ' -f1)"
[ "$BEFORE" = "$EMPTY" ] && exit 0
[ -f "$HASH_FILE" ] && [ "$(cat "$HASH_FILE")" = "$BEFORE" ] && exit 0

OUT="$("$LINT" "$@" --base HEAD --fix --format text 2>&1)"
RC=$?
AFTER="$(diff_hash)"

if [ "$RC" -eq 0 ]; then
  echo "$AFTER" > "$HASH_FILE"
  rm -f "$BLOCK_FILE"
  exit 0
fi

if [ "$RC" -ne 1 ]; then
  # ツール自体の失敗(jb未導入・ビルド前など)は差し戻さない
  MSG="rider_lint 実行失敗(rc=$RC): $(printf '%s' "$OUT" | tail -3 | tr '\n' ' ')"
  python3 -I -c 'import json,sys; print(json.dumps({"systemMessage": sys.argv[1]}, ensure_ascii=False))' "$MSG"
  exit 0
fi

BLOCKS=0
[ -f "$BLOCK_FILE" ] && BLOCKS="$(cat "$BLOCK_FILE")"
BLOCKS=$((BLOCKS + 1))
echo "$BLOCKS" > "$BLOCK_FILE"

if [ "$BLOCKS" -gt "$MAX_BLOCKS" ]; then
  echo "$AFTER" > "$HASH_FILE"
  rm -f "$BLOCK_FILE"
  MSG="Rider検査の指摘が残っています(差し戻し上限のため終了しました):
$OUT"
  python3 -I -c 'import json,sys; print(json.dumps({"systemMessage": sys.argv[1]}, ensure_ascii=False))' "$MSG"
  exit 0
fi

{
  echo "Rider(ReSharper CLI)の検査で、あなたが変更した行に指摘があります。修正してください。"
  if [ "$AFTER" != "$BEFORE" ]; then
    echo "(機械的に直せるものは cleanupcode で自動修正済みです。ファイルが書き換わっているので、編集前に必ず再Readしてください)"
  fi
  echo
  echo "$OUT"
} >&2
exit 2
