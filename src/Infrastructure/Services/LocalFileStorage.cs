using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using gis_photo_sharing_app.Application.Common.Interfaces;
using Microsoft.AspNetCore.Hosting;

namespace gis_photo_sharing_app.Infrastructure.Services;

public class LocalFileStorage : IFileStorage
{
    private readonly IWebHostEnvironment _env;
    private const string UploadsFolder = "uploads";
    private const string PhotosSubFolder = "photos";

    public LocalFileStorage(IWebHostEnvironment env)
    {
        _env = env;
    }

    public async Task<string> SavePhotoAsync(Stream fileStream, string fileName, CancellationToken cancellationToken = default)
    {
        var uploadsPath = Path.Combine(_env.WebRootPath ?? Path.Combine(_env.ContentRootPath, "wwwroot"), UploadsFolder, PhotosSubFolder);
        Directory.CreateDirectory(uploadsPath);

        var ext = Path.GetExtension(fileName)?.ToLowerInvariant() ?? ".jpg";
        if (ext != ".jpg" && ext != ".jpeg" && ext != ".png" && ext != ".gif" && ext != ".webp")
            ext = ".jpg";

        var safeName = $"{Guid.NewGuid():N}{ext}";
        var fullPath = Path.Combine(uploadsPath, safeName);

        await using (var fs = new FileStream(fullPath, FileMode.Create, FileAccess.Write, FileShare.None))
        {
            await fileStream.CopyToAsync(fs, cancellationToken);
        }

        return $"/{UploadsFolder}/{PhotosSubFolder}/{safeName}";
    }

    public string GetPhotoUrl(string filePath)
    {
        if (string.IsNullOrEmpty(filePath)) return string.Empty;
        return filePath.StartsWith("/") ? filePath : $"/{filePath}";
    }
}
