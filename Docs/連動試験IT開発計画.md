# 連動試験IT開発計画書

## 1. 概要

### 1.1 目的
各駅の連動装置が正しく動作することを自動テストで検証する統合テスト(IT)フレームワークを構築する。特にCI環境での並列実行を可能にし、効率的なテスト実行を実現する。

### 1.2 スコープ
本計画書はPhase1の実装範囲を定義する。Phase2（実際の連動試験観点の実装）は別途計画する。

### 1.3 Phase1の成果物
- 駅単位でテストを実行できる基本フレームワーク
- GitHub Actions Matrixによる並列実行の仕組み
- 失敗試験があっても続行する機構
- 各進路の基本構成テスト（てこ・着点操作 → 信号開通確認）

---

## 2. 要件定義

### 2.1 機能要件

#### 2.1.1 テスト対象駅
連動図表CSVファイルが存在する全駅を対象とする。

**対象駅一覧（2025年1月時点）:**
- TH58, TH59, TH61, TH62, TH63, TH64, TH65, TH66S, TH67, TH70, TH71, TH75, TH76

**データソース:**
- `Traincrew_MultiATS_Server.Crew/Data/RendoTable/{StationId}.csv`

#### 2.1.2 テスト実行モード
- **全駅モード**: 全ての対象駅のテストを実行（環境変数未指定時）
- **単一駅モード**: 環境変数で1駅を指定してテスト実行（例: `TEST_STATION_ID=TH76`）
- **複数駅モード**: 環境変数でカンマ区切りで複数駅を指定（例: `TEST_STATION_ID=TH58,TH59,TH61`）
  - GitHub Actions Matrixの並列実行数上限に達した場合に、駅をグループ化して実行可能

#### 2.1.3 テストケース生成
テストケースはコード内にハードコードせず、データベースから動的に生成する。

**生成ロジック:**
1. 指定駅IDに紐づく全進路をデータベースから取得
2. 各進路に対して以下の情報を収集:
   - 進路ID、進路名
   - てこID、てこ操作方向（`route_lever_destination_button.direction`）
   - 着点ボタン名（nullの場合もある）
   - 対応する信号機名（`signal_route`テーブルから取得）
3. 進路ごとにテストケースを1つ生成

**取得クエリ例（EF Core）:**
```csharp
// ApplicationDbContextを使用してデータ取得
// 1. 対象駅の取得
var stations = await _context.Stations
    .Where(s => stationIds.Contains(s.Id))
    .ToDictionaryAsync(s => s.Id, s => s.Name);

// 2. 対象駅の進路とてこ・着点の関連を取得
var routeLeverButtons = await _context.RouteLeverDestinationButtons
    .Include(rldb => rldb.Lever)
    .Include(rldb => rldb.DestinationButton)
    .Where(rldb => stationIds.Contains(rldb.Lever.StationId))
    .ToListAsync();

// 3. 進路IDのリストを取得
var routeIds = routeLeverButtons.Select(rldb => rldb.RouteId).Distinct().ToList();

// 4. 進路情報を取得
var routes = await _context.Routes
    .Where(r => routeIds.Contains(r.Id))
    .ToDictionaryAsync(r => r.Id);

// 5. 進路と信号機の関連を取得
var signalRoutes = await _context.SignalRoutes
    .Where(sr => routeIds.Contains(sr.RouteId))
    .ToListAsync();

// 6. テストケース生成
var testCases = routeLeverButtons.Select(rldb =>
{
    var route = routes[rldb.RouteId];
    var signalRoute = signalRoutes.FirstOrDefault(sr => sr.RouteId == rldb.RouteId);

    return new RouteTestCase
    {
        StationId = route.StationId!,
        StationName = stations[route.StationId!],
        RouteId = route.Id,
        RouteName = route.Name,
        LeverName = rldb.Lever.Name,
        LeverDirection = rldb.Direction == LR.Left ? LCR.Left : LCR.Right,
        DestinationButtonName = rldb.DestinationButtonName,
        SignalName = signalRoute?.SignalName ?? throw new InvalidOperationException($"進路 {route.Name} に信号機が関連付けられていません")
    };
}).ToList();
```

