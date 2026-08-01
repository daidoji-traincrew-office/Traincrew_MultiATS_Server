#!/usr/bin/env bash
# ロードテスト前後の DB 書き込み統計を比較し、
# 「間引きが効いていること」と「データが実際に更新され続けていること」の両方を確認する。
set -euo pipefail

CLIENTS="${1:-14}"
SECONDS_RUN="${2:-30}"
PORT="${PORT:-5154}"
DB_CONTAINER="${DB_CONTAINER:-database-db-1}"

psql() { docker exec "$DB_CONTAINER" psql -U postgres -d postgres -tAc "$1"; }

STATS_SQL="select relname, n_tup_ins, n_tup_upd, n_tup_del
           from pg_stat_user_tables
           where relname in ('train_car_state','train_state','track_circuit_state',
                             'train_signal_state','protection_zone_state')
           order by relname;"

echo "=== ロードテスト前 ==="
psql "$STATS_SQL" | tee /tmp/db_before.txt

curl -s --max-time $((SECONDS_RUN + 60)) -X POST \
  "http://localhost:$PORT/test/load?clients=$CLIENTS&seconds=$SECONDS_RUN" > /dev/null

echo
echo "=== ロードテスト後 ==="
psql "$STATS_SQL" | tee /tmp/db_after.txt

echo
echo "=== 差分 (${SECONDS_RUN}s, ${CLIENTS} 列車) ==="
python3 - "$SECONDS_RUN" <<'PY'
import sys
secs = int(sys.argv[1])
def load(path):
    rows = {}
    for line in open(path):
        line = line.strip()
        if not line:
            continue
        name, ins, upd, dele = line.split('|')
        rows[name] = (int(ins), int(upd), int(dele))
    return rows

before, after = load('/tmp/db_before.txt'), load('/tmp/db_after.txt')
print(f"{'table':<22}{'INSERT':>9}{'UPDATE':>9}{'DELETE':>9}{'書込/秒':>10}")
for name in sorted(after):
    b = before.get(name, (0, 0, 0))
    d = [a - x for a, x in zip(after[name], b)]
    print(f"{name:<22}{d[0]:>9}{d[1]:>9}{d[2]:>9}{sum(d) / secs:>10.1f}")
PY

echo
echo "=== 登録された列車と在線 ==="
psql "select train_number, dia_number, driver_id from train_state order by train_number;"
echo "-- 在線中の軌道回路数:"
psql "select count(*) from track_circuit_state where is_short_circuit;"
echo "-- 直近の車両状態サンプル (BC圧/電流が更新されているか):"
psql "select train_state_id, index, bc_press, ampare, door_close
      from train_car_state order by train_state_id, index limit 5;"
