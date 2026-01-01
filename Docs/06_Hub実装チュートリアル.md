# 06_Hubメソッド実装チュートリアル

## 概要

このドキュメントでは、SignalR Hubのメソッド実装フローを、具体的な例を使って説明します。
新しいHub Methodを追加する際の手順を、Interface定義からRepository実装まで順を追って解説します。

---

## 実装フロー

Hub実装は以下の順序で行います：

```
1. Interface定義 (Common プロジェクト)
   ↓
2. Body定義 (Common プロジェクト)
   ↓
3. Hub実装 (コアライブラリ)
   ↓
4. Service実装 (コアライブラリ)
   ↓
5. Repository実装 (コアライブラリ)
   ↓
6. DI登録 (Crew/Passenger プロジェクト Program.cs)
```

---

## 具体例: Interlocking Hub の実装

実際の `InterlockingHub` を例に、各ステップを解説します。

---

## ステップ1: Interface定義

まず、クライアント・サーバー間で共有するインターフェースを定義します。

**ファイル:** `Traincrew_MultiATS_Server.Common/Contract/IInterlockingHubContract.cs`

```csharp
using Traincrew_MultiATS_Server.Common.Models;

namespace Traincrew_MultiATS_Server.Common.Contract;

/// <summary>
/// サーバー側が提供するメソッド (クライアント → サーバー)
/// </summary>
public interface IInterlockingHubContract
{
    /// <summary>
    /// 連動データを取得する
    /// </summary>
    Task<DataToInterlocking> SendData_Interlocking(List<string> activeStationsList);

    /// <summary>
    /// 物理てこデータを設定する
    /// </summary>
    Task<InterlockingLeverData> SetPhysicalLeverData(InterlockingLeverData leverData);

    /// <summary>
    /// 物理鍵てこデータを設定する
    /// </summary>
    Task<InterlockingKeyLeverData> SetPhysicalKeyLeverData(InterlockingKeyLeverData keyLeverData);

    /// <summary>
    /// 着点ボタン状態を設定する
    /// </summary>
    Task<DestinationButtonData> SetDestinationButtonState(DestinationButtonData buttonData);
}

/// <summary>
/// クライアント側が実装するメソッド (サーバー → クライアント)
/// </summary>
public interface IInterlockingClientContract
{
    /// <summary>
    /// 連動データを受信する
    /// </summary>
    Task ReceiveData(DataToInterlocking data);

    /// <summary>
    /// 信号データを受信する
    /// </summary>
    Task ReceiveSignalData(List<SignalData> signalData);
}
```

**重要ポイント:**
- `IInterlockingHubContract`: クライアントから呼び出されるサーバーメソッド
- `IInterlockingClientContract`: サーバーからクライアントへ通知するメソッド
- すべてのメソッドは `Task` または `Task<T>` を返す

---

## ステップ2: Body定義

次に、クライアントとやり取りをするためのデータ型を定義します。

**ファイル:** `Traincrew_MultiATS_Server.Common/Models/Interlocking.cs`

```csharp
namespace Traincrew_MultiATS_Server.Common.Models;

/// <summary>
/// 連動データ全体
/// </summary>
public class DataToInterlocking
{
    /// <summary>
    /// 軌道回路情報リスト
    /// </summary>
    public List<TrackCircuitData> TrackCircuits { get; set; }

    /// <summary>
    /// 転てつ器情報リスト
    /// </summary>
    public List<SwitchData> Points { get; set; }

    /// <summary>
    /// 物理てこ情報リスト
    /// </summary>
    public List<InterlockingLeverData> PhysicalLevers { get; set; }

    /// <summary>
    /// 物理鍵てこ情報リスト
    /// </summary>
    public List<InterlockingKeyLeverData> PhysicalKeyLevers { get; set; }

    /// <summary>
    /// 着点ボタン情報リスト
    /// </summary>
    public List<DestinationButtonData> PhysicalButtons { get; set; }

    /// <summary>
    /// 方向てこ情報リスト
    /// </summary>
    public List<DirectionData> Directions { get; set; }

    /// <summary>
    /// 列番情報リスト
    /// </summary>
    public List<InterlockingRetsubanData> Retsubans { get; set; }

    /// <summary>
    /// 表示灯情報リスト
    /// </summary>
    public Dictionary<string, bool> Lamps { get; set; }

    /// <summary>
    /// TST時差
    /// </summary>
    public int TimeOffset { get; set; }
}

/// <summary>
/// 物理てこデータ
/// </summary>
public class InterlockingLeverData
{
    /// <summary>
    /// てこ名称
    /// </summary>
    public string Name { get; set; } = "";

    /// <summary>
    /// てこの状態 (Left/Center/Right)
    /// </summary>
    public LCR State { get; set; } = LCR.Center;
}

/// <summary>
/// 転てつ器データ
/// </summary>
public class SwitchData
{
    /// <summary>
    /// 転てつ器名称
    /// </summary>
    public string Name { get; set; } = "";

    /// <summary>
    /// 転てつ器状態 (Normal/Reverse/Center)
    /// </summary>
    public NRC State { get; set; } = NRC.Center;
}
```

