using System.Net.Http;
using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;

public class GeocodingService
{
    private readonly HttpClient _httpClient;
    private readonly IMemoryCache _cache;
    private readonly ILogger<GeocodingService> _logger;

    public GeocodingService(HttpClient httpClient, IMemoryCache cache, ILogger<GeocodingService> logger)
    {
        _httpClient = httpClient;
        _cache = cache;
        _logger = logger;
    }

    private static readonly SemaphoreSlim _throttle = new SemaphoreSlim(1, 1);
    private static readonly TimeSpan _throttleDelay = TimeSpan.FromMilliseconds(503);

    public async Task<(double lat, double lon)?> GeocodeAsync(string location)
    {
        if (string.IsNullOrWhiteSpace(location))
            return null;

        string cacheKey = $"geocode:{location.Trim().ToLower()}";

        if (_cache.TryGetValue(cacheKey, out (double lat, double lon) cachedResult))
        {
            _logger.LogInformation($"Cache hit for: {location}");
            return cachedResult;
        }

        await _throttle.WaitAsync();
        try
        {
            _logger.LogInformation($"Sending geocode request for: {location}");

            //string url = $"https://nominatim.openstreetmap.org/search?format=json&q={Uri.EscapeDataString(location)}"; //nominatim uit
            string url = $"https://geocode.xyz/{Uri.EscapeDataString(location)}?json=1";


            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Add("User-Agent", "BitByBitTrashAPI/1.0");

            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            //var results = JsonSerializer.Deserialize<List<NominatimResponse>>(json);  //nominatim uit

            //if (results?.Count > 0 &&
            //    double.TryParse(results[0].Lat, out var lat) &&
            //    double.TryParse(results[0].Lon, out var lon))
            //{
            //    var result = (lat, lon);
            //    _cache.Set(cacheKey, result, TimeSpan.FromHours(12)); // tweak duration as needed
            //    return result;
            //}

            var resultObj = JsonSerializer.Deserialize<GeocodeXyzResponse>(json);

            if (resultObj != null &&
                double.TryParse(resultObj.latt, out var lat) &&
                double.TryParse(resultObj.longt, out var lon))
            {
                var result = (lat, lon);
                _cache.Set(cacheKey, result, TimeSpan.FromHours(168));
                return result;
            }

        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error while geocoding '{location}'");
        }
        finally
        {
            await Task.Delay(_throttleDelay); // Respect OpenStreetMap rate limits
            _throttle.Release();
        }

        return null;
    }


    //private class NominatimResponse
    //{
    //    public string Lat { get; set; }
    //    public string Lon { get; set; }
    //}

    private class GeocodeXyzResponse
    {
        public string latt { get; set; }
        public string longt { get; set; }
    }

}
