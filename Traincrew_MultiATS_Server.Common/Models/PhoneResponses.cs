namespace Traincrew_MultiATS_Server.Common.Models;

/// <summary>
/// Callの戻り値。IsConnected=true: 相手に着信中, false: 話中/未登録
/// </summary>
public record CallResponse(bool IsConnected);

