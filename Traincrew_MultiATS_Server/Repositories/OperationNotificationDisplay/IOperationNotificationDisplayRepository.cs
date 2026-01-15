namespace Traincrew_MultiATS_Server.Repositories.OperationNotificationDisplay;

public interface IOperationNotificationDisplayRepository
{
    /// <summary>
    /// 既存の全ての表示名を取得する
    /// </summary>
    /// <param name="cancellationToken">キャンセルトークン</param>
    /// <returns>表示名のリスト</returns>
    Task<List<string>> GetAllNames(CancellationToken cancellationToken = default);

    /// <summary>
    /// OperationNotificationDisplayを追加する
    /// </summary>
    /// <param name="operationNotificationDisplay">追加するOperationNotificationDisplay</param>
    /// <param name="cancellationToken">キャンセルトークン</param>
    Task AddAsync(Models.OperationNotificationDisplay operationNotificationDisplay, CancellationToken cancellationToken = default);
}
