namespace gis_photo_sharing_app.Application.Photos.Queries.GetPhotos;

public record PhotoDto
{
    public int Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string FilePath { get; init; } = string.Empty;
    public string? ThumbnailPath { get; init; }
    public double Latitude { get; init; }
    public double Longitude { get; init; }
    public string? TakenAt { get; init; }
}
