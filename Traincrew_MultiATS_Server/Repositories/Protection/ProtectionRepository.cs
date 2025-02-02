using Microsoft.EntityFrameworkCore;
using Traincrew_MultiATS_Server.Data;
using Traincrew_MultiATS_Server.Models;

namespace Traincrew_MultiATS_Server.Repositories.Protection;

public class ProtectionRepository(ApplicationDbContext context) : IProtectionRepository
{
    public async Task<bool> IsProtectionEnabled(int minProtectionZone, int maxProtectionZone)
    {
        return await context.protectionZoneStates
            .Where(x => minProtectionZone <=  x.ProtectionZone && x.ProtectionZone <= maxProtectionZone)
            .AnyAsync();
    }

    public async Task EnableProtection(string trainNumber, List<int> protectionZones)
    {
        context.protectionZoneStates.AddRange(protectionZones
            .Select(protectionZone => new ProtectionZoneState
            {
                TrainNumber = trainNumber,
                ProtectionZone = protectionZone
            }));
        await context.SaveChangesAsync();
    }

    public async Task DisableProtection(string trainNumber)
    {
        await context.protectionZoneStates
            .Where(x => x.TrainNumber == trainNumber)
            .ExecuteDeleteAsync();
    }
}