using System.IO;
using System.Text.Json;
using System.Threading.RateLimiting;
using gis_photo_sharing_app.Application;
using gis_photo_sharing_app.Application.Common.Models;
using gis_photo_sharing_app.Infrastructure;
using gis_photo_sharing_app.WebUI.Middleware;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;

namespace gis_photo_sharing_app.WebUI
{
    public class Startup
    {
        private readonly string? _contentRootPath;

        public Startup(IConfiguration configuration, IWebHostEnvironment? environment = null)
        {
            Configuration = configuration;
            _contentRootPath = environment?.ContentRootPath;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddApplication();
            services.AddInfrastructure(Configuration, _contentRootPath);
            services.Configure<MapSettings>(Configuration.GetSection("MapSettings"));
            services.Configure<ForwardedHeadersOptions>(options =>
            {
                options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
                options.ForwardLimit = 1;
                options.KnownNetworks.Clear();
                options.KnownProxies.Clear();

                var configuredProxies = Configuration.GetSection("ReverseProxy:KnownProxies").Get<string[]>()
                    ?? Array.Empty<string>();
                foreach (var configuredProxy in configuredProxies)
                {
                    if (System.Net.IPAddress.TryParse(configuredProxy, out var address))
                        options.KnownProxies.Add(address);
                }
            });

            services.AddControllers().AddJsonOptions(o =>
            {
                o.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
                o.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
            });

            services.AddRateLimiter(options =>
            {
                options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
                options.AddPolicy("auth", context =>
                    RateLimitPartition.GetFixedWindowLimiter(
                        context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                        _ => new FixedWindowRateLimiterOptions
                        {
                            PermitLimit = 10,
                            Window = TimeSpan.FromMinutes(1),
                            QueueLimit = 0,
                            AutoReplenishment = true,
                        }));

                options.OnRejected = async (context, cancellationToken) =>
                {
                    context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                    context.HttpContext.Response.ContentType = "application/problem+json";
                    await context.HttpContext.Response.WriteAsync(
                        "{\"status\":429,\"title\":\"Too many requests\",\"detail\":\"Wait a moment before trying again.\"}",
                        cancellationToken);
                };
            });

            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo
                {
                    Title = "GIS PhotoShare API",
                    Version = "v1",
                    Description = "REST API for photos, auth, uploads and GIS metadata. Use the Vue SPA at / for the web UI. Authorize with JWT from POST /api/auth/login or POST /api/auth/register.",
                });
                c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Description = "JWT: paste `Bearer {your_token}` from login/register response.",
                    Name = "Authorization",
                    In = ParameterLocation.Header,
                    Type = SecuritySchemeType.ApiKey,
                    Scheme = "Bearer",
                });
                c.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" },
                        },
                        Array.Empty<string>()
                    },
                });
            });

            services.AddCors(options =>
            {
                options.AddDefaultPolicy(builder =>
                {
                    builder.WithOrigins(
                            "http://localhost:8080",
                            "https://localhost:8080",
                            "http://localhost:5173",
                            "https://localhost:44387",
                            "http://localhost:15248",
                            "https://localhost:5001",
                            "http://localhost:5000")
                        .AllowAnyMethod()
                        .AllowAnyHeader();
                });
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            var distPath = Path.Combine(env.ContentRootPath, "clientapp", "dist");
            var hasSpa = Directory.Exists(distPath);
            PhysicalFileProvider? distProvider = hasSpa ? new PhysicalFileProvider(distPath) : null;

            if (Configuration.GetValue<bool>("ReverseProxy:Enabled"))
            {
                app.UseForwardedHeaders();
            }

            app.UseMiddleware<ApiExceptionMiddleware>();

            if (!env.IsDevelopment())
            {
                app.UseHsts();
            }

            var redirectToHttps = Configuration.GetValue<bool?>("Https:RedirectEnabled")
                ?? !env.IsDevelopment();
            if (redirectToHttps)
            {
                app.UseHttpsRedirection();
            }

            app.Use(async (context, next) =>
            {
                context.Response.Headers["X-Content-Type-Options"] = "nosniff";
                context.Response.Headers["X-Frame-Options"] = "DENY";
                context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
                context.Response.Headers["Permissions-Policy"] = "geolocation=(self), camera=(), microphone=()";
                context.Response.Headers["Cross-Origin-Opener-Policy"] = "same-origin";
                await next();
            });

            // wwwroot: uploaded images (/uploads/...)
            app.UseStaticFiles();

            // Vue CLI output: JS/CSS and index.html
            if (distProvider != null)
            {
                app.UseDefaultFiles(new DefaultFilesOptions
                {
                    FileProvider = distProvider,
                    RequestPath = "",
                });
                app.UseStaticFiles(new StaticFileOptions
                {
                    FileProvider = distProvider,
                    RequestPath = "",
                });
            }

            if (env.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI(c =>
                {
                    c.SwaggerEndpoint("/swagger/v1/swagger.json", "GIS PhotoShare API v1");
                    c.RoutePrefix = "swagger";
                });
            }

            app.UseRouting();

            app.UseCors();

            app.UseRateLimiter();

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
                endpoints.MapGet("/health", () => Results.Ok(new { status = "ok" }));

                endpoints.Map("/api/{**path}", context =>
                {
                    context.Response.StatusCode = StatusCodes.Status404NotFound;
                    context.Response.ContentType = "application/problem+json";
                    return context.Response.WriteAsync(
                        "{\"status\":404,\"title\":\"API endpoint not found.\"}");
                });

                if (distProvider != null)
                {
                    endpoints.MapFallbackToFile("index.html", new StaticFileOptions { FileProvider = distProvider });
                }
            });
        }
    }
}
