using gis_photo_sharing_app.Application.Common.Interfaces;
using gis_photo_sharing_app.Application.Photos.Commands.CreatePhoto;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace gis_photo_sharing_app.WebUI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UploadController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IFileStorage _fileStorage;

    public UploadController(IMediator mediator, IFileStorage fileStorage)
    {
        _mediator = mediator;
        _fileStorage = fileStorage;
    }

    [HttpPost("photo")]
    [RequestSizeLimit(10_485_760)] // 10 MB
    public async Task<ActionResult<int>> UploadPhoto(
        [FromForm] string title,
        [FromForm] string? description,
        [FromForm] double latitude,
        [FromForm] double longitude,
        [FromForm] IFormFile? file,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(title))
            return BadRequest("Title is required.");

        string filePath = "/uploads/placeholder.jpg";
        if (file != null && file.Length > 0)
        {
            var allowed = new[] { "image/jpeg", "image/png", "image/gif", "image/webp" };
            if (!allowed.Contains(file.ContentType.ToLowerInvariant()))
                return BadRequest("Only JPEG, PNG, GIF, and WebP images are allowed.");

            filePath = await _fileStorage.SavePhotoAsync(file.OpenReadStream(), file.FileName, cancellationToken);
        }

        var id = await _mediator.Send(new CreatePhotoCommand(
            title,
            description,
            filePath,
            null,
            latitude,
            longitude,
            null), cancellationToken);

        return Created($"/api/photos/{id}", new { id });
    }
}
