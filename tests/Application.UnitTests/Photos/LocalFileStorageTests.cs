using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using gis_photo_sharing_app.Application.Common.Exceptions;
using gis_photo_sharing_app.Application.Common.Models;
using gis_photo_sharing_app.Infrastructure.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using NUnit.Framework;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Metadata.Profiles.Exif;
using SixLabors.ImageSharp.PixelFormats;

namespace gis_photo_sharing_app.Application.UnitTests.Photos;

[TestFixture]
public class LocalFileStorageTests
{
    [Test]
    public async Task PublicImageIsStoredWithThumbnailAndMetadataStripped()
    {
        var root = CreateTemporaryRoot();
        try
        {
            var storage = CreateStorage(root);
            await using var source = await CreateExifJpegAsync();

            var result = await storage.SavePhotoAsync(source, "gps-photo.jpg", isPublic: true);

            Assert.That(result.IsPublic, Is.True);
            Assert.That(result.FilePath, Does.StartWith("/uploads/photos/"));
            Assert.That(result.ThumbnailPath, Does.StartWith("/uploads/thumbnails/"));

            var absolutePath = Path.Combine(root, "wwwroot", result.FilePath.TrimStart('/')
                .Replace('/', Path.DirectorySeparatorChar));
            Assert.That(File.Exists(absolutePath), Is.True);

            using var stored = await Image.LoadAsync(absolutePath);
            Assert.That(stored.Metadata.ExifProfile, Is.Null);

            var storedFile = await storage.OpenStoredFileAsync(result.FilePath);
            Assert.That(storedFile, Is.Not.Null);
            Assert.That(storedFile!.ContentType, Is.EqualTo("image/jpeg"));
            storedFile.Content.Dispose();

            var thumbnail = await storage.OpenStoredFileAsync(result.ThumbnailPath);
            Assert.That(thumbnail, Is.Not.Null);
            thumbnail!.Content.Dispose();
        }
        finally
        {
            DeleteTemporaryRoot(root);
        }
    }

    [Test]
    public async Task PrivateImageIsStoredOutsideTheWebRoot()
    {
        var root = CreateTemporaryRoot();
        try
        {
            var storage = CreateStorage(root);
            await using var source = await CreateExifJpegAsync();

            var result = await storage.SavePhotoAsync(source, "gps-photo.jpg", isPublic: false);

            Assert.That(result.IsPublic, Is.False);
            Assert.That(result.FilePath, Does.StartWith("private://photos/"));
            Assert.That(result.ThumbnailPath, Does.StartWith("private://thumbnails/"));

            var publicDirectory = Path.Combine(root, "wwwroot", "uploads", "photos");
            Assert.That(Directory.Exists(publicDirectory), Is.False);

            var storedFile = await storage.OpenStoredFileAsync(result.FilePath);
            Assert.That(storedFile, Is.Not.Null);
            storedFile!.Content.Dispose();

            await storage.DeleteStoredFilesAsync(result.FilePath, result.ThumbnailPath);
            Assert.That(await storage.OpenStoredFileAsync(result.FilePath), Is.Null);
            Assert.That(await storage.OpenStoredFileAsync(result.ThumbnailPath), Is.Null);
        }
        finally
        {
            DeleteTemporaryRoot(root);
        }
    }

    [Test]
    public async Task MalformedContentIsRejected()
    {
        var root = CreateTemporaryRoot();
        try
        {
            var storage = CreateStorage(root);
            await using var source = new MemoryStream(new byte[] { 1, 2, 3, 4, 5 });

            Assert.ThrowsAsync<InvalidImageException>(
                async () =>
                {
                    await storage.SavePhotoAsync(source, "fake.jpg", isPublic: true);
                });
        }
        finally
        {
            DeleteTemporaryRoot(root);
        }
    }

    private static LocalFileStorage CreateStorage(string root)
    {
        var environment = new Mock<IWebHostEnvironment>();
        environment.SetupGet(env => env.ContentRootPath).Returns(root);
        environment.SetupGet(env => env.WebRootPath).Returns(Path.Combine(root, "wwwroot"));

        return new LocalFileStorage(
            environment.Object,
            Options.Create(new PhotoSettings()),
            NullLogger<LocalFileStorage>.Instance);
    }

    private static string CreateTemporaryRoot()
    {
        var root = Path.Combine(Path.GetTempPath(), $"gis-photo-storage-{Guid.NewGuid():N}");
        Directory.CreateDirectory(root);
        return root;
    }

    private static void DeleteTemporaryRoot(string root)
    {
        try
        {
            if (Directory.Exists(root))
                Directory.Delete(root, recursive: true);
        }
        catch
        {
            // Best-effort cleanup on Windows file handles.
        }
    }

    private static async Task<MemoryStream> CreateExifJpegAsync()
    {
        var stream = new MemoryStream();
        using (var image = new Image<Rgba32>(40, 30))
        {
            var profile = new ExifProfile();
            profile.SetValue(ExifTag.GPSLatitudeRef, "N");
            profile.SetValue(ExifTag.GPSLatitude, new[]
            {
                new Rational(4, 1),
                new Rational(48, 1),
                new Rational(33, 1),
            });
            profile.SetValue(ExifTag.GPSLongitudeRef, "E");
            profile.SetValue(ExifTag.GPSLongitude, new[]
            {
                new Rational(7, 1),
                new Rational(2, 1),
                new Rational(59, 1),
            });
            image.Metadata.ExifProfile = profile;

            await image.SaveAsJpegAsync(stream);
        }

        stream.Position = 0;
        return stream;
    }
}
