# PhoneService IHubContext依存除去 実装計画

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** PhoneServiceからIHubContext依存を除去し、戻り値ベースでHub層に通知責務を移動する

**Architecture:** Serviceメソッドが結果型を返し、Hub側でパターンマッチしてSignalR通知を実行する。結果型は`PhoneServiceResults.cs`に集約。

**Tech Stack:** C# record型、パターンマッチ、xUnit + Moq

---

### Task 1: 結果型を作成する

**Files:**
- Create: `Traincrew_MultiATS_Server/Models/PhoneServiceResults.cs`

**Step 1: 結果型ファイルを作成**

```csharp
namespace Traincrew_MultiATS_Server.Models;

public record LoginResult(string ConnectionId);

public abstract record CallResult
{
    public record Incoming(string CallerNumber, string CallerConnectionId, List<string> MemberConnectionIds) : CallResult;
    public record TargetBusy(string ConnectionId) : CallResult;
    public record CallerNotRegistered() : CallResult;
}

public abstract record AnswerResult
{
    public record Answered(string CallerConnectionId, string AnswererConnectionId, List<string> OtherMemberConnectionIds) : AnswerResult;
    public record SessionNotFound(string ConnectionId) : AnswerResult;
}

public record SingleNotifyResult(string? TargetConnectionId, string FromConnectionId);
```

**Step 2: ビルド確認**

Run: `dotnet build Traincrew_MultiATS_Server`
Expected: Build succeeded

**Step 3: コミット**

```bash
git add Traincrew_MultiATS_Server/Models/PhoneServiceResults.cs
git commit -m "feat: PhoneService結果型を追加"
```

---

### Task 2: IPhoneServiceインターフェースの戻り値型を変更する

**Files:**
- Modify: `Traincrew_MultiATS_Server/Services/PhoneService.cs:10-21` (IPhoneService interface)

**Step 1: インターフェースを変更**

`IPhoneService`インターフェースの戻り値型を変更する:

```csharp
public interface IPhoneService
{
    Task<LoginResult> LoginAsync(string connectionId, string myNumber);
    Task<CallResult> CallAsync(string connectionId, string targetNumber);
    Task<AnswerResult> AnswerAsync(string connectionId, string callerConnectionId);
    Task<SingleNotifyResult?> RejectAsync(string connectionId, string callerConnectionId);
    Task<SingleNotifyResult?> HangupAsync(string connectionId, string targetConnectionId);
    Task<SingleNotifyResult?> BusyAsync(string connectionId, string callerConnectionId);
    Task<SingleNotifyResult?> HoldAsync(string connectionId, string targetId);
    Task<SingleNotifyResult?> ResumeAsync(string connectionId, string targetId);
    Task<SingleNotifyResult?> OnDisconnectedAsync(string connectionId);
}
```

必要なusingを追加: `using Traincrew_MultiATS_Server.Models;`

**Step 2: この時点ではビルドエラーが出ることを確認（PhoneService実装が未対応）**

Run: `dotnet build Traincrew_MultiATS_Server`
Expected: FAIL（実装クラスが旧シグネチャのため）

---

### Task 3: PhoneService実装を変更する — Login, Call

**Files:**
- Modify: `Traincrew_MultiATS_Server/Services/PhoneService.cs`

**Step 1: コンストラクタからIHubContextを削除し、LoginAsync/CallAsyncを変更**

コンストラクタを以下に変更:

```csharp
public class PhoneService(
    PhoneSessionStore sessionStore,
    IPhoneSessionRepository sessionRepository,
    IDateTimeRepository dateTimeRepository
) : IPhoneService
```

不要なusingを削除:
- `using Microsoft.AspNetCore.SignalR;`
- `using Traincrew_MultiATS_Server.Common.Contract;`
- `using Traincrew_MultiATS_Server.Hubs;`

`using Traincrew_MultiATS_Server.Models;`を追加。

`LoginAsync`を変更:

