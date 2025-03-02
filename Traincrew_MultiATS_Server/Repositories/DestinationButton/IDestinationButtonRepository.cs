namespace Traincrew_MultiATS_Server.Repositories.DestinationButton;

public interface IDestinationButtonRepository
{
    Task<Dictionary<string, Models.DestinationButton>> GetAllButtons();
    Task<Models.DestinationButton?> GetButtonByName(string name);
    Task<List<Models.DestinationButton?>> GetButtonsByStationNames(List<string> stationNames);
}