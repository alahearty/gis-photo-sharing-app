using gis_photo_sharing_app.Application.Photos;
using gis_photo_sharing_app.Application.Photos.Commands.CreatePhoto;
using gis_photo_sharing_app.Application.Photos.Commands.UpdatePhoto;
using gis_photo_sharing_app.Application.Photos.Queries.GetPhotos;
using NUnit.Framework;

namespace gis_photo_sharing_app.Application.UnitTests.Photos;

[TestFixture]
public class PhotoValidationTests
{
    [Test]
    public void CreatePhotoRejectsOutOfRangeCoordinates()
    {
        var validator = new CreatePhotoCommandValidator();
        var result = validator.Validate(new CreatePhotoCommand(
            "Photo",
            null,
            "/uploads/photo.jpg",
            null,
            91,
            -181,
            null));

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors.Exists(error => error.PropertyName == nameof(CreatePhotoCommand.Latitude)), Is.True);
        Assert.That(result.Errors.Exists(error => error.PropertyName == nameof(CreatePhotoCommand.Longitude)), Is.True);
    }

    [Test]
    public void CreatePhotoAcceptsPortHarcourtCoordinates()
    {
        var validator = new CreatePhotoCommandValidator();
        var result = validator.Validate(new CreatePhotoCommand(
            "Port Harcourt",
            "Description",
            "/uploads/photo.jpg",
            "/uploads/thumb.jpg",
            4.8156,
            7.0498,
            "2026-10-23",
            "Port Harcourt",
            "Tourism"));

        Assert.That(result.IsValid, Is.True);
    }

    [Test]
    public void UpdatePhotoRejectsOutOfRangeCoordinatesAndEmptyTitle()
    {
        var validator = new UpdatePhotoCommandValidator();
        var result = validator.Validate(new UpdatePhotoCommand(
            1,
            "",
            null,
            -91,
            181));

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors.Exists(error => error.PropertyName == nameof(UpdatePhotoCommand.Title)), Is.True);
        Assert.That(result.Errors.Exists(error => error.PropertyName == nameof(UpdatePhotoCommand.Latitude)), Is.True);
        Assert.That(result.Errors.Exists(error => error.PropertyName == nameof(UpdatePhotoCommand.Longitude)), Is.True);
    }

    [Test]
    public void PhotoQueryRejectsInvertedBoundsAndUnknownSort()
    {
        var validator = new GetPhotosQueryValidator();
        var result = validator.Validate(new GetPhotosQuery(new PhotoQueryFilter(
            MinLatitude: 10,
            MaxLatitude: 5,
            MinLongitude: 20,
            MaxLongitude: 10,
            Sort: "random")));

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors.Exists(error => error.ErrorMessage.Contains("Minimum latitude")), Is.True);
        Assert.That(result.Errors.Exists(error => error.ErrorMessage.Contains("Minimum longitude")), Is.True);
        Assert.That(result.Errors.Exists(error => error.ErrorMessage.Contains("Sort")), Is.True);
    }

    [Test]
    public void PhotoQueryAcceptsValidFilters()
    {
        var validator = new GetPhotosQueryValidator();
        var result = validator.Validate(new GetPhotosQuery(new PhotoQueryFilter(
            Search: "Port Harcourt",
            Category: "Tourism",
            MinLatitude: 4,
            MaxLatitude: 5,
            MinLongitude: 7,
            MaxLongitude: 8,
            Sort: "newest")));

        Assert.That(result.IsValid, Is.True);
    }
}
