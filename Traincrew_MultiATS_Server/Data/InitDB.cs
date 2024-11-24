using Discord;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace Traincrew_MultiATS_Server.Data
{

    public class InitDB
    {
        public static async Task initDB()
        {
            var builder = WebApplication.CreateBuilder();
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"))
                .Options;
            var jsonstring = File.ReadAllText("./Data/DB_Base.json");
            var DBBase = JsonSerializer.Deserialize<Models.DBBasejson>(jsonstring);
            using(var context = new ApplicationDbContext(options))
            {
                context.Database.EnsureCreated();

                    var Circuits = new List<Models.Circuit>();
                    foreach(var item in DBBase.trackCircuitList)
                    {
                        Circuits.Add(new Models.Circuit{Name = item.Name, BougoZone = 1, DiaName = "", isLock = false});
                    }
                    context.Circuits.AddRange(Circuits);
                    context.SaveChanges();
                    
            }
        }
    }
}
