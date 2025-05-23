namespace Traincrew_MultiATS_Server.Repositories.TtcWindowLink
{
    public interface ITtcWindowLinkRepository
    {
        Task<List<Models.TtcWindowLink>> GetAllTtcWindowLink();
        Task<List<Models.TtcWindowLink>> GetTtcWindowLinkById(List<ulong> ttcWindowLinkIds);
        Task<List<Models.TtcWindowLinkRouteCondition>> GetAllTtcWindowLinkRouteConditions();
        Task<List<Models.TtcWindowLinkRouteCondition>> ttcWindowLinkRouteConditionsById(ulong ttcWindowLinkId);
    }
}
