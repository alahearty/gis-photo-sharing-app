using System.Collections.Generic;

namespace gis_photo_sharing_app.Application.Common.Models;

public record PagedResult<T>(
    IReadOnlyList<T> Items,
    int TotalCount,
    int Page,
    int PageSize)
{
    public int TotalPages => PageSize <= 0
        ? 0
        : (int)System.Math.Ceiling(TotalCount / (double)PageSize);
}
