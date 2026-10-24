namespace gis_photo_sharing_app.Application.Photos;

public record PhotoQueryFilter(
    bool Mine = false,
    string? CreatedByUserId = null,
    string? Search = null,
    string? Category = null,
    System.DateTime? From = null,
    System.DateTime? To = null,
    double? MinLatitude = null,
    double? MaxLatitude = null,
    double? MinLongitude = null,
    double? MaxLongitude = null,
    string Sort = "newest");
