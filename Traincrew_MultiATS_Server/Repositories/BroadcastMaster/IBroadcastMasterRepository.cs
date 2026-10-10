using Traincrew_MultiATS_Server.Services;

namespace Traincrew_MultiATS_Server.Repositories.BroadcastMaster;

public interface IBroadcastMasterRepository
{
    /// <summary>
    /// 配信が毎ティック使う、起動後不変のマスタ(interlocking_object系以外)を全件読み込む。
    /// 起動時のBroadcastMasterStoreのロード専用。
    /// </summary>
    Task<BroadcastMasterData> LoadAsync(CancellationToken cancellationToken = default);
}