**注意事項:**
- `Route`は`InterlockingObject`を継承しているため、`Id`, `Name`, `StationId`プロパティを持つ
- `Lever`も`InterlockingObject`を継承しているため、同様のプロパティを持つ
- `RouteLeverDestinationButton`の`Direction`は`LR`型（Left/Right）だが、てこ操作では`LCR`型（Left/Center/Right）を使用するため変換が必要
- N+1問題を回避するため、事前に必要なデータを取得してからメモリ上で結合

#### 2.1.4 進路構成テストフロー

各進路のテストは以下の手順で実行:

```
1. テスト準備
   - InterlockingHubに接続
   - 初期状態の取得（SendData_Interlocking）

2. てこ操作
   - SetPhysicalLeverData(LeverName, State)
   - State = LCR.Left (Direction == Left の場合) または LCR.Right (Direction == Right の場合)
   - route_lever_destination_button.direction に応じて設定

3. 着点操作（着点ボタンが存在する場合のみ）
   - SetDestinationButtonState(ButtonName, IsRaised=RaiseDrop.Raise)

4. 信号開通確認（即時）
   - SendData_Interlocking()で最新状態を取得
   - 対応信号機のAspectがStop以外であることを確認
   - 開通していれば成功

5. 信号開通確認（転てつ器転換待機後）
   - 即時確認で開通していない場合のみ実行
   - 7秒間待機（転てつ器の転換完了を考慮）
   - SendData_Interlocking()で最新状態を取得
   - 対応信号機のAspectがStop以外であることを確認

6. クリーンアップ
   - SetPhysicalLeverData(LeverName, State=LCR.Center)
   - てこを定位に戻す
```

**成功条件:**
- 信号機のAspectが`Stop`以外になること（`Proceed`, `SlowProceeed`等）

**失敗条件:**
- てこ操作がエラーを返す
- 着点操作がエラーを返す
- 10秒以内に信号機が開通しない

#### 2.1.5 失敗時の動作
- 1つの進路テストが失敗しても、その駅の他の進路テストは続行
- 1つの駅のテストが失敗しても、他の駅のテストは続行（GitHub Actions Matrix）
- 失敗した進路は xUnit のテストケース失敗として記録

**テストケース名の例:**
- `TH76_1Rのてこと着点を倒して進路が開通すること`
- `TH71_3LAのてこと着点を倒して進路が開通すること`

---

### 2.2 非機能要件

#### 2.2.1 実行時間
- **1進路あたり**: 最大7秒（転てつ器転換時間含む）
- **1駅あたり**: Phase1で約10分を想定
- **全駅（並列実行時）**: 最も遅い駅の時間に依存（約10分）

#### 2.2.2 並列実行
- GitHub Actions Matrixで駅単位の並列実行
- 各駅のテストは独立して実行可能（依存関係なし）

#### 2.2.3 テストの独立性
- 各進路のテストは完全に独立
- 進路間の実行順序に依存しない
- データベース初期化は`InitDBHostedService`が自動実行

---

## 3. 設計

### 3.1 テストクラス構成

#### 3.1.1 ディレクトリ構造
```
Traincrew_MultiATS_Server.IT/
├── InterlockingLogic/
│   ├── InterlockingLogicTest.cs            # 統一テストクラス
│   └── RouteTestCase.cs                    # テストケースBody
└── TestUtilities/
    └── RouteTestCaseGenerator.cs           # テストケース生成ヘルパー
```

#### 3.1.2 クラス設計

**RouteTestCase.cs**
```csharp
public record RouteTestCase
{
    public required string StationId { get; init; }
    public required string StationName { get; init; }
    public required ulong RouteId { get; init; }
    public required string RouteName { get; init; }
    public required string LeverName { get; init; }
    public required LCR LeverDirection { get; init; }
    public string? DestinationButtonName { get; init; }
    public required string SignalName { get; init; }

    // テストケース名生成
    public string GetTestCaseName()
        => $"{StationId}_{RouteName}のてこと着点を倒して進路が開通すること";
}
```

**RouteTestCaseGenerator.cs**
```csharp
public class RouteTestCaseGenerator
{
    private readonly IRouteRepository _routeRepository;
    private readonly IStationRepository _stationRepository;

    public async Task<List<RouteTestCase>> GenerateTestCasesAsync(string[]? stationIds = null)
    {
        // stationIdsがnullの場合は全駅
        // stationIdsが指定された場合はその駅のみ（複数可）
        // データベースから進路、てこ、着点、信号機の情報を取得
        // RouteTestCaseのリストを生成
    }
}
```

