using Microsoft.EntityFrameworkCore;
using Traincrew_MultiATS_Server.Data;

namespace Traincrew_MultiATS_Server.Repositories.TrainSignalState;

public class TrainSignalStateRepository(ApplicationDbContext context) : ITrainSignalStateRepository
{
    public async Task<List<Models.TrainSignalState>> GetByTrainNumber(string trainNumber)
    {
        return await context.TrainSignalStates
            .Where(tss => tss.TrainNumber == trainNumber)
            .ToListAsync();
    }

    public async Task<List<string>> GetSignalNamesByTrainNumber(string trainNumber)
    {
        return await context.TrainSignalStates
            .Where(tss => tss.TrainNumber == trainNumber)
            .Select(tss => tss.SignalName)
            .ToListAsync();
    }

    public async Task UpdateByTrainNumber(string trainNumber, List<string> visibleSignalNames)
    {
        // 既存のTrainSignalStateを取得
        var existingStates = await GetByTrainNumber(trainNumber);

        var existingSignalNames = existingStates.Select(s => s.SignalName).ToHashSet();
        var newSignalNames = visibleSignalNames.ToHashSet();

        // DBにのみあるものを削除
        var toRemove = existingStates.Where(s => !newSignalNames.Contains(s.SignalName)).ToList();
        if (toRemove.Count != 0)
        {
            context.TrainSignalStates.RemoveRange(toRemove);
        }

        // AtsToServerDataにあってDBにないものを追加
        var toAdd = newSignalNames
            .Except(existingSignalNames)
            .Select(signalName => new Models.TrainSignalState
            {
                TrainNumber = trainNumber,
                SignalName = signalName
            }).ToList();
        if (toAdd.Count != 0)
        {
            context.TrainSignalStates.AddRange(toAdd);
        }

        if (toRemove.Count != 0 || toAdd.Count != 0)
        {
            await context.SaveChangesAsync();
        }
    }

    public async Task DeleteByTrainNumber(string trainNumber)
    {
        await context.TrainSignalStates
            .Where(tss => tss.TrainNumber == trainNumber)
            .ExecuteDeleteAsync();
    }
}