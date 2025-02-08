using Microsoft.AspNetCore.SignalR;
using Traincrew_MultiATS_Server.Services;
using Microsoft.AspNetCore.Authorization;
using OpenIddict.Validation.AspNetCore;


namespace Traincrew_MultiATS_Server.Hubs;

[Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
public class InterlockingHub(TrackCircuitService trackCircuitService) : Hub
{
	public async Task<Models.DataToInterlocking> SendData_TID(Models.DataFromInterlocking dataFromInterlocking)
	{
		Models.DataToInterlocking response = new Models.DataToInterlocking();
		response.TrackCircuitDataList = await trackCircuitService.GetAllTrackCircuitDataList();
		return response;
	}
}