**InterlockingLogicTest.cs**
```csharp
[Collection("WebApplication")]
public class InterlockingLogicTest(WebApplicationFixture factory)
{
    private readonly WebApplicationFixture _factory = factory;
    private static readonly string? _targetStationIds = Environment.GetEnvironmentVariable("TEST_STATION_ID");

    // テストケース生成
    public static IEnumerable<object[]> GetTestCases()
    {
        // 環境変数TEST_STATION_IDで駅を指定可能
        // 単一駅: TEST_STATION_ID=TH76
        // 複数駅: TEST_STATION_ID=TH58,TH59,TH61 (カンマ区切り)
        // 未指定の場合は全駅のテストケースを生成

        string[]? stationIds = null;
        if (!string.IsNullOrEmpty(_targetStationIds))
        {
            stationIds = _targetStationIds.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        }

        var generator = new RouteTestCaseGenerator(...);
        var testCases = generator.GenerateTestCasesAsync(stationIds).GetAwaiter().GetResult();
        return testCases.Select(tc => new object[] { tc });
    }

    [Theory]
    [MemberData(nameof(GetTestCases))]
    public async Task 進路構成テスト(RouteTestCase testCase)
    {
        var (connection, hub) = _factory.CreateInterlockingHub();
        await connection.StartAsync();

        try
        {
            var success = await ExecuteRouteTestAsync(testCase, hub);
            Assert.True(success,
                $"進路 {testCase.RouteName} の信号機が開通しませんでした");
        }
        finally
        {
            await connection.StopAsync();
        }
    }

    // 共通のテストロジック
    private async Task<bool> ExecuteRouteTestAsync(
        RouteTestCase testCase,
        IInterlockingHubContract hub)
    {
        // 1. てこ操作: SetPhysicalLeverData(testCase.LeverName, testCase.LeverDirection)
        // 2. 着点操作: SetDestinationButtonState(testCase.DestinationButtonName, RaiseDrop.Raise)
        // 3. 即時確認: SendData_Interlocking() → 信号確認
        // 4. 7秒待機 → 再確認（即時確認で開通していない場合）
        // 5. クリーンアップ: SetPhysicalLeverData(testCase.LeverName, LCR.Center)
    }
}
```

### 3.2 GitHub Actions設計

#### 3.2.1 ワークフローファイル
**`.github/workflows/interlockingLogicTest.yml`**

```yaml
name: Interlocking Logic Integration Test

on:
  push:
    branches: [ main, develop ]
  pull_request:
    branches: [ main ]
  workflow_dispatch:

jobs:
  test:
    runs-on: ubuntu-latest
    strategy:
      fail-fast: false  # 駅ごとの失敗で全体を止めない
      matrix:
        # 単一駅での並列実行（デフォルト）
        station:
          - TH58
          - TH59
          - TH61
          - TH62
          - TH63
          - TH64
          - TH65
          - TH66S
          - TH67
          - TH70
          - TH71
          - TH75
          - TH76

        # Matrix上限に達した場合は、以下のようにグループ化可能
        # station:
        #   - TH58,TH59,TH61
        #   - TH62,TH63,TH64
        #   - TH65,TH66S,TH67
        #   - TH70,TH71
        #   - TH75,TH76

    steps:
      - name: Checkout code
        uses: actions/checkout@v3

      - name: Setup .NET
        uses: actions/setup-dotnet@v3
        with:
          dotnet-version: '8.0.x'

      - name: Restore dependencies
        run: dotnet restore

      - name: Build
        run: dotnet build --no-restore --configuration Release

      - name: Start Database
        run: docker compose -f Database/compose.yml up -d

      - name: Run Tests for ${{ matrix.station }}
        run: |
          dotnet test Traincrew_MultiATS_Server.IT \
            --no-build \
            --configuration Release \
            --filter "FullyQualifiedName~InterlockingLogicTest" \
            --logger "trx;LogFileName=${{ matrix.station }}_results.trx"
        env:
          TEST_STATION_ID: ${{ matrix.station }}

      - name: Upload Test Results
        if: always()
        uses: actions/upload-artifact@v3
        with:
          name: test-results-${{ matrix.station }}
          path: "**/*_results.trx"

      - name: Output Database Logs
        if: always()
        run: docker compose -f Database/compose.yml logs
```

