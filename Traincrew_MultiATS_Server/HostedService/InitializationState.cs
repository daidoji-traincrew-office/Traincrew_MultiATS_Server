namespace Traincrew_MultiATS_Server.HostedService;

/// <summary>
/// アプリケーションの初期化完了状態を保持するシングルトン
/// InitDbHostedService がDB初期化オーケストレータとスケジューラ起動を完走した後にのみ true にする。
/// ヘルスチェックエンドポイント(/healthz)から参照される。
/// </summary>
public class InitializationState
{
    /// <summary>
    /// 初期化が完了しているかどうか
    /// HostedServiceのStartAsyncからWebサーバのリクエスト処理スレッドまで、
    /// 単純な書き込み後読み込みの可視性のみが必要なため volatile bool で十分
    /// </summary>
    private volatile bool _isInitialized;

    public bool IsInitialized => _isInitialized;

    public void MarkInitialized()
    {
        _isInitialized = true;
    }
}
