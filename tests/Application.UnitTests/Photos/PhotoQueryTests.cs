using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using gis_photo_sharing_app.Application.Common.Interfaces;
using gis_photo_sharing_app.Application.Photos;
using gis_photo_sharing_app.Application.Photos.Queries.GetPhoto;
using gis_photo_sharing_app.Application.Photos.Queries.GetPhotoGallery;
using gis_photo_sharing_app.Application.Photos.Queries.GetPhotoGeoJson;
using gis_photo_sharing_app.Application.Photos.Queries.GetPhotos;
using gis_photo_sharing_app.Domain.Entities;
using gis_photo_sharing_app.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Moq;
using NUnit.Framework;

namespace gis_photo_sharing_app.Application.UnitTests.Photos;

[TestFixture]
public class PhotoQueryTests
{
    private static readonly DateTime Now = new(2026, 10, 23, 9, 0, 0, DateTimeKind.Utc);

    [Test]
    public async Task AnonymousQueryReturnsOnlyPublicPhotos()
    {
        await using var context = CreateContext(null);
        await SeedAsync(context);

        var handler = CreatePhotosHandler(context, null);
        var result = await handler.Handle(new GetPhotosQuery(new PhotoQueryFilter()), CancellationToken.None);

        Assert.That(result.Select(photo => photo.Title), Is.EquivalentTo(new[]
        {
            "Port Harcourt public",
            "Elsewhere public",
        }));
    }

    [Test]
    public async Task SignedInQueryIncludesOwnPrivatePhotoButNotAnotherUsersPrivatePhoto()
    {
        await using var context = CreateContext("owner-a");
        await SeedAsync(context);

        var handler = CreatePhotosHandler(context, "owner-a");
        var result = await handler.Handle(new GetPhotosQuery(new PhotoQueryFilter()), CancellationToken.None);

        Assert.That(result.Select(photo => photo.Title), Is.EquivalentTo(new[]
        {
            "Port Harcourt public",
            "Owner private",
            "Elsewhere public",
        }));
    }

    [Test]
    public async Task MineFilterIsAppliedAgainstTheAuthenticatedUser()
    {
        await using var context = CreateContext("owner-a");
        await SeedAsync(context);

        var handler = CreatePhotosHandler(context, "owner-a");
        var result = await handler.Handle(
            new GetPhotosQuery(new PhotoQueryFilter(Mine: true)),
            CancellationToken.None);

        Assert.That(result.Select(photo => photo.Title), Is.EquivalentTo(new[]
        {
            "Port Harcourt public",
            "Owner private",
        }));
        Assert.That(result.All(photo => photo.IsOwner), Is.True);
    }

    [Test]
    public async Task MineFilterWithoutAnAuthenticatedUserReturnsNoPhotos()
    {
        await using var context = CreateContext(null);
        await SeedAsync(context);

        var handler = CreatePhotosHandler(context, null);
        var result = await handler.Handle(
            new GetPhotosQuery(new PhotoQueryFilter(Mine: true)),
            CancellationToken.None);

        Assert.That(result, Is.Empty);
    }

    [Test]
    public async Task SearchCategoryAndBoundsFiltersAreCombined()
    {
        await using var context = CreateContext(null);
        await SeedAsync(context);

        var handler = CreatePhotosHandler(context, null);
        var result = await handler.Handle(
            new GetPhotosQuery(new PhotoQueryFilter(
                Search: "Harcourt",
                Category: "Tourism",
                MinLatitude: 4.7,
                MaxLatitude: 4.9,
                MinLongitude: 6.9,
                MaxLongitude: 7.1)),
            CancellationToken.None);

        Assert.That(result.Select(photo => photo.Title), Is.EquivalentTo(new[]
        {
            "Port Harcourt public",
        }));
    }

    [Test]
    public async Task SearchAndCategoryFiltersAreCaseInsensitive()
    {
        await using var context = CreateContext(null);
        await SeedAsync(context);

        var handler = CreatePhotosHandler(context, null);
        var result = await handler.Handle(
            new GetPhotosQuery(new PhotoQueryFilter(Search: "harcourt", Category: "tourism")),
            CancellationToken.None);

        Assert.That(result.Select(photo => photo.Title), Is.EquivalentTo(new[]
        {
            "Port Harcourt public",
        }));
    }

    [Test]
    public async Task DateFilterExcludesPhotographsOutsideTheRange()
    {
        await using var context = CreateContext(null);
        await SeedAsync(context);

        var handler = CreatePhotosHandler(context, null);
        var included = await handler.Handle(
            new GetPhotosQuery(new PhotoQueryFilter(From: Now.Date, To: Now.Date)),
            CancellationToken.None);
        var excluded = await handler.Handle(
            new GetPhotosQuery(new PhotoQueryFilter(To: Now.Date.AddDays(-1))),
            CancellationToken.None);

        Assert.That(included, Is.Not.Empty);
        Assert.That(excluded, Is.Empty);
    }

    [Test]
    public async Task TitleSortIsStableAndAlphabetical()
    {
        await using var context = CreateContext(null);
        await SeedAsync(context);

        var handler = CreatePhotosHandler(context, null);
        var result = await handler.Handle(
            new GetPhotosQuery(new PhotoQueryFilter(Sort: "title")),
            CancellationToken.None);

        var titles = result.Select(photo => photo.Title).ToArray();
        Assert.That(titles, Is.Ordered);
    }

