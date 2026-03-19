using gis_photo_sharing_app.Infrastructure.Identity;
using gis_photo_sharing_app.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace WebUI;

public class Program
{
    public static async Task Main(string[] args)
    {
        var host = CreateHostBuilder(args).Build();

        using (var scope = host.Services.CreateScope())
        {
            var services = scope.ServiceProvider;
            try
            {
                var context = services.GetRequiredService<ApplicationDbContext>();
                var config = services.GetRequiredService<IConfiguration>();
                var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
                if (config.GetValue<bool>("UseInMemoryDatabase"))
                {
                    await ApplicationDbContextSeed.SeedDefaultUserAsync(userManager);
                    await ApplicationDbContextSeed.SeedSampleDataAsync(context);
                }
            }
            catch { /* Seed optional */ }
        }

        await host.RunAsync();
    }

    public static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .ConfigureWebHostDefaults(webBuilder =>
            {
                webBuilder.UseStartup<gis_photo_sharing_app.WebUI.Startup>();
            });
}
