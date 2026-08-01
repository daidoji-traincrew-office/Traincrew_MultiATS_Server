#!/usr/bin/env bash
# SendData_ATS ロードテスト中の Server + DB の CPU 使用率を実測する。
#
# 使い方: Doc/measure_cpu.sh [ラベル] [クライアント数] [秒数]
#
# 出力は「1 コア = 100%」換算。目標は Server + DB 合計で 200%（4 コアの 50%）以下。
set -euo pipefail

LABEL="${1:-run}"
CLIENTS="${2:-14}"
SECONDS_RUN="${3:-30}"
PORT="${PORT:-5154}"
DB_CONTAINER="${DB_CONTAINER:-database-db-1}"

SERVER_PID="$(pgrep -f 'Traincrew_MultiATS_Server.Crew/bin' | head -1)"
if [ -z "$SERVER_PID" ]; then
  echo "サーバープロセスが見つかりません" >&2
  exit 1
fi

CLK="$(getconf CLK_TCK)"

server_jiffies() {
  awk '{print $14 + $15}' "/proc/$SERVER_PID/stat"
}

# コンテナの cgroup から累積 CPU 時間(usec)を読む
db_cgroup_path() {
  local id
  id="$(docker inspect -f '{{.Id}}' "$DB_CONTAINER")"
  for p in \
    "/sys/fs/cgroup/system.slice/docker-$id.scope/cpu.stat" \
    "/sys/fs/cgroup/docker/$id/cpu.stat"; do
    [ -r "$p" ] && { echo "$p"; return; }
  done
}

DB_CPU_STAT="$(db_cgroup_path || true)"
db_usec() {
  if [ -n "$DB_CPU_STAT" ]; then
    awk '/^usage_usec/{print $2}' "$DB_CPU_STAT"
  else
    # フォールバック: ホスト上の postgres プロセスを合算
    local total=0
    for p in $(pgrep -f 'postgres:' || true); do
      [ -r "/proc/$p/stat" ] || continue
      total=$((total + $(awk '{print $14 + $15}' "/proc/$p/stat")))
    done
    echo $((total * 1000000 / CLK))
  fi
}

echo "=== CPU 計測: $LABEL (clients=$CLIENTS, ${SECONDS_RUN}s) ==="
echo "server pid=$SERVER_PID / db cpu.stat=${DB_CPU_STAT:-fallback}"

# ウォームアップ（JIT・接続プール・キャッシュを温める）
curl -s -X POST "http://localhost:$PORT/test/load?clients=$CLIENTS&seconds=5" > /dev/null
sleep 2

S0="$(server_jiffies)"; D0="$(db_usec)"; T0="$(date +%s.%N)"
curl -s --max-time $((SECONDS_RUN + 60)) -X POST \
  "http://localhost:$PORT/test/load?clients=$CLIENTS&seconds=$SECONDS_RUN" > /dev/null
T1="$(date +%s.%N)"; S1="$(server_jiffies)"; D1="$(db_usec)"

python3 - "$LABEL" "$S0" "$S1" "$D0" "$D1" "$T0" "$T1" "$CLK" "$CLIENTS" "$SECONDS_RUN" <<'PY'
import sys
label, s0, s1, d0, d1, t0, t1, clk, clients, secs = sys.argv[1:]
elapsed = float(t1) - float(t0)
server_pct = (int(s1) - int(s0)) / int(clk) / elapsed * 100
db_pct = (int(d1) - int(d0)) / 1e6 / elapsed * 100
total = server_pct + db_pct
print(f"\n--- {label} ---")
print(f"経過時間      : {elapsed:.1f} s")
print(f"Server CPU    : {server_pct:7.1f} %  (1コア=100%)")
print(f"DB CPU        : {db_pct:7.1f} %")
print(f"合計          : {total:7.1f} %  = 4コア中 {total/4:.1f} %")
print(f"判定          : {'OK (4コア50%以下)' if total <= 200 else 'NG (目標200%超過)'}")
PY

echo
echo "--- フェーズ別 wall time (上位) ---"
curl -s "http://localhost:$PORT/test/perf-stats" | python3 -c '
import json,sys
stats = json.load(sys.stdin)
rows = sorted(stats.items(), key=lambda kv: -kv[1]["totalMs"])
print(f"{"phase":<45}{"count":>8}{"mean ms":>10}{"total ms":>11}")
for name, v in rows[:20]:
    print(f"{name:<45}{v["count"]:>8}{v["meanMs"]:>10.3f}{v["totalMs"]:>11.1f}")
' || echo "(perf-stats 取得失敗)"
