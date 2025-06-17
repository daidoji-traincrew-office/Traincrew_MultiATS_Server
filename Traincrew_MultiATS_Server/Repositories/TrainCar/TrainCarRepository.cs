using Microsoft.EntityFrameworkCore;
using Traincrew_MultiATS_Server.Data;
using Traincrew_MultiATS_Server.Models;

namespace Traincrew_MultiATS_Server.Repositories.TrainCar;

public class TrainCarRepository(ApplicationDbContext context) : ITrainCarRepository
{
    public async Task<List<TrainCarState>> GetByTrainNumber(string trainNumber)
    {
        return await context.TrainCarStates
            .Join(context.TrainStates,
                car => car.TrainStateId,
                train => train.Id,
                (car, train) => new { car, train })
            .Where(x => x.train.TrainNumber == trainNumber)
            .OrderBy(x => x.car.Index)
            .Select(x => x.car)
            .ToListAsync();
    }

    public async Task UpdateAll(long trainStateId, List<TrainCarState> carStates)
    {
        await using var transaction = await context.Database.BeginTransactionAsync();
        var oldCarStates = await context.TrainCarStates
            .Where(car => car.TrainStateId == trainStateId)
            .OrderBy(car => car.Index)
            .ToListAsync();
        // newCarStatesでループを回して更新または追加を行う
        for (var i = 0; i < carStates.Count; i++)
        {
            var newCarState = carStates[i];
            newCarState.Index = i + 1;
            newCarState.TrainStateId = trainStateId;
            if (i < oldCarStates.Count)
            {
                // 更新
                context.TrainCarStates.Update(newCarState);
            }
            else
            {
                // 新規追加
                context.TrainCarStates.Add(newCarState);
            }
        }
        
        // 削除処理
        for (var i = carStates.Count; i < oldCarStates.Count; i++)
        {
            context.TrainCarStates.Remove(oldCarStates[i]);
        }
        
        await context.SaveChangesAsync();
        await transaction.CommitAsync();
        // 関係するEntityのトラッキングを解除
        carStates.ForEach(carState =>
        {
            context.TrainCarStates.Entry(carState).State = EntityState.Detached;
        });
    }

    public async Task DeleteByTrainNumber(string trainNumber)
    {
        await context.TrainCarStates
            .Join(context.TrainStates,
                car => car.TrainStateId,
                train => train.Id,
                (car, train) => new { car, train })
            .Where(x => x.train.TrainNumber == trainNumber)
            .Select(x => x.car)
            .ExecuteDeleteAsync();
    }

    public async Task<List<TrainCarState>> GetAllOrderByTrainStateIdAndIndex()
    {
        return await context.TrainCarStates
            .OrderBy(car => car.TrainStateId)
            .ThenBy(car => car.Index)
            .ToListAsync();
    }
}
