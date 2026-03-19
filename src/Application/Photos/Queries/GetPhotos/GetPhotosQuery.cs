using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace gis_photo_sharing_app.Application.Photos.Queries.GetPhotos;

public record GetPhotosQuery : IRequest<List<PhotoDto>>;

public class GetPhotosQueryHandler : IRequestHandler<GetPhotosQuery, List<PhotoDto>>
{
    private readonly Application.Common.Interfaces.IApplicationDbContext _context;

    public GetPhotosQueryHandler(Application.Common.Interfaces.IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<PhotoDto>> Handle(GetPhotosQuery request, CancellationToken cancellationToken)
    {
        return await _context.Photos
            .Select(p => new PhotoDto
            {
                Id = p.Id,
                Title = p.Title,
                Description = p.Description,
                FilePath = p.FilePath,
                ThumbnailPath = p.ThumbnailPath,
                Latitude = p.Latitude,
                Longitude = p.Longitude,
                TakenAt = p.TakenAt,
            })
            .ToListAsync(cancellationToken);
    }
}
