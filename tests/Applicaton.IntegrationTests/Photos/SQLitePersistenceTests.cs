using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using gis_photo_sharing_app.Application.Common.Interfaces;
using gis_photo_sharing_app.Application.Photos;
using gis_photo_sharing_app.Application.Photos.Queries.GetPhotos;
using gis_photo_sharing_app.Domain.Entities;
using gis_photo_sharing_app.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NUnit.Framework;

namespace gis_photo_sharing_app.Application.IntegrationTests.Photos;

[TestFixture]
public class SQLitePersistenceTests
{
    [Test]
    public async Task PhotoMetadataPersistsAcrossContextRecreation()
    {
        var databasePath = Path.Combine(Path.GetTempPath(), $"gis-photo-{Guid.NewGuid():N}.db");
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite($"Data Source={databasePath};Pooling=False")
            .Options;

        var currentUser = Mock.Of<ICurrentUserService>(user => user.UserId == "persistence-user");
        var dateTime = Mock.Of<IDateTime>(clock => clock.Now == new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc));

        try
        {
            await using (var context = new ApplicationDbContext(options, currentUser, dateTime))
            {
                await context.Database.EnsureCreatedAsync();
                context.Photos.Add(new Photo
                {
                    Title = "Persistent photo",
                    Description = "Stored in SQLite",
                    FilePath = "/uploads/photos/persistent.png",
                    ThumbnailPath = "/uploads/thumbnails/persistent.png",
                    Latitude = -33.93,
                    Longitude = 18.86,
                    CreatedByUserId = "persistence-user",
                    Created = dateTime.Now,
                    CreatedBy = "persistence-user"
                });
                await context.SaveChangesAsync();
            }

            await using (var context = new ApplicationDbContext(options, currentUser, dateTime))
            {
                var photo = await context.Photos.SingleAsync();
                Assert.That(photo.Title, Is.EqualTo("Persistent photo"));
                Assert.That(photo.FilePath, Is.EqualTo("/uploads/photos/persistent.png"));
                Assert.That(photo.ThumbnailPath, Is.EqualTo("/uploads/thumbnails/persistent.png"));
                Assert.That(photo.Latitude, Is.EqualTo(-33.93));
                Assert.That(photo.CreatedByUserId, Is.EqualTo("persistence-user"));
            }
        }
        finally
        {
            if (File.Exists(databasePath))
                File.Delete(databasePath);
        }
    }

    [Test]
    public async Task CaseInsensitiveSearchAndCategoryFilterWorkOnSqlite()
    {
        var databasePath = Path.Combine(Path.GetTempPath(), $"gis-photo-filter-{Guid.NewGuid():N}.db");
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite($"Data Source={databasePath};Pooling=False")
            .Options;

        var currentUser = Mock.Of<ICurrentUserService>(user => user.UserId == string.Empty);
        var dateTime = Mock.Of<IDateTime>(clock => clock.Now == new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc));

        try
        {
            await using (var context = new ApplicationDbContext(options, currentUser, dateTime))
            {
                await context.Database.EnsureCreatedAsync();
                context.Photos.Add(new Photo
                {
                    Title = "Rumuola flyover",
                    FilePath = "/uploads/photos/rumuola.jpg",
                    Latitude = 4.8246,
                    Longitude = 7.0358,
                    Category = "Infrastructure",
                    IsPublic = true,
                    CreatedByUserId = "filter-user",
                });
                await context.SaveChangesAsync();

                var media = new Mock<IPhotoMediaUrlService>();
                media.Setup(service => service.BuildImageUrl(It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<bool>()))
                    .Returns("/api/photos/image");

                var handler = new GetPhotosQueryHandler(context, currentUser, media.Object);
                var result = await handler.Handle(
                    new GetPhotosQuery(new PhotoQueryFilter(Search: "rumuola", Category: "infrastructure")),
                    CancellationToken.None);

                Assert.That(result, Has.Count.EqualTo(1));
                Assert.That(result[0].Title, Is.EqualTo("Rumuola flyover"));
            }
        }
        finally
        {
            if (File.Exists(databasePath))
                File.Delete(databasePath);
        }
    }

    [Test]
    public async Task LegacySqliteSchemaIsUpgradedWithoutLosingPhotoData()
    {
        var databasePath = Path.Combine(Path.GetTempPath(), $"gis-photo-legacy-{Guid.NewGuid():N}.db");
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite($"Data Source={databasePath};Pooling=False")
            .Options;

        var currentUser = Mock.Of<ICurrentUserService>(user => user.UserId == "legacy-user");
        var dateTime = Mock.Of<IDateTime>(clock => clock.Now == new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc));

        try
        {
            await using (var context = new ApplicationDbContext(options, currentUser, dateTime))
            {
                await context.Database.OpenConnectionAsync();
                await context.Database.ExecuteSqlRawAsync(
                    """
                    CREATE TABLE "Photos" (
                        "Id" INTEGER NOT NULL CONSTRAINT "PK_Photos" PRIMARY KEY AUTOINCREMENT,
                        "Title" TEXT NOT NULL,
                        "Description" TEXT NULL,
                        "FilePath" TEXT NOT NULL,
                        "ThumbnailPath" TEXT NULL,
                        "Latitude" REAL NOT NULL,
                        "Longitude" REAL NOT NULL,
                        "TakenAt" TEXT NULL,
                        "CreatedByUserId" TEXT NULL,
                        "CreatedBy" TEXT NOT NULL,
                        "Created" TEXT NOT NULL,
                        "LastModifiedBy" TEXT NOT NULL,
                        "LastModified" TEXT NULL
                    );
                    """);

                await context.Database.ExecuteSqlRawAsync(
                    """
                    INSERT INTO "Photos"
                        ("Title", "Description", "FilePath", "Latitude", "Longitude", "CreatedByUserId", "CreatedBy", "Created", "LastModifiedBy")
                    VALUES
                        ('Legacy photo', 'Existing record', '/uploads/photos/legacy.jpg', 4.8156, 7.0498, 'legacy-user', 'legacy-user', '2026-01-01 12:00:00', 'legacy-user');
                    """);

                var applied = await SqliteSchemaUpgrader.UpgradeAsync(
                    context,
                    NullLogger.Instance);

                Assert.That(applied, Is.Not.Empty);
                Assert.That(applied, Has.Some.Contains("LocationName"));
                Assert.That(applied, Has.Some.Contains("Category"));
                Assert.That(applied, Has.Some.Contains("IsPublic"));

                var secondRun = await SqliteSchemaUpgrader.UpgradeAsync(
                    context,
                    NullLogger.Instance);
                Assert.That(secondRun, Is.Empty);
            }

            await using (var context = new ApplicationDbContext(options, currentUser, dateTime))
            {
                var photo = await context.Photos.SingleAsync();
                Assert.That(photo.Title, Is.EqualTo("Legacy photo"));
                Assert.That(photo.FilePath, Is.EqualTo("/uploads/photos/legacy.jpg"));
                Assert.That(photo.Latitude, Is.EqualTo(4.8156));
                Assert.That(photo.Longitude, Is.EqualTo(7.0498));
                Assert.That(photo.CreatedByUserId, Is.EqualTo("legacy-user"));
                Assert.That(photo.Category, Is.EqualTo("General"));
                Assert.That(photo.IsPublic, Is.True);
                Assert.That(photo.LocationName, Is.Null);
            }
        }
        finally
        {
            if (File.Exists(databasePath))
                File.Delete(databasePath);
        }
    }
}
