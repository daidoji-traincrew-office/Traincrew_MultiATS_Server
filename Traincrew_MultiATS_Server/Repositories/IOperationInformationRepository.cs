using Traincrew_MultiATS_Server.Models;

namespace Traincrew_MultiATS_Server.Repositories;

public interface IOperationInformationRepository
{
    Task<List<OperationInformationState>> GetOperationInformationsByNow(DateTime now);
    Task AddOperationInformation(OperationInformationState state);
    Task UpdateOperationInformation(OperationInformationState state);
}
