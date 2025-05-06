namespace Traincrew_MultiATS_Server.Repositories.TtcWindow
{
    public interface ITtcWindowRepository
    {
        Task<List<Models.TtcWindow>> GetAllTtcWindowWithState();
        Task<List<Models.TtcWindow>> GetTtcWindowWithStateByName(List<string> name);
        Task<List<Models.TtcWindowTrackCircuit>> GetWindowTrackCircuits();
        Task<List<Models.TtcWindowTrackCircuit>> ttcWindowTrackCircuitsById(List<string> ttcWindowName);
    }
}
