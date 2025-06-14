using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using OpenIddict.Validation.AspNetCore;
using Traincrew_MultiATS_Server.Authentication;
using Traincrew_MultiATS_Server.Common.Contract;
using Traincrew_MultiATS_Server.Common.Models;
using Traincrew_MultiATS_Server.Services;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace Traincrew_MultiATS_Server.Hubs;

// 運転士 or 車掌使用可能
[Authorize(
    AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme,
    Policy = "TrainPolicy"
)]
public class TrainHub(
    TrainService trainService,
    EnableAuthorizationStore enableAuthorizationStore
) : Hub<ITrainClientContract>, ITrainHubContract
{
    public async Task<ServerToATSData> SendData_ATS(AtsToServerData clientData)
    {
        var enableAuthorization = enableAuthorizationStore.EnableAuthorization;
        // MemberIDを取得
        var memberIdString = Context.User?.FindFirst(Claims.Subject)?.Value;
        if (!ulong.TryParse(memberIdString, out var memberId))
        {
            if (enableAuthorization)
            {
                // Authorizationが有効な場合は、MemberIDの取得に失敗したらエラー
                throw new InvalidOperationException("Failed to retrieve MemberID.");
            }
            // Authorizationが無効な場合は、memberIdを0に設定(ローカル開発用)
            memberId = 0;
        }
        return await trainService.CreateAtsData(memberId, clientData);
    }
}