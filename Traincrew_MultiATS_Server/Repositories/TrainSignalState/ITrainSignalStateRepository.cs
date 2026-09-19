namespace Traincrew_MultiATS_Server.Repositories.TrainSignalState;

public interface ITrainSignalStateRepository
{
    /// <summary>
    /// 指定された列車番号のTrainSignalStateをすべて削除
    /// </summary>
    /// <param name="trainNumber">列車番号</param>
    Task DeleteByTrainNumber(string trainNumber);
}
