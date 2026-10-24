using System.Collections.Generic;
using System.Linq;
using gis_photo_sharing_app.Application.Common.Models;
using gis_photo_sharing_app.WebUI.Configuration;
using gis_photo_sharing_app.WebUI.Controllers;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NUnit.Framework;

namespace gis_photo_sharing_app.Application.IntegrationTests.Configuration;

public class ConfigurationHardeningTests
{
    [Test]
    [NonParallelizable]
    public void AspNetCoreEnvironmentVariableNamesBindNestedConnectionAndJwtSettings()
    {
        const string prefix = "GIS_PHOTOSHARE_CONFIG_TEST_";
        const string sqlite = "Data Source=isolated-binding-test.db";
        const string jwt = "test-only-uC8#Qp2!vN7@xR5$kL9%tM4&zB6*eD3";

        try
        {
            System.Environment.SetEnvironmentVariable(prefix + "ConnectionStrings__SqliteConnection", sqlite);
            System.Environment.SetEnvironmentVariable(prefix + "Jwt__Key", jwt);

            var configuration = new ConfigurationBuilder()
                .AddEnvironmentVariables(prefix)
                .Build();

            configuration.GetConnectionString("SqliteConnection").Should().Be(sqlite);
            configuration["Jwt:Key"].Should().Be(jwt);
        }
        finally
        {
            System.Environment.SetEnvironmentVariable(prefix + "ConnectionStrings__SqliteConnection", null);
            System.Environment.SetEnvironmentVariable(prefix + "Jwt__Key", null);
        }
    }

    [Test]
    public void PhotoSettingsBindingDoesNotDuplicateConfiguredCategories()
    {
        var values = new Dictionary<string, string?>
        {
            ["PhotoSettings:Categories:0"] = "General",
            ["PhotoSettings:Categories:1"] = "Landscape",
            ["PhotoSettings:Categories:2"] = "general",
            ["PhotoSettings:AllowedContentTypes:0"] = "image/jpeg",
        };
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(values).Build();
        var services = new ServiceCollection();
        services.AddOptions();
        services.Configure<PhotoSettings>(configuration.GetSection("PhotoSettings"));
        services.PostConfigure<PhotoSettings>(settings => settings.Normalise());

        using var provider = services.BuildServiceProvider();
        var settings = provider.GetRequiredService<IOptions<PhotoSettings>>().Value;

        settings.Categories.Should().Equal("General", "Landscape");
        settings.AllowedContentTypes.Should().Equal("image/jpeg");
    }

    [Test]
    public void EmptyPhotoSettingsUseSafeDefaultsInSettingsResponse()
    {
        var photoSettings = new PhotoSettings();
        photoSettings.Normalise();
        var controller = new SettingsController(
            Options.Create(new MapSettings()),
            Options.Create(photoSettings),
            Options.Create(new GeocodingSettings()));

        var result = controller.Get().Result.Should().BeOfType<OkObjectResult>().Subject;
        var response = result.Value.Should().BeOfType<ApplicationSettingsResponse>().Subject;

        response.Photo.Categories.Should().Equal(PhotoSettings.DefaultCategories);
        response.Photo.Categories.Should().OnlyHaveUniqueItems();
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("dev-secret-key-min-32-chars-long!!")]
    [TestCase("replace-with-a-random-secret-at-least-32-characters")]
    [TestCase("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa")]
    public void ProductionJwtValidationRejectsMissingOrPredictableValues(string? key)
    {
        JwtKeyValidator.TryValidateProductionKey(key, out _).Should().BeFalse();
    }

    [Test]
    public void ProductionJwtValidationAcceptsAHighEntropyValue()
    {
        const string generatedValue = "uC8#Qp2!vN7@xR5$kL9%tM4&zB6*eD3?gH1+wF0=";

        JwtKeyValidator.TryValidateProductionKey(generatedValue, out _).Should().BeTrue();
    }
}
