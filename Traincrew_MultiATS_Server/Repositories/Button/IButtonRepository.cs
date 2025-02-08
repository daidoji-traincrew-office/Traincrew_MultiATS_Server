namespace Traincrew_MultiATS_Server.Repositories.Button;

public interface IButtonRepository
{
    Task<Dictionary<string, Models.Button>> GetAllButtons();
}