**重要ポイント:**
- クライアント・サーバー両方で使用するため、Commonプロジェクトに配置
- プロパティには必ずXMLコメントを記述
- デフォルト値を設定しておくと安全

---

## ステップ3: Hub実装

SignalR Hubを実装します。

**ファイル:** `Traincrew_MultiATS_Server/Hubs/InterlockingHub.cs`

```csharp
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using OpenIddict.Validation.AspNetCore;
using Traincrew_MultiATS_Server.Common.Contract;
using Traincrew_MultiATS_Server.Common.Models;
using Traincrew_MultiATS_Server.Services;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace Traincrew_MultiATS_Server.Hubs;

/// <summary>
/// 連動Hub (信号係員操作可・司令主任鍵使用可)
/// </summary>
[Authorize(
    AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme,
    Policy = "InterlockingPolicy"
)]
public class InterlockingHub(InterlockingService interlockingService)
    : Hub<IInterlockingClientContract>, IInterlockingHubContract
{
    /// <summary>
    /// 連動データを送信する
    /// </summary>
    public async Task<DataToInterlocking> SendData_Interlocking(List<string> activeStationsList)
    {
        return await interlockingService.SendData_Interlocking();
    }

    /// <summary>
    /// 物理てこデータを設定する
    /// </summary>
    public async Task<InterlockingLeverData> SetPhysicalLeverData(InterlockingLeverData leverData)
    {
        return await interlockingService.SetPhysicalLeverData(leverData);
    }

    /// <summary>
    /// 物理鍵てこデータを設定する
    /// </summary>
    public async Task<InterlockingKeyLeverData> SetPhysicalKeyLeverData(InterlockingKeyLeverData keyLeverData)
    {
        // 認証情報からMemberIDを取得
        var memberIdString = Context.User?.FindFirst(Claims.Subject)?.Value;
        ulong? memberId = memberIdString != null ? ulong.Parse(memberIdString) : null;

        return await interlockingService.SetPhysicalKeyLeverData(keyLeverData, memberId);
    }

    /// <summary>
    /// 着点ボタン状態を設定する
    /// </summary>
    public async Task<DestinationButtonData> SetDestinationButtonState(DestinationButtonData buttonData)
    {
        return await interlockingService.SetDestinationButtonState(buttonData);
    }
}
```

**重要ポイント:**
- `Hub<IInterlockingClientContract>` でクライアント通知用インターフェースを指定
- `IInterlockingHubContract` を実装してサーバーメソッドを提供
- `[Authorize]` で認証・認可を設定
- Hubメソッド内では基本的にServiceを呼び出すだけ
- `Context.User` でクライアントの認証情報を取得可能

---

## ステップ4: Service実装

ビジネスロジックを実装するServiceを作成します。

**ファイル:** `Traincrew_MultiATS_Server/Services/InterlockingService.cs`

