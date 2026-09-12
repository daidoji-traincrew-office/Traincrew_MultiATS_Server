# SendData_ATS パフォーマンス計測

`TrainHub.SendData_ATS` → `TrainService.CreateAtsData` は ATS クライアントから 10 回/秒で叩かれる
ホットパスで、サーバーと DB の CPU を最も強く支配している。本ドキュメントはその改善の計測記録。

計測手順は `.claude/commands/measure_senddata_ats.md` を参照。

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

### 次の改善候補（この計測時点）

| 優先度 | フェーズ | 平均 ms/call | 備考 |
|---|---|---:|---|
| 高 | RegisterOrUpdateTrainState | 0.8187 | 変化が無い場合の UPDATE 抑止が効きそう |
| 高 | GetOperationNotificationDataByTrackCircuitIds | 0.8101 | 運転告知器はほぼ不変。キャッシュ余地あり |
| 中 | GetTrackCircuitsByTrainNumber / ByNames | 0.5362 / 0.4937 | 不変マスタとのマスタ/状態分離 |
| 中 | GetServerModeAsyncWithoutLock | 0.4302 | 毎 call の SELECT。短 TTL キャッシュ |
| 中 | SetTrackCircuitDataList / Clear | 0.4342 / 0.4094 | 在線に変化が無いときの UPDATE 抑止 |

### 計測上の注意

- `TestController.LoadTest` は BC圧・電流値を毎回乱数にし、ドア状態を `moveIntervalMs`(既定 3000ms)
  ごとに反転させる。つまり差分判定は「常にスキップ」ではなく 3 秒に 1 回書く形で計測されている。
- ベースライン・変更後とも、本計測の前に 5 秒のウォームアップ負荷をかけている。
- `dotnet-trace` の対象は必ず `pgrep -f "Traincrew_MultiATS_Server.Crew/bin/Release"` の
  バイナリ PID を使うこと。`dotnet run` ラッパーを掴むとアプリのフレームが出ない。
