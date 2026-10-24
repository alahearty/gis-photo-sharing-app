using gis_photo_sharing_app.Application.Common.Interfaces;
using gis_photo_sharing_app.Application.Photos.Queries.GetPhoto;
using gis_photo_sharing_app.Application.Photos.Queries.GetPhotos;
using gis_photo_sharing_app.Domain.Entities;

namespace gis_photo_sharing_app.Application.Photos;

public static class PhotoDtoMapper
{
    public static PhotoDto Map(Photo photo, string? currentUserId, IPhotoMediaUrlService mediaUrlService)
    {
        return new PhotoDto
        {
            Id = photo.Id,
            CreatedByUserId = photo.CreatedByUserId,
            Title = photo.Title,
            Description = photo.Description,
            FilePath = photo.FilePath,
            ThumbnailPath = photo.ThumbnailPath,
            ImageUrl = mediaUrlService.BuildImageUrl(photo.Id, photo.IsPublic),
            ThumbnailUrl = mediaUrlService.BuildImageUrl(photo.Id, photo.IsPublic, thumbnail: true),
            Latitude = photo.Latitude,
            Longitude = photo.Longitude,
            LocationName = photo.LocationName,
            Category = photo.Category,
            IsPublic = photo.IsPublic,
            TakenAt = photo.TakenAt,
            CreatedAt = photo.Created,
            UpdatedAt = photo.LastModified,
            IsOwner = IsOwner(photo, currentUserId),
        };
    }

    public static PhotoDetailDto MapDetail(Photo photo, string? currentUserId, IPhotoMediaUrlService mediaUrlService)
    {
        return new PhotoDetailDto
        {
            Id = photo.Id,
            CreatedByUserId = photo.CreatedByUserId,
            Title = photo.Title,
            Description = photo.Description,
            FilePath = photo.FilePath,
            ThumbnailPath = photo.ThumbnailPath,
            ImageUrl = mediaUrlService.BuildImageUrl(photo.Id, photo.IsPublic),
            ThumbnailUrl = mediaUrlService.BuildImageUrl(photo.Id, photo.IsPublic, thumbnail: true),
            Latitude = photo.Latitude,
            Longitude = photo.Longitude,
            LocationName = photo.LocationName,
            Category = photo.Category,
            IsPublic = photo.IsPublic,
            TakenAt = photo.TakenAt,
            CreatedAt = photo.Created,
            UpdatedAt = photo.LastModified,
            IsOwner = IsOwner(photo, currentUserId),
        };
    }

    private static bool IsOwner(Photo photo, string? currentUserId)
    {
        return !string.IsNullOrWhiteSpace(currentUserId) &&
               string.Equals(photo.CreatedByUserId, currentUserId, System.StringComparison.Ordinal);
    }
}
