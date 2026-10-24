using System.Collections.Generic;

namespace gis_photo_sharing_app.Application.Common.Models;

public class MapSettings
{
    public double DefaultLatitude { get; set; } = 4.8156;
    public double DefaultLongitude { get; set; } = 7.0498;
    public int DefaultZoom { get; set; } = 12;
    public int MinimumZoom { get; set; } = 3;
    public int MaximumZoom { get; set; } = 19;
    public string DefaultLocationName { get; set; } = "Port Harcourt, Rivers State, Nigeria";
    public List<MapLayerSettings> Layers { get; set; } = new();
}

public class MapLayerSettings
{
    public string Name { get; set; } = string.Empty;
    public string UrlTemplate { get; set; } = string.Empty;
    public string Attribution { get; set; } = string.Empty;
    public int MaximumZoom { get; set; } = 19;
    public bool IsDefault { get; set; }
}
