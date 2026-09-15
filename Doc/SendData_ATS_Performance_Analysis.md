# SendData_ATS パフォーマンス計測

`TrainHub.SendData_ATS` → `TrainService.CreateAtsData` は ATS クライアントから 10 回/秒で叩かれる
ホットパスで、サーバーと DB の CPU を最も強く支配している。本ドキュメントはその改善の計測記録。

計測手順は `.claude/commands/measure_senddata_ats.md` を参照。

> **⚠️ 計測間で絶対値を比較しないこと。**
> 計測 1・2 は EF Core の SQL ログ（`Logging:LogLevel:Default = Information`）が
> 有効なまま測っている。計測 3 以降はこれを `Warning` に落としているため、
> 同じコードでもベースラインが 87.9% → 62.7% と約 25pt 違う。
> 比較が意味を持つのは**同じ計測セクション内のラウンドロビン同士**だけ。
> 詳細は計測 3 の「計測条件」を参照。

---

## 計測 1：BC圧・電流値の DB 書き込み廃止 + TrainCarState 差分更新（`c963576`）

### 計測条件

| 項目 | 値 |
|------|-----|
| ベースライン | `70a94cd` — `main` (`c22e36a`) + 計測基盤のみ |
| 変更後 | `fbfecf4` — 上記 + `c963576` (BC圧・電流値の書き込み廃止 + 差分更新) |
| 計測ツール | `dotnet-trace` (cpu-sampling, 35 秒) + `SpanTimingCollector` (ActivityListener による in-process 集計) |
| 負荷条件 | `POST /test/load?clients=14&seconds=30&cars=10&rate=10` |
| 実呼び出し数 | ベースライン 3,921 / 変更後 3,984 |
| トレースファイル | `Trace/measure_baseline_main.nettrace` (37 MB) / `Trace/measure_no_carstate_write.nettrace` (41 MB) |
| DB | `Database/compose.yml` の PostgreSQL、`server_state.mode = public` |

### 変更内容

1. `UpdateTrainCarStates` が `TrainCarState.BcPress` / `Ampare` を書かなくなった。
   この 2 列の消費者は Passenger API (`ToCarState`) だけで、信号・連動・スケジューラは参照していない。
2. 残る編成構成（両数・車種・パンタ・運転台・車掌室・電動機・ドア状態）を
   `readonly record struct` の配列として `IMemoryCache` に保持し、
   前回書き込みと一致していれば `TrainCarRepository.UpdateAll` を丸ごと呼ばない（SELECT ごと消える）。

### フェーズ別比較（`SpanTimingCollector`, 平均 ms/call）

ベースラインメソッド（`IsUserBannedAsync` -0.7%、`GetNextSignalNames` -0.5%、
`GetServerModeAsyncWithoutLock` -0.3%）がほぼ動いていないので、正規化なしで直接比較できる。

| フェーズ | ベースライン | 変更後 | 差分 |
|---|---:|---:|---:|
| **CreateAtsData (total)** | **7.5040** | **5.8301** | **-22.3%** |
| **UpdateTrainCarStates** | **1.2629** | **0.0507** | **-96.0%** |
| CommitTransaction | 0.7351 | 0.1768 | -75.9% |
| RegisterOrUpdateTrainState | 0.8061 | 0.8187 | +1.6% |
| GetOperationNotificationDataByTrackCircuitIds | 0.7416 | 0.8101 | +9.2% |
| GetTrackCircuitsByTrainNumber | 0.5164 | 0.5362 | +3.8% |
| GetTrackCircuitsByNames | 0.4923 | 0.4937 | +0.3% |
| GetServerModeAsyncWithoutLock | 0.4317 | 0.4302 | -0.3% |
| SetTrackCircuitDataList | 0.4255 | 0.4342 | +2.0% |
| ClearTrackCircuitDataList | 0.4139 | 0.4094 | -1.1% |
| GetNextSignalNames | 0.3394 | 0.3376 | -0.5% |
| IsUserBannedAsync | 0.3319 | 0.3296 | -0.7% |
| IsProtectionEnabledForTrackCircuits | 0.3305 | 0.3341 | +1.1% |
| UpdateTrainSignalState | 0.3120 | 0.3072 | -1.5% |
| UpdateBougoState | 0.3028 | 0.3005 | -0.8% |
| BeginTransaction | 0.0207 | 0.0198 | -4.0% |

