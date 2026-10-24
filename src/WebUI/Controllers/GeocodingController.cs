using System.Net;
using gis_photo_sharing_app.Application.Common.Interfaces;
using gis_photo_sharing_app.Application.Common.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace gis_photo_sharing_app.WebUI.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class GeocodingController : ControllerBase
{
    private readonly IGeocodingService _geocodingService;
    private readonly GeocodingSettings _settings;

    public GeocodingController(
        IGeocodingService geocodingService,
        IOptions<GeocodingSettings> settings)
    {
        _geocodingService = geocodingService;
        _settings = settings.Value;
    }

    [HttpGet("reverse")]
    public async Task<ActionResult<GeocodingResult>> Reverse(
        [FromQuery] double latitude,
        [FromQuery] double longitude,
        CancellationToken cancellationToken)
    {
        if (!_settings.Enabled)
            return StatusCode((int)HttpStatusCode.ServiceUnavailable,
                new { error = "Reverse geocoding is not enabled on this server." });

        if (double.IsNaN(latitude) || double.IsInfinity(latitude) ||
            double.IsNaN(longitude) || double.IsInfinity(longitude) ||
            latitude < -90 || latitude > 90 ||
            longitude < -180 || longitude > 180)
        {
            return BadRequest(new { error = "Coordinates are outside the valid range." });
        }

        if (Math.Abs(latitude) < 0.0000001 && Math.Abs(longitude) < 0.0000001)
            return BadRequest(new { error = "Select a location before searching for a place name." });

        try
        {
            var result = await _geocodingService.ReverseAsync(latitude, longitude, cancellationToken);
            if (result == null)
                return NotFound(new { error = "No nearby place name was found." });

            return Ok(result);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return StatusCode((int)HttpStatusCode.ServiceUnavailable,
                new { error = "The geocoding service took too long to respond. Please try again later." });
        }
        catch (HttpRequestException)
        {
            return StatusCode((int)HttpStatusCode.ServiceUnavailable,
                new { error = "The geocoding service is currently unavailable. Please try again later." });
        }
    }
}
