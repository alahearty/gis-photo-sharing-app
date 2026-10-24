using System;
using System.IO;
using System.Text;
using gis_photo_sharing_app.Application.Common.Interfaces;
using gis_photo_sharing_app.Application.Common.Models;
using gis_photo_sharing_app.Infrastructure.Files;
using gis_photo_sharing_app.Infrastructure.Identity;
using gis_photo_sharing_app.Infrastructure.Persistence;
using gis_photo_sharing_app.Infrastructure.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace gis_photo_sharing_app.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(
            this IServiceCollection services,
            IConfiguration configuration,
            string? contentRootPath = null)
        {
            if (configuration.GetValue<bool>("UseInMemoryDatabase"))
            {
                services.AddDbContext<ApplicationDbContext>(options =>
                    options.UseInMemoryDatabase("gis_photo_sharing_appDb"));
            }
            else if (string.Equals(configuration["DatabaseProvider"], "SqlServer", StringComparison.OrdinalIgnoreCase))
            {
                services.AddDbContext<ApplicationDbContext>(options =>
                    options.UseSqlServer(
                        configuration.GetConnectionString("DefaultConnection"),
                        b => b.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName)));
            }
            else
            {
                // SQLite is the zero-install persistent default for student handover.
                var connectionString = configuration.GetConnectionString("SqliteConnection")
                    ?? "Data Source=gis_photo_sharing_app.db";
                var sqlite = new SqliteConnectionStringBuilder(connectionString);

                if (!Path.IsPathRooted(sqlite.DataSource) && sqlite.DataSource != ":memory:")
                {
                    var dataDirectory = Path.Combine(
                        contentRootPath ?? Directory.GetCurrentDirectory(),
                        "App_Data");
                    Directory.CreateDirectory(dataDirectory);
                    sqlite.DataSource = Path.Combine(dataDirectory, sqlite.DataSource);
                }

                services.AddDbContext<ApplicationDbContext>(options =>
                    options.UseSqlite(sqlite.ConnectionString));
            }

            services.AddScoped<IApplicationDbContext>(provider => provider.GetRequiredService<ApplicationDbContext>());

            services.AddDefaultIdentity<ApplicationUser>()
                .AddEntityFrameworkStores<ApplicationDbContext>();

            services.Configure<IdentityOptions>(options =>
            {
                options.Password.RequiredLength = 8;
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireUppercase = false;
                options.Password.RequireNonAlphanumeric = false;

                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.AllowedForNewUsers = true;
            });

            var jwtKey = configuration["Jwt:Key"] ?? "dev-secret-key-min-32-chars-long!!";
            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,
                        ValidIssuer = configuration["Jwt:Issuer"] ?? "gis-photo-sharing-app",
                        ValidAudience = configuration["Jwt:Audience"] ?? "gis-photo-sharing-app",
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
                    };
                });

            services.Configure<PhotoSettings>(configuration.GetSection("PhotoSettings"));
            services.PostConfigure<PhotoSettings>(settings => settings.Normalise());
            services.Configure<GeocodingSettings>(configuration.GetSection("Geocoding"));
            services.AddMemoryCache();
            services.AddDataProtection();

            services.AddHttpContextAccessor();
            services.AddScoped<ICurrentUserService, CurrentUserService>();
            services.AddTransient<IDateTime, DateTimeService>();
            services.AddScoped<IFileStorage, LocalFileStorage>();
            services.AddTransient<IIdentityService, IdentityService>();
            services.AddTransient<ICsvFileBuilder, CsvFileBuilder>();
            services.AddSingleton<IPhotoMediaUrlService, PhotoMediaUrlService>();
            services.AddTransient<IExifLocationReader, ExifLocationReader>();
            services.AddHttpClient<IGeocodingService, GeocodingService>(client =>
            {
                client.Timeout = TimeSpan.FromSeconds(
                    configuration.GetValue("Geocoding:TimeoutSeconds", 8));
            });

            return services;
        }
    }
}
