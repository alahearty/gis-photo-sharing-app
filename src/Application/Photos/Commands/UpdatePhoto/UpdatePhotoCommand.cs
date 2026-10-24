using System.Threading;
using System.Threading.Tasks;
using gis_photo_sharing_app.Application.Common.Interfaces;
using gis_photo_sharing_app.Application.Photos;
using MediatR;

namespace gis_photo_sharing_app.Application.Photos.Commands.UpdatePhoto;

public record UpdatePhotoCommand(
    int Id,
    string Title,
    string? Description,
    double Latitude,
    double Longitude,
    string? LocationName = null,
    string? Category = null,
    bool? IsPublic = null,
    string? TakenAt = null) : IRequest<PhotoMutationResult>;

public class UpdatePhotoCommandHandler : IRequestHandler<UpdatePhotoCommand, PhotoMutationResult>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public UpdatePhotoCommandHandler(IApplicationDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<PhotoMutationResult> Handle(UpdatePhotoCommand request, CancellationToken cancellationToken)
    {
        var photo = await _context.Photos.FindAsync(new object[] { request.Id }, cancellationToken);
        if (photo == null) return PhotoMutationResult.NotFound;

        var userId = _currentUserService.UserId;
        if (photo.CreatedByUserId != userId)
            return PhotoMutationResult.Forbidden;

        photo.Title = request.Title.Trim();
        photo.Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim();
        photo.Latitude = request.Latitude;
        photo.Longitude = request.Longitude;

        if (request.LocationName != null)
            photo.LocationName = string.IsNullOrWhiteSpace(request.LocationName) ? null : request.LocationName.Trim();

        if (request.Category != null)
            photo.Category = string.IsNullOrWhiteSpace(request.Category) ? "General" : request.Category.Trim();

        if (request.IsPublic.HasValue)
            photo.IsPublic = request.IsPublic.Value;

        if (request.TakenAt != null)
            photo.TakenAt = string.IsNullOrWhiteSpace(request.TakenAt) ? null : request.TakenAt.Trim();

        await _context.SaveChangesAsync(cancellationToken);
        return PhotoMutationResult.Ok;
    }
}