```csharp
public Task<LoginResult> LoginAsync(string connectionId, string myNumber)
{
    sessionStore.Register(connectionId, myNumber);
    return Task.FromResult(new LoginResult(connectionId));
}
```

`CallAsync`を変更:

```csharp
public async Task<CallResult> CallAsync(string connectionId, string targetNumber)
{
    var callerNumber = sessionStore.GetNumberByConnectionId(connectionId);
    if (callerNumber == null)
    {
        return new CallResult.CallerNotRegistered();
    }

    var members = sessionStore.GetMembersByNumber(targetNumber);
    if (members == null || members.Count == 0)
    {
        return new CallResult.TargetBusy(connectionId);
    }

    var now = dateTimeRepository.GetNow();
    await sessionRepository.CreateSessionAsync(callerNumber, connectionId, targetNumber, now);

    return new CallResult.Incoming(callerNumber, connectionId, members.ToList());
}
```

---

### Task 4: PhoneService実装を変更する — Answer

**Files:**
- Modify: `Traincrew_MultiATS_Server/Services/PhoneService.cs`

**Step 1: AnswerAsyncを変更**

```csharp
public async Task<AnswerResult> AnswerAsync(string connectionId, string callerConnectionId)
{
    var session = await sessionRepository.GetActiveSessionByCallerConnectionIdAsync(callerConnectionId);
    if (session == null)
    {
        return new AnswerResult.SessionNotFound(connectionId);
    }

    await sessionRepository.SetAnsweredAsync(session.Id, connectionId);

    var otherMembers = new List<string>();
    var targetNumber = sessionStore.GetNumberByConnectionId(connectionId);
    if (targetNumber != null)
    {
        var members = sessionStore.GetMembersByNumber(targetNumber);
        if (members != null)
        {
            otherMembers = members.Where(id => id != connectionId).ToList();
        }
    }

    return new AnswerResult.Answered(callerConnectionId, connectionId, otherMembers);
}
```

---

### Task 5: PhoneService実装を変更する — Reject, Busy, Hangup, Hold, Resume, OnDisconnected

**Files:**
- Modify: `Traincrew_MultiATS_Server/Services/PhoneService.cs`

**Step 1: 残りのメソッドを変更**

`RejectAsync`:
```csharp
public async Task<SingleNotifyResult?> RejectAsync(string connectionId, string callerConnectionId)
{
    var session = await sessionRepository.GetActiveSessionByCallerConnectionIdAsync(callerConnectionId);
    if (session == null)
    {
        return null;
    }

    await sessionRepository.UpdateStatusAsync(session.Id, PhoneCallStatus.Rejected);
    return new SingleNotifyResult(callerConnectionId, connectionId);
}
```

`HangupAsync`:
```csharp
public async Task<SingleNotifyResult?> HangupAsync(string connectionId, string targetConnectionId)
{
    var sessionAsCaller = await sessionRepository.GetActiveSessionByCallerConnectionIdAsync(connectionId);
    if (sessionAsCaller != null)
    {
        await sessionRepository.EndSessionAsync(sessionAsCaller.Id);
        return sessionAsCaller.TargetConnectionId != null
            ? new SingleNotifyResult(sessionAsCaller.TargetConnectionId, connectionId)
            : null;
    }

    var sessionAsTarget = await sessionRepository.GetActiveSessionByTargetConnectionIdAsync(connectionId);
    if (sessionAsTarget != null)
    {
        await sessionRepository.EndSessionAsync(sessionAsTarget.Id);
        return new SingleNotifyResult(sessionAsTarget.CallerConnectionId, connectionId);
    }

    return null;
}
```

`BusyAsync`:
```csharp
public async Task<SingleNotifyResult?> BusyAsync(string connectionId, string callerConnectionId)
{
    var session = await sessionRepository.GetActiveSessionByCallerConnectionIdAsync(callerConnectionId);
    if (session == null)
    {
        return null;
    }

    await sessionRepository.UpdateStatusAsync(session.Id, PhoneCallStatus.Busy);
    return new SingleNotifyResult(callerConnectionId, connectionId);
}
```

