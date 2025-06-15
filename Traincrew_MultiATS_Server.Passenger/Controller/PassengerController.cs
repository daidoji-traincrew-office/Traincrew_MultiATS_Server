using Microsoft.AspNetCore.Mvc;
using Traincrew_MultiATS_Server.Services;

namespace Traincrew_MultiATS_Server.Passenger.Controller;

[ApiController]
public class PassengerController(PassengerService passengerService): ControllerBase
{
    [HttpGet("api/train")] 
    public async Task<IActionResult> GetTrainInfoAsync()
    {
        return Ok(await passengerService.GetServerToPassengerData());
    }
}