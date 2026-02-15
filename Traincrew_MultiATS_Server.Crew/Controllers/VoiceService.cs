using System.Collections.Concurrent;
using Grpc.Core;
using Microsoft.AspNetCore.Authorization;
using OpenIddict.Validation.AspNetCore;
using RailwayPhone.Protos;

namespace Traincrew_MultiATS_Server.Crew.Controllers;

/// <summary>
/// gRPC音声リレーサービス（双方向ストリーミング）
/// </summary>
[Authorize(
    AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme,
    Policy = "PhonePolicy"
)]
public class VoiceService(ILogger<VoiceService> logger) : VoiceRelay.VoiceRelayBase
{
    // 接続中のクライアントの「出口（書き込みストリーム）」をIDで管理する辞書
    private static readonly ConcurrentDictionary<string, IServerStreamWriter<VoiceData>> _users = new();

    public override async Task JoinSession(
        IAsyncStreamReader<VoiceData> requestStream,
        IServerStreamWriter<VoiceData> responseStream,
        ServerCallContext context)
    {
        var myId = "";

        try
        {
            // クライアントからの最初のパケットを待つ（ここでID登録する）
            await foreach (var data in requestStream.ReadAllAsync())
            {
                // 初回接続時の処理
                if (string.IsNullOrEmpty(myId))
                {
                    myId = data.ClientId;
                    _users[myId] = responseStream;
                    logger.LogInformation("[Voice] Client Joined: {ClientId}", myId);
                }

                // 送信処理: ターゲットが辞書にいれば、その人のストリームに書き込む
                if (string.IsNullOrEmpty(data.TargetId) || !_users.TryGetValue(data.TargetId, out var targetStream))
                {
                    continue;
                }

                try
                {
                    // 相手に転送
                    await targetStream.WriteAsync(new VoiceData
                    {
                        ClientId = data.ClientId, // 誰から来たか
                        AudioContent = data.AudioContent
                    });
                }
                catch (System.Exception ex)
                {
                    // 送信失敗（相手が切断など）
                    logger.LogWarning("[Voice] Failed to send to {TargetId}: {Message}", data.TargetId, ex.Message);
                }
            }
        }
        catch (System.Exception ex)
        {
            logger.LogError(ex, "[Voice] Error in JoinSession");
        }
        finally
        {
            // 切断時の後始末
            if (!string.IsNullOrEmpty(myId))
            {
                _users.TryRemove(myId, out _);
                logger.LogInformation("[Voice] Client Left: {ClientId}", myId);
            }
        }
    }
}