### CPU サンプリング（`dotnet-trace` → Speedscope, thread-ms/call）

| フェーズ | ベースライン | 変更後 | 差分 |
|---|---:|---:|---:|
| CreateAtsData (total) | 1.3013 | 1.2170 | -6.5% |
| UpdateTrainCarStates | 0.1602 | 0.0065 | -95.9% |
| TrainCarRepository.UpdateAll | 0.3285 | 0.0103 | -96.9% |
| IsUserBannedAsync (ベースライン) | 0.1830 | 0.1867 | +2.0% |
| GetNextSignalNames (ベースライン) | 0.1162 | 0.1303 | +12.1% |
| thread pool idle (WaitNative) | 48.5% | 68.2% | — |

スレッドプールのアイドル率が 48.5% → 68.2% に上がっており、サーバー全体の仕事が減っている。
CPU サンプリングは DB 待ちを拾わないため `CreateAtsData` 全体の改善幅（-6.5%）は
wall time の -22.3% より小さく出る。実効的な改善は wall time の方を見るのが正しい。

### DB 書き込み量（`pg_stat_user_tables` 差分, 30 秒間）

| テーブル | ベースライン UPDATE | 変更後 UPDATE |
|---|---:|---:|
| **train_car_state** | **38,350 (1,278/秒)** | **1,400 (47/秒)** |
| track_circuit_state | 280 | 280 |

**-96.3%**。残る 47/秒はロードテストが 3 秒ごとにドア状態を反転させるため
（14 列車 × 10 両 ÷ 3 秒 ≈ 47）で、差分判定が正しく効いている証拠でもある。
実運用では編成構成が変わるのは乗務開始時と増解結時だけなので、さらに少なくなる。

### 考察

- `UpdateTrainCarStates` が 1.26 ms → 0.05 ms になり、`CreateAtsData` 全体の 22% が消えた。
  1 列車あたり毎秒 (両数 × 10) 本発生していた UPDATE がほぼ無くなったことが直接効いている。
- `CommitTransaction` の -75.9% は副次効果。`SaveChangesAsync` で
  flush する変更エンティティが 10 行/call から 0 になったため。
- `GetOperationNotificationDataByTrackCircuitIds` の +9.2% は、
  DB の負荷が下がって相対的に他フェーズの比重が上がったことによる見かけの増加と考えられる
  （絶対値で 0.07 ms、ベースラインメソッドのばらつきと同程度）。

### 副作用

- `train_car_state.bc_press` / `ampare` 列は残るが、以後つねに 0 になる。
  計測後の DB でも全行 0 であることを確認済み。
- したがって Passenger API (`PassengerController.GetTrainInfoAsync`) が返す
  `CarState.BC_Press` / `Ampare` は常に 0 になる。
  DTO と `Traincrew_MultiATS_Server.Common/Models/Passenger.cs` のスキーマ仕様は変えていない。
- 表示ラグは発生しない。編成構成の変化（ドア開閉を含む）は従来どおり即座に DB へ反映される。

### Server / DB の CPU 内訳（`Doc/measure_cpu.sh`）

「保存先を Redis 等のメモリストアに替えれば更に下がるのか」を判断するために、
同じ負荷条件で Crew プロセスと PostgreSQL コンテナの CPU を別々に実測した。
値は 1 コア = 100% 換算（計測機は 24 コア）。

| | Server CPU | DB CPU | 合計 |
|---|---:|---:|---:|
| ベースライン | 62.3% | 35.7% | 98.0% |
| 変更後 | 54.9% | 30.1% | 84.9% |
| **差** | **-7.4pt** | **-5.6pt** | **-13.1pt (-13.4%)** |

