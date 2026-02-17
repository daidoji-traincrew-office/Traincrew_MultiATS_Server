namespace Traincrew_MultiATS_Server.Common.Models;

/// <summary>
/// Callの戻り値。IsConnected=true: 相手に着信中, false: 話中/未登録
/// </summary>
public record CallResponse(bool IsConnected);

/// <summary>
/// Answerの戻り値。応答時にWebRTCシグナリング用のConnectionIdを交換する。
/// </summary>
public record AnswerResponse(string MyConnectionId, string CallerConnectionId);
