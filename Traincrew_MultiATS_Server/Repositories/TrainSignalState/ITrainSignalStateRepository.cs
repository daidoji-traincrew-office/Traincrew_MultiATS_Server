namespace Traincrew_MultiATS_Server.Repositories.TrainSignalState;

public interface ITrainSignalStateRepository
{
    /// <summary>
    /// 指定された列車番号の信号機名リストを取得
    /// </summary>
    Task<List<string>> GetSignalNamesByTrainNumber(string trainNumber);

    /// <summary>
    /// 指定された列車番号のTrainSignalStateを更新
    /// DBにのみあるものは削除し、visibleSignalNamesにあるものは追加する
    /// </summary>
    /// <param name="trainNumber">列車番号</param>
    /// <param name="visibleSignalNames">可視信号機名リスト</param>
    Task UpdateByTrainNumber(string trainNumber, List<string> visibleSignalNames);

    /// <summary>
    /// 指定された列車番号のTrainSignalStateをすべて削除
    /// </summary>
    /// <param name="trainNumber">列車番号</param>
    Task DeleteByTrainNumber(string trainNumber);
}