**複数駅グループ化の例:**
```yaml
# Matrix並列実行数上限に達した場合のグループ化例
matrix:
  station_group:
    - name: "group1"
      stations: "TH58,TH59,TH61"
    - name: "group2"
      stations: "TH62,TH63,TH64"
    - name: "group3"
      stations: "TH65,TH66S,TH67"
    - name: "group4"
      stations: "TH70,TH71"
    - name: "group5"
      stations: "TH75,TH76"

# 実行ステップ
env:
  TEST_STATION_ID: ${{ matrix.station_group.stations }}
```

### 3.3 データアクセス設計

#### 3.3.1 必要なRepository拡張
既存の`WebApplicationFixture`に以下のメソッドを追加:

```csharp
public IRouteRepository CreateRouteRepository()
{
    var scope = Services.CreateScope();
    return scope.ServiceProvider.GetRequiredService<IRouteRepository>();
}

public ISignalRepository CreateSignalRepository()
{
    var scope = Services.CreateScope();
    return scope.ServiceProvider.GetRequiredService<ISignalRepository>();
}
```

#### 3.3.2 IRouteRepositoryの拡張
進路とてこ・着点の情報を一括取得するメソッドを追加（パフォーマンス向上）:

```csharp
public interface IRouteRepository
{
    // 既存メソッド...

    Task<List<RouteWithLeverAndSignal>> GetRoutesWithLeverAndSignalAsync(
        string stationId);
}

public record RouteWithLeverAndSignal
{
    public string StationId { get; init; }
    public string StationName { get; init; }
    public ulong RouteId { get; init; }
    public string RouteName { get; init; }
    public string LeverName { get; init; }
    public LR Direction { get; init; }
    public string? DestinationButtonName { get; init; }
    public string? SignalName { get; init; }
}
```

---

## 4. 実装計画

### 4.1 タスク分解

#### Phase 1.1: 基盤整備
- [ ] `RouteTestCase`モデルの作成
- [ ] `RouteTestCaseGenerator`の実装
- [ ] `IRouteRepository`の拡張（`GetRoutesWithLeverAndSignalAsync`）
- [ ] `WebApplicationFixture`の拡張（Repository生成メソッド追加）

#### Phase 1.2: テスト基底クラスの実装
- [ ] `InterlockingLogicTestBase`の実装
  - [ ] `ExecuteRouteTestAsync`メソッド（てこ操作 → 着点操作 → 待機 → 確認）
  - [ ] タイムアウト処理
  - [ ] クリーンアップ処理
  - [ ] エラーハンドリング

#### Phase 1.3: テストクラスの実装
- [ ] `InterlockingLogicTest`クラスの実装
  - 環境変数による駅指定機能
  - 動的テストケース生成

#### Phase 1.4: GitHub Actions設定
- [ ] `interlockingLogicTest.yml`の作成
- [ ] Matrixによる並列実行の検証
- [ ] テスト結果アーティファクトのアップロード設定

#### Phase 1.5: 統合テスト
- [ ] ローカル環境での単一駅テスト実行
- [ ] 複数駅の並列実行テスト
- [ ] 失敗時の動作確認（fail-fast: falseの検証）
- [ ] CI環境での実行確認

### 4.2 実装順序
1. Phase 1.1（基盤整備）
2. Phase 1.2（テスト共通ロジック）
3. Phase 1.3（テストクラス実装）
4. 1駅（TH76）での動作確認
5. Phase 1.4（GitHub Actions）
6. Phase 1.5（統合テスト）

### 4.3 見積もり工数
- Phase 1.1: 4時間
- Phase 1.2: 6時間
- Phase 1.3: 4時間
- 動作確認（TH76）: 2時間
- Phase 1.4: 3時間
- Phase 1.5: 5時間
- **合計: 約24時間（3日間）**

---

## 5. テスト戦略

### 5.1 テストレベル
- **単体テスト**: `RouteTestCaseGenerator`のロジック
- **統合テスト**: 本IT（進路構成の動作確認）
- **E2Eテスト**: CI環境での全駅並列実行

