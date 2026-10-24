namespace gis_photo_sharing_app.Application.Common.Interfaces;

public interface IPhotoMediaUrlService
{
    string BuildImageUrl(int photoId, bool isPublic, bool thumbnail = false);
    bool ValidateAccessToken(string? token, int photoId, bool thumbnail);
}
