using Traincrew_MultiATS_Server.Models;

namespace Traincrew_MultiATS_Server.Repositories.OperationInformation;

public interface IOperationInformationRepository
{
    Task<List<OperationInformationState>> GetByNowOrderByTypeAndId(DateTime now);
    Task<List<OperationInformationState>> GetAllOrderByTypeAndId();
    Task<OperationInformationState> Add(OperationInformationState state);
    Task<OperationInformationState> Update(OperationInformationState state);
    Task DeleteById(long id);
}
