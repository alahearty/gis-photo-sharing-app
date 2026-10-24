using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using gis_photo_sharing_app.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace gis_photo_sharing_app.Application.Photos.Queries.GetPhotoGeoJson;

public record GeoJsonFeatureCollection(
    string Type,
    IReadOnlyList<GeoJsonFeature> Features);

public record GeoJsonFeature(
    string Type,
    GeoJsonGeometry Geometry,
    GeoJsonProperties Properties);

public record GeoJsonGeometry(
    string Type,
    IReadOnlyList<double> Coordinates);

public record GeoJsonProperties(
    int Id,
    string Title,
    string? Description,
    string? LocationName,
    string? Category,
    string? TakenAt,
    DateTime CreatedAt,
    string ImageUrl,
    string ThumbnailUrl);

public record GetPhotoGeoJsonQuery(PhotoQueryFilter Filter) : IRequest<GeoJsonFeatureCollection>;

public class GetPhotoGeoJsonQueryHandler : IRequestHandler<GetPhotoGeoJsonQuery, GeoJsonFeatureCollection>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IPhotoMediaUrlService _mediaUrlService;

    public GetPhotoGeoJsonQueryHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        IPhotoMediaUrlService mediaUrlService)
    {
        _context = context;
        _currentUserService = currentUserService;
        _mediaUrlService = mediaUrlService;
    }

    public async Task<GeoJsonFeatureCollection> Handle(
        GetPhotoGeoJsonQuery request,
        CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId;

        var photos = await _context.Photos
            .AsNoTracking()
            .ApplyFilters(request.Filter, userId)
            .Where(p => p.IsPublic)
            .OrderBy(p => p.Id)
            .ToListAsync(cancellationToken);

        var features = new List<GeoJsonFeature>(photos.Count);
        foreach (var photo in photos)
        {
            if (!IsValidCoordinate(photo.Latitude, photo.Longitude))
                continue;

            features.Add(new GeoJsonFeature(
                "Feature",
                new GeoJsonGeometry(
                    "Point",
                    new[] { photo.Longitude, photo.Latitude }),
                new GeoJsonProperties(
                    photo.Id,
                    photo.Title,
                    photo.Description,
                    photo.LocationName,
                    photo.Category,
                    photo.TakenAt,
                    photo.Created,
                    _mediaUrlService.BuildImageUrl(photo.Id, photo.IsPublic),
                    _mediaUrlService.BuildImageUrl(photo.Id, photo.IsPublic, thumbnail: true))));
        }

        return new GeoJsonFeatureCollection("FeatureCollection", features);
    }

    private static bool IsValidCoordinate(double latitude, double longitude)
    {
        return !double.IsNaN(latitude) && !double.IsNaN(longitude) &&
               !double.IsInfinity(latitude) && !double.IsInfinity(longitude) &&
               latitude >= -90 && latitude <= 90 &&
               longitude >= -180 && longitude <= 180;
    }
}
