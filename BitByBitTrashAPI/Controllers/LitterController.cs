using Microsoft.AspNetCore.Mvc;
using BitByBitTrashAPI.Models;
using Microsoft.AspNetCore.Authorization;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using BitByBitTrashAPI.Service;

namespace BitByBitTrashAPI.Controllers
{
    [ApiController]
    [Route("Litter")]
    public class LitterController : ControllerBase
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly LitterDbContext _dbContext;
        public LitterController(IHttpClientFactory httpClientFactory, IConfiguration configuration, LitterDbContext dbContext)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            _dbContext = dbContext;
        }

      //  [Authorize(Roles = "Beheerder, IT, Gebruiker")]
        [HttpGet(Name = "GetLitter")]
        public async Task<IActionResult> Get()
        {
            // 1. Authenticate with sensor API
            var token = await GetSensorApiToken();
            if (string.IsNullOrEmpty(token) || token.StartsWith("Auth failed") || token.StartsWith("SensoringToken"))
                return StatusCode(502, $"Failed to authenticate with sensor API: {token}");

            // 2. Fetch litter data from sensor API
            var litterData = await GetSensorLitterData(token);
            if (litterData == null)
                return StatusCode(502, "Failed to fetch litter data");

            // 3. Fetch weather data
            var weatherData = await GetWeatherData();
            double? temperature = null;
            try
            {
                var root = weatherData as JsonElement?;
                if (root.HasValue && root.Value.TryGetProperty("current", out var current))
                {
                    if (current.TryGetProperty("temperature_2m", out var tempProp))
                        temperature = tempProp.GetDouble();
                }
            }
            catch { /* ignore, temperature stays null */ }

            // 4. Save each litter item as TrashPickup with temperature
            if (litterData is JsonElement litterRoot && litterRoot.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in litterRoot.EnumerateArray())
                {
                    var pickup = new TrashPickup
                    {
                        TrashType = item.GetProperty("trashType").GetString(),
                        Location = item.GetProperty("location").GetString(),
                        Confidence = item.TryGetProperty("confidence", out var conf) ? conf.GetDouble() : 1.0,
                        Time = DateTime.Now,
                        Temperature = temperature
                    };
                    _dbContext.LitterModels.Add(pickup);
                }
                await _dbContext.SaveChangesAsync();
            }

            // 5. Combine and return
            return Ok(new { litter = litterData, weather = weatherData });
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

            if (!response.IsSuccessStatusCode) {
                var errorBody = await response.Content.ReadAsStringAsync();
                return $"Auth failed: Status {response.StatusCode}, Body: {errorBody}";
            }
            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.TryGetProperty("access_token", out var tokenProp))
                return tokenProp.GetString() ?? string.Empty;
            return $"Auth succeeded but no access_token in response: {json}";
        }

        private async Task<object> GetSensorLitterData(string token)
        {
            var client = _httpClientFactory.CreateClient();
            var url = "https://bugbusterscontainer.redbay-0ee43133.northeurope.azurecontainerapps.io/api/Garbage/all";
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

        [Authorize(Roles = "Beheerder, IT, Gebruiker")]
        [HttpPost]
        public async Task<IActionResult> CreatePickup([FromBody] TrashPickup pickup)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // Fetch weather data
            var weatherData = await GetWeatherData();
            double? temperature = null;
            try
            {
                // Try to extract temperature_2m from the weather API response
                var root = weatherData as JsonElement?;
                if (root.HasValue && root.Value.TryGetProperty("current", out var current))
                {
                    if (current.TryGetProperty("temperature_2m", out var tempProp))
                        temperature = tempProp.GetDouble();
                }
            }
            catch { /* ignore, temperature stays null */ }
            pickup.Temperature = temperature;

            _dbContext.LitterModels.Add(pickup);
            await _dbContext.SaveChangesAsync();
            return CreatedAtAction(nameof(Get), new { id = pickup.Id }, pickup);
        }

    }
}
