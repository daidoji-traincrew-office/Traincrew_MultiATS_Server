using Microsoft.AspNetCore.Authorization;
using Traincrew_MultiATS_Server.Models;

namespace Traincrew_MultiATS_Server.Authentication;

public class DiscordRoleRequirement : IAuthorizationRequirement
{
    public required Predicate<TraincrewRole> Condition { get; init; }
}