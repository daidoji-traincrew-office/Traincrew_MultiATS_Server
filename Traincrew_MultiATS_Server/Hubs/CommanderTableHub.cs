using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using OpenIddict.Validation.AspNetCore;
using Traincrew_MultiATS_Server.Models;
using Traincrew_MultiATS_Server.Services;

namespace Traincrew_MultiATS_Server.Hubs;

// 司令員操作可 
[Authorize(
    AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme,
    Policy = "CommanderTablePolicy"
)]
public class CommanderTableHub(TrackCircuitService trackCircuitService) : Hub
{
    public async Task<DataToCommanderTable> SendData_CommanderTable()
    {
        return new()
        {
            TroubleDataList = [],
            KokuchiDataList = [],
            TrackCircuitDataList = await trackCircuitService.GetAllTrackCircuitDataList() 
        };
    }

    public async Task SendTroubleData(TroubleData troubleData)
    {
        
    }
    
    public async Task SendKokuchiData(KokuchiData kokuchiData)
    {
        
    }
    
    public async Task SendTrackCircuitData(TrackCircuitData trackCircuitData)
    {
        // 受け取ったtrackCircuitDataの値を設定する
        await trackCircuitService.SetTrackCircuitData(trackCircuitData); 
    }
    
    public async Task DeleteTrain(string trainName)
    {
        await trackCircuitService.ClearTrackCircuitByTrainNumber(trainName);
    }
}