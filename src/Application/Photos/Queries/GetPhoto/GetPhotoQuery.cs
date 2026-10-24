using System.Threading;
using System.Threading.Tasks;
using gis_photo_sharing_app.Application.Common.Interfaces;
using MediatR;

namespace gis_photo_sharing_app.Application.Photos.Queries.GetPhoto;

public record GetPhotoQuery(int Id) : IRequest<PhotoDetailDto?>;

public class GetPhotoQueryHandler : IRequestHandler<GetPhotoQuery, PhotoDetailDto?>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IPhotoMediaUrlService _mediaUrlService;

    public GetPhotoQueryHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        IPhotoMediaUrlService mediaUrlService)
    {
        _context = context;
        _currentUserService = currentUserService;
        _mediaUrlService = mediaUrlService;
    }

    public async Task<PhotoDetailDto?> Handle(GetPhotoQuery request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId;
        var photo = await _context.Photos.FindAsync(new object[] { request.Id }, cancellationToken);
        if (photo == null) return null;

        if (!photo.IsPublic && !IsOwner(photo, userId))
            return null;

        return PhotoDtoMapper.MapDetail(photo, userId, _mediaUrlService);
    }

    private static bool IsOwner(Domain.Entities.Photo photo, string? userId)
    {
        return !string.IsNullOrWhiteSpace(userId) &&
               string.Equals(photo.CreatedByUserId, userId, System.StringComparison.Ordinal);
    }
}
