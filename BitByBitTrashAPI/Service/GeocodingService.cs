using System.Globalization;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

public class GeocodingService
{
    private readonly IHttpClientFactory _httpClientFactory;

    public GeocodingService(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public async Task<(double lat, double lon)?> GeocodeAsync(string address)
    {
        var client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.UserAgent.ParseAdd("BitByBitTrashApp/1.0");

        var url = $"https://nominatim.openstreetmap.org/search?q={Uri.EscapeDataString(address)}&format=json";
        var response = await client.GetAsync(url);
        if (!response.IsSuccessStatusCode) return null;

        var json = await response.Content.ReadAsStringAsync();
        var results = JsonSerializer.Deserialize<List<NominatimResult>>(json);

        return results?.FirstOrDefault() is { } result
            ? (
                double.Parse(result.lat, CultureInfo.InvariantCulture),
                double.Parse(result.lon, CultureInfo.InvariantCulture)
              )
            : null;
    }

    private class NominatimResult
    {
        public string lat { get; set; }
        public string lon { get; set; }
    }
}