わかったこと:

1. **PostgreSQL はもともと主たる消費者ではない。** 合計 98% のうち DB は 35.7%、
   Crew プロセスが 62.3%。保存先の選択は最初から小さい方の皿の話だった。
2. **行単位 UPDATE は想定より安い。** 毎秒 1,231 本（38,350 → 1,400）の UPDATE を消して
   DB CPU の節約は 5.6pt にとどまる。1 行あたり約 4.5 µs の DB CPU に相当する。
3. **削れた 13.1pt の内訳は Crew 側 7.4 : DB 側 5.6。**
   節約された wall time 1.18 ms/call のうち Crew の CPU は約 0.57 ms/call 相当で、
   残りは DB 待ち。つまり今回の効果の過半は「EF Core の行単位更新をやめたこと」であって、
   「PostgreSQL に書かなくなったこと」ではない。

### メモリストア（Redis 等）への移行についての結論

**現時点では見合わない。**

- 車両状態の書き込みコストを完全にゼロにしても、上記のとおり削れるのは 5.6pt であり、
  それは本変更で既に回収済み。
- 残る DB CPU 30.1% は `GetTrackCircuitsBy*` や `RegisterOrUpdateTrainState` などの
  他クエリであり、電流計・BC 圧とは無関係。Redis はここには一切効かない。
- Redis 導入のコストは `Database/compose.yml` とルートの 4 サービス構成の両方、
  IT の `WebApplicationFactory` フィクスチャ、self-hosted runner の CI、接続 Secrets に及ぶ。

将来 BC 圧・電流値を復活させる場合は、**PostgreSQL のまま「1 列車 1 行 (jsonb) + UNLOGGED テーブル」**
で十分と見込まれる。10 行 → 1 行で書き込み回数が 1/10、UNLOGGED で WAL が消えるため、
上で測った 5.6pt よりさらに一桁下に収まるはず。
メモリストアを検討する価値が出るのは、在線位置など他の高頻度・揮発状態もまとめて逃がす
構想が立ったときで、そのときは基盤の判断として測り直すこと。

### 次の改善候補（この計測時点）

| 優先度 | フェーズ | 平均 ms/call | 備考 |
|---|---|---:|---|
| 高 | RegisterOrUpdateTrainState | 0.8187 | 変化が無い場合の UPDATE 抑止が効きそう |
| 高 | GetOperationNotificationDataByTrackCircuitIds | 0.8101 | 運転告知器はほぼ不変。キャッシュ余地あり |
| 中 | GetTrackCircuitsByTrainNumber / ByNames | 0.5362 / 0.4937 | 不変マスタとのマスタ/状態分離 |
| 中 | GetServerModeAsyncWithoutLock | 0.4302 | 毎 call の SELECT。短 TTL キャッシュ |
| 中 | SetTrackCircuitDataList / Clear | 0.4342 / 0.4094 | 在線に変化が無いときの UPDATE 抑止 |

いずれも Crew プロセス側の CPU が支配的（上記 CPU 内訳を参照）なので、
DB アクセスを減らすだけでなく EF Core の使い方（行単位更新・エンティティ materialize）
を見直す方が効く可能性が高い。

### 計測上の注意

- `TestController.LoadTest` は BC圧・電流値を毎回乱数にし、ドア状態を `moveIntervalMs`(既定 3000ms)
  ごとに反転させる。つまり差分判定は「常にスキップ」ではなく 3 秒に 1 回書く形で計測されている。
- ベースライン・変更後とも、本計測の前に 5 秒のウォームアップ負荷をかけている。
- `dotnet-trace` の対象は必ず `pgrep -f "Traincrew_MultiATS_Server.Crew/bin/Release"` の
  バイナリ PID を使うこと。`dotnet run` ラッパーを掴むとアプリのフレームが出ない。

---

