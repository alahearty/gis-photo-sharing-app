using System;
using System.Collections.Generic;
using System.Linq;

namespace gis_photo_sharing_app.Application.Common.Models;

public class PhotoSettings
{
    public static readonly string[] DefaultCategories =
    {
        "General", "Landscape", "Buildings", "Roads", "Environment", "Tourism", "Infrastructure", "Other",
    };

    public static readonly string[] DefaultAllowedContentTypes =
    {
        "image/jpeg", "image/png", "image/gif", "image/webp",
    };

    public long MaxUploadBytes { get; set; } = 10 * 1024 * 1024;
    public int MaxImagePixels { get; set; } = 50_000_000;
    public int MaxImageDimension { get; set; } = 12_000;
    public int ThumbnailSize { get; set; } = 360;
    public int MaximumImageWidth { get; set; } = 2560;
    public string? StorageRoot { get; set; }
    public List<string> Categories { get; set; } = new();
    public List<string> AllowedContentTypes { get; set; } = new();

    public void Normalise()
    {
        Categories = NormaliseList(Categories, DefaultCategories);
        AllowedContentTypes = NormaliseList(AllowedContentTypes, DefaultAllowedContentTypes);
    }

    private static List<string> NormaliseList(IEnumerable<string>? configured, IEnumerable<string> fallback)
    {
        var values = (configured ?? Array.Empty<string>())
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        return values.Count > 0 ? values : fallback.ToList();
    }
}
