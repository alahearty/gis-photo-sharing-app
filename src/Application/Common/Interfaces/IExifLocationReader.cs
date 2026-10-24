using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace gis_photo_sharing_app.Application.Common.Interfaces;

public record ExifPhotoMetadata(
    double? Latitude,
    double? Longitude,
    double? AltitudeMetres,
    string? TakenAt,
    bool HasLocation);

public interface IExifLocationReader
{
    Task<ExifPhotoMetadata> ReadAsync(Stream stream, CancellationToken cancellationToken = default);
}