## 計測 2：サーバ状態キャッシュ + 在線変化なし時の UPDATE 抑止（`a6f3f67` / `d11b846`）

計測 1 の「次の改善候補」表に残っていた中位 2 項目を `perf/server-state-cache` から
チェリーピックし、**それぞれ単独で適用した場合**と**両方入れた場合**の CPU を実測した。

| ラベル | 内容 | コミット |
|---|---|---|
| **A** | サーバ状態系 getter（ServerMode / TimeOffset / SelectedDiagramId / IsUserBanned）を `IMemoryCache` TTL 10 秒でキャッシュ。setter で `cache.Remove` して即時無効化。`CreateAtsData` は `GetServerModeCachedAsync` を呼ぶ | `a6f3f67`（`c186124` + `128579d` + UT 修正 2 件の squash） |
| **B** | `SetTrackCircuitDataList` / `ClearTrackCircuitDataList` が対象 0 件なら早期 return | `d11b846`（`012b4ac`） |

### 計測条件

| 項目 | 値 |
|------|-----|
| ベースライン | `7c1dd02` |
| 計測ツール | `Doc/measure_cpu.sh`（`/proc/<pid>/stat` と DB コンテナの cgroup `cpu.stat`）+ `SpanTimingCollector` |
| 負荷条件 | `POST /test/load?clients=14&seconds=30&cars=10&rate=10`（前段に 5 秒のウォームアップ） |
| 実呼び出し数 | 約 4,000 /run |
| 起動条件 | Release ビルドのバイナリを直接起動（`ASPNETCORE_ENVIRONMENT=Development`, port 5154） |
| DB | `Database/compose.yml` の PostgreSQL、`server_state.mode = public` |
| 試行 | 4 条件を**ラウンドロビンで交互に** 5 ラウンド、中央値を採用 |

#### インターリーブが必要だった理由

最初は条件ごとに 3 回連続で測ったが、**測定順にサーバ CPU が単調に悪化**した。
対照としてベースラインを最後に測り直すと合計 84.9% → 94.4% と 9.5pt も上がっており、
「条件を変えた効果」と「時間方向のドリフト」が分離できていなかった
（サーバ CPU が 54% と 63% の二峰に振れる。DB CPU は常に安定していた）。

そこで 4 条件のバイナリを `Traincrew_MultiATS_Server.Crew/bin/variant_{base,a,b,ab}` に
別々にビルドし、1 run ごとにプロセスを入れ替えてラウンドロビンで測る方式に変えた。
ドリフトが全条件に均等に配分され、ベースラインのばらつきは 87.4〜89.2%（1.8pt 幅）に収まった。
**以後の CPU 比較はこの方式で測ること。条件ごとの連続測定は信用できない。**

### CPU 使用率（1 コア = 100% 換算、中央値と 5 run のレンジ）

| 条件 | Server CPU | DB CPU | 合計 |
|---|---:|---:|---:|
| ベースライン | 56.3 (55.9–56.9) | 32.0 (31.1–32.3) | **87.9** (87.4–89.2) |
| **A のみ** | 49.6 (47.7–56.3) | 29.4 (28.6–29.6) | **79.3** (76.4–85.7) |
| **B のみ** | 52.1 (50.2–53.6) | 27.4 (27.0–27.6) | **79.4** (77.6–81.0) |
| **A + B** | 44.6 (43.2–48.5) | 25.0 (23.7–25.5) | **69.9** (68.2–72.9) |

| 条件 | ΔServer | ΔDB | Δ合計 | 改善率 |
|---|---:|---:|---:|---:|
| A のみ | -6.7pt | -2.6pt | **-8.6pt** | **-9.8%** |
| B のみ | -4.2pt | -4.6pt | **-8.5pt** | **-9.7%** |
| A + B | -11.7pt | -7.0pt | **-18.0pt** | **-20.5%** |

単独の改善幅の和が -17.1pt、両方入れた実測が -18.0pt でほぼ加算的。
2 つの改善は互いに干渉していない。