```csharp
using Traincrew_MultiATS_Server.Common.Models;
using Traincrew_MultiATS_Server.Repositories.Lever;
using Traincrew_MultiATS_Server.Repositories.DestinationButton;
using Traincrew_MultiATS_Server.Repositories.Mutex;
using Traincrew_MultiATS_Server.Repositories.General;

namespace Traincrew_MultiATS_Server.Services;

public class InterlockingService(
    ILeverRepository leverRepository,
    IDestinationButtonRepository destinationButtonRepository,
    IGeneralRepository generalRepository,
    TrackCircuitService trackCircuitService,
    SwitchingMachineService switchingMachineService,
    DirectionRouteService directionRouteService,
    IMutexRepository mutexRepository,
    ILogger<InterlockingService> logger)
{
    /// <summary>
    /// 連動データを送信する
    /// </summary>
    public async Task<DataToInterlocking> SendData_Interlocking()
    {
        // Mutexを取得して排他制御
        await using var mutex = await mutexRepository.AcquireAsync(nameof(InterlockingService));

        // 各種データを取得
        var trackCircuits = await trackCircuitService.GetAllTrackCircuitDataList();
        var switchingDatas = await switchingMachineService.GetAllSwitchData();
        var lever = await leverRepository.GetAllWithState();
        var destinationButtons = await destinationButtonRepository.GetAllWithState();
        var directions = await directionRouteService.GetAllDirectionData();

        // レスポンスを組み立て
        var response = new DataToInterlocking
        {
            TrackCircuits = trackCircuits,
            Points = switchingDatas,
            PhysicalLevers = lever.Select(ToLeverData).ToList(),
            PhysicalButtons = destinationButtons
                .Select(button => ToDestinationButtonData(button.DestinationButtonState))
                .ToList(),
            Directions = directions
        };

        return response;
    }

    /// <summary>
    /// 物理てこデータを設定する
    /// </summary>
    public async Task<InterlockingLeverData> SetPhysicalLeverData(InterlockingLeverData leverData)
    {
        await using var mutex = await mutexRepository.AcquireAsync(nameof(InterlockingService));

        // てこを取得
        var lever = await leverRepository.GetLeverByNameWithState(leverData.Name);
        if (lever == null)
        {
            throw new ArgumentException("Invalid lever name");
        }

        // 状態を更新
        var oldState = lever.LeverState.IsReversed;
        lever.LeverState.IsReversed = leverData.State;

        // ログ出力
        if (oldState != leverData.State)
        {
            logger.LogInformation("Lever {LeverName} state changed: {OldState} -> {NewState}",
                leverData.Name, oldState, leverData.State);
        }

        // 保存
        await generalRepository.Save(lever.LeverState);

        return ToLeverData(lever);
    }

    /// <summary>
    /// Entity → Body 変換
    /// </summary>
    private static InterlockingLeverData ToLeverData(Models.Lever lever)
    {
        return new InterlockingLeverData
        {
            Name = lever.Name,
            State = lever.LeverState.IsReversed
        };
    }

    private static DestinationButtonData ToDestinationButtonData(DestinationButtonState state)
    {
        return new DestinationButtonData
        {
            Name = state.Name,
            IsRaised = state.IsRaised == RaiseDrop.Raise
        };
    }
}
```

**重要ポイント:**
- 複数のRepositoryを組み合わせてビジネスロジックを実装
- `IMutexRepository` で排他制御を実現（並行アクセス対策）
- Entity → Body 変換メソッドを用意（`ToLeverData` など）
- ログ出力で重要な操作を記録
- `GeneralRepository` で単一エンティティの更新を行う

---

## ステップ5: Repository実装

データベースアクセスを行うRepositoryを実装します。

### 3.1 インターフェース定義

**ファイル:** `Traincrew_MultiATS_Server/Repositories/Lever/ILeverRepository.cs`

```csharp
namespace Traincrew_MultiATS_Server.Repositories.Lever;

public interface ILeverRepository
{
    /// <summary>
    /// 名前でてこを取得する（状態含む）
    /// </summary>
    Task<Models.Lever?> GetLeverByNameWithState(string leverName);

    /// <summary>
    /// すべてのてこを取得する（状態含む）
    /// </summary>
    Task<List<Models.Lever>> GetAllWithState();
}
```

### 3.2 実装クラス

**ファイル:** `Traincrew_MultiATS_Server/Repositories/Lever/LeverRepository.cs`

```csharp
using Microsoft.EntityFrameworkCore;
using Traincrew_MultiATS_Server.Data;

namespace Traincrew_MultiATS_Server.Repositories.Lever;

public class LeverRepository(ApplicationDbContext context) : ILeverRepository
{
    public async Task<Models.Lever?> GetLeverByNameWithState(string leverName)
    {
        return await context.Levers
            .Include(l => l.LeverState)
            .FirstOrDefaultAsync(l => l.Name == leverName);
    }

    public async Task<List<Models.Lever>> GetAllWithState()
    {
        return await context.Levers
            .Include(l => l.LeverState)
            .ToListAsync();
    }
}
```

**重要ポイント:**
- Primary Constructor を使用 (`ApplicationDbContext context`)
- `.Include()` で関連エンティティを明示的にロード（N+1問題を回避）
- Repository のみが `ApplicationDbContext` にアクセスできる

---

## ステップ5: Hub実装

SignalR Hubを実装します。

**ファイル:** `Traincrew_MultiATS_Server/Hubs/InterlockingHub.cs`

