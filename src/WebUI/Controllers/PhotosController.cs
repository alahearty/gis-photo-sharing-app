using gis_photo_sharing_app.Application.Photos;
using gis_photo_sharing_app.Application.Photos.Commands.CreatePhoto;
using gis_photo_sharing_app.Application.Photos.Commands.DeletePhoto;
using gis_photo_sharing_app.Application.Photos.Commands.UpdatePhoto;
using gis_photo_sharing_app.Application.Photos.Queries.GetPhoto;
using gis_photo_sharing_app.Application.Photos.Queries.GetPhotoFile;
using gis_photo_sharing_app.Application.Photos.Queries.GetPhotoGallery;
using gis_photo_sharing_app.Application.Photos.Queries.GetPhotoGeoJson;
using gis_photo_sharing_app.Application.Photos.Queries.GetPhotos;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace gis_photo_sharing_app.WebUI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PhotosController : ControllerBase
{
    private readonly IMediator _mediator;

    public PhotosController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<ActionResult<List<PhotoDto>>> GetPhotos(
        [FromQuery] string? createdByUserId,
        [FromQuery] bool mine,
        [FromQuery] string? search,
        [FromQuery] string? category,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] double? minLat,
        [FromQuery] double? maxLat,
        [FromQuery] double? minLng,
        [FromQuery] double? maxLng,
        [FromQuery] string? sort,
        CancellationToken cancellationToken)
    {
        if (mine && User.Identity?.IsAuthenticated != true)
            return Unauthorized(new { error = "Sign in to view your photos." });

        var filter = BuildFilter(
            mine,
            createdByUserId,
            search,
            category,
            from,
            to,
            minLat,
            maxLat,
            minLng,
            maxLng,
            sort);

        var photos = await _mediator.Send(new GetPhotosQuery(filter), cancellationToken);
        return Ok(photos);
    }

    [HttpGet("gallery")]
    [Authorize]
    public async Task<ActionResult> GetGallery(
        [FromQuery] bool mine = true,
        [FromQuery] string? search = null,
        [FromQuery] string? category = null,
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null,
        [FromQuery] string? sort = "newest",
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 24,
        CancellationToken cancellationToken = default)
    {
        var filter = BuildFilter(
            mine,
            null,
            search,
            category,
            from,
            to,
            null,
            null,
            null,
            null,
            sort);

        var result = await _mediator.Send(
            new GetPhotoGalleryQuery(filter, page, pageSize),
            cancellationToken);

        return Ok(result);
    }

    [HttpGet("geojson")]
    public async Task<ActionResult<GeoJsonFeatureCollection>> GetGeoJson(
        [FromQuery] string? search,
        [FromQuery] string? category,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] double? minLat,
        [FromQuery] double? maxLat,
        [FromQuery] double? minLng,
        [FromQuery] double? maxLng,
        [FromQuery] string? sort,
        CancellationToken cancellationToken)
    {
        var filter = BuildFilter(
            false,
            null,
            search,
            category,
            from,
            to,
            minLat,
            maxLat,
            minLng,
            maxLng,
            sort);

        var geoJson = await _mediator.Send(new GetPhotoGeoJsonQuery(filter), cancellationToken);
        return Ok(geoJson);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<PhotoDetailDto>> GetPhoto(int id, CancellationToken cancellationToken)
    {
        var photo = await _mediator.Send(new GetPhotoQuery(id), cancellationToken);
        if (photo == null) return NotFound();
        return Ok(photo);
    }

    [HttpGet("{id:int}/file")]
    public Task<IActionResult> GetPhotoFile(
        int id,
        [FromQuery] string? token,
        CancellationToken cancellationToken)
    {
        return GetMedia(id, thumbnail: false, token, cancellationToken);
    }

    [HttpGet("{id:int}/thumbnail")]
    public Task<IActionResult> GetPhotoThumbnail(
        int id,
        [FromQuery] string? token,
        CancellationToken cancellationToken)
    {
        return GetMedia(id, thumbnail: true, token, cancellationToken);
    }

    [HttpPut("{id:int}")]
    [Authorize]
    public async Task<ActionResult> UpdatePhoto(
        int id,
        [FromBody] UpdatePhotoRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new UpdatePhotoCommand(
                id,
                request.Title,
                request.Description,
                request.Latitude,
                request.Longitude,
                request.LocationName,
                request.Category,
                request.IsPublic,
                request.TakenAt),
            cancellationToken);

        return result switch
        {
            PhotoMutationResult.NotFound => NotFound(),
            PhotoMutationResult.Forbidden => Forbid(),
            _ => NoContent(),
        };
    }

    [HttpDelete("{id:int}")]
    [Authorize]
    public async Task<ActionResult> DeletePhoto(int id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new DeletePhotoCommand(id), cancellationToken);
        return result switch
        {
            PhotoMutationResult.NotFound => NotFound(),
            PhotoMutationResult.Forbidden => Forbid(),
            _ => NoContent(),
        };
    }

    [HttpPost]
    [Authorize]
    public async Task<ActionResult<int>> CreatePhoto(
        [FromBody] CreatePhotoRequest request,
        CancellationToken cancellationToken)
    {
        if (request.IsPublic == false)
        {
            return BadRequest(new
            {
                error = "Use the photo upload endpoint to create private photographs.",
            });
        }

        if (!IsSafePublicStoragePath(request.FilePath))
        {
            return BadRequest(new
            {
                error = "FilePath must reference an existing public /uploads/ image. Use the photo upload endpoint for new photographs.",
            });
        }

        var thumbnailPath = IsSafePublicStoragePath(request.ThumbnailPath)
            ? request.ThumbnailPath
            : null;

        var id = await _mediator.Send(
            new CreatePhotoCommand(
                request.Title,
                request.Description,
                request.FilePath,
                thumbnailPath,
                request.Latitude,
                request.Longitude,
                request.TakenAt,
                request.LocationName,
                request.Category,
                true),
            cancellationToken);

        return CreatedAtAction(nameof(GetPhoto), new { id }, id);
    }

    private static bool IsSafePublicStoragePath(string? path)
    {
        if (string.IsNullOrWhiteSpace(path)) return false;
        if (!path.StartsWith("/uploads/", StringComparison.OrdinalIgnoreCase)) return false;
        return !path.Contains("..", StringComparison.Ordinal);
    }

    private async Task<IActionResult> GetMedia(
        int id,
        bool thumbnail,
        string? token,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new GetPhotoFileQuery(id, thumbnail, token),
            cancellationToken);

        if (result == null)
            return NotFound();

        Response.Headers.CacheControl = result.IsPublic
            ? "public,max-age=86400"
            : "private,no-store";
        Response.Headers["X-Content-Type-Options"] = "nosniff";

        return new FileStreamResult(result.Content, result.ContentType) { EnableRangeProcessing = true };
    }

    private static PhotoQueryFilter BuildFilter(
        bool mine,
        string? createdByUserId,
        string? search,
        string? category,
        DateTime? from,
        DateTime? to,
        double? minLat,
        double? maxLat,
        double? minLng,
        double? maxLng,
        string? sort)
    {
        return new PhotoQueryFilter(
            mine,
            createdByUserId,
            search,
            category,
            from,
            to,
            minLat,
            maxLat,
            minLng,
            maxLng,
            string.IsNullOrWhiteSpace(sort) ? "newest" : sort);
    }
}

public record UpdatePhotoRequest(
    string Title,
    string? Description,
    double Latitude,
    double Longitude,
    string? LocationName = null,
    string? Category = null,
    bool? IsPublic = null,
    string? TakenAt = null);

public record CreatePhotoRequest(
    string Title,
    string? Description,
    string FilePath,
    string? ThumbnailPath,
    double Latitude,
    double Longitude,
    string? TakenAt,
    string? LocationName = null,
    string? Category = null,
    bool IsPublic = true);
