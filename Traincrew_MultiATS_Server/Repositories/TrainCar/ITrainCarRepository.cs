using Traincrew_MultiATS_Server.Models;

namespace Traincrew_MultiATS_Server.Repositories.TrainCar;

public interface ITrainCarRepository
{
    /// <summary>
    /// 列車番号で車両情報を取得する
    /// </summary>
    /// <param name="trainNumber">列車番号</param>
    /// <returns>車両状態リスト</returns>
    Task<List<TrainCarState>> GetByTrainNumber(string trainNumber);

    /// <summary>
    /// 指定列車番号の車両情報を全て更新する
    /// </summary>
    /// <param name="trainStateId">列車状態ID</param>
    /// <param name="carStates">車両状態リスト</param>
    Task UpdateAll(long trainStateId, List<TrainCarState> carStates);

    /// <summary>
    /// 指定列車番号の車両情報を全て削除する
    /// </summary>
    /// <param name="trainNumber">列車番号</param>
    Task DeleteByTrainNumber(string trainNumber);

    /// <summary>
    /// すべての車両情報を取得する
    /// </summary>
    /// <returns>全車両状態リスト</returns>
    Task<List<TrainCarState>> GetAll();
}