`HoldAsync`:
```csharp
public async Task<SingleNotifyResult?> HoldAsync(string connectionId, string targetId)
{
    var sessionAsCaller = await sessionRepository.GetActiveSessionByCallerConnectionIdAsync(connectionId);
    if (sessionAsCaller != null && sessionAsCaller.TargetConnectionId == targetId)
    {
        await sessionRepository.UpdateStatusAsync(sessionAsCaller.Id, PhoneCallStatus.Held);
        return new SingleNotifyResult(targetId, connectionId);
    }

    var sessionAsTarget = await sessionRepository.GetActiveSessionByTargetConnectionIdAsync(connectionId);
    if (sessionAsTarget != null && sessionAsTarget.CallerConnectionId == targetId)
    {
        await sessionRepository.UpdateStatusAsync(sessionAsTarget.Id, PhoneCallStatus.Held);
        return new SingleNotifyResult(targetId, connectionId);
    }

    return null;
}
```

`ResumeAsync`:
```csharp
public async Task<SingleNotifyResult?> ResumeAsync(string connectionId, string targetId)
{
    var sessionAsCaller = await sessionRepository.GetActiveSessionByCallerConnectionIdAsync(connectionId);
    if (sessionAsCaller != null && sessionAsCaller.TargetConnectionId == targetId)
    {
        await sessionRepository.UpdateStatusAsync(sessionAsCaller.Id, PhoneCallStatus.Answered);
        return new SingleNotifyResult(targetId, connectionId);
    }

    var sessionAsTarget = await sessionRepository.GetActiveSessionByTargetConnectionIdAsync(connectionId);
    if (sessionAsTarget != null && sessionAsTarget.CallerConnectionId == targetId)
    {
        await sessionRepository.UpdateStatusAsync(sessionAsTarget.Id, PhoneCallStatus.Answered);
        return new SingleNotifyResult(targetId, connectionId);
    }

    return null;
}
```

`OnDisconnectedAsync`:
```csharp
public async Task<SingleNotifyResult?> OnDisconnectedAsync(string connectionId)
{
    sessionStore.Unregister(connectionId);

    var sessionAsCaller = await sessionRepository.GetActiveSessionByCallerConnectionIdAsync(connectionId);
    if (sessionAsCaller != null)
    {
        await sessionRepository.EndSessionAsync(sessionAsCaller.Id);
        return sessionAsCaller.TargetConnectionId != null
            ? new SingleNotifyResult(sessionAsCaller.TargetConnectionId, connectionId)
            : null;
    }

    var sessionAsTarget = await sessionRepository.GetActiveSessionByTargetConnectionIdAsync(connectionId);
    if (sessionAsTarget != null)
    {
        await sessionRepository.EndSessionAsync(sessionAsTarget.Id);
        return new SingleNotifyResult(sessionAsTarget.CallerConnectionId, connectionId);
    }

    return null;
}
```

**Step 2: ビルドはまだ失敗（PhoneHubが旧インターフェースを使用中）**

---

### Task 6: PhoneHubを変更する

**Files:**
- Modify: `Traincrew_MultiATS_Server/Hubs/PhoneHub.cs`

**Step 1: PhoneHubの全メソッドを変更して結果型を処理する**