### フェーズ別 wall time（`SpanTimingCollector`, ms/call）

| フェーズ | ベースライン | A のみ | B のみ | A + B |
|---|---:|---:|---:|---:|
| `GetServerModeAsyncWithoutLock` → `GetServerModeCachedAsync` | 0.3985 | **0.0062** | 0.3952 | **0.0066** |
| `IsUserBannedAsync` | 0.3188 | **0.0076** | 0.3156 | **0.0073** |
| `SetTrackCircuitDataList` | 0.4185 | 0.4199 | **0.0183** | **0.0180** |
| `ClearTrackCircuitDataList` | 0.3901 | 0.3881 | **0.0163** | **0.0169** |
| `CreateAtsData`（全体） | 5.6537 | 4.8490 | 4.8302 | **4.0561** |

`CreateAtsData` 全体で **5.65 → 4.06 ms/call (-28.2%)**。
A が -0.80 ms、B が -0.82 ms を削り、合計 -1.60 ms で、こちらも加算的。

### B の節約は「行の書き込み」ではなく「文の発行」

`pg_stat_user_tables` の差分を取ると、`track_circuit_state` の UPDATE 行数は
ベースラインも A+B も **308 行 / 30 秒で変化なし**だった。

| テーブル | ベースライン UPDATE | A+B UPDATE |
|---|---:|---:|
| track_circuit_state | 308 | 308 |
| train_car_state | 1,400 | 1,400 |

`n_tup_upd` は「実際に更新された行数」なので、**0 行にヒットする UPDATE 文は
もともとカウントされていなかった**。つまり B が消したのは書き込みそのものではなく、
1 call あたり 2 本の「何も更新しない UPDATE 文」の
**EF Core 側の `ExecuteUpdate` 組み立て + ラウンドトリップ + PostgreSQL 側の parse/plan** である。
それだけで DB CPU が 4.6pt 落ちる。空振りのクエリは想像以上に高い。

（`pg_stat_statements` はこの DB に導入していないため、文単位の回数は取れていない。）

### 副作用

- **TTL 10 秒のラグ**: setter は同一プロセス内では `cache.Remove` で即時無効化するが、
  Crew と Passenger は別プロセスで同じ DB を見ている。Crew 側で BAN / 時刻オフセット /
  選択ダイヤを変えても、Passenger 側のキャッシュは最大 10 秒古いままになる。
- `ServerMode` については `PassengerController.GetServerModeAsync` が非キャッシュの
  `GetServerModeAsyncWithoutLock` を呼ぶので影響しない。
  `ServerModeScheduler` / `CommanderTableHub` もミューテックス付きの `GetServerModeAsync`
  のままなので、運用上のモード切替の即時性は保たれている。
- `IsUserBannedAsync` もキャッシュ対象に入った。計測 1 まで同メソッドを
  「コード変更のないベースラインメソッド」として `dotnet-trace` の
  スレッド数正規化に使っていたが、**以後その役は `GetNextSignalNames` に移す**
  （本計測でも 0.3276 → 0.3305 ms/call でほぼ動いていない）。
- B は在線に変化がある call では従来どおり UPDATE を発行する。在線更新のログ
  （`[在線更新]`）も従来どおり出る。

### 検証

- UT 132 件成功（A で追加された `ServerServiceCacheTest` / `BannedUserServiceCacheTest` の 10 件を含む）
- IT 19 件成功・2 スキップ（スキップはスナップショット生成用で従来どおり）

### 次の改善候補（この計測時点）

