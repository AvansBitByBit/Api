using System.Net.Http;
using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;

namespace BitByBitTrashAPI.Service;

public class GeocodingService : IGeocodingService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<GeocodingService> _logger;
    private readonly IMemoryCache _cache;

    // Bekende straten → vaste coördinaten
    private static readonly List<(string straat, (double lat, double lon))> StraatCoordinaten = new()
    {
        ("Stationslaan", (51.5967095, 4.78359)),
        ("Grote Markt", (51.5877167, 4.7762418)),
        ("Mgr. Hopmansstraat", (51.5865, 4.7765)),
        ("Nieuwe Ginnekenstraat", (51.5823, 4.7773)),
        ("Hogeschoollaan", (51.5847430, 4.7976005))
    };

    public GeocodingService(IHttpClientFactory httpClientFactory, ILogger<GeocodingService> logger, IMemoryCache cache)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _cache = cache;
    }

    public async Task<(double lat, double lon)?> GetCoordinatesFromAddressAsync(string address)
    {
        // 1. Check of het een bekende straat bevat
        foreach (var (straat, coords) in StraatCoordinaten)
        {
            if (address.Contains(straat, StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogInformation("Adres '{Address}' matched vaste straat '{Straat}', gebruik cached coördinaten.", address, straat);
                return coords;
            }
        }

        // 2. Check cache
        if (_cache.TryGetValue(address, out (double lat, double lon) cached))
            return cached;

        // 3. Externe geocoding request via OpenStreetMap
        try
        {
            var client = _httpClientFactory.CreateClient();
            var url = $"https://nominatim.openstreetmap.org/search?format=json&q={Uri.EscapeDataString(address)}";
            client.DefaultRequestHeaders.UserAgent.ParseAdd("BitByBitTrashAPI/1.0");

            var response = await client.GetAsync(url);
            if (!response.IsSuccessStatusCode) return null;

            var json = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<JsonElement>(json);

            if (result.GetArrayLength() > 0)
            {
                var first = result[0];
                var lat = double.Parse(first.GetProperty("lat").GetString() ?? "0", System.Globalization.CultureInfo.InvariantCulture);
                var lon = double.Parse(first.GetProperty("lon").GetString() ?? "0", System.Globalization.CultureInfo.InvariantCulture);

                _cache.Set(address, (lat, lon), TimeSpan.FromHours(12));
                return (lat, lon);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Geocoding failed for '{Address}': {Error}", address, ex.Message);
        }

        return null;
    }
}