```csharp
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using OpenIddict.Validation.AspNetCore;
using Traincrew_MultiATS_Server.Common.Contract;
using Traincrew_MultiATS_Server.Models;
using Traincrew_MultiATS_Server.Services;

namespace Traincrew_MultiATS_Server.Hubs;

[Authorize(
    AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme,
    Policy = "PhonePolicy"
)]
public class PhoneHub(IPhoneService phoneService) : Hub<IPhoneClientContract>, IPhoneHubContract
{
    public async Task Login(string myNumber)
    {
        var result = await phoneService.LoginAsync(Context.ConnectionId, myNumber);
        await Clients.Client(result.ConnectionId).ReceiveLoginSuccess(result.ConnectionId);
    }

    public async Task Call(string targetNumber)
    {
        var result = await phoneService.CallAsync(Context.ConnectionId, targetNumber);
        switch (result)
        {
            case CallResult.Incoming incoming:
                foreach (var id in incoming.MemberConnectionIds)
                {
                    await Clients.Client(id).ReceiveIncoming(incoming.CallerNumber, incoming.CallerConnectionId);
                }
                break;
            case CallResult.TargetBusy busy:
                await Clients.Client(busy.ConnectionId).ReceiveBusy(busy.ConnectionId);
                break;
        }
    }

    public async Task Answer(string callerConnectionId)
    {
        var result = await phoneService.AnswerAsync(Context.ConnectionId, callerConnectionId);
        switch (result)
        {
            case AnswerResult.Answered answered:
                await Clients.Client(answered.CallerConnectionId).ReceiveAnswered(answered.AnswererConnectionId);
                foreach (var id in answered.OtherMemberConnectionIds)
                {
                    await Clients.Client(id).ReceiveCancel(answered.CallerConnectionId);
                }
                break;
            case AnswerResult.SessionNotFound notFound:
                await Clients.Client(notFound.ConnectionId).ReceiveHangup(callerConnectionId);
                break;
        }
    }

    public async Task Reject(string callerConnectionId)
    {
        var result = await phoneService.RejectAsync(Context.ConnectionId, callerConnectionId);
        if (result?.TargetConnectionId != null)
        {
            await Clients.Client(result.TargetConnectionId).ReceiveReject(result.FromConnectionId);
        }
    }

    public async Task Hangup(string targetConnectionId)
    {
        var result = await phoneService.HangupAsync(Context.ConnectionId, targetConnectionId);
        if (result?.TargetConnectionId != null)
        {
            await Clients.Client(result.TargetConnectionId).ReceiveHangup(result.FromConnectionId);
        }
    }

    public async Task Busy(string callerConnectionId)
    {
        var result = await phoneService.BusyAsync(Context.ConnectionId, callerConnectionId);
        if (result?.TargetConnectionId != null)
        {
            await Clients.Client(result.TargetConnectionId).ReceiveBusy(result.FromConnectionId);
        }
    }

    public async Task Hold(string targetId)
    {
        var result = await phoneService.HoldAsync(Context.ConnectionId, targetId);
        if (result?.TargetConnectionId != null)
        {
            await Clients.Client(result.TargetConnectionId).ReceiveHoldRequest(result.FromConnectionId);
        }
    }

    public async Task Resume(string targetId)
    {
        var result = await phoneService.ResumeAsync(Context.ConnectionId, targetId);
        if (result?.TargetConnectionId != null)
        {
            await Clients.Client(result.TargetConnectionId).ReceiveResumeRequest(result.FromConnectionId);
        }
    }

    public override async Task OnDisconnectedAsync(System.Exception? exception)
    {
        var result = await phoneService.OnDisconnectedAsync(Context.ConnectionId);
        if (result?.TargetConnectionId != null)
        {
            await Clients.Client(result.TargetConnectionId).ReceiveHangup(result.FromConnectionId);
        }
        await base.OnDisconnectedAsync(exception);
    }
}
```

**Step 2: ビルド確認**

Run: `dotnet build`
Expected: Build succeeded

**Step 3: コミット**

```bash
git add Traincrew_MultiATS_Server/Services/PhoneService.cs Traincrew_MultiATS_Server/Hubs/PhoneHub.cs
git commit -m "refactor: PhoneServiceからIHubContext依存を除去し戻り値ベースに変更"
```

---

### Task 7: PhoneServiceのユニットテストを追加する — Login, Call

**Files:**
- Create: `Traincrew_MultiATS_Server.UT/Services/PhoneServiceTest.cs`

