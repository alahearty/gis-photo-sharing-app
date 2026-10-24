namespace gis_photo_sharing_app.Application.Common.Models;

public class GeocodingSettings
{
    public bool Enabled { get; set; } = true;
    public string BaseUrl { get; set; } = "https://nominatim.openstreetmap.org/";
    public string UserAgent { get; set; } = "GISPhotoShare/1.0";
    public string Attribution { get; set; } = "Geocoding © OpenStreetMap contributors (Nominatim)";
    public int MinimumIntervalMilliseconds { get; set; } = 1100;
    public int TimeoutSeconds { get; set; } = 8;
}
