using Microsoft.AspNetCore.Mvc;
using BitByBitTrashAPI.Models;
using Microsoft.AspNetCore.Authorization;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace BitByBitTrashAPI.Controllers
{
    [ApiController]
    [Route("Litter")]
    public class LitterController : ControllerBase
    {
        private readonly GeocodingService _geocodingService;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        public LitterController(IHttpClientFactory httpClientFactory, IConfiguration configuration, GeocodingService geocodingService)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            _geocodingService = geocodingService;
        }

        [Authorize(Roles = "Beheerder, IT, Gebruiker")]
        [HttpGet(Name = "GetLitter")]
        public async Task<IActionResult> Get()
        {
           

            // 1. Authenticate with sensor API
            var token = await GetSensorApiToken();
            if (string.IsNullOrEmpty(token))
                return StatusCode(502, "Failed to authenticate with sensor API");

            //// 2. Fetch litter data from sensor API
            //var litterData = await GetSensorLitterData(token);
            //if (litterData == null)
            //    return StatusCode(502, "Failed to fetch litter data");

            var litterRaw = await GetSensorLitterData(token);
            if (litterRaw == null)
                return StatusCode(502, "Failed to fetch litter data");

            using var doc = JsonDocument.Parse((string)litterRaw);
            var litterArray = doc.RootElement;

            if (litterArray.ValueKind != JsonValueKind.Array)
                return StatusCode(500, "Litter data is not a JSON array.");


            // 3. Fetch weather data (replace with your location and API key)
            var weatherData = await GetWeatherData();

            // Hier word de geocoding geimplementeerd zodat we de locatie in lat/lon krijgen.
            var enrichedLitter = new List<object>();

            foreach (var item in litterArray.EnumerateArray())
            {
                var location = item.GetProperty("location").GetString();
                var coords = await _geocodingService.GeocodeAsync(location);

                if (coords != null)
                {
                    enrichedLitter.Add(new
                    {
                        id = item.GetProperty("id").GetString(),
                        location,
                        latitude = coords.Value.lat,
                        longitude = coords.Value.lon,
                        trash_type = item.GetProperty("trash_type").GetString(),
                        time = item.GetProperty("time").GetString()
                    });
                }
            }


            return Ok(new { litter = enrichedLitter, weather = weatherData });

        }

        private async Task<string> GetSensorApiToken()
        {
             string SensoringToken = _configuration["SensoringToken"];
            if (string.IsNullOrEmpty(SensoringToken))
                return "SensoringToken is not configured";

            var client = _httpClientFactory.CreateClient();
            var url = "https://bugbusterscontainer.redbay-0ee43133.northeurope.azurecontainerapps.io/api/Auth/token";
            var body = new
            {
                clientId = "BitByBit",
                clientSecret = SensoringToken,
            };

            var content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");
            var response = await client.PostAsync(url, content);
            if (!response.IsSuccessStatusCode) return string.Empty;
            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.TryGetProperty("token", out var tokenProp))
                return tokenProp.GetString() ?? string.Empty;
            return string.Empty;
        }

        private async Task<object> GetSensorLitterData(string token)
        {
            var client = _httpClientFactory.CreateClient();
            var url = "https://bugbusterscontainer.redbay-0ee43133.northeurope.azurecontainerapps.io/api/Litter";
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            var response = await client.GetAsync(url);
            if (!response.IsSuccessStatusCode) return new { error = "Failed to fetch litter data" };
            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<object>(json) ?? new { error = "Empty litter data" };
        }

        private async Task<object> GetWeatherData()
        {
            var client = _httpClientFactory.CreateClient();
            // Free weather API: Open-Meteo (no API key required)
            var url = "https://api.open-meteo.com/v1/forecast?latitude=51.571915&longitude=4.768323&current=temperature_2m,precipitation,rain,showers,snowfall,cloudcover,windspeed_10m,winddirection_10m,weathercode&timezone=auto";
            var response = await client.GetAsync(url);
            if (!response.IsSuccessStatusCode) return new { error = "Failed to fetch weather data" };
            var json = await response.Content.ReadAsStringAsync();
            // If you want to use a strongly typed model, replace 'object' with WeatherModel and update the return type
            return JsonSerializer.Deserialize<object>(json) ?? new { error = "Empty weather data" };
        }
    }
}
