using System;
using System.IO;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace gis_photo_sharing_app.Infrastructure.Persistence;

public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Path.Combine(Directory.GetCurrentDirectory(), "../WebUI"))
            .AddJsonFile("appsettings.json", optional: false)
            .Build();

        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
        var connStr = configuration.GetConnectionString("DefaultConnection")
            ?? "Server=(localdb)\\mssqllocaldb;Database=gis_photo_sharing_app;Trusted_Connection=True;";
        optionsBuilder.UseSqlServer(connStr, b => b.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName));

        var currentUserService = new DesignTimeCurrentUserService();
        var dateTime = new DesignTimeDateTimeService();

        return new ApplicationDbContext(optionsBuilder.Options, currentUserService, dateTime);
    }

    private class DesignTimeCurrentUserService : Application.Common.Interfaces.ICurrentUserService
    {
        public string UserId => "design-time-user";
    }

    private class DesignTimeDateTimeService : Application.Common.Interfaces.IDateTime
    {
        public DateTime Now => DateTime.UtcNow;
    }
}
