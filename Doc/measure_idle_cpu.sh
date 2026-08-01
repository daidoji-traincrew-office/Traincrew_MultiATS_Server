#!/usr/bin/env bash
# ロードを掛けずに、各種スケジューラだけが動いている状態の CPU を測る。
# SendData_ATS 由来の CPU と、それ以外(連動・転てつ器・TID 等)の CPU を切り分けるために使う。
set -euo pipefail

DURATION="${1:-20}"
DB_CONTAINER="${DB_CONTAINER:-database-db-1}"
SERVER_PID="$(pgrep -f 'bin/Release/net8.0/Traincrew_MultiATS_Server.Crew$' | head -1)"
CLK="$(getconf CLK_TCK)"
CID="$(docker inspect -f '{{.Id}}' "$DB_CONTAINER")"
DB_CPU_STAT="/sys/fs/cgroup/system.slice/docker-$CID.scope/cpu.stat"

s0=$(awk '{print $14 + $15}' "/proc/$SERVER_PID/stat")
d0=$(awk '/^usage_usec/{print $2}' "$DB_CPU_STAT")
sleep "$DURATION"
s1=$(awk '{print $14 + $15}' "/proc/$SERVER_PID/stat")
d1=$(awk '/^usage_usec/{print $2}' "$DB_CPU_STAT")

python3 -c "
s = ($s1 - $s0) / $CLK / $DURATION * 100
d = ($d1 - $d0) / 1e6 / $DURATION * 100
print(f'--- アイドル時 (負荷なし・スケジューラのみ / {$DURATION}s) ---')
print(f'Server CPU : {s:7.1f} %')
print(f'DB CPU     : {d:7.1f} %')
print(f'合計       : {s + d:7.1f} %  = 4コア中 {(s + d) / 4:.1f} %')
"
