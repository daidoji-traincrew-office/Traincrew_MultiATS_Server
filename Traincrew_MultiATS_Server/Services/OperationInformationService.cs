using Traincrew_MultiATS_Server.Common.Models;
using Traincrew_MultiATS_Server.Models;
using Traincrew_MultiATS_Server.Repositories.Datetime;
using Traincrew_MultiATS_Server.Repositories.OperationInformation;

namespace Traincrew_MultiATS_Server.Services;

public class OperationInformationService(
    IOperationInformationRepository operationInformationRepository,
    IDateTimeRepository dateTimeRepository
)
{
    public async Task AddOperationInformation(OperationInformationData data)
    {
        var operationInformationState = new OperationInformationState
        {
            Type = data.Type,
            Content = data.Content,
            StartTime = data.StartTime,
            EndTime = data.EndTime,
        };

        await operationInformationRepository.Add(operationInformationState);
    }
    
    public async Task UpdateOperationInformation(OperationInformationData data)
    {
        var operationInformationState = new OperationInformationState
        {
            Id = data.Id,
            Type = data.Type,
            Content = data.Content,
            StartTime = data.StartTime,
            EndTime = data.EndTime,
        };
        await operationInformationRepository.Update(operationInformationState);
    }

    public async Task<List<OperationInformationData>> GetOperationInformations()
    {
        var now = dateTimeRepository.GetNow();
        var states = await operationInformationRepository.GetByNow(now);
        return states.Select(ToOperationInformationData).ToList();
    }

    public async Task<List<OperationInformationData>> GetAllOperationInformations()
    {
        var states = await operationInformationRepository.GetAll();
        return states.Select(ToOperationInformationData).ToList();
    }

    public async Task DeleteOperationInformation(long id)
    {
        await operationInformationRepository.DeleteById(id);
    }

    private static OperationInformationData ToOperationInformationData(OperationInformationState x)
    {
        return new()
        {
            Id = x.Id,
            Content = x.Content,
            Type = x.Type,
            StartTime = x.StartTime,
            EndTime = x.EndTime
        };
    }
}
