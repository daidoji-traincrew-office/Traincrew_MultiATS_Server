#!/usr/bin/env bash
# Claude Code の SessionStart / Stop / SubagentStop フック。変更した .cs を Rider(ReSharper CLI) で検査する。
# 引数はそのまま rider_lint.sh に渡す (例: --sln X.sln --exclude public/)。
#  - SessionStart: 開始時点で変更済み/未追跡の .cs を <cache>/<session>.dirty に記録するだけ(stdoutには何も出さない)。
#  - Stop/SubagentStop: .dirty に記録されたファイル(ユーザーが手で編集中)は --fix の対象から外し、検査のみ行う。
#    指摘は exit 2 + stderr で差し戻す。同一session/agentでの差し戻しは MAX_BLOCKS 回まで。
set -uo pipefail

MAX_BLOCKS=2
HOOK_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
LINT="$HOOK_DIR/../../.github/scripts/rider_lint.sh"
[ -x "$LINT" ] || exit 0

INPUT="$(cat)"
read_field() {
  printf '%s' "$INPUT" | python3 -I -c 'import sys,json
try:
    v = json.load(sys.stdin).get(sys.argv[1])
except Exception:
    v = None
print("" if v is None else (str(v).lower() if isinstance(v, bool) else v))' "$1" 2>/dev/null
}
sanitize() { local s="${1//[^A-Za-z0-9_-]/_}"; echo "$s"; }
SESSION="$(sanitize "$(read_field session_id)")"; [ -n "$SESSION" ] || SESSION="nosession"
AGENT="$(sanitize "$(read_field agent_id)")"
EVENT="$(read_field hook_event_name)"
SOURCE="$(read_field source)"
ACTIVE="$(read_field stop_hook_active)"

cd "${CLAUDE_PROJECT_DIR:-.}" 2>/dev/null || true
ROOT="$(git rev-parse --show-toplevel 2>/dev/null)" || exit 0
cd "$ROOT" || exit 0

CACHE="${XDG_CACHE_HOME:-$HOME/.cache}/rider-lint"
mkdir -p "$CACHE"
find "$CACHE" -maxdepth 1 -type f \( -name '*.hash' -o -name '*.blocks' -o -name '*.dirty' -o -name '*.lock' -o -name '*.notified' \) -mtime +14 -delete 2>/dev/null || true

HASH_FILE="$CACHE/$SESSION.hash"
DIRTY_FILE="$CACHE/$SESSION.dirty"
BLOCK_FILE="$CACHE/$SESSION${AGENT:+_$AGENT}.blocks"
NOTIFIED_FILE="$CACHE/$SESSION.notified"

say() { python3 -I -c 'import json,sys; print(json.dumps({"systemMessage": sys.argv[1]}, ensure_ascii=False))' "$1"; }

if [ "$EVENT" = "SessionStart" ]; then
  if [ "$SOURCE" = "startup" ] || [ "$SOURCE" = "clear" ] || [ ! -f "$DIRTY_FILE" ]; then
    {
      git -c core.quotePath=false diff --name-only --diff-filter=d HEAD -- '*.cs'
      git -c core.quotePath=false ls-files --others --exclude-standard -- '*.cs'
    } > "$DIRTY_FILE" 2>/dev/null
    find "$CACHE" -maxdepth 1 -type f \( -name "$SESSION.hash" -o -name "$SESSION.notified" -o -name "$SESSION*.blocks" \) -delete 2>/dev/null || true
  fi
  exit 0
fi

# 同時実行(メインとサブエージェントのStopなど)を直列化
if command -v flock >/dev/null 2>&1; then
  exec 9>"$CACHE/$SESSION.lock"
  flock 9
fi

[ "$ACTIVE" = "true" ] || : > "$BLOCK_FILE"

diff_hash() {
  {
    git -c core.quotePath=false diff HEAD -- '*.cs'
    git ls-files --others --exclude-standard -z -- '*.cs' | xargs -0 -r cat
  } 2>/dev/null | sha256sum | cut -d' ' -f1
}

BEFORE="$(diff_hash)"
EMPTY="$(printf '' | sha256sum | cut -d' ' -f1)"
[ "$BEFORE" = "$EMPTY" ] && exit 0
[ -f "$HASH_FILE" ] && [ "$(cat "$HASH_FILE")" = "$BEFORE" ] && exit 0

NOFIX_ARGS=()
[ -f "$DIRTY_FILE" ] && NOFIX_ARGS=(--no-fix-list "$DIRTY_FILE")

OUT="$("$LINT" "$@" "${NOFIX_ARGS[@]+"${NOFIX_ARGS[@]}"}" --base HEAD --fix --format text 2>&1)"
RC=$?
AFTER="$(diff_hash)"

if [ "$RC" -eq 0 ]; then
  echo "$AFTER" > "$HASH_FILE"
  : > "$BLOCK_FILE"
  if [ "$AFTER" != "$BEFORE" ]; then
    say "Rider検査: 指摘は cleanupcode による自動修正で解消しました(ファイルが書き換わっています)。"
  fi
  exit 0
fi

if [ "$RC" -ne 1 ]; then
  # ツール自体の失敗(jb未導入・ビルド前など)は差し戻さない。同じ内容では再実行しない
  echo "$AFTER" > "$HASH_FILE"
  if [ ! -f "$NOTIFIED_FILE" ]; then
    : > "$NOTIFIED_FILE"
    say "rider_lint 実行失敗(rc=$RC)。この内容では再実行しません: $(printf '%s' "$OUT" | tail -3 | tr '\n' ' ')"
  fi
  exit 0
fi

BLOCKS="$(cat "$BLOCK_FILE" 2>/dev/null)"
BLOCKS=$(( ${BLOCKS:-0} + 1 ))
echo "$BLOCKS" > "$BLOCK_FILE"

if [ "$BLOCKS" -gt "$MAX_BLOCKS" ]; then
  echo "$AFTER" > "$HASH_FILE"
  : > "$BLOCK_FILE"
  say "Rider検査の指摘が残っています(差し戻し上限のため終了しました):
$OUT"
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
