using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using gis_photo_sharing_app.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace gis_photo_sharing_app.Application.Photos.Queries.GetPhotos;

public record GetPhotosQuery(PhotoQueryFilter Filter) : IRequest<List<PhotoDto>>;

public class GetPhotosQueryHandler : IRequestHandler<GetPhotosQuery, List<PhotoDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IPhotoMediaUrlService _mediaUrlService;

    public GetPhotosQueryHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        IPhotoMediaUrlService mediaUrlService)
    {
        _context = context;
        _currentUserService = currentUserService;
        _mediaUrlService = mediaUrlService;
    }

    public async Task<List<PhotoDto>> Handle(GetPhotosQuery request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId;

        var photos = await _context.Photos
            .AsNoTracking()
            .ApplyFilters(request.Filter, userId)
            .ToListAsync(cancellationToken);

        return photos
            .Select(photo => PhotoDtoMapper.Map(photo, userId, _mediaUrlService))
            .ToList();
    }
}
