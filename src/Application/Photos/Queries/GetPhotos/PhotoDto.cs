using System;

namespace gis_photo_sharing_app.Application.Photos.Queries.GetPhotos;

public record PhotoDto
{
    public int Id { get; init; }
    public string? CreatedByUserId { get; init; }
    public string Title { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string FilePath { get; init; } = string.Empty;
    public string? ThumbnailPath { get; init; }
    public string ImageUrl { get; init; } = string.Empty;
    public string ThumbnailUrl { get; init; } = string.Empty;
    public double Latitude { get; init; }
    public double Longitude { get; init; }
    public string? LocationName { get; init; }
    public string? Category { get; init; }
    public bool IsPublic { get; init; }
    public string? TakenAt { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
    public bool IsOwner { get; init; }
}
