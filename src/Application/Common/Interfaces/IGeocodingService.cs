using System.Threading;
using System.Threading.Tasks;

namespace gis_photo_sharing_app.Application.Common.Interfaces;

public record GeocodingResult(
    string DisplayName,
    string? PlaceName,
    double Latitude,
    double Longitude,
    string Attribution);

public interface IGeocodingService
{
    Task<GeocodingResult?> ReverseAsync(
        double latitude,
        double longitude,
        CancellationToken cancellationToken = default);
}