    [Test]
    public async Task PrivatePhotoDetailIsOnlyVisibleToItsOwner()
    {
        await using var context = CreateContext("owner-a");
        await SeedAsync(context);
        var photo = await context.Photos.SingleAsync(item => item.Title == "Owner private");

        var otherHandler = new GetPhotoQueryHandler(
            context,
            MockUser("owner-b").Object,
            MockMedia().Object);
        var ownerHandler = new GetPhotoQueryHandler(
            context,
            MockUser("owner-a").Object,
            MockMedia().Object);

        Assert.That(await otherHandler.Handle(new GetPhotoQuery(photo.Id), CancellationToken.None), Is.Null);
        var ownerResult = await ownerHandler.Handle(new GetPhotoQuery(photo.Id), CancellationToken.None);
        Assert.That(ownerResult, Is.Not.Null);
        Assert.That(ownerResult!.IsOwner, Is.True);
    }

    [Test]
    public async Task GalleryIsPagedAndReportsTheTotalCount()
    {
        await using var context = CreateContext("owner-a");
        await SeedAsync(context);

        var handler = new GetPhotoGalleryQueryHandler(
            context,
            MockUser("owner-a").Object,
            MockMedia().Object);

        var page = await handler.Handle(
            new GetPhotoGalleryQuery(new PhotoQueryFilter(Mine: true), Page: 2, PageSize: 1),
            CancellationToken.None);

        Assert.That(page.TotalCount, Is.EqualTo(2));
        Assert.That(page.TotalPages, Is.EqualTo(2));
        Assert.That(page.Items, Has.Count.EqualTo(1));
    }

    [Test]
    public async Task GeoJsonUsesLongitudeLatitudeOrderAndExcludesPrivatePhotos()
    {
        await using var context = CreateContext("owner-a");
        await SeedAsync(context);

        var handler = new GetPhotoGeoJsonQueryHandler(
            context,
            MockUser("owner-a").Object,
            MockMedia().Object);

        var result = await handler.Handle(
            new GetPhotoGeoJsonQuery(new PhotoQueryFilter(Sort: "title")),
            CancellationToken.None);

        Assert.That(result.Type, Is.EqualTo("FeatureCollection"));
        Assert.That(result.Features.Any(feature => feature.Properties.Title == "Owner private"), Is.False);

        var publicFeature = result.Features.Single(feature => feature.Properties.Title == "Port Harcourt public");
        Assert.That(publicFeature.Geometry.Coordinates[0], Is.EqualTo(7.0498));
        Assert.That(publicFeature.Geometry.Coordinates[1], Is.EqualTo(4.8156));
    }

    private static async Task SeedAsync(ApplicationDbContext context)
    {
        context.Photos.AddRange(
            new Photo
            {
                Title = "Port Harcourt public",
                Description = "A public photo in Port Harcourt",
                FilePath = "/uploads/test-public.jpg",
                Latitude = 4.8156,
                Longitude = 7.0498,
                LocationName = "Port Harcourt",
                Category = "Tourism",
                IsPublic = true,
                CreatedByUserId = "owner-a",
            },
            new Photo
            {
                Title = "Owner private",
                Description = "Only the owner can see this",
                FilePath = "private://photos/test-private.jpg",
                Latitude = 4.82,
                Longitude = 7.03,
                LocationName = "Port Harcourt",
                Category = "Tourism",
                IsPublic = false,
                CreatedByUserId = "owner-a",
            },
            new Photo
            {
                Title = "Elsewhere public",
                Description = "A public photo outside Port Harcourt",
                FilePath = "/uploads/test-elsewhere.jpg",
                Latitude = 9.03,
                Longitude = 8.68,
                LocationName = "Abuja",
                Category = "Landscape",
                IsPublic = true,
                CreatedByUserId = "owner-b",
            });

        await context.SaveChangesAsync();
    }

    private static ApplicationDbContext CreateContext(string? currentUserId)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"photos-{Guid.NewGuid():N}")
            .Options;

        return new ApplicationDbContext(options, MockUser(currentUserId).Object, MockDate().Object);
    }

    private static Mock<ICurrentUserService> MockUser(string? userId)
    {
        var mock = new Mock<ICurrentUserService>();
        mock.SetupGet(service => service.UserId).Returns(userId);
        return mock;
    }

    private static Mock<IDateTime> MockDate()
    {
        var mock = new Mock<IDateTime>();
        mock.SetupGet(service => service.Now).Returns(Now);
        return mock;
    }

    private static Mock<IPhotoMediaUrlService> MockMedia()
    {
        var mock = new Mock<IPhotoMediaUrlService>();
        mock.Setup(service => service.BuildImageUrl(It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .Returns("/api/photos/image");
        return mock;
    }

    private static GetPhotosQueryHandler CreatePhotosHandler(
        ApplicationDbContext context,
        string? userId)
    {
        return new GetPhotosQueryHandler(context, MockUser(userId).Object, MockMedia().Object);
    }
}
