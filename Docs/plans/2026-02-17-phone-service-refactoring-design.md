# PhoneService リファクタリング設計書

## 目的

PhoneServiceからIHubContext依存を除去し、SignalRに依存しないドメインロジックのみをServiceに配置する。テスタビリティの向上とアーキテクチャルール（Controller/Hub → Service → Repository）への準拠を実現する。

## アプローチ

**戻り値ベース方式**: Serviceメソッドが「誰に何を通知すべきか」を結果型として返し、Hub側で実際のSignalR送信を行う。

## 結果型定義

`Traincrew_MultiATS_Server/Models/PhoneServiceResults.cs` に配置。

```csharp
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

## 変更対象ファイル

1. **新規**: `Traincrew_MultiATS_Server/Models/PhoneServiceResults.cs` — 結果型
2. **変更**: `Traincrew_MultiATS_Server/Services/PhoneService.cs` — IHubContext削除、戻り値型変更
3. **変更**: `Traincrew_MultiATS_Server/Hubs/PhoneHub.cs` — 結果型に基づくSignalR通知

## PhoneService メソッド変更一覧

| メソッド | 現在の戻り値 | 変更後の戻り値 |
|---------|------------|--------------|
| LoginAsync | Task | Task\<LoginResult\> |
| CallAsync | Task | Task\<CallResult\> |
| AnswerAsync | Task | Task\<AnswerResult\> |
| RejectAsync | Task | Task\<SingleNotifyResult?\> |
| HangupAsync | Task | Task\<SingleNotifyResult?\> |
| BusyAsync | Task | Task\<SingleNotifyResult?\> |
| HoldAsync | Task | Task\<SingleNotifyResult?\> |
| ResumeAsync | Task | Task\<SingleNotifyResult?\> |
| OnDisconnectedAsync | Task | Task\<SingleNotifyResult?\> |

## PhoneHub 通知ロジック

Hub側で結果型をパターンマッチし、対応するクライアントメソッドを呼び出す。

- `CallResult.Incoming` → 各メンバーに `ReceiveIncoming`
- `CallResult.TargetBusy` → 発信者に `ReceiveBusy`
- `AnswerResult.Answered` → 発信者に `ReceiveAnswered` + 他メンバーに `ReceiveCancel`
- `AnswerResult.SessionNotFound` → 自分に `ReceiveHangup`
- `SingleNotifyResult` → `TargetConnectionId` が非nullの場合、対応する通知メソッドを呼び出す

## SingleNotifyResult の使い分け

Reject/Busy/Hangup/Hold/Resume/OnDisconnected は全て `SingleNotifyResult?` を返す。Hub側ではメソッド名に応じて適切な通知メソッドを使い分ける:

- `Reject` → `ReceiveReject`
- `Busy` → `ReceiveBusy`
- `Hangup` / `OnDisconnected` → `ReceiveHangup`
- `Hold` → `ReceiveHoldRequest`
- `Resume` → `ReceiveResumeRequest`