| 優先度 | フェーズ | 平均 ms/call | 備考 |
|---|---|---:|---|
| 高 | GetOperationNotificationDataByTrackCircuitIds | 0.7715 | 運転告知器はほぼ不変。`perf/server-state-cache` の `f450510` にキャッシュ実装あり |
| 高 | GetTrackCircuitsByTrainNumber / ByNames | 0.6330 / 0.4883 | `986333d` がマスタ/状態分離 + 1 クエリ化で 3.19 → 1.15 ms を報告 |
| 高 | RegisterOrUpdateTrainState | 0.5846 | 変化が無い場合の UPDATE 抑止 |
| 中 | IsProtectionEnabledForTrackCircuits / UpdateBougoState | 0.3325 / 0.2925 | `34db59d` が防護無線のキャッシュ化 + 未発報時 DELETE 停止 |
| 中 | UpdateTrainSignalState | 0.3015 | `81d2b99` が可視信号機に変化が無い場合のスキップ |

本計測で「空振りのクエリを 1 本消すと DB CPU が 2pt 級で落ちる」ことが分かったので、
残りの候補も**行数ではなく文の本数**を基準に優先度を付けるのが正しい。

## 計測 3：運転告知器・防護無線・可視信号機のキャッシュ化（`02d22ad` / `4c31597` / `ca1e287`）

計測 2 の「次の改善候補」表に残っていた 3 項目を `perf/server-state-cache` から
チェリーピックし、**それぞれ単独で適用した場合**と**3 つまとめて入れた場合**の CPU を実測した。

| ラベル | 内容 | コミット |
|---|---|---|
| **C** | 運転告知器の「軌道回路↔告知器の対応関係」を TTL 無しで、「告知器の状態」を TTL 10 秒 + 明示無効化でキャッシュ。ホットパスの 4 テーブル JOIN を消す。`SetNoneWhereKaijoOrTorikeshiAndSpendMuchTime` は対象 0 件なら UPDATE を発行しない | `02d22ad`（`f450510`） |
| **D** | 発報中の防護無線を全件キャッシュし、発報判定を in-memory で行う。`UpdateBougoState` はキャッシュ上の現状と一致していれば DB を触らない（＝**未発報の列車に対する毎コールの DELETE が消える**） | `4c31597`（`34db59d`） |
| **E** | `UpdateTrainSignalState` を `TrainService` の private メソッドに切り出し、前回書き込んだ可視信号機と同じなら既存状態の SELECT ごとスキップ | `ca1e287`（`81d2b99`） |

### 計測条件

| 項目 | 値 |
|------|-----|
| ベースライン | `88d075b` |
| 計測ツール | `/proc/<pid>/stat` と DB コンテナの cgroup `cpu.stat` + `SpanTimingCollector` |
| 負荷条件 | `POST /test/load?clients=14&seconds=30&cars=10&rate=10`（前段に 5 秒のウォームアップ） |
| 実呼び出し数 | 4,056〜4,118 /run（条件間の差は 1.5% 以内） |
| 起動条件 | Release ビルドのバイナリを直接起動（`ASPNETCORE_ENVIRONMENT=Development`, port 5154） |
| DB | `Database/compose.yml` の PostgreSQL、`server_state.mode = public` |
| 試行 | 5 条件を**ラウンドロビンで交互に** 5 ラウンド、中央値を採用 |

#### ⚠️ 本計測から EF Core の SQL ログを落とした。計測 1・2 とは絶対値を比較できない

`appsettings.Development.json` の `Logging:LogLevel:Default` が `Information` のため、
既定では **EF Core が発行した SQL を 1 本ずつ全文フォーマットしてログ出力する**。
計測 1・2 はこれが有効なまま測っていた。

これは「クエリを減らす」系の施策を測るときに**効果を過大評価する**。
クエリを 1 本消すと、DB の実コストだけでなく**そのクエリのログ整形コストも同時に消える**ためで、
本来サーバ側のログ整形である分まで「DB を軽くした効果」として計上されてしまう。

そこで本計測では全 variant に同条件で以下を渡し、SQL ログを止めた。

```
Logging__LogLevel__Microsoft.EntityFrameworkCore.Database.Command=Warning
```

