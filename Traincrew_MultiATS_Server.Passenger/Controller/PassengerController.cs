using Microsoft.AspNetCore.Mvc;
using Traincrew_MultiATS_Server.Services;

namespace Traincrew_MultiATS_Server.Passenger.Controller;

[ApiController]
public class PassengerController(
    IPassengerService passengerService,
    IServerService serverService) : ControllerBase
{
    [HttpGet("api/train")]
    public async Task<IActionResult> GetTrainInfoAsync()
    {
        return Ok(await passengerService.GetServerToPassengerData());
    }

    /// <summary>
    /// 現在のサーバーモードを返す。CIが運転会実施中かを判定するために使う無認証エンドポイント。
    /// mutexを取ると運転会中のスケジューラと競合するため、ロックなしで読む。
    /// </summary>
    [HttpGet("api/server-mode")]
    public async Task<IActionResult> GetServerModeAsync()
    {
        var mode = await serverService.GetServerModeAsyncWithoutLock();
        return Ok(new { mode = mode.ToString() });
    }
}