using Traincrew_MultiATS_Server.Models;

namespace Traincrew_MultiATS_Server.Repositories.OperationInformation;

public interface IOperationInformationRepository
{
    Task<List<OperationInformationState>> GetByNow(DateTime now);
    Task<List<OperationInformationState>> GetAll();
    Task<OperationInformationState> Add(OperationInformationState state);
    Task<OperationInformationState> Update(OperationInformationState state);
    Task DeleteById(long id);
}
