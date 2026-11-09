using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Moq;
using Traincrew_MultiATS_Server.Authentication;
using Traincrew_MultiATS_Server.Exception.DiscordAuthenticationException;
using Traincrew_MultiATS_Server.Models;
using Traincrew_MultiATS_Server.Services;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace Traincrew_MultiATS_Server.UT.Authentication;

/// <summary>
/// Discord のロールによる認可のテスト。Fail は 403 になる。
/// </summary>
public class DiscordRoleHandlerTest
{
    private const ulong MemberId = 900000000000740692UL;

    private readonly Mock<IDiscordService> _discordServiceMock = new();
    private readonly DiscordRoleHandler _handler;

    private readonly DiscordRoleRequirement _beginnerRequirement = new()
    {
        Condition = role => role.IsBeginner
    };

    public DiscordRoleHandlerTest()
    {
        _handler = new(_discordServiceMock.Object);
    }

    [Fact(DisplayName = "入鋏ロールがあれば成功すること")]
    public async Task Beginner_Succeeds()
    {
        SetupRole(CreateRole(isBeginner: true));

        var context = await HandleAsync(MemberId.ToString());

        Assert.True(context.HasSucceeded);
        _discordServiceMock.Verify(s => s.GetRoleByMemberId(MemberId), Times.Once);
    }

    [Fact(DisplayName = "入鋏ロールが無ければ失敗すること")]
    public async Task NotBeginner_Fails()
    {
        SetupRole(CreateRole(isBeginner: false));

        var context = await HandleAsync(MemberId.ToString());

        Assert.True(context.HasFailed);
    }

    [Fact(DisplayName = "運転会サーバーのメンバーでなければ、例外を外に出さずに失敗すること")]
    public async Task NotMember_FailsWithoutException()
    {
        _discordServiceMock
            .Setup(s => s.GetRoleByMemberId(MemberId))
            .ThrowsAsync(new DiscordAuthenticationException($"Member ID: {MemberId} not found."));

        var context = await HandleAsync(MemberId.ToString());

        Assert.True(context.HasFailed);
    }

    [Theory(DisplayName = "sub が無い・Discord ID として読めないなら、Discord に問い合わせずに失敗すること")]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("alice")]
    [InlineData("-1")]
    public async Task InvalidSubject_Fails(string? subject)
    {
        var context = await HandleAsync(subject);

        Assert.True(context.HasFailed);
        _discordServiceMock.Verify(s => s.GetRoleByMemberId(It.IsAny<ulong?>()), Times.Never);
    }

    private void SetupRole(TraincrewRole role)
    {
        _discordServiceMock
            .Setup(s => s.GetRoleByMemberId(MemberId))
            .ReturnsAsync(role);
    }

    private async Task<AuthorizationHandlerContext> HandleAsync(string? subject)
    {
        var claims = subject is null ? [] : new[] { new Claim(Claims.Subject, subject) };
        var user = new ClaimsPrincipal(new ClaimsIdentity(claims, "Test"));
        var context = new AuthorizationHandlerContext([_beginnerRequirement], user, null);
        await _handler.HandleAsync(context);
        return context;
    }

    private static TraincrewRole CreateRole(bool isBeginner) => new()
    {
        IsBeginner = isBeginner,
        IsDriver = false,
        IsDriverManager = false,
        IsConductor = false,
        IsCommander = false,
        IsSignalman = false,
        IsAdministrator = false
    };
}
