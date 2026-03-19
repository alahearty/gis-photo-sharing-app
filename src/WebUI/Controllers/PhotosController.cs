using gis_photo_sharing_app.Application.Photos.Commands.CreatePhoto;
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
    public async Task<ActionResult<List<PhotoDto>>> GetPhotos(CancellationToken cancellationToken)
    {
        var photos = await _mediator.Send(new GetPhotosQuery(), cancellationToken);
        return Ok(photos);
    }

    [HttpPost]
    public async Task<ActionResult<int>> CreatePhoto([FromBody] CreatePhotoRequest request, CancellationToken cancellationToken)
    {
        var id = await _mediator.Send(new CreatePhotoCommand(
            request.Title,
            request.Description,
            request.FilePath,
            request.ThumbnailPath,
            request.Latitude,
            request.Longitude,
            request.TakenAt), cancellationToken);
        return CreatedAtAction(nameof(GetPhotos), new { id }, id);
    }
}

public record CreatePhotoRequest(
    string Title,
    string? Description,
    string FilePath,
    string? ThumbnailPath,
    double Latitude,
    double Longitude,
    string? TakenAt);
