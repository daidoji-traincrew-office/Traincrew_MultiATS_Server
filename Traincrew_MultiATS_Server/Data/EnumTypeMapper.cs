using Npgsql.TypeMapping;
using Traincrew_MultiATS_Server.Common.Models;
using Traincrew_MultiATS_Server.Models;

namespace Traincrew_MultiATS_Server.Data;

public static class EnumTypeMapper
{
    public static void MapEnumForNpgsql(INpgsqlTypeMapper typeMapper)
    {
        typeMapper.MapEnum<LockType>();
        typeMapper.MapEnum<NR>();
        typeMapper.MapEnum<NRC>();
        typeMapper.MapEnum<LCR>();
        typeMapper.MapEnum<ObjectType>();
        typeMapper.MapEnum<SignalIndication>();
        typeMapper.MapEnum<LockConditionType>();
        typeMapper.MapEnum<LeverType>();
        typeMapper.MapEnum<RouteType>();
        typeMapper.MapEnum<RaiseDrop>();
        typeMapper.MapEnum<RaiseDropWithForce>();
        typeMapper.MapEnum<OperationNotificationType>();
        typeMapper.MapEnum<LR>();
        typeMapper.MapEnum<TtcWindowType>();
        typeMapper.MapEnum<TtcWindowLinkType>();
        typeMapper.MapEnum<OperationInformationType>();
        typeMapper.MapEnum<ServerMode>();
        typeMapper.MapEnum<ThrowOutControlType>();
    }
}