同じ `88d075b` 相当のベースラインが計測 2 の **87.9%** に対し本計測では **62.7%** になっている。
差の約 25pt はほぼ全てこの SQL ログである。**計測 1・2 の絶対値および改善幅（pt）は
ログ整形コストを含んだ数字なので、本計測の数字と直接比較してはいけない。**
本計測はベースラインを同じラウンドロビン内で測り直しているので、計測 3 の中では整合している。

### CPU 使用率（1 コア = 100% 換算、中央値と 5 run のレンジ）

| 条件 | Server CPU | DB CPU | 合計 |
|---|---:|---:|---:|
| ベースライン | 38.6 (37.9–39.3) | 24.2 (23.3–25.1) | **62.7** (61.2–64.4) |
| **C のみ**（運転告知器） | 35.1 (34.6–35.9) | 16.5 (16.1–17.0) | **51.5** (50.8–52.8) |
| **D のみ**（防護無線） | 34.7 (32.8–37.8) | 21.8 (21.4–22.3) | **56.7** (54.2–59.5) |
| **E のみ**（可視信号機） | 36.7 (36.5–39.5) | 23.6 (23.1–24.3) | **60.5** (59.7–63.1) |
| **C + D + E** | 27.3 (26.7–27.6) | 13.8 (13.4–13.9) | **41.2** (40.2–41.4) |

| 条件 | ΔServer | ΔDB | Δ合計 | 改善率 |
|---|---:|---:|---:|---:|
| C のみ | -3.5pt | -7.7pt | **-11.2pt** | **-17.9%** |
| D のみ | -3.9pt | -2.4pt | **-6.0pt** | **-9.6%** |
| E のみ | -1.9pt | -0.6pt | **-2.2pt** | **-3.5%** |
| **C + D + E** | **-11.3pt** | **-10.4pt** | **-21.5pt** | **-34.3%** |

単独の改善幅の和が -19.4pt、3 つ入れた実測が -21.5pt で、**2.1pt ほど超加算**。
計測 2 がほぼ完全な加算だったのに対し、今回はまとめて入れた方がわずかに得をしている。
クエリが減って接続プールの待ちと GC 圧が下がる分と思われるが、
差は 1 run のレンジ（ベースラインで 3.2pt 幅）と同程度なので、断定はできない。

**E の扱いに注意。** E 単独の -2.2pt は、ベースラインのレンジ（61.2–64.4）と
E のレンジ（59.7–63.1）が重なっており、5 run の中央値では分離できているものの
**単独の CPU 効果としては主張が弱い**。後述のとおり wall time は確実に落ちている
（0.2732 → 0.0413 ms/call）ので、施策自体は効いている。

### フェーズ別 wall time（`SpanTimingCollector`, ms/call, 5 ラウンドの中央値）

| フェーズ | ベースライン | C のみ | D のみ | E のみ | C+D+E |
|---|---:|---:|---:|---:|---:|
| `GetOperationNotificationDataByTrackCircuitIds` | 0.8808 | **0.0036** | 0.8288 | 0.8469 | **0.0024** |
| `IsProtectionEnabledForTrackCircuits` | 0.3132 | 0.3137 | **0.0042** | 0.3104 | **0.0042** |
| `UpdateBougoState` | 0.2753 | 0.2770 | **0.0019** | 0.2725 | **0.0018** |
| `UpdateTrainSignalState` | 0.2732 | 0.2723 | 0.2764 | **0.0413** | **0.0413** |
| `GetNextSignalNames`（対照） | 0.3117 | 0.3095 | 0.3087 | 0.3145 | 0.3156 |
| `CreateAtsData`（全体） | 3.9733 | 3.1393 | 3.3695 | 3.7295 | **2.3639** |

`CreateAtsData` 全体で **3.97 → 2.36 ms/call (-40.5%)**。
C が -0.83 ms、D が -0.60 ms、E が -0.24 ms を削り、和は -1.67 ms、
まとめて入れた実測は -1.61 ms でこちらはほぼ一致している。

対照に置いた `GetNextSignalNames` は全 5 条件で 0.3087〜0.3156（振れ幅 2.2%）に収まっており、
条件間でスレッド数や負荷の偏りが出ていないことを確認できた。

