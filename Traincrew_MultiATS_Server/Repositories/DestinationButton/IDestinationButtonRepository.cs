namespace Traincrew_MultiATS_Server.Repositories.DestinationButton;

public interface IDestinationButtonRepository
{
    Task<List<Models.DestinationButton>> GetAllWithState();
    Task<Models.DestinationButton?> GetButtonByName(string name);
    Task<List<Models.DestinationButton>> GetButtonsByStationIds(List<string> stationIds);
}