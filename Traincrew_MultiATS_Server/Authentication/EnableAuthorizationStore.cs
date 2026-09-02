namespace Traincrew_MultiATS_Server.Authentication;

public class EnableAuthorizationStore(bool enableAuthorization = true)
{
    /// <summary>
    /// Authorization機能を有効にするかどうか
    /// </summary>
    public bool EnableAuthorization { get; } = enableAuthorization;
}