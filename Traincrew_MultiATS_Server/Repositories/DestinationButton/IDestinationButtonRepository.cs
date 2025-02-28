namespace Traincrew_MultiATS_Server.Repositories.DestinationButton;

public interface IDestinationButtonRepository
{
    Task<Dictionary<string, Models.DestinationButton>> GetAllButtons();
}