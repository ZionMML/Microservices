using System.Runtime.CompilerServices;
using CommandService.Models;
using CommandService.SyncDataServices.Grpc;

namespace CommandService.Data;

public class PrepDb
{
    public static void PrepPopulation(IApplicationBuilder application)
    {
        using (var serviceScope = application.ApplicationServices.CreateScope())
        {
            var grpcClient = serviceScope.ServiceProvider.GetRequiredService<IPlatformDataClient>();

            var platforms = grpcClient.ReturnAllPlatforms();

            SeedData(serviceScope.ServiceProvider.GetRequiredService<ICommandRepo>(), platforms);
        }
    }

    private static void SeedData(ICommandRepo repo, IEnumerable<Platform> platforms)
    {
        Console.WriteLine("-->Seeding new platforms...");

        foreach (var plat in platforms)
        {
            if (!repo.ExternalPlatformExists(plat.Id))
            {
                repo.CreatePlatform(plat);
            }
            repo.SaveChanges();
        }
    }
}
