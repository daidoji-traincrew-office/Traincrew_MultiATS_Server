namespace Traincrew_MultiATS_Server.Repositories.NextSignal;

public interface INextSignalRepository
{
    /// <summary>
    /// 指定された信号機名のリストに一致するNextSignalをDepth降順で取得します。
    /// </summary>
    /// <param name="signalNames">信号機名のリスト</param>
    /// <returns>NextSignalのリスト（Depth降順）</returns>
    public Task<List<Models.NextSignal>> GetNextSignalByNamesOrderByDepthDesc(List<string> signalNames);

    /// <summary>
    /// 指定された信号機名のリストとDepthに完全一致するNextSignalを取得します。
    /// </summary>
    /// <param name="signalNames">信号機名のリスト</param>
    /// <param name="depth">完全一致させるDepth値</param>
    /// <returns>条件に一致するNextSignalのリスト</returns>
    public Task<List<Models.NextSignal>> GetByNamesAndDepth(List<string> signalNames, int depth);

    /// <summary>
    /// 指定されたDepthに完全一致するすべてのNextSignalを取得します。
    /// </summary>
    /// <param name="depth">完全一致させるDepth値</param>
    /// <returns>条件に一致するNextSignalのリスト</returns>
    public Task<List<Models.NextSignal>> GetAllByDepth(int depth);

    /// <summary>
    /// 指定された信号機名のリストに一致し、Depth以下のNextSignalをDepth昇順で取得します。
    /// </summary>
    /// <param name="signalNames">信号機名のリスト</param>
    /// <param name="depth">最大Depth値（この値以下のものを取得）</param>
    /// <returns>NextSignalのリスト（Depth昇順）</returns>
    public Task<List<Models.NextSignal>> GetByNamesAndMaxDepthOrderByDepth(List<string> signalNames, int depth);
}