**Docs:** テスト命名規則は `{MethodName}_{Condition}_{ExpectedResult}` (CLAUDE.md参照)

**Step 1: テストファイルを作成**

```csharp
using Moq;
using Traincrew_MultiATS_Server.Models;
using Traincrew_MultiATS_Server.Repositories.Datetime;
using Traincrew_MultiATS_Server.Repositories.PhoneSession;
using Traincrew_MultiATS_Server.Services;

namespace Traincrew_MultiATS_Server.UT.Services;

public class PhoneServiceTest
{
    private readonly PhoneSessionStore _sessionStore = new();
    private readonly Mock<IPhoneSessionRepository> _sessionRepository = new();
    private readonly Mock<IDateTimeRepository> _dateTimeRepository = new();
    private readonly PhoneService _sut;

    public PhoneServiceTest()
    {
        _sut = new PhoneService(_sessionStore, _sessionRepository.Object, _dateTimeRepository.Object);
    }

    [Fact]
    public async Task LoginAsync_ValidInput_ReturnsLoginResultWithConnectionId()
    {
        var result = await _sut.LoginAsync("conn1", "1234");

        Assert.Equal("conn1", result.ConnectionId);
    }

    [Fact]
    public async Task LoginAsync_ValidInput_RegistersInSessionStore()
    {
        await _sut.LoginAsync("conn1", "1234");

        Assert.Equal("1234", _sessionStore.GetNumberByConnectionId("conn1"));
    }

    [Fact]
    public async Task CallAsync_CallerNotRegistered_ReturnsCallerNotRegistered()
    {
        var result = await _sut.CallAsync("conn1", "5678");

        Assert.IsType<CallResult.CallerNotRegistered>(result);
    }

    [Fact]
    public async Task CallAsync_NoMembersOnline_ReturnsTargetBusy()
    {
        _sessionStore.Register("conn1", "1234");

        var result = await _sut.CallAsync("conn1", "5678");

        var busy = Assert.IsType<CallResult.TargetBusy>(result);
        Assert.Equal("conn1", busy.ConnectionId);
    }

    [Fact]
    public async Task CallAsync_MembersOnline_ReturnsIncomingWithMembers()
    {
        _sessionStore.Register("conn1", "1234");
        _sessionStore.Register("conn2", "5678");
        _dateTimeRepository.Setup(d => d.GetNow()).Returns(new DateTime(2026, 1, 1));

        var result = await _sut.CallAsync("conn1", "5678");

        var incoming = Assert.IsType<CallResult.Incoming>(result);
        Assert.Equal("1234", incoming.CallerNumber);
        Assert.Equal("conn1", incoming.CallerConnectionId);
        Assert.Contains("conn2", incoming.MemberConnectionIds);
    }
}
```

**Step 2: テスト実行**

Run: `dotnet test Traincrew_MultiATS_Server.UT --filter "FullyQualifiedName~PhoneServiceTest"`
Expected: 5 tests passed

**Step 3: コミット**

```bash
git add Traincrew_MultiATS_Server.UT/Services/PhoneServiceTest.cs
git commit -m "test: PhoneService Login/Callのユニットテストを追加"
```

---

### Task 8: PhoneServiceのユニットテストを追加する — Answer, Reject, Hangup

**Files:**
- Modify: `Traincrew_MultiATS_Server.UT/Services/PhoneServiceTest.cs`

**Step 1: テストメソッドを追加**

