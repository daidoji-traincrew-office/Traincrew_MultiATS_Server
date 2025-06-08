using Traincrew_MultiATS_Server.Models;

namespace Traincrew_MultiATS_Server.Repositories.Train;

public interface ITrainRepository
{
    /// <summary>
    /// 列車番号から列車を取得する
    /// </summary>
    /// <param name="trainNumber">列車番号</param>
    /// <returns>列車の状態</returns>
    Task<TrainState?> GetTrainByNumber(string trainNumber);
 
    /// <summary>
    /// 列車番号を指定して列車を削除する。
    /// </summary>
    /// <param name="trainNumber">列車番号</param>
    Task DeleteTrain(string trainNumber);
}