using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using PlatformService.Models;

namespace PlatformService.Data;

public static class PrepDb
{
    public static void PrepPopulation(IApplicationBuilder app, bool isProd)
    {
        using (var serviceScope = app.ApplicationServices.CreateScope())
        {
            SeedData(serviceScope.ServiceProvider.GetRequiredService<AppDbContext>(), isProd);
        }
    }

    private static void SeedData(AppDbContext context, bool isProd)
    {
        if (isProd)
        {
            Console.WriteLine("--> Attempting to apply migrations...");

            var retryCount = 0;
            var maxRetries = 5;

            while (retryCount < maxRetries)
            {
                try
                {
                    context.Database.Migrate();
                    Console.WriteLine("--> Migrations applied successfully");
                    break;
                }
                catch (Exception ex)
                {
                    retryCount++;
                    Console.WriteLine($"--> Could not run migrations:: {ex.Message}");
                    Thread.Sleep(2000 * retryCount); // exponential backoff
                }
            }
        }
        if (!context.Platforms.Any())
        {
            Console.WriteLine("--> Seeding Data...");

            //context.Database.ExecuteSqlRaw("SET IDENTITY_INSERT Platforms ON;");

            context.Platforms.AddRange(
                new Platform()
                {
                    Name = "Dot Net",
                    Publisher = "Microsoft",
                    Cost = "Free",
                },
                new Platform()
                {
                    Name = "SQL Server Express",
                    Publisher = "Microsoft",
                    Cost = "Free",
                },
                new Platform()
                {
                    Name = "Kubernetes",
                    Publisher = "Cloud Native Coomputing Foundation",
                    Cost = "Free",
                }
            );

            context.SaveChanges();
        }
        else
        {
            Console.WriteLine("--> We already have data");
        }
    }
}
