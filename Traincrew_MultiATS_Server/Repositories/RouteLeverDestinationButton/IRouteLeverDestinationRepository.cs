namespace Traincrew_MultiATS_Server.Repositories.RouteLeverDestinationButton;

public interface IRouteLeverDestinationRepository
{
   Task<List<Models.RouteLeverDestinationButton>> GetAll();
}