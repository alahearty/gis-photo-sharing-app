using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using gis_photo_sharing_app.Infrastructure.Services;
using NUnit.Framework;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Metadata.Profiles.Exif;
using SixLabors.ImageSharp.PixelFormats;

namespace gis_photo_sharing_app.Application.UnitTests.Photos;

[TestFixture]
public class ExifLocationReaderTests
{
    [Test]
    public async Task ReadsNorthEastDmsCoordinatesAndCaptureDate()
    {
        await using var stream = await CreateJpegAsync(
            latitudeRef: "N",
            latitude: new[] { new Rational(4, 1), new Rational(48, 1), new Rational(33, 1) },
            longitudeRef: "E",
            longitude: new[] { new Rational(7, 1), new Rational(2, 1), new Rational(59, 1) },
            dateTaken: "2026:10:23 09:15:00");

        var reader = new ExifLocationReader();
        var metadata = await reader.ReadAsync(stream, CancellationToken.None);

        Assert.That(metadata.HasLocation, Is.True);
        Assert.That(metadata.Latitude, Is.EqualTo(4.8091667).Within(0.0000001));
        Assert.That(metadata.Longitude, Is.EqualTo(7.0497222).Within(0.0000001));
        Assert.That(metadata.TakenAt, Is.EqualTo("2026-10-23T09:15:00"));
    }

    [Test]
    public async Task AppliesSouthAndWestHemispheres()
    {
        await using var stream = await CreateJpegAsync(
            latitudeRef: "S",
            latitude: new[] { new Rational(33, 1), new Rational(55, 1), new Rational(0, 1) },
            longitudeRef: "W",
            longitude: new[] { new Rational(18, 1), new Rational(25, 1), new Rational(0, 1) },
            dateTaken: null);

        var reader = new ExifLocationReader();
        var metadata = await reader.ReadAsync(stream, CancellationToken.None);

        Assert.That(metadata.HasLocation, Is.True);
        Assert.That(metadata.Latitude, Is.EqualTo(-33.9166667).Within(0.0000001));
        Assert.That(metadata.Longitude, Is.EqualTo(-18.4166667).Within(0.0000001));
    }

    [Test]
    public async Task ReturnsNoLocationWhenExifIsMissing()
    {
        await using var stream = new MemoryStream();
        using (var image = new Image<Rgba32>(10, 10))
        {
            await image.SaveAsJpegAsync(stream);
        }
        stream.Position = 0;

        var reader = new ExifLocationReader();
        var metadata = await reader.ReadAsync(stream, CancellationToken.None);

        Assert.That(metadata.HasLocation, Is.False);
        Assert.That(metadata.Latitude, Is.Null);
        Assert.That(metadata.Longitude, Is.Null);
    }

    [Test]
    public async Task ReturnsNoLocationForMalformedInput()
    {
        await using var stream = new MemoryStream(new byte[] { 1, 2, 3, 4, 5 });
        var reader = new ExifLocationReader();

        var metadata = await reader.ReadAsync(stream, CancellationToken.None);

        Assert.That(metadata.HasLocation, Is.False);
    }

    private static async Task<MemoryStream> CreateJpegAsync(
        string latitudeRef,
        Rational[] latitude,
        string longitudeRef,
        Rational[] longitude,
        string? dateTaken)
    {
        var stream = new MemoryStream();
        using (var image = new Image<Rgba32>(10, 10))
        {
            var profile = new ExifProfile();
            profile.SetValue(ExifTag.GPSLatitudeRef, latitudeRef);
            profile.SetValue(ExifTag.GPSLatitude, latitude);
            profile.SetValue(ExifTag.GPSLongitudeRef, longitudeRef);
            profile.SetValue(ExifTag.GPSLongitude, longitude);
            if (dateTaken != null)
                profile.SetValue(ExifTag.DateTimeOriginal, dateTaken);

            image.Metadata.ExifProfile = profile;
            await image.SaveAsJpegAsync(stream);
        }

        stream.Position = 0;
        return stream;
    }
}
