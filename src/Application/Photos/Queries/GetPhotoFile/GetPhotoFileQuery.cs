using System.IO;
using System.Threading;
using System.Threading.Tasks;
using gis_photo_sharing_app.Application.Common.Interfaces;
using MediatR;

namespace gis_photo_sharing_app.Application.Photos.Queries.GetPhotoFile;

public record PhotoFileResult(
    Stream Content,
    string ContentType,
    string FileName,
    bool IsPublic);

public record GetPhotoFileQuery(
    int Id,
    bool Thumbnail,
    string? AccessToken = null) : IRequest<PhotoFileResult?>;

public class GetPhotoFileQueryHandler : IRequestHandler<GetPhotoFileQuery, PhotoFileResult?>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IPhotoMediaUrlService _mediaUrlService;
    private readonly IFileStorage _fileStorage;

    public GetPhotoFileQueryHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        IPhotoMediaUrlService mediaUrlService,
        IFileStorage fileStorage)
    {
        _context = context;
        _currentUserService = currentUserService;
        _mediaUrlService = mediaUrlService;
        _fileStorage = fileStorage;
    }

    public async Task<PhotoFileResult?> Handle(
        GetPhotoFileQuery request,
        CancellationToken cancellationToken)
    {
        var photo = await _context.Photos.FindAsync(new object[] { request.Id }, cancellationToken);
        if (photo == null) return null;

        var isOwner = !string.IsNullOrWhiteSpace(_currentUserService.UserId) &&
                      string.Equals(photo.CreatedByUserId, _currentUserService.UserId,
                          System.StringComparison.Ordinal);

        if (!photo.IsPublic && !isOwner &&
            !_mediaUrlService.ValidateAccessToken(request.AccessToken, photo.Id, request.Thumbnail))
        {
            return null;
        }

        var storedPath = request.Thumbnail
            ? photo.ThumbnailPath ?? photo.FilePath
            : photo.FilePath;

        var stored = await _fileStorage.OpenStoredFileAsync(storedPath, cancellationToken);
        if (stored == null && request.Thumbnail)
            stored = await _fileStorage.OpenStoredFileAsync(photo.FilePath, cancellationToken);

        if (stored == null) return null;

        return new PhotoFileResult(stored.Content, stored.ContentType, stored.FileName, photo.IsPublic);
    }
}
