using System;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using gis_photo_sharing_app.Application.Common.Interfaces;
using gis_photo_sharing_app.Application.Common.Models;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace gis_photo_sharing_app.Infrastructure.Services;

public class GeocodingService : IGeocodingService
{
    private static readonly SemaphoreSlim Throttle = new(1, 1);
    private static DateTime _lastRequestUtc = DateTime.MinValue;

    private readonly HttpClient _httpClient;
    private readonly GeocodingSettings _settings;
    private readonly IMemoryCache _cache;
    private readonly ILogger<GeocodingService> _logger;

    public GeocodingService(
        HttpClient httpClient,
        IOptions<GeocodingSettings> settings,
        IMemoryCache cache,
        ILogger<GeocodingService> logger)
    {
        _httpClient = httpClient;
        _settings = settings.Value;
        _cache = cache;
        _logger = logger;
    }

    public async Task<GeocodingResult?> ReverseAsync(
        double latitude,
        double longitude,
        CancellationToken cancellationToken = default)
    {
        if (!_settings.Enabled)
            return null;

        var cacheKey = string.Create(
            CultureInfo.InvariantCulture,
            $"reverse-geocode:{Math.Round(latitude, 5)}:{Math.Round(longitude, 5)}");

        if (_cache.TryGetValue(cacheKey, out GeocodingResult? cached))
            return cached;

        await Throttle.WaitAsync(cancellationToken);
        try
        {
            var sinceLast = DateTime.UtcNow - _lastRequestUtc;
            var minimumInterval = TimeSpan.FromMilliseconds(Math.Max(0, _settings.MinimumIntervalMilliseconds));
            if (sinceLast < minimumInterval)
                await Task.Delay(minimumInterval - sinceLast, cancellationToken);

            var baseUrl = _settings.BaseUrl.EndsWith('/') ? _settings.BaseUrl : _settings.BaseUrl + "/";
            var url = string.Format(
                CultureInfo.InvariantCulture,
                "{0}reverse?format=jsonv2&lat={1:F7}&lon={2:F7}&zoom=16&addressdetails=1",
                baseUrl,
                latitude,
                longitude);

            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.TryAddWithoutValidation("User-Agent", _settings.UserAgent);
            request.Headers.TryAddWithoutValidation("Accept", "application/json");
            request.Headers.TryAddWithoutValidation("Accept-Language", "en");

            using var response = await _httpClient.SendAsync(
                request,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken);

            _lastRequestUtc = DateTime.UtcNow;

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Reverse geocoding provider returned {StatusCode}.", (int)response.StatusCode);
                return null;
            }

            using var document = await JsonDocument.ParseAsync(
                await response.Content.ReadAsStreamAsync(cancellationToken),
                cancellationToken: cancellationToken);

            if (!document.RootElement.TryGetProperty("display_name", out var displayNameElement))
                return null;

            var displayName = displayNameElement.GetString();
            if (string.IsNullOrWhiteSpace(displayName))
                return null;

            var result = new GeocodingResult(
                displayName,
                BuildPlaceName(document.RootElement),
                latitude,
                longitude,
                _settings.Attribution);

            _cache.Set(cacheKey, result, TimeSpan.FromMinutes(10));
            return result;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Reverse geocoding failed.");
            throw new HttpRequestException("The geocoding service is currently unavailable.", ex);
        }
        finally
        {
            Throttle.Release();
        }
    }

    private static string? BuildPlaceName(JsonElement root)
    {
        if (!root.TryGetProperty("address", out var address) ||
            address.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        string? Get(string name)
        {
            return address.TryGetProperty(name, out var value) &&
                   value.ValueKind == JsonValueKind.String
                ? value.GetString()
                : null;
        }

        var locality = Get("suburb")
            ?? Get("neighbourhood")
            ?? Get("city")
            ?? Get("town")
            ?? Get("village")
            ?? Get("county");

        var state = Get("state");
        var country = Get("country");

        var parts = new[] { locality, state, country }
            .Where(part => !string.IsNullOrWhiteSpace(part))
            .Distinct(StringComparer.OrdinalIgnoreCase);

        var combined = string.Join(", ", parts);
        return string.IsNullOrWhiteSpace(combined) ? null : combined;
    }
}
