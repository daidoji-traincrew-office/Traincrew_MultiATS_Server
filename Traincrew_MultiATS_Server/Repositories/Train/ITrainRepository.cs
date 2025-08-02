using Traincrew_MultiATS_Server.Models;

namespace Traincrew_MultiATS_Server.Repositories.Train;

public interface ITrainRepository
{
    /// <summary>
    /// ダイヤ番号から列車を取得する
    /// </summary>
    /// <param name="diaNumber">ダイヤ番号</param>
    /// <returns>列車の状態リスト</returns>
    Task<List<TrainState>> GetByDiaNumber(int diaNumber);

    /// <summary>
    /// 列車番号から列車を取得する
    /// </summary>
    /// <param name="trainNumbers">列車番号のリスト</param>
    /// <returns>列車の状態</returns>
    Task<List<TrainState>> GetByTrainNumbers(ICollection<string> trainNumbers);

    /// <summary>
    /// 運転士IDから列車を取得する
    /// </summary>
    /// <param name="driverId">運転士ID</param>
    /// <returns></returns>
    Task<TrainState?> GetByDriverId(ulong driverId);

    /// <summary>
    /// すべての列車情報を取得する
    /// </summary>
    /// <returns>全列車状態リスト</returns>
    Task<List<TrainState>> GetAll();

    /// <summary>
    /// IDから列車情報を取得する
    /// </summary>
    /// <param name="id">列車状態ID</param>
    /// <returns>列車の状態</returns>
    Task<TrainState?> GetById(long id);

    /// <summary>
    /// 列車番号を指定して列車を削除する。
    /// </summary>
    /// <param name="trainNumber">列車番号</param>
    Task DeleteByTrainNumber(string trainNumber);

    /// <summary>
    /// 列車情報を新規登録する
    /// </summary>
    /// <param name="trainState">列車状態</param>
    Task Create(TrainState trainState);

    /// <summary>
    /// 列車情報を更新する
    /// </summary>
    /// <param name="trainState">列車状態</param>
    Task Update(TrainState trainState);
}