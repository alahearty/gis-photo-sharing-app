using System.Threading;
using System.Threading.Tasks;
using gis_photo_sharing_app.Application.Common.Interfaces;
using gis_photo_sharing_app.Domain.Entities;
using MediatR;

namespace gis_photo_sharing_app.Application.Photos.Commands.CreatePhoto;

public record CreatePhotoCommand(
    string Title,
    string? Description,
    string FilePath,
    string? ThumbnailPath,
    double Latitude,
    double Longitude,
    string? TakenAt,
    string? LocationName = null,
    string? Category = null,
    bool IsPublic = true) : IRequest<int>;

public class CreatePhotoCommandHandler : IRequestHandler<CreatePhotoCommand, int>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public CreatePhotoCommandHandler(IApplicationDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<int> Handle(CreatePhotoCommand request, CancellationToken cancellationToken)
    {
        var entity = new Photo
        {
            Title = request.Title.Trim(),
            Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
            FilePath = request.FilePath,
            ThumbnailPath = request.ThumbnailPath,
            Latitude = request.Latitude,
            Longitude = request.Longitude,
            LocationName = string.IsNullOrWhiteSpace(request.LocationName) ? null : request.LocationName.Trim(),
            Category = string.IsNullOrWhiteSpace(request.Category) ? "General" : request.Category.Trim(),
            IsPublic = request.IsPublic,
            TakenAt = string.IsNullOrWhiteSpace(request.TakenAt) ? null : request.TakenAt.Trim(),
            CreatedByUserId = _currentUserService.UserId,
        };

        _context.Photos.Add(entity);
        await _context.SaveChangesAsync(cancellationToken);
        return entity.Id;
    }
}