```csharp
[Fact]
public async Task AnswerAsync_SessionNotFound_ReturnsSessionNotFound()
{
    _sessionRepository
        .Setup(r => r.GetActiveSessionByCallerConnectionIdAsync("caller1"))
        .ReturnsAsync((PhoneCallSession?)null);

    var result = await _sut.AnswerAsync("conn1", "caller1");

    var notFound = Assert.IsType<AnswerResult.SessionNotFound>(result);
    Assert.Equal("conn1", notFound.ConnectionId);
}

[Fact]
public async Task AnswerAsync_SessionExists_ReturnsAnsweredWithOtherMembers()
{
    var session = new PhoneCallSession
    {
        Id = 1, CallerNumber = "1234", CallerConnectionId = "caller1", TargetNumber = "5678"
    };
    _sessionRepository
        .Setup(r => r.GetActiveSessionByCallerConnectionIdAsync("caller1"))
        .ReturnsAsync(session);
    _sessionStore.Register("conn1", "5678");
    _sessionStore.Register("conn3", "5678");

    var result = await _sut.AnswerAsync("conn1", "caller1");

    var answered = Assert.IsType<AnswerResult.Answered>(result);
    Assert.Equal("caller1", answered.CallerConnectionId);
    Assert.Equal("conn1", answered.AnswererConnectionId);
    Assert.Contains("conn3", answered.OtherMemberConnectionIds);
    Assert.DoesNotContain("conn1", answered.OtherMemberConnectionIds);
}

[Fact]
public async Task RejectAsync_SessionNotFound_ReturnsNull()
{
    _sessionRepository
        .Setup(r => r.GetActiveSessionByCallerConnectionIdAsync("caller1"))
        .ReturnsAsync((PhoneCallSession?)null);

    var result = await _sut.RejectAsync("conn1", "caller1");

    Assert.Null(result);
}

[Fact]
public async Task RejectAsync_SessionExists_ReturnsNotifyResult()
{
    var session = new PhoneCallSession
    {
        Id = 1, CallerNumber = "1234", CallerConnectionId = "caller1", TargetNumber = "5678"
    };
    _sessionRepository
        .Setup(r => r.GetActiveSessionByCallerConnectionIdAsync("caller1"))
        .ReturnsAsync(session);

    var result = await _sut.RejectAsync("conn1", "caller1");

    Assert.NotNull(result);
    Assert.Equal("caller1", result.TargetConnectionId);
    Assert.Equal("conn1", result.FromConnectionId);
}

[Fact]
public async Task HangupAsync_AsCaller_ReturnsNotifyToTarget()
{
    var session = new PhoneCallSession
    {
        Id = 1, CallerNumber = "1234", CallerConnectionId = "conn1",
        TargetNumber = "5678", TargetConnectionId = "conn2"
    };
    _sessionRepository
        .Setup(r => r.GetActiveSessionByCallerConnectionIdAsync("conn1"))
        .ReturnsAsync(session);

    var result = await _sut.HangupAsync("conn1", "conn2");

    Assert.NotNull(result);
    Assert.Equal("conn2", result.TargetConnectionId);
    Assert.Equal("conn1", result.FromConnectionId);
}

[Fact]
public async Task HangupAsync_NoSession_ReturnsNull()
{
    _sessionRepository
        .Setup(r => r.GetActiveSessionByCallerConnectionIdAsync("conn1"))
        .ReturnsAsync((PhoneCallSession?)null);
    _sessionRepository
        .Setup(r => r.GetActiveSessionByTargetConnectionIdAsync("conn1"))
        .ReturnsAsync((PhoneCallSession?)null);

    var result = await _sut.HangupAsync("conn1", "conn2");

    Assert.Null(result);
}
```

**Step 2: テスト実行**

Run: `dotnet test Traincrew_MultiATS_Server.UT --filter "FullyQualifiedName~PhoneServiceTest"`
Expected: 11 tests passed

**Step 3: コミット**

```bash
git add Traincrew_MultiATS_Server.UT/Services/PhoneServiceTest.cs
git commit -m "test: PhoneService Answer/Reject/Hangupのユニットテストを追加"
```

---

### Task 9: 全テスト実行 + 最終確認

**Step 1: 全テスト実行**

Run: `dotnet test`
Expected: All tests passed

**Step 2: ビルド確認**

Run: `dotnet build`
Expected: Build succeeded
