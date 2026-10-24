using System;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using gis_photo_sharing_app.Application.Common.Interfaces;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Metadata.Profiles.Exif;

namespace gis_photo_sharing_app.Infrastructure.Services;

public class ExifLocationReader : IExifLocationReader
{
    public async Task<ExifPhotoMetadata> ReadAsync(
        Stream stream,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var image = await Image.LoadAsync(
                new DecoderOptions { MaxFrames = 1 },
                stream,
                cancellationToken);

            var profile = image.Metadata.ExifProfile;
            if (profile == null)
                return new ExifPhotoMetadata(null, null, null, null, false);

            var latitude = ReadCoordinate(
                profile,
                ExifTag.GPSLatitude,
                ExifTag.GPSLatitudeRef,
                "S",
                -90,
                90);

            var longitude = ReadCoordinate(
                profile,
                ExifTag.GPSLongitude,
                ExifTag.GPSLongitudeRef,
                "W",
                -180,
                180);

            var altitude = ReadAltitude(profile);
            var takenAt = ReadTakenAt(profile);

            return new ExifPhotoMetadata(
                latitude,
                longitude,
                altitude,
                takenAt,
                latitude.HasValue && longitude.HasValue);
        }
        catch
        {
            return new ExifPhotoMetadata(null, null, null, null, false);
        }
    }

    private static double? ReadCoordinate(
        ExifProfile profile,
        ExifTag<Rational[]> coordinateTag,
        ExifTag<string> referenceTag,
        string negativeReference,
        double minimum,
        double maximum)
    {
        if (!profile.TryGetValue(coordinateTag, out var coordinateValue))
            return null;

        var parts = coordinateValue.Value;
        if (parts == null || parts.Length < 3)
            return null;

        var degrees = ToDouble(parts[0]);
        var minutes = ToDouble(parts[1]);
        var seconds = ToDouble(parts[2]);
        if (!degrees.HasValue || !minutes.HasValue || !seconds.HasValue)
            return null;

        var result = degrees.Value + (minutes.Value / 60d) + (seconds.Value / 3600d);

        if (profile.TryGetValue(referenceTag, out var referenceValue) &&
            !string.IsNullOrWhiteSpace(referenceValue.Value) &&
            referenceValue.Value.Trim().StartsWith(negativeReference, StringComparison.OrdinalIgnoreCase))
        {
            result = -result;
        }

        if (double.IsNaN(result) || double.IsInfinity(result) ||
            result < minimum || result > maximum)
        {
            return null;
        }

        return Math.Round(result, 7, MidpointRounding.AwayFromZero);
    }

    private static double? ReadAltitude(ExifProfile profile)
    {
        if (!profile.TryGetValue(ExifTag.GPSAltitude, out var altitudeValue))
            return null;

        var rational = altitudeValue.Value;
        var altitude = ToDouble(rational);
        if (!altitude.HasValue)
            return null;

        if (profile.TryGetValue(ExifTag.GPSAltitudeRef, out var referenceValue) &&
            referenceValue.Value == 1)
        {
            altitude = -altitude;
        }

        return Math.Round(altitude.Value, 2, MidpointRounding.AwayFromZero);
    }

    private static string? ReadTakenAt(ExifProfile profile)
    {
        foreach (var tag in new[] { ExifTag.DateTimeOriginal, ExifTag.DateTimeDigitized })
        {
            if (!profile.TryGetValue(tag, out var value) || string.IsNullOrWhiteSpace(value.Value))
                continue;

            if (DateTime.TryParseExact(
                    value.Value.Trim(),
                    "yyyy:MM:dd HH:mm:ss",
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out var parsed))
            {
                return parsed.ToString("yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture);
            }

            if (DateTime.TryParse(
                    value.Value,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.AssumeLocal,
                    out parsed))
            {
                return parsed.ToString("yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture);
            }
        }

        return null;
    }

    private static double? ToDouble(Rational rational)
    {
        if (rational.Denominator == 0)
            return null;

        return rational.Numerator / (double)rational.Denominator;
    }
}
