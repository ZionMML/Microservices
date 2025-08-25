using Microsoft.IdentityModel.Tokens;
using PlatformService.Models;

namespace PlatformService.Data;

public static class PrepDb
{
    public static void PrepPopulation(IApplicationBuilder app)
    {
        using (var serviceScope = app.ApplicationServices.CreateScope())
        {
            SeedData(serviceScope.ServiceProvider.GetRequiredService<AppDbContext>());
        }
    }

    private static void SeedData(AppDbContext context)
    {
        if (!context.Platforms.Any())
        {
            Console.WriteLine("--> Sedding Data...");

            context.Platforms.AddRange(
                new Platform() { Id = 1, Name = "Dot Net", Publisher = "Microsoft", Cost = "Free" },
                new Platform() { Id = 2, Name = "SQL Server Express", Publisher = "Microsoft", Cost = "Free" },
                new Platform() { Id = 3, Name = "Kubernetes", Publisher = "Cloud Native Coomputing Foundation", Cost = "Free" }
            );

            context.SaveChanges();
        }
        else
        {
            Console.WriteLine("--> We already have data");
        }
    }
}