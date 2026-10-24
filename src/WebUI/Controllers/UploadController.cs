using System.Globalization;
using gis_photo_sharing_app.Application.Common.Exceptions;
using gis_photo_sharing_app.Application.Common.Interfaces;
using gis_photo_sharing_app.Application.Common.Models;
using gis_photo_sharing_app.Application.Photos.Commands.CreatePhoto;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace gis_photo_sharing_app.WebUI.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UploadController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IFileStorage _fileStorage;
    private readonly IExifLocationReader _exifLocationReader;
    private readonly PhotoSettings _settings;

    public UploadController(
        IMediator mediator,
        IFileStorage fileStorage,
        IExifLocationReader exifLocationReader,
        IOptions<PhotoSettings> settings)
    {
        _mediator = mediator;
        _fileStorage = fileStorage;
        _exifLocationReader = exifLocationReader;
        _settings = settings.Value;
    }

    [HttpPost("photo")]
    [RequestSizeLimit(11_010_048)]
    [RequestFormLimits(MultipartBodyLengthLimit = 11_010_048)]
    public async Task<ActionResult<int>> UploadPhoto(
        [FromForm] string? title,
        [FromForm] string? description,
        [FromForm] double? latitude,
        [FromForm] double? longitude,
        [FromForm] string? locationName,
        [FromForm] string? category,
        [FromForm] bool? isPublic,
        [FromForm] string? takenAt,
        [FromForm] IFormFile? file,
        CancellationToken cancellationToken)
    {
        var validation = ValidateCommon(
            title,
            description,
            latitude,
            longitude,
            locationName,
            category,
            takenAt,
            file);

        if (validation != null)
            return BadRequest(new { error = validation });

        var normalizedCategory = NormalizeCategory(category) ?? "General";
        var normalizedTakenAt = NormalizeTakenAt(takenAt);

        PhotoSaveResult saved;
        try
        {
            saved = await _fileStorage.SavePhotoAsync(
                file!.OpenReadStream(),
                file.FileName,
                isPublic ?? true,
                cancellationToken);
        }
        catch (InvalidImageException ex)
        {
            return BadRequest(new { error = ex.Message });
        }

        try
        {
            var id = await _mediator.Send(new CreatePhotoCommand(
                title!,
                description,
                saved.FilePath,
                saved.ThumbnailPath,
                latitude!.Value,
                longitude!.Value,
                normalizedTakenAt,
                locationName,
                normalizedCategory,
                isPublic ?? true), cancellationToken);

            return Created($"/api/photos/{id}", new { id });
        }
        catch
        {
            await _fileStorage.DeleteStoredFilesAsync(
                saved.FilePath,
                saved.ThumbnailPath,
                CancellationToken.None);
            throw;
        }
    }

    [HttpPost("exif")]
    [RequestSizeLimit(11_010_048)]
    [RequestFormLimits(MultipartBodyLengthLimit = 11_010_048)]
    public async Task<ActionResult<ExifLocationResponse>> DetectExif(
        [FromForm] IFormFile? file,
        CancellationToken cancellationToken)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new { error = "Select an image to inspect." });

        if (file.Length > _settings.MaxUploadBytes)
            return BadRequest(new { error = $"The image must not exceed {MaxUploadMegabytes()} MB." });

        if (!IsAllowedContentType(file.ContentType))
            return BadRequest(new { error = "Only JPEG, PNG, GIF, and WebP images are supported." });

        var metadata = await _exifLocationReader.ReadAsync(file.OpenReadStream(), cancellationToken);

        return Ok(new ExifLocationResponse(
            metadata.HasLocation,
            metadata.Latitude,
            metadata.Longitude,
            metadata.AltitudeMetres,
            metadata.TakenAt));
    }

    private string? ValidateCommon(
        string? title,
        string? description,
        double? latitude,
        double? longitude,
        string? locationName,
        string? category,
        string? takenAt,
        IFormFile? file)
    {
        if (string.IsNullOrWhiteSpace(title))
            return "Title is required.";

        if (title.Trim().Length > 200)
            return "Title must not exceed 200 characters.";

        if (description?.Length > 2000)
            return "Description must not exceed 2,000 characters.";

        if (file == null || file.Length == 0)
            return "Select a photo to upload.";

        if (file.Length > _settings.MaxUploadBytes)
            return $"The image must not exceed {MaxUploadMegabytes()} MB.";

        if (!IsAllowedContentType(file.ContentType))
            return "Only JPEG, PNG, GIF, and WebP images are supported.";

        if (!latitude.HasValue || !longitude.HasValue)
            return "Latitude and longitude are required.";

        if (double.IsNaN(latitude.Value) || double.IsInfinity(latitude.Value) ||
            double.IsNaN(longitude.Value) || double.IsInfinity(longitude.Value))
        {
            return "Latitude and longitude must be valid numbers.";
        }

        if (latitude.Value < -90 || latitude.Value > 90)
            return "Latitude must be between -90 and 90.";

        if (longitude.Value < -180 || longitude.Value > 180)
            return "Longitude must be between -180 and 180.";

        if (locationName?.Trim().Length > 200)
            return "Location name must not exceed 200 characters.";

        if (category?.Trim().Length > 50)
            return "Category must not exceed 50 characters.";

        if (!string.IsNullOrWhiteSpace(category) && NormalizeCategory(category) == null)
            return "Choose a category from the available list.";

        if (!string.IsNullOrWhiteSpace(takenAt) && NormalizeTakenAt(takenAt) == null)
            return "Capture date must be a valid date.";

        return null;
    }

    private bool IsAllowedContentType(string? contentType)
    {
        if (string.IsNullOrWhiteSpace(contentType))
            return false;

        return _settings.AllowedContentTypes
            .Any(allowed => string.Equals(allowed, contentType, StringComparison.OrdinalIgnoreCase));
    }

    private string? NormalizeCategory(string? category)
    {
        if (string.IsNullOrWhiteSpace(category))
            return "General";

        return _settings.Categories
            .FirstOrDefault(configured =>
                string.Equals(configured, category.Trim(), StringComparison.OrdinalIgnoreCase));
    }

    private static string? NormalizeTakenAt(string? takenAt)
    {
        if (string.IsNullOrWhiteSpace(takenAt))
            return null;

        if (DateTimeOffset.TryParse(
                takenAt,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeLocal,
                out var parsed))
        {
            return parsed.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        }

        if (DateTime.TryParse(
                takenAt,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeLocal,
                out var dateOnly))
        {
            return dateOnly.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        }

        return null;
    }

    private int MaxUploadMegabytes()
    {
        return Math.Max(1, (int)(_settings.MaxUploadBytes / (1024 * 1024)));
    }
}

public record ExifLocationResponse(
    bool HasLocation,
    double? Latitude,
    double? Longitude,
    double? AltitudeMetres,
    string? TakenAt);
