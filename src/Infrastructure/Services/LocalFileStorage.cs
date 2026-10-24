using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using gis_photo_sharing_app.Application.Common.Exceptions;
using gis_photo_sharing_app.Application.Common.Interfaces;
using gis_photo_sharing_app.Application.Common.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Formats.Gif;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace gis_photo_sharing_app.Infrastructure.Services;

public class LocalFileStorage : IFileStorage
{
    private const string UploadsFolder = "uploads";
    private const string PhotosSubFolder = "photos";
    private const string ThumbnailsSubFolder = "thumbnails";
    private const string PrivateScheme = "private://";
    private const string PrivateFolder = "private-uploads";

    private static readonly Dictionary<string, string> ContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        [".jpg"] = "image/jpeg",
        [".jpeg"] = "image/jpeg",
        [".png"] = "image/png",
        [".gif"] = "image/gif",
        [".webp"] = "image/webp",
    };

    private readonly IWebHostEnvironment _env;
    private readonly PhotoSettings _settings;
    private readonly ILogger<LocalFileStorage> _logger;

    public LocalFileStorage(
        IWebHostEnvironment env,
        IOptions<PhotoSettings> settings,
        ILogger<LocalFileStorage> logger)
    {
        _env = env;
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task<PhotoSaveResult> SavePhotoAsync(
        Stream fileStream,
        string fileName,
        bool isPublic,
        CancellationToken cancellationToken = default)
    {
        using var buffer = new MemoryStream();
        await fileStream.CopyToAsync(buffer, cancellationToken);
        buffer.Position = 0;

        ImageInfo? imageInfo;
        try
        {
            imageInfo = Image.Identify(
                new DecoderOptions { MaxFrames = 1 },
                buffer);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            throw new InvalidImageException(
                "The selected file is not a valid image. Please choose a JPEG, PNG, GIF, or WebP image.");
        }

        if (imageInfo == null ||
            imageInfo.Metadata.DecodedImageFormat == null ||
            GetExtension(imageInfo.Metadata.DecodedImageFormat.Name) == null)
        {
            throw new InvalidImageException(
                "The selected file is not a valid image. Please choose a JPEG, PNG, GIF, or WebP image.");
        }

        ValidateDimensions(imageInfo.Width, imageInfo.Height);
        buffer.Position = 0;

        Image image;
        try
        {
            image = await Image.LoadAsync(
                new DecoderOptions { MaxFrames = 1 },
                buffer,
                cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            throw new InvalidImageException(
                "The selected file is not a valid image. Please choose a JPEG, PNG, GIF, or WebP image.");
        }

        using (image)
        {
            ValidateDimensions(image.Width, image.Height);

            var format = image.Metadata.DecodedImageFormat
                ?? throw new InvalidImageException("The image format could not be determined.");

            var extension = GetExtension(format.Name);
            if (extension == null)
            {
                throw new InvalidImageException(
                    "Only JPEG, PNG, GIF, and WebP images are supported.");
            }

            image.Mutate(context => context.AutoOrient());
            image.Metadata.ExifProfile = null;
            image.Metadata.IptcProfile = null;
            image.Metadata.XmpProfile = null;

            if (image.Width > _settings.MaximumImageWidth)
            {
                image.Mutate(context => context.Resize(new ResizeOptions
                {
                    Size = new Size(_settings.MaximumImageWidth, _settings.MaximumImageWidth),
                    Mode = ResizeMode.Max,
                }));
            }

            var safeName = $"{Guid.NewGuid():N}{extension}";
            var photoPath = isPublic
                ? Path.Combine(GetPublicRoot(), PhotosSubFolder)
                : Path.Combine(GetPrivateRoot(), PhotosSubFolder);

            Directory.CreateDirectory(photoPath);
            var fullPath = Path.Combine(photoPath, safeName);
            string? thumbnailFullPath = null;

            try
            {
                await SaveNormalisedImageAsync(image, fullPath, extension, cancellationToken);

                var thumbnailName = $"{Path.GetFileNameWithoutExtension(safeName)}.jpg";
                var thumbnailDirectory = isPublic
                    ? Path.Combine(GetPublicRoot(), ThumbnailsSubFolder)
                    : Path.Combine(GetPrivateRoot(), ThumbnailsSubFolder);

                Directory.CreateDirectory(thumbnailDirectory);
                thumbnailFullPath = Path.Combine(thumbnailDirectory, thumbnailName);

                using var thumbnail = image.Clone(context =>
                {
                    context.BackgroundColor(Color.White);
                    context.Resize(new ResizeOptions
                    {
                        Size = new Size(_settings.ThumbnailSize, _settings.ThumbnailSize),
                        Mode = ResizeMode.Max,
                    });
                });

                await thumbnail.SaveAsJpegAsync(
                    thumbnailFullPath,
                    new JpegEncoder { Quality = 82 },
                    cancellationToken);

                var storedFilePath = isPublic
                    ? $"/{UploadsFolder}/{PhotosSubFolder}/{safeName}"
                    : $"{PrivateScheme}{PhotosSubFolder}/{safeName}";
                var storedThumbnailPath = isPublic
                    ? $"/{UploadsFolder}/{ThumbnailsSubFolder}/{thumbnailName}"
                    : $"{PrivateScheme}{ThumbnailsSubFolder}/{thumbnailName}";

                return new PhotoSaveResult(storedFilePath, storedThumbnailPath, isPublic);
            }
            catch
            {
                TryDeleteFile(fullPath);
                if (thumbnailFullPath != null)
                    TryDeleteFile(thumbnailFullPath);
                throw;
            }
        }
    }

    public Task<StoredFileResult?> OpenStoredFileAsync(
        string? storedPath,
        CancellationToken cancellationToken = default)
    {
        var resolved = ResolveCandidates(storedPath).FirstOrDefault(File.Exists);
        if (resolved == null)
            return Task.FromResult<StoredFileResult?>(null);

        var extension = Path.GetExtension(resolved);
        var contentType = ContentTypes.TryGetValue(extension, out var type)
            ? type
            : "application/octet-stream";

        Stream stream = new FileStream(
            resolved,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            64 * 1024,
            useAsync: true);

        return Task.FromResult<StoredFileResult?>(
            new StoredFileResult(stream, contentType, Path.GetFileName(resolved)));
    }

    public string GetPhotoUrl(string filePath)
    {
        if (string.IsNullOrEmpty(filePath)) return string.Empty;
        if (filePath.StartsWith(PrivateScheme, StringComparison.OrdinalIgnoreCase))
            return string.Empty;
        return filePath.StartsWith("/") ? filePath : $"/{filePath}";
    }

    public Task DeleteStoredFilesAsync(
        string? filePath,
        string? thumbnailPath,
        CancellationToken cancellationToken = default)
    {
        DeleteStoredFile(filePath);
        DeleteStoredFile(thumbnailPath);
        return Task.CompletedTask;
    }

    private void ValidateDimensions(int width, int height)
    {
        if (width <= 0 || height <= 0)
            throw new InvalidImageException("The image has invalid dimensions.");

        if (width > _settings.MaxImageDimension || height > _settings.MaxImageDimension)
        {
            throw new InvalidImageException(
                $"The image is too large. The maximum dimension is {_settings.MaxImageDimension} pixels.");
        }

        var pixels = (long)width * height;
        if (pixels > _settings.MaxImagePixels)
        {
            throw new InvalidImageException(
                $"The image contains too many pixels. The maximum is {_settings.MaxImagePixels:N0}.");
        }
    }

    private static async Task SaveNormalisedImageAsync(
        Image image,
        string fullPath,
        string extension,
        CancellationToken cancellationToken)
    {
        switch (extension.ToLowerInvariant())
        {
            case ".jpg":
            case ".jpeg":
                await image.SaveAsJpegAsync(fullPath, new JpegEncoder { Quality = 86 }, cancellationToken);
                break;
            case ".png":
                await image.SaveAsPngAsync(fullPath, new PngEncoder(), cancellationToken);
                break;
            case ".gif":
                await image.SaveAsGifAsync(fullPath, new GifEncoder(), cancellationToken);
                break;
            case ".webp":
                await image.SaveAsWebpAsync(fullPath, new WebpEncoder { Quality = 82 }, cancellationToken);
                break;
            default:
                throw new InvalidImageException("Unsupported image format.");
        }
    }

    private static string? GetExtension(string formatName)
    {
        return formatName.ToUpperInvariant() switch
        {
            "JPEG" => ".jpg",
            "PNG" => ".png",
            "GIF" => ".gif",
            "WEBP" => ".webp",
            _ => null,
        };
    }

    private string GetWebRoot()
    {
        return _env.WebRootPath ?? Path.Combine(_env.ContentRootPath, "wwwroot");
    }

    private string? GetStorageRoot()
    {
        if (string.IsNullOrWhiteSpace(_settings.StorageRoot))
            return null;

        var configured = _settings.StorageRoot.Trim();
        var rooted = Path.IsPathRooted(configured)
            ? configured
            : Path.Combine(_env.ContentRootPath, configured);

        return Path.GetFullPath(rooted);
    }

    private string GetPublicRoot()
    {
        var storageRoot = GetStorageRoot();
        return storageRoot == null
            ? Path.Combine(GetWebRoot(), UploadsFolder)
            : Path.Combine(storageRoot, "public");
    }

    private string GetPrivateRoot()
    {
        var storageRoot = GetStorageRoot();
        return storageRoot == null
            ? Path.Combine(_env.ContentRootPath, "App_Data", PrivateFolder)
            : Path.Combine(storageRoot, "private");
    }

    private string GetLegacyPublicRoot()
    {
        return Path.Combine(GetWebRoot(), UploadsFolder);
    }

    private string GetLegacyPrivateRoot()
    {
        return Path.Combine(_env.ContentRootPath, "App_Data", PrivateFolder);
    }

    private IEnumerable<string> ResolveCandidates(string? storedPath)
    {
        if (string.IsNullOrWhiteSpace(storedPath))
            yield break;

        if (storedPath.StartsWith(PrivateScheme, StringComparison.OrdinalIgnoreCase))
        {
            var relative = storedPath[PrivateScheme.Length..]
                .Replace('/', Path.DirectorySeparatorChar)
                .TrimStart(Path.DirectorySeparatorChar);

            var primary = ResolveUnderRoot(GetPrivateRoot(), relative);
            if (primary != null) yield return primary;

            if (GetStorageRoot() != null)
            {
                var legacy = ResolveUnderRoot(GetLegacyPrivateRoot(), relative);
                if (legacy != null && !string.Equals(legacy, primary, StringComparison.OrdinalIgnoreCase))
                    yield return legacy;
            }

            yield break;
        }

        var publicRelative = storedPath
            .Replace('/', Path.DirectorySeparatorChar)
            .TrimStart(Path.DirectorySeparatorChar);

        if (!publicRelative.StartsWith(UploadsFolder + Path.DirectorySeparatorChar,
                StringComparison.OrdinalIgnoreCase))
        {
            yield break;
        }

        var relativeWithoutUploads = publicRelative[(UploadsFolder.Length + 1)..];
        string? storagePath = null;

        if (GetStorageRoot() != null)
        {
            storagePath = ResolveUnderRoot(GetPublicRoot(), relativeWithoutUploads);
            if (storagePath != null) yield return storagePath;
        }

        var legacyPublic = ResolveUnderRoot(GetLegacyPublicRoot(), relativeWithoutUploads);
        if (legacyPublic != null &&
            !string.Equals(legacyPublic, storagePath, StringComparison.OrdinalIgnoreCase))
        {
            yield return legacyPublic;
        }
    }

    private static string? ResolveUnderRoot(string root, string relative)
    {
        if (relative.Split(Path.DirectorySeparatorChar, StringSplitOptions.RemoveEmptyEntries)
            .Any(segment => segment == ".."))
        {
            return null;
        }

        var rootFull = Path.GetFullPath(root);
        var full = Path.GetFullPath(Path.Combine(rootFull, relative));

        return full.StartsWith(rootFull, StringComparison.OrdinalIgnoreCase) ? full : null;
    }

    private void DeleteStoredFile(string? storedPath)
    {
        foreach (var fullPath in ResolveCandidates(storedPath))
            TryDeleteFile(fullPath);
    }

    private void TryDeleteFile(string fullPath)
    {
        try
        {
            if (File.Exists(fullPath))
                File.Delete(fullPath);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not delete stored file {FileName}.", Path.GetFileName(fullPath));
        }
    }
}