```csharp
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using OpenIddict.Validation.AspNetCore;
using Traincrew_MultiATS_Server.Common.Contract;
using Traincrew_MultiATS_Server.Common.Models;
using Traincrew_MultiATS_Server.Services;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace Traincrew_MultiATS_Server.Hubs;

/// <summary>
/// 連動Hub (信号係員操作可・司令主任鍵使用可)
/// </summary>
[Authorize(
    AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme,
    Policy = "InterlockingPolicy"
)]
public class InterlockingHub(InterlockingService interlockingService)
    : Hub<IInterlockingClientContract>, IInterlockingHubContract
{
    /// <summary>
    /// 連動データを送信する
    /// </summary>
    public async Task<DataToInterlocking> SendData_Interlocking(List<string> activeStationsList)
    {
        return await interlockingService.SendData_Interlocking();
    }

    /// <summary>
    /// 物理てこデータを設定する
    /// </summary>
    public async Task<InterlockingLeverData> SetPhysicalLeverData(InterlockingLeverData leverData)
    {
        return await interlockingService.SetPhysicalLeverData(leverData);
    }

    /// <summary>
    /// 物理鍵てこデータを設定する
    /// </summary>
    public async Task<InterlockingKeyLeverData> SetPhysicalKeyLeverData(InterlockingKeyLeverData keyLeverData)
    {
        // 認証情報からMemberIDを取得
        var memberIdString = Context.User?.FindFirst(Claims.Subject)?.Value;
        ulong? memberId = memberIdString != null ? ulong.Parse(memberIdString) : null;

        return await interlockingService.SetPhysicalKeyLeverData(keyLeverData, memberId);
    }

    /// <summary>
    /// 着点ボタン状態を設定する
    /// </summary>
    public async Task<DestinationButtonData> SetDestinationButtonState(DestinationButtonData buttonData)
    {
        return await interlockingService.SetDestinationButtonState(buttonData);
    }
}
```

**重要ポイント:**
- `Hub<IInterlockingClientContract>` でクライアント通知用インターフェースを指定
- `IInterlockingHubContract` を実装してサーバーメソッドを提供
- `[Authorize]` で認証・認可を設定
- Hubメソッド内では基本的にServiceを呼び出すだけ
- `Context.User` でクライアントの認証情報を取得可能

---

## ステップ6: DI登録 (Program.cs)

最後に、依存性注入コンテナに登録します。

**ファイル:** `Traincrew_MultiATS_Server.Crew/Program.cs`

### 6.1 ConfigureDependencyInjectionService メソッド

```csharp
private static void ConfigureDependencyInjectionService(WebApplicationBuilder builder, bool enableAuthorization)
{
    // DI周り
    builder.Services
        // Repository登録 (Scoped)
        .AddScoped<ILeverRepository, LeverRepository>()
        .AddScoped<IDestinationButtonRepository, DestinationButtonRepository>()
        .AddScoped<IGeneralRepository, GeneralRepository>()
        .AddScoped<IRouteRepository, RouteRepository>()
        .AddScoped<ISignalRepository, SignalRepository>()
        .AddScoped<ITrackCircuitRepository, TrackCircuitRepository>()
        .AddScoped<ISwitchingMachineRepository, SwitchingMachineRepository>()
        .AddScoped<IStationRepository, StationRepository>()
        // ... 他のRepository

        // Service登録 (Scoped)
        .AddScoped<InterlockingService>()
        .AddScoped<TrackCircuitService>()
        .AddScoped<SwitchingMachineService>()
        .AddScoped<DirectionRouteService>()
        .AddScoped<SignalService>()
        .AddScoped<RouteService>()
        .AddScoped<TrainService>()
        .AddScoped<TIDService>()
        // ... 他のService

        // Singleton登録
        .AddSingleton<DiscordService>()
        .AddSingleton<SchedulerManager>()
        .AddSingleton<IMutexRepository, MutexRepository>();
}
```

### 6.2 ConfigureEndpoints メソッド
```csharp
private static List<IEndpointConventionBuilder> ConfigureEndpoints(WebApplication app)
{
    // SignalR Hubエンドポイントの設定
    return
    [
        app.MapControllers(),
        app.MapHub<TrainHub>("/hub/train"),
        app.MapHub<TIDHub>("/hub/TID"),
        app.MapHub<CTCPHub>("/hub/CTCP"),
        app.MapHub<InterlockingHub>("/hub/interlocking"),  // ← ここに追加
        app.MapHub<CommanderTableHub>("/hub/commander_table"),
    ];
}
```

