using Microsoft.Extensions.DependencyInjection;
using Traincrew_MultiATS_Server.IT.Fixture;
using Traincrew_MultiATS_Server.Repositories.UserDisconnection;

namespace Traincrew_MultiATS_Server.IT.Hubs;

[Collection("WebApplication")]
public class CommanderTableHubTest(WebApplicationFixture factory)
{
    [Fact(DisplayName = "ユーザーをBANできること")]
    public async Task BanUser_SuccessfullyBansUser()
    {
        const ulong testUserId = 12345UL;

        var (connection, hub) = factory.CreateCommanderTableHub();

        await using (connection)
        {
            await connection.StartAsync(TestContext.Current.CancellationToken);

            try
            {
                // Act: ユーザーをBAN
                await hub.BanUser(testUserId);

                // Assert: BANされたことを確認
                await using var scope = factory.Services.CreateAsyncScope();
                var repository = scope.ServiceProvider.GetRequiredService<IUserDisconnectionRepository>();
                var isBanned = await repository.IsUserBannedAsync(testUserId);
                Assert.True(isBanned);
            }
            finally
            {
                // Cleanup: BANを解除
                await hub.UnbanUser(testUserId);
            }
        }
    }

    [Fact(DisplayName = "ユーザーのBANを解除できること")]
    public async Task UnbanUser_SuccessfullyUnbansUser()
    {
        const ulong testUserId = 67890UL;

        var (connection, hub) = factory.CreateCommanderTableHub();

        await using (connection)
        {
            await connection.StartAsync(TestContext.Current.CancellationToken);

            // Arrange: 事前にユーザーをBANしておく
            await hub.BanUser(testUserId);

            try
            {
                // Act: BANを解除
                await hub.UnbanUser(testUserId);

                // Assert: BAN解除されたことを確認
                await using var scope = factory.Services.CreateAsyncScope();
                var repository = scope.ServiceProvider.GetRequiredService<IUserDisconnectionRepository>();
                var isBanned = await repository.IsUserBannedAsync(testUserId);
                Assert.False(isBanned);
            }
            finally
            {
                // Cleanup: 念のため再度BAN解除
                await using var scope = factory.Services.CreateAsyncScope();
                var repository = scope.ServiceProvider.GetRequiredService<IUserDisconnectionRepository>();
                if (await repository.IsUserBannedAsync(testUserId))
                {
                    await hub.UnbanUser(testUserId);
                }
            }
        }
    }

    [Fact(DisplayName = "同一ユーザーを複数回BANしてもエラーにならないこと")]
    public async Task BanUser_MultipleTimes_DoesNotThrowError()
    {
        const ulong testUserId = 11111UL;

        var (connection, hub) = factory.CreateCommanderTableHub();

        await using (connection)
        {
            await connection.StartAsync(TestContext.Current.CancellationToken);

            try
            {
                // Act: 複数回BAN
                await hub.BanUser(testUserId);
                await hub.BanUser(testUserId); // 2回目

                // Assert: エラーが発生せず、BANされたままであること
                await using var scope = factory.Services.CreateAsyncScope();
                var repository = scope.ServiceProvider.GetRequiredService<IUserDisconnectionRepository>();
                var isBanned = await repository.IsUserBannedAsync(testUserId);
                Assert.True(isBanned);
            }
            finally
            {
                // Cleanup: BANを解除
                await hub.UnbanUser(testUserId);
            }
        }
    }

    [Fact(DisplayName = "BANされていないユーザーをUnbanしてもエラーにならないこと")]
    public async Task UnbanUser_NotBannedUser_DoesNotThrowError()
    {
        const ulong testUserId = 22222UL;

        var (connection, hub) = factory.CreateCommanderTableHub();

        await using (connection)
        {
            await connection.StartAsync(TestContext.Current.CancellationToken);

            // Act & Assert: BANされていないユーザーをUnbanしてもエラーにならないこと
            await hub.UnbanUser(testUserId);

            // Assert: BANされていないことを確認
            await using var scope = factory.Services.CreateAsyncScope();
            var repository = scope.ServiceProvider.GetRequiredService<IUserDisconnectionRepository>();
            var isBanned = await repository.IsUserBannedAsync(testUserId);
            Assert.False(isBanned);
        }
    }

    [Fact(DisplayName = "複数のユーザーを独立してBAN/Unbanできること")]
    public async Task BanAndUnban_MultipleUsers_WorksIndependently()
    {
        const ulong user1 = 33333UL;
        const ulong user2 = 44444UL;

        var (connection, hub) = factory.CreateCommanderTableHub();

        await using (connection)
        {
            await connection.StartAsync(TestContext.Current.CancellationToken);

            try
            {
                // Act: 2人のユーザーをBAN
                await hub.BanUser(user1);
                await hub.BanUser(user2);

                // Assert: 両方BANされていること
                await using (var scope = factory.Services.CreateAsyncScope())
                {
                    var repository = scope.ServiceProvider.GetRequiredService<IUserDisconnectionRepository>();
                    Assert.True(await repository.IsUserBannedAsync(user1));
                    Assert.True(await repository.IsUserBannedAsync(user2));
                }

                // Act: user1のみBAN解除
                await hub.UnbanUser(user1);

                // Assert: user1はBAN解除、user2はBANされたまま
                await using (var scope = factory.Services.CreateAsyncScope())
                {
                    var repository = scope.ServiceProvider.GetRequiredService<IUserDisconnectionRepository>();
                    Assert.False(await repository.IsUserBannedAsync(user1));
                    Assert.True(await repository.IsUserBannedAsync(user2));
                }
            }
            finally
            {
                // Cleanup: 両方のBANを解除
                await hub.UnbanUser(user1);
                await hub.UnbanUser(user2);
            }
        }
    }

    [Fact(DisplayName = "GetBannedUserIdsAsync でBANされたユーザーリストを取得できること")]
    public async Task GetBannedUserIds_ReturnsCorrectList()
    {
        const ulong user1 = 55555UL;
        const ulong user2 = 66666UL;

        var (connection, hub) = factory.CreateCommanderTableHub();

        await using (connection)
        {
            await connection.StartAsync(TestContext.Current.CancellationToken);

            try
            {
                // Arrange: 2人のユーザーをBAN
                await hub.BanUser(user1);
                await hub.BanUser(user2);

                // Act: BANされたユーザーリストを取得
                await using var scope = factory.Services.CreateAsyncScope();
                var repository = scope.ServiceProvider.GetRequiredService<IUserDisconnectionRepository>();
                var bannedUsers = await repository.GetBannedUserIdsAsync();

                // Assert: リストに両方含まれていること
                Assert.Contains(user1, bannedUsers);
                Assert.Contains(user2, bannedUsers);
            }
            finally
            {
                // Cleanup: 両方のBANを解除
                await hub.UnbanUser(user1);
                await hub.UnbanUser(user2);
            }
        }
    }
}