### 5.2 テストデータ
- データベースから動的に生成（ハードコード不要）
- 連動図表CSV（`RendoTable/*.csv`）は参照のみ

### 5.3 テスト環境
- **ローカル**: Docker Compose + dotnet test
- **CI**: GitHub Actions + Docker Compose
- **データベース**: PostgreSQL（`InitDBHostedService`による自動初期化）

---

## 6. リスクと対策

### 6.1 リスク

| リスク | 影響度 | 対策 |
|--------|--------|------|
| 1駅のテストが10分を超える | 中 | タイムアウト値の調整、進路数の多い駅の優先実装 |
| データベースの初期化に時間がかかる | 低 | `InitDBHostedService`の最適化は別タスク |
| 転てつ器の転換待ち時間が7秒を超える | 中 | 待機時間を環境変数で調整可能にする |
| xUnit の Theory で大量のテストケースを扱えない | 低 | ClassData/MemberDataの最適化 |
| GitHub Actions の並列実行数制限 | 低 | Freeプランでは20並列まで可能、現状13駅なので問題なし |

### 6.2 前提条件
- データベースのスキーマ（`route`, `signal_route`, `route_lever_destination_button`等）が正しく定義されている
- `InitDBHostedService`がテスト起動時に正常動作する
- SignalR Hubの接続が安定している

### 6.3 制約事項
- Phase1では進路構成の基本動作のみテスト
- 連動試験観点（鎖錠条件、解錠条件、矛盾検査等）はPhase2で実装

---

## 7. Phase2への展望（参考）

Phase2では以下の観点を追加予定:

- **鎖錠条件の検証**: 進路構成時に正しい軌道回路・転てつ器が鎖錠されるか
- **解錠条件の検証**: 列車通過後に正しく解錠されるか
- **矛盾検査**: 対向進路が構成されないことの確認
- **接近鎖錠**: 接近鎖錠区間での動作確認
- **信号現示の正当性**: 場内/出発/入換信号の現示が正しいか

Phase2の実装計画は別途策定する。

---

## 8. 付録

### 8.1 対象駅一覧（詳細）

| 駅ID | 駅名 | 連動図表ファイル | 備考 |
|------|------|------------------|------|
| TH58 | 赤山町 | TH58.csv | - |
| TH59 | 西赤山 | TH59.csv | - |
| TH61 | 日野森 | TH61.csv | - |
| TH62 | 高見沢 | TH62.csv | - |
| TH63 | 水越 | TH63.csv | - |
| TH64 | 藤江 | TH64.csv | - |
| TH65 | 大道寺 | TH65.csv | - |
| TH66S | 江ノ原信号場 | TH66S.csv | 信号場 |
| TH67 | 新野崎 | TH67.csv | - |
| TH70 | 浜園 | TH70.csv | - |
| TH71 | 津崎 | TH71.csv | - |
| TH75 | 駒野 | TH75.csv | - |
| TH76 | 館浜 | TH76.csv | - |

### 8.2 参考資料
- [WebApplicationFixture.cs](Traincrew_MultiATS_Server.IT/Fixture/WebApplicationFixture.cs)
- [InterlockingHubTest.cs](Traincrew_MultiATS_Server.IT/Hubs/InterlockingHubTest.cs)
- [既存のGitHub Actions設定](.github/workflows/integrationTest.yml)
- [Route.cs](Traincrew_MultiATS_Server/Models/Route.cs)
- [SignalRoute.cs](Traincrew_MultiATS_Server/Models/SignalRoute.cs)

---

**作成日**: 2025-01-14
**バージョン**: 1.2
**更新履歴**:
- v1.0: 初版作成
- v1.1: 設計変更
  - てこ操作方向をLCR型に変更（route_lever_destination_button.directionに対応）
  - 信号開通確認を即時と7秒後の2段階に変更
  - クリーンアップをLCR.Centerに変更
  - 駅別テストクラスから統一テストクラス（環境変数による制御）に変更
  - 駅名をCSVファイルから取得するように変更
- v1.2: 複数駅指定機能の追加
  - TEST_STATION_IDでカンマ区切りの複数駅指定に対応
  - GitHub Actions Matrixの並列実行数上限対策としてグループ化可能に

**承認**: 未承認
