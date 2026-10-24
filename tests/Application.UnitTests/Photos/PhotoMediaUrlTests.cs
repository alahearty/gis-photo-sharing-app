using System;
using gis_photo_sharing_app.Infrastructure.Services;
using Microsoft.AspNetCore.DataProtection;
using NUnit.Framework;

namespace gis_photo_sharing_app.Application.UnitTests.Photos;

[TestFixture]
public class PhotoMediaUrlTests
{
    private PhotoMediaUrlService _service;

    [SetUp]
    public void SetUp()
    {
        _service = new PhotoMediaUrlService(new EphemeralDataProtectionProvider());
    }

    [Test]
    public void PublicPhotoUrlsDoNotContainTokens()
    {
        Assert.That(_service.BuildImageUrl(12, true), Is.EqualTo("/api/photos/12/file"));
        Assert.That(_service.BuildImageUrl(12, true, thumbnail: true), Is.EqualTo("/api/photos/12/thumbnail"));
    }

    [Test]
    public void PrivatePhotoUrlContainsAValidToken()
    {
        var url = _service.BuildImageUrl(7, false);
        Assert.That(url, Does.StartWith("/api/photos/7/file?token="));

        var token = Uri.UnescapeDataString(url.Substring(url.IndexOf("token=", StringComparison.Ordinal) + 6));
        Assert.That(_service.ValidateAccessToken(token, 7, thumbnail: false), Is.True);
        Assert.That(_service.ValidateAccessToken(token, 7, thumbnail: true), Is.False);
        Assert.That(_service.ValidateAccessToken(token, 8, thumbnail: false), Is.False);
        Assert.That(_service.ValidateAccessToken(token + "x", 7, thumbnail: false), Is.False);
    }

    [Test]
    public void ThumbnailTokenIsBoundToTheThumbnailResource()
    {
        var url = _service.BuildImageUrl(9, false, thumbnail: true);
        Assert.That(url, Does.StartWith("/api/photos/9/thumbnail?token="));

        var token = Uri.UnescapeDataString(url.Substring(url.IndexOf("token=", StringComparison.Ordinal) + 6));
        Assert.That(_service.ValidateAccessToken(token, 9, thumbnail: true), Is.True);
        Assert.That(_service.ValidateAccessToken(token, 9, thumbnail: false), Is.False);
    }
}
