using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using gis_photo_sharing_app.Application.Common.Interfaces;
using gis_photo_sharing_app.Application.Common.Models;
using gis_photo_sharing_app.Application.Photos.Queries.GetPhotos;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace gis_photo_sharing_app.Application.Photos.Queries.GetPhotoGallery;

public record GetPhotoGalleryQuery(
    PhotoQueryFilter Filter,
    int Page = 1,
    int PageSize = 24) : IRequest<PagedResult<PhotoDto>>;

public class GetPhotoGalleryQueryHandler : IRequestHandler<GetPhotoGalleryQuery, PagedResult<PhotoDto>>
{
    private const int MaximumPageSize = 96;

    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IPhotoMediaUrlService _mediaUrlService;

    public GetPhotoGalleryQueryHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        IPhotoMediaUrlService mediaUrlService)
    {
        _context = context;
        _currentUserService = currentUserService;
        _mediaUrlService = mediaUrlService;
    }

    public async Task<PagedResult<PhotoDto>> Handle(
        GetPhotoGalleryQuery request,
        CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId;
        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, MaximumPageSize);

        var query = _context.Photos
            .AsNoTracking()
            .ApplyFilters(request.Filter, userId);

        var total = await query.CountAsync(cancellationToken);
        var photos = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var items = photos
            .ConvertAll(photo => PhotoDtoMapper.Map(photo, userId, _mediaUrlService));

        return new PagedResult<PhotoDto>(items, total, page, pageSize);
    }
}
