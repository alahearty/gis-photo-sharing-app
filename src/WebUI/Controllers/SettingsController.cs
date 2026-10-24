using gis_photo_sharing_app.Application.Common.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace gis_photo_sharing_app.WebUI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SettingsController : ControllerBase
{
    private readonly MapSettings _mapSettings;
    private readonly PhotoSettings _photoSettings;
    private readonly GeocodingSettings _geocodingSettings;

    public SettingsController(
        IOptions<MapSettings> mapSettings,
        IOptions<PhotoSettings> photoSettings,
        IOptions<GeocodingSettings> geocodingSettings)
    {
        _mapSettings = mapSettings.Value;
        _photoSettings = photoSettings.Value;
        _geocodingSettings = geocodingSettings.Value;
    }

    [HttpGet]
    [AllowAnonymous]
    public ActionResult<ApplicationSettingsResponse> Get()
    {
        return Ok(new ApplicationSettingsResponse(
            new MapSettingsResponse(
                _mapSettings.DefaultLatitude,
                _mapSettings.DefaultLongitude,
                _mapSettings.DefaultZoom,
                _mapSettings.MinimumZoom,
                _mapSettings.MaximumZoom,
                _mapSettings.DefaultLocationName,
                _mapSettings.Layers),
            new PhotoSettingsResponse(
                _photoSettings.MaxUploadBytes,
                _photoSettings.MaxImagePixels,
                _photoSettings.MaxImageDimension,
                _photoSettings.ThumbnailSize,
                _photoSettings.Categories,
                _photoSettings.AllowedContentTypes),
            new GeocodingSettingsResponse(
                _geocodingSettings.Enabled,
                _geocodingSettings.Attribution)));
    }
}

public record ApplicationSettingsResponse(
    MapSettingsResponse Map,
    PhotoSettingsResponse Photo,
    GeocodingSettingsResponse Geocoding);

public record MapSettingsResponse(
    double DefaultLatitude,
    double DefaultLongitude,
    int DefaultZoom,
    int MinimumZoom,
    int MaximumZoom,
    string DefaultLocationName,
    IReadOnlyList<MapLayerSettings> Layers);

public record PhotoSettingsResponse(
    long MaxUploadBytes,
    int MaxImagePixels,
    int MaxImageDimension,
    int ThumbnailSize,
    IReadOnlyList<string> Categories,
    IReadOnlyList<string> AllowedContentTypes);

public record GeocodingSettingsResponse(
    bool Enabled,
    string Attribution);
