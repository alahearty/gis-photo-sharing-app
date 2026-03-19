using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace gis_photo_sharing_app.Application.Common.Interfaces;

public interface IFileStorage
{
    Task<string> SavePhotoAsync(Stream fileStream, string fileName, CancellationToken cancellationToken = default);
    string GetPhotoUrl(string filePath);
}
