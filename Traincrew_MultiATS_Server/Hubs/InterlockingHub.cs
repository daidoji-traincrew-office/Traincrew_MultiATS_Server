using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using OpenIddict.Validation.AspNetCore;
using Traincrew_MultiATS_Server.Common.Contract;
using Traincrew_MultiATS_Server.Common.Models;
using Traincrew_MultiATS_Server.Services;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace Traincrew_MultiATS_Server.Hubs;

// 信号係員操作可・司令主任鍵使用可 
[Authorize(
    AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme,
    Policy = "InterlockingPolicy"
)]
public class InterlockingHub(InterlockingService interlockingService) : Hub<IInterlockingClientContract>, IInterlockingHubContract
{
    public async Task<DataToInterlocking> SendData_Interlocking(List<string> activeStationsList)
    {
        return await interlockingService.SendData_Interlocking();
    }

    public async Task<InterlockingLeverData> SetPhysicalLeverData(InterlockingLeverData leverData)
    {
        return await interlockingService.SetPhysicalLeverData(leverData);
    }

    public async Task<InterlockingKeyLeverData> SetPhysicalKeyLeverData(InterlockingKeyLeverData keyLeverData)
    {
        // MemberIDを取得
        var memberIdString = Context.User?.FindFirst(Claims.Subject)?.Value;
        ulong? memberId = memberIdString != null ? ulong.Parse(memberIdString) : null;
        return await interlockingService.SetPhysicalKeyLeverData(keyLeverData, memberId);
    }

    public async Task<DestinationButtonData> SetDestinationButtonState(DestinationButtonData buttonData)
    {
        return await interlockingService.SetDestinationButtonState(buttonData);
    }
}
