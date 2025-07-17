using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Npgsql;

namespace Traincrew_MultiATS_Server.Data;

// dbcontext optimizeで使うのでUnusedではない
// ReSharper disable once UnusedType.Global
public class DesignTimeApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        try
        {
            var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
            // NpgsqlDataSourceBuilderを使ってNpgsqlのEnumマッピングを行う
            // connectionStringは適当にでも入れないとBuild()が失敗するので、適当な値を入れる(実際のDB接続は発生しない)
            var dataSourceBuilder = new NpgsqlDataSourceBuilder("Host=localhost");
            EnumTypeMapper.MapEnumForNpgsql(dataSourceBuilder);
            optionsBuilder.UseNpgsql(dataSourceBuilder.Build());
            // OpenIddictの設定を追加
            // Todo: 本来はDbCotextを分けるべきだが、PropertyのSnakeCase化が適用されているので、一旦一緒くたに設定してしまう
            optionsBuilder.UseOpenIddict();
            return new(optionsBuilder.Options);
        }
        catch (System.Exception ex)
        {
            Console.WriteLine($"{ex.Message} {ex.StackTrace}");
            throw;
        }
    }
}