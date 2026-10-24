using System.Threading;
using System.Threading.Tasks;
using gis_photo_sharing_app.Application.Common.Interfaces;
using gis_photo_sharing_app.Application.Photos;
using MediatR;

namespace gis_photo_sharing_app.Application.Photos.Commands.DeletePhoto;

public record DeletePhotoCommand(int Id) : IRequest<PhotoMutationResult>;

public class DeletePhotoCommandHandler : IRequestHandler<DeletePhotoCommand, PhotoMutationResult>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IFileStorage _fileStorage;

    public DeletePhotoCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        IFileStorage fileStorage)
    {
        _context = context;
        _currentUserService = currentUserService;
        _fileStorage = fileStorage;
    }

    public async Task<PhotoMutationResult> Handle(DeletePhotoCommand request, CancellationToken cancellationToken)
    {
        var photo = await _context.Photos.FindAsync(new object[] { request.Id }, cancellationToken);
        if (photo == null) return PhotoMutationResult.NotFound;

        var userId = _currentUserService.UserId;
        if (photo.CreatedByUserId != userId)
            return PhotoMutationResult.Forbidden;

        var filePath = photo.FilePath;
        var thumbPath = photo.ThumbnailPath;
        _context.Photos.Remove(photo);
        await _context.SaveChangesAsync(cancellationToken);
        await _fileStorage.DeleteStoredFilesAsync(filePath, thumbPath, cancellationToken);
        return PhotoMutationResult.Ok;
    }
}
