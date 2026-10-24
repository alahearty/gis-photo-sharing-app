using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace gis_photo_sharing_app.Application.Common.Interfaces;

public record PhotoSaveResult(string FilePath, string? ThumbnailPath, bool IsPublic);

public record StoredFileResult(Stream Content, string ContentType, string FileName);

public interface IFileStorage
{
    Task<PhotoSaveResult> SavePhotoAsync(
        Stream fileStream,
        string fileName,
        bool isPublic,
        CancellationToken cancellationToken = default);

    Task<StoredFileResult?> OpenStoredFileAsync(
        string? storedPath,
        CancellationToken cancellationToken = default);

    string GetPhotoUrl(string filePath);

    Task DeleteStoredFilesAsync(
        string? filePath,
        string? thumbnailPath,
        CancellationToken cancellationToken = default);
}