### 消えたのはやはり「文の本数」

計測 2 で得た「空振りのクエリを 1 本消すと DB CPU が 2pt 級で落ちる」は今回も成立した。

- **D** が消したのは `UpdateBougoState` の `DisableProtection`、すなわち
  `DELETE FROM protection_zone_state WHERE train_number = @p` である。
  ロードテストは全クライアントが `BougoState = false` なので、この DELETE は
  **毎コール 1 行も消さずに往復していた**。`n_tup_del` は 0 行の DELETE を数えないため
  この削減は行数の統計には一切現れないが、DB CPU は 2.4pt 落ちている。
- **C** の -7.7pt が突出しているのは、消したのが 4 テーブル JOIN という
  「重い 1 本」だったため。文の本数だけでなく 1 本あたりの重さも効く。

### 副作用

- **キャッシュはプロセス内**。Crew と Passenger は別プロセスで同じ DB を見るため、
  一方の書き込みが他方に伝わるまで最大 10 秒かかる。計測 2 の `ServerMode` / BAN と
  同じ性質で、既に受け入れている制約。
  - ただし**運転告知器については実害が無い**。告知器を書くのは `CommanderTableHub`（Crew）だけで、
    キャッシュを読むのも `GetOperationNotificationDataByTrackCircuitIds`（ATS = Crew）だけ。
    Passenger が使う `GetOperationNotificationData()` は非キャッシュの `GetAllDisplay()` のまま。
  - 防護無線も書き込みは全て `ProtectionService` 経由で、Crew 内で明示的に無効化される。
- **`operationnotification:topology` だけは TTL 無し。** 軌道回路↔告知器の対応は
  初期化時に投入されるマスタなので不変だが、実行中に DB を直接書き換えても反映されない。
  変えたらプロセス再起動が要る。
- **告知器の判定ロジックが変わった。** 従来は EF の `Select` が軌道回路ごとに 1 行返すため
  `displays.Count != 1` が事実上「入力軌道回路がちょうど 1 本」を要求していた。
  新実装は「全入力軌道回路が同一告知器に属し、かつ告知器の軌道回路集合と入力集合が一致」で判定する。
  現行データでは全 48 告知器が軌道回路 1 本ずつなので挙動は変わらないが、
  **2 本以上持つ告知器を追加すると挙動が変わる**（新実装の方が正しい）。
- **`trackCircuits` が空のとき `Min()`/`Max()` で例外になる不具合を D で併せて修正した。**
- キャッシュ化により以下が未使用になったが、元コミットどおり残してある。
  - `IOperationNotificationRepository.GetDisplayByTrackCircuitIds`
  - `IProtectionRepository.IsProtectionEnabled`

### 検証

- UT 135 件成功
- IT 19 件成功・2 スキップ（スキップはスナップショット生成用で従来どおり）

### 次の改善候補（この計測時点）

| 優先度 | フェーズ | 平均 ms/call | 備考 |
|---|---|---:|---|
| 高 | GetTrackCircuitsByTrainNumber / ByNames | 0.6690 / 0.4918 | `986333d` がマスタ/状態分離 + 1 クエリ化で 3.19 → 1.15 ms を報告 |
| 高 | RegisterOrUpdateTrainState | 0.5438 | 変化が無い場合の UPDATE 抑止 |
| 中 | GetNextSignalNames | 0.3156 | `1e45c71` に次信号機マスタのキャッシュ実装あり |
| 低 | CommitTransaction | 0.1722 | ホットパスのトランザクション自体を見直す余地 |

**`GetNextSignalNames` に手を入れるときは対照メソッドを差し替えること。**
計測 2 で `IsUserBannedAsync` からこのメソッドに移したばかりだが、
これを最適化すると対照として使えなくなる。残っている非キャッシュの単純 SELECT としては
`CommitTransaction` か `RegisterOrUpdateTrainState` が候補になる。