### 6.3 Main メソッド

```csharp
public static async Task Main(string[] args)
{
    var builder = WebApplication.CreateBuilder(args);
    var isDevelopment = builder.Environment.IsDevelopment();
    var enableAuthorization = !isDevelopment || builder.Configuration.GetValue<bool>("EnableAuthorization");

    // サービス設定
    ConfigureServices(builder, isDevelopment, enableAuthorization, enableOtlp);

    var app = builder.Build();

    // アプリケーション設定
    await Configure(app, isDevelopment, enableAuthorization);

    await app.RunAsync();
}

private static void ConfigureServices(WebApplicationBuilder builder,
    bool isDevelopment,
    bool enableAuthorization,
    bool enableOtlp)
{
    ConfigureLoggingService(builder);
    ConfigureControllersService(builder);
    ConfigureDatabaseService(builder);
    ConfigureSignalRService(builder);
    ConfigureAuthenticationService(builder);
    ConfigureAuthorizationService(builder);
    ConfigureDependencyInjectionService(builder, enableAuthorization);  // ← DI登録
    ConfigureHostedServices(builder);
}
```

**重要ポイント:**
- Repository・Serviceは **Scoped** ライフタイムで登録
  - リクエストごとに新しいインスタンスを生成
  - DbContextと同じライフタイム
- Singleton（DiscordService、MutexRepositoryなど）はアプリケーション全体で共有
- `MapHub<T>()` でHubエンドポイントをマッピング
- URLパス (`/hub/interlocking`) はクライアント側と一致させる
- 認証・認可の設定は `ConfigureAuthorizationService` で定義

**ライフタイムの使い分け:**
- **Scoped**: DbContext、Repository、Service（リクエスト単位）
- **Singleton**: Discord Bot、Mutex、Scheduler（アプリケーション全体）
- **Transient**: 軽量で状態を持たないサービス（使用頻度低）

---

## 補足: Schedulerによるデータプッシュ

Hubからクライアントに定期的にデータをプッシュする場合、Schedulerを使用します。

**ファイル:** `Traincrew_MultiATS_Server/Scheduler/InterlockingHubScheduler.cs`

```csharp
using Microsoft.AspNetCore.SignalR;
using Traincrew_MultiATS_Server.Common.Contract;
using Traincrew_MultiATS_Server.Hubs;
using Traincrew_MultiATS_Server.Services;

namespace Traincrew_MultiATS_Server.Scheduler;

public class InterlockingHubScheduler(IServiceScopeFactory serviceScopeFactory)
    : Scheduler(serviceScopeFactory)
{
    // 250ms間隔で実行
    protected override int Interval => 250;

    protected override async Task ExecuteTaskAsync(IServiceScope scope, Activity? activity)
    {
        // HubContextとServiceを取得
        var hubContext = scope.ServiceProvider
            .GetRequiredService<IHubContext<InterlockingHub, IInterlockingClientContract>>();
        var interlockingService = scope.ServiceProvider
            .GetRequiredService<InterlockingService>();

        // データ取得
        var data = await interlockingService.SendData_Interlocking();

        // 全クライアントに送信
        await hubContext.Clients.All.ReceiveData(data);
    }
}
```

**Program.csに登録:**
```csharp
// SchedulerManager経由でSchedulerを起動
builder.Services.AddSingleton<InterlockingHubScheduler>();
builder.Services.AddHostedService<SchedulerManager>();
```

---

## まとめ

Hub実装の流れは以下の通りです：

1. **Interface定義**: `IXxxHubContract` と `IXxxClientContract` を定義
2. **Body定義**: DTOクラスを定義
3. **Hub実装**: SignalR Hubを実装
4. **Service実装**: ビジネスロジック層を実装
5. **Repository実装**: データアクセス層を実装
6. **DI登録**: Program.csでDIコンテナに登録

**推奨事項:**
- 各層の責務を明確に分離する
- **Hub → Service → Repository → DbContext** の順でアクセス
- Repositoryのみが `DbContext` にアクセスする
- Hubメソッドはシンプルに保ち、ロジックはServiceに委譲する
- DTOには必ずXMLコメントを記述する
- ログ出力で重要な操作を記録する
- 単一エンティティの更新は `GeneralRepository` を使用
- 一括更新・削除は `ExecuteUpdateAsync` / `ExecuteDeleteAsync` を使用

---

## 次のステップ

- **03_コーディング規約.md**: コーディングルールを確認
- **05_テスト戦略.md**: Hub実装のテスト方法を学ぶ
