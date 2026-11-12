namespace Traincrew_MultiATS_Server.Repositories.Datetime;

public interface IDateTimeRepository
{
    /// <summary>
    /// サーバーのローカル時刻を返す。インスタンスを作った時点の値で固定される(1 スコープの中で同じ値)。
    /// </summary>
    DateTime GetNow();

    /// <summary>
    /// 現在のUTCを返す。<see cref="GetNow"/> と違い、呼ぶたびにその時点の時刻を返す(経過時間を測るため)。
    /// </summary>
    DateTimeOffset GetUtcNow();
}
