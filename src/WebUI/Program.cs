using gis_photo_sharing_app.Infrastructure.Identity;
using gis_photo_sharing_app.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using gis_photo_sharing_app.WebUI.Configuration;

namespace WebUI;

public class Program
{
    public static async Task Main(string[] args)
    {
        var host = CreateHostBuilder(args).Build();

        using (var scope = host.Services.CreateScope())
        {
            var services = scope.ServiceProvider;
            var logger = services.GetRequiredService<ILogger<Program>>();
            var environment = services.GetRequiredService<IWebHostEnvironment>();
            var startupConfiguration = services.GetRequiredService<IConfiguration>();

            if (!environment.IsDevelopment())
            {
                var jwtKey = startupConfiguration["Jwt:Key"];
                if (!JwtKeyValidator.TryValidateProductionKey(jwtKey, out var reason))
                {
                    throw new InvalidOperationException(
                        $"Configure Jwt:Key with a unique randomly generated production secret. {reason}");
                }
            }

            try
            {
                var context = services.GetRequiredService<ApplicationDbContext>();
                var config = services.GetRequiredService<IConfiguration>();
                var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
                var useInMemory = config.GetValue<bool>("UseInMemoryDatabase");

                if (!useInMemory)
                {
                    if (context.Database.IsSqlite())
                    {
                        // SQLite uses the current EF model to create a self-contained local database.
                        await context.Database.EnsureCreatedAsync();
                        await SqliteSchemaUpgrader.UpgradeAsync(context, logger);
                    }
                    else
                    {
                        await context.Database.MigrateAsync();
                    }
                }

                if (config.GetValue<bool>("DatabaseSeed:DefaultUser"))
                {
                    await ApplicationDbContextSeed.SeedDefaultUserAsync(userManager);
                }

                if (config.GetValue<bool>("DatabaseSeed:SamplePhotos"))
                {
                    var seedOwner = await userManager.FindByNameAsync("administrator@localhost");
                    await ApplicationDbContextSeed.SeedSampleDataAsync(context, seedOwner?.Id);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Database initialization or seeding failed. If SQL Server is unavailable, set UseInMemoryDatabase to true in appsettings.");
            }
        }

        await host.RunAsync();
    }

    public static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .ConfigureLogging((context, logging) =>
            {
                // Windows Event Log can require privileges that ordinary development/service accounts do not have.
                logging.ClearProviders();
                logging.AddConfiguration(context.Configuration.GetSection("Logging"));
                logging.AddConsole();
                logging.AddDebug();
            })
            .ConfigureWebHostDefaults(webBuilder =>
            {
                webBuilder.UseStartup<gis_photo_sharing_app.WebUI.Startup>();
            });
}
