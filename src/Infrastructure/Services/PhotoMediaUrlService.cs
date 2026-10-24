using System;
using gis_photo_sharing_app.Application.Common.Interfaces;
using Microsoft.AspNetCore.DataProtection;

namespace gis_photo_sharing_app.Infrastructure.Services;

public class PhotoMediaUrlService : IPhotoMediaUrlService
{
    private static readonly TimeSpan TokenLifetime = TimeSpan.FromMinutes(30);
    private readonly IDataProtector _protector;

    public PhotoMediaUrlService(IDataProtectionProvider dataProtectionProvider)
    {
        _protector = dataProtectionProvider.CreateProtector("GISPhotoShare.PhotoMedia.v1");
    }

    public string BuildImageUrl(int photoId, bool isPublic, bool thumbnail = false)
    {
        var action = thumbnail ? "thumbnail" : "file";
        var url = $"/api/photos/{photoId}/{action}";

        if (isPublic)
            return url;

        var expires = DateTimeOffset.UtcNow.Add(TokenLifetime).ToUnixTimeSeconds();
        var payload = $"{photoId}:{(thumbnail ? 1 : 0)}:{expires}";
        var token = Uri.EscapeDataString(_protector.Protect(payload));
        return $"{url}?token={token}";
    }

    public bool ValidateAccessToken(string? token, int photoId, bool thumbnail)
    {
        if (string.IsNullOrWhiteSpace(token))
            return false;

        try
        {
            var payload = _protector.Unprotect(token);
            var parts = payload.Split(':');
            if (parts.Length != 3)
                return false;

            if (!int.TryParse(parts[0], out var tokenPhotoId) || tokenPhotoId != photoId)
                return false;

            if (!int.TryParse(parts[1], out var thumbFlag) || (thumbFlag == 1) != thumbnail)
                return false;

            if (!long.TryParse(parts[2], out var expires))
                return false;

            return DateTimeOffset.UtcNow.ToUnixTimeSeconds() <= expires;
        }
        catch
        {
            return false;
        }
    }
}
