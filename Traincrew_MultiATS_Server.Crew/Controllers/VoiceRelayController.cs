using System.Collections.Concurrent;
using Grpc.Core;
using Microsoft.AspNetCore.Authorization;
using OpenIddict.Abstractions;
using OpenIddict.Validation.AspNetCore;
using RailwayPhone.Protos;
using Traincrew_MultiATS_Server.Services;

namespace Traincrew_MultiATS_Server.Crew.Controllers;

/// <summary>
/// gRPC音声リレーサービス（双方向ストリーミング）
/// </summary>
[Authorize(
    AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme,
    Policy = "PhonePolicy"
)]
public class VoiceRelayController(
    ILogger<VoiceRelayController> logger,
    PhoneSessionStore sessionStore
) : VoiceRelay.VoiceRelayBase
{
    // 接続中のクライアントの「出口（書き込みストリーム）」をUserIDで管理する辞書
    private static readonly ConcurrentDictionary<string, IServerStreamWriter<VoiceData>> _users = new();

    public override async Task JoinSession(
        IAsyncStreamReader<VoiceData> requestStream,
        IServerStreamWriter<VoiceData> responseStream,
        ServerCallContext context)
    {
        var myUserId = context.GetHttpContext()?.User
            .FindFirst(OpenIddictConstants.Claims.Subject)?.Value;
        if (string.IsNullOrEmpty(myUserId))
        {
            throw new RpcException(new Status(StatusCode.Unauthenticated, "User not authenticated"));
        }

        _users[myUserId] = responseStream;
        logger.LogInformation("[Voice] Client Joined: {UserId}", myUserId);

        try
        {
            await foreach (var data in requestStream.ReadAllAsync())
            {
                var partnerId = sessionStore.GetCallPartnerUserId(myUserId);
                if (partnerId == null || !_users.TryGetValue(partnerId, out var targetStream))
                {
                    continue;
                }

                try
                {
                    await targetStream.WriteAsync(new VoiceData
                    {
                        AudioContent = data.AudioContent
                    });
                }
                catch (System.Exception ex)
                {
                    logger.LogWarning("[Voice] Failed to send to {PartnerId}: {Message}", partnerId, ex.Message);
                }
            }
        }
        catch (System.Exception ex)
        {
            logger.LogError(ex, "[Voice] Error in JoinSession");
        }
        finally
        {
            _users.TryRemove(myUserId, out _);
            logger.LogInformation("[Voice] Client Left: {UserId}", myUserId);
        }
    }
}
