using System;
using System.Threading;
using System.Threading.Tasks;
using gis_photo_sharing_app.Application.Common.Interfaces;
using gis_photo_sharing_app.Application.Photos;
using gis_photo_sharing_app.Application.Photos.Commands.CreatePhoto;
using gis_photo_sharing_app.Application.Photos.Commands.DeletePhoto;
using gis_photo_sharing_app.Application.Photos.Commands.UpdatePhoto;
using gis_photo_sharing_app.Domain.Entities;
using gis_photo_sharing_app.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Moq;
using NUnit.Framework;

namespace gis_photo_sharing_app.Application.UnitTests.Photos;

[TestFixture]
public class PhotoCommandTests
{
    [Test]
    public async Task CreatePhotoSetsOwnerAndMetadataDefaults()
    {
        await using var context = CreateContext("owner-a");
        var handler = new CreatePhotoCommandHandler(context, MockUser("owner-a").Object);

        var id = await handler.Handle(
            new CreatePhotoCommand(
                "  Port Harcourt view  ",
                " Description ",
                "/uploads/photo.jpg",
                "/uploads/thumb.jpg",
                4.8156,
                7.0498,
                "2026-10-23",
                " Port Harcourt ",
                "Tourism",
                true),
            CancellationToken.None);

        var photo = await context.Photos.SingleAsync(item => item.Id == id);
        Assert.That(photo.Title, Is.EqualTo("Port Harcourt view"));
        Assert.That(photo.Description, Is.EqualTo("Description"));
        Assert.That(photo.LocationName, Is.EqualTo("Port Harcourt"));
        Assert.That(photo.Category, Is.EqualTo("Tourism"));
        Assert.That(photo.CreatedByUserId, Is.EqualTo("owner-a"));
        Assert.That(photo.IsPublic, Is.True);
    }

    [Test]
    public async Task CreatePhotoUsesGeneralWhenCategoryIsEmpty()
    {
        await using var context = CreateContext("owner-a");
        var handler = new CreatePhotoCommandHandler(context, MockUser("owner-a").Object);

        var id = await handler.Handle(
            new CreatePhotoCommand("Photo", null, "/uploads/photo.jpg", null, 4.8, 7.0, null),
            CancellationToken.None);

        var photo = await context.Photos.SingleAsync(item => item.Id == id);
        Assert.That(photo.Category, Is.EqualTo("General"));
    }

    [Test]
    public async Task UpdatePhotoRejectsANonOwner()
    {
        await using var context = CreateContext("owner-b");
        var photo = await SeedPhotoAsync(context, "owner-a");
        var handler = new UpdatePhotoCommandHandler(context, MockUser("owner-b").Object);

        var result = await handler.Handle(
            new UpdatePhotoCommand(photo.Id, "Changed", null, 5, 8),
            CancellationToken.None);

        Assert.That(result, Is.EqualTo(PhotoMutationResult.Forbidden));
        var unchanged = await context.Photos.SingleAsync(item => item.Id == photo.Id);
        Assert.That(unchanged.Title, Is.EqualTo(photo.Title));
    }

    [Test]
    public async Task UpdatePhotoUpdatesCoordinatesAndVisibilityForTheOwner()
    {
        await using var context = CreateContext("owner-a");
        var photo = await SeedPhotoAsync(context, "owner-a");
        var handler = new UpdatePhotoCommandHandler(context, MockUser("owner-a").Object);

        var result = await handler.Handle(
            new UpdatePhotoCommand(
                photo.Id,
                "Updated",
                "Updated description",
                9.03,
                8.68,
                "Abuja",
                "Landscape",
                false,
                "2026-10-22"),
            CancellationToken.None);

        Assert.That(result, Is.EqualTo(PhotoMutationResult.Ok));
        var updated = await context.Photos.SingleAsync(item => item.Id == photo.Id);
        Assert.That(updated.Title, Is.EqualTo("Updated"));
        Assert.That(updated.Latitude, Is.EqualTo(9.03));
        Assert.That(updated.Longitude, Is.EqualTo(8.68));
        Assert.That(updated.LocationName, Is.EqualTo("Abuja"));
        Assert.That(updated.Category, Is.EqualTo("Landscape"));
        Assert.That(updated.IsPublic, Is.False);
        Assert.That(updated.TakenAt, Is.EqualTo("2026-10-22"));
    }

    [Test]
    public async Task DeletePhotoRejectsANonOwnerAndDoesNotRemoveFiles()
    {
        await using var context = CreateContext("owner-b");
        var photo = await SeedPhotoAsync(context, "owner-a");
        var storage = new Mock<IFileStorage>();
        var handler = new DeletePhotoCommandHandler(context, MockUser("owner-b").Object, storage.Object);

        var result = await handler.Handle(new DeletePhotoCommand(photo.Id), CancellationToken.None);

        Assert.That(result, Is.EqualTo(PhotoMutationResult.Forbidden));
        storage.Verify(
            service => service.DeleteStoredFilesAsync(
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Test]
    public async Task DeletePhotoRemovesTheRecordAndFilesForTheOwner()
    {
        await using var context = CreateContext("owner-a");
        var photo = await SeedPhotoAsync(context, "owner-a");
        var storage = new Mock<IFileStorage>();
        var handler = new DeletePhotoCommandHandler(context, MockUser("owner-a").Object, storage.Object);

        var result = await handler.Handle(new DeletePhotoCommand(photo.Id), CancellationToken.None);

        Assert.That(result, Is.EqualTo(PhotoMutationResult.Ok));
        Assert.That(await context.Photos.CountAsync(), Is.Zero);
        storage.Verify(
            service => service.DeleteStoredFilesAsync(
                photo.FilePath,
                photo.ThumbnailPath,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    private static async Task<Photo> SeedPhotoAsync(ApplicationDbContext context, string ownerId)
    {
        var photo = new Photo
        {
            Title = "Original",
            Description = "Original description",
            FilePath = "/uploads/photos/original.jpg",
            ThumbnailPath = "/uploads/thumbnails/original.jpg",
            Latitude = 4.8156,
            Longitude = 7.0498,
            Category = "Tourism",
            IsPublic = true,
            CreatedByUserId = ownerId,
        };

        context.Photos.Add(photo);
        await context.SaveChangesAsync();
        return photo;
    }

    private static ApplicationDbContext CreateContext(string? currentUserId)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"commands-{Guid.NewGuid():N}")
            .Options;

        var dateTime = new Mock<IDateTime>();
        dateTime.SetupGet(service => service.Now).Returns(new DateTime(2026, 10, 23, 9, 0, 0, DateTimeKind.Utc));

        return new ApplicationDbContext(options, MockUser(currentUserId).Object, dateTime.Object);
    }

    private static Mock<ICurrentUserService> MockUser(string? userId)
    {
        var mock = new Mock<ICurrentUserService>();
        mock.SetupGet(service => service.UserId).Returns(userId);
        return mock;
    }
}
