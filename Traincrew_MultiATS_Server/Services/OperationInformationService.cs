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
    public async Task<OperationInformationData> AddOperationInformation(OperationInformationData data)
    {
        var operationInformationState = new OperationInformationState
        {
            Type = data.Type,
            Content = data.Content,
            StartTime = data.StartTime,
            EndTime = data.EndTime,
        };

        var result = await operationInformationRepository.Add(operationInformationState);
        return ToOperationInformationData(result);
    }
    
    public async Task<OperationInformationData> UpdateOperationInformation(OperationInformationData data)
    {
        var operationInformationState = new OperationInformationState
        {
            Id = data.Id,
            Type = data.Type,
            Content = data.Content,
            StartTime = data.StartTime,
            EndTime = data.EndTime,
        };
        var result = await operationInformationRepository.Update(operationInformationState);
        return ToOperationInformationData(result);
    }

    public async Task<List<OperationInformationData>> GetOperationInformations()
    {
        return await GetOperationInformationsOrderByType();
    }

    public async Task<List<OperationInformationData>> GetAllOperationInformations()
    {
        return await GetAllOperationInformationsOrderByType();
    }

    public async Task<List<OperationInformationData>> GetOperationInformationsOrderByType()
    {
        var now = dateTimeRepository.GetNow();
        var states = await operationInformationRepository.GetByNowOrderByTypeAndId(now);
        return states.Select(ToOperationInformationData).ToList();
    }

    public async Task<List<OperationInformationData>> GetAllOperationInformationsOrderByType()
    {
        var states = await operationInformationRepository.GetAllOrderByTypeAndId();
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
