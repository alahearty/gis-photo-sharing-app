using System;
using System.Linq;
using gis_photo_sharing_app.Domain.Entities;

namespace gis_photo_sharing_app.Application.Photos;

public static class PhotosQueryExtensions
{
    public static IQueryable<Photo> ApplyVisibility(this IQueryable<Photo> query, string? currentUserId)
    {
        if (string.IsNullOrWhiteSpace(currentUserId))
            return query.Where(p => p.IsPublic);

        return query.Where(p => p.IsPublic || p.CreatedByUserId == currentUserId);
    }

    public static IQueryable<Photo> ApplyFilters(
        this IQueryable<Photo> query,
        PhotoQueryFilter filter,
        string? currentUserId)
    {
        query = query.ApplyVisibility(currentUserId);

        if (filter.Mine)
        {
            if (string.IsNullOrWhiteSpace(currentUserId))
                return query.Where(_ => false);

            query = query.Where(p => p.CreatedByUserId == currentUserId);
        }
        else if (!string.IsNullOrWhiteSpace(filter.CreatedByUserId))
        {
            query = query.Where(p => p.CreatedByUserId == filter.CreatedByUserId);
        }

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var search = filter.Search.Trim().ToLower();
            query = query.Where(p =>
                p.Title.ToLower().Contains(search) ||
                (p.Description != null && p.Description.ToLower().Contains(search)) ||
                (p.LocationName != null && p.LocationName.ToLower().Contains(search)));
        }

        if (!string.IsNullOrWhiteSpace(filter.Category))
        {
            var category = filter.Category.Trim().ToLower();
            query = query.Where(p => p.Category != null && p.Category.ToLower() == category);
        }

        if (filter.From.HasValue)
        {
            var from = filter.From.Value.Date;
            query = query.Where(p => p.Created >= from);
        }

        if (filter.To.HasValue)
        {
            var toExclusive = filter.To.Value.Date.AddDays(1);
            query = query.Where(p => p.Created < toExclusive);
        }

        if (filter.MinLatitude.HasValue)
            query = query.Where(p => p.Latitude >= filter.MinLatitude.Value);

        if (filter.MaxLatitude.HasValue)
            query = query.Where(p => p.Latitude <= filter.MaxLatitude.Value);

        if (filter.MinLongitude.HasValue)
            query = query.Where(p => p.Longitude >= filter.MinLongitude.Value);

        if (filter.MaxLongitude.HasValue)
            query = query.Where(p => p.Longitude <= filter.MaxLongitude.Value);

        return ApplySort(query, filter.Sort);
    }

    private static IQueryable<Photo> ApplySort(IQueryable<Photo> query, string? sort)
    {
        return sort?.ToLowerInvariant() switch
        {
            "oldest" => query.OrderBy(p => p.Created).ThenBy(p => p.Id),
            "title" => query.OrderBy(p => p.Title).ThenBy(p => p.Id),
            "title_desc" => query.OrderByDescending(p => p.Title).ThenBy(p => p.Id),
            _ => query.OrderByDescending(p => p.Created).ThenByDescending(p => p.Id),
        };
    }
}
