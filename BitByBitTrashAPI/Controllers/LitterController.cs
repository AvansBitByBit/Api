using Microsoft.AspNetCore.Mvc;
using BitByBitTrashAPI.Models;
using Microsoft.AspNetCore.Authorization;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using BitByBitTrashAPI.Service;
using Microsoft.EntityFrameworkCore;

namespace BitByBitTrashAPI.Controllers
{
    [ApiController]
    [Route("Litter")]
    public class LitterController : ControllerBase
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly LitterDbContext _dbContext;
        private readonly IHistoricalWeatherService _historicalWeatherService;
        private readonly ILogger<LitterController> _logger;
        private readonly IGeocodingService _geocodingService;

        public LitterController(IHttpClientFactory httpClientFactory, IGeocodingService geocodingService, IConfiguration configuration, LitterDbContext dbContext, IHistoricalWeatherService historicalWeatherService, ILogger<LitterController> logger)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            _dbContext = dbContext;
            _historicalWeatherService = historicalWeatherService;
            _geocodingService = geocodingService;
            _logger = logger;
        }


        [HttpGet]
public async Task<IActionResult> Get()
{
    // 1. Trigger verrijking (optioneel)
    await GetNew(); // of: await _enricherService.RunAsync();

    // 2. Geef eigen opgeslagen data terug
    var pickups = await _dbContext.LitterModels
        .OrderByDescending(x => x.Time)
        .Select(p => new
        {
            id = p.Id,
            time = p.Time,
            trashType = p.TrashType,
            location = p.Location,
            latitude = p.Latitude,
            longitude = p.Longitude,
            confidence = p.Confidence,
            temperature = p.Temperature
        })
        .ToListAsync();

    return Ok(pickups);
}

        [HttpGet("New")]
        public async Task<IActionResult> GetNew()
        {
            var token = await GetSensorApiToken();
            if (string.IsNullOrEmpty(token) || token.StartsWith("Auth failed"))
                return StatusCode(502, $"Failed to authenticate with sensor API: {token}");

            var litterData = await GetSensorLitterData(token);
            if (litterData == null)
                return StatusCode(502, "Failed to fetch litter data");

            var weatherData = await GetWeatherData();

            var enrichedLitterData = new List<object>();

            // Haal alle bestaande records op uit je eigen database
            var existingData = await _dbContext.LitterModels
                .Select(x => new { x.Location, Date = x.Time.Date })
                .ToListAsync();

            if (litterData is JsonElement litterRoot && litterRoot.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in litterRoot.EnumerateArray())
                {
                    var trashType = item.GetProperty("trashType").GetString() ?? "unknown";
                    var location = item.GetProperty("location").GetString() ?? "";
                    var confidence = item.TryGetProperty("confidence", out var conf) ? conf.GetDouble() : 1.0;

                    var itemTime = item.TryGetProperty("time", out var timeProperty) &&
                                   DateTime.TryParse(timeProperty.GetString(), out var parsedTime)
                                   ? parsedTime : DateTime.Now;

                    // ⛔ Als deze combinatie al in DB zit → skip
                    bool exists = existingData.Any(x =>
                        x.Location == location && x.Date == itemTime.Date);

                    if (exists)
                    {
                        _logger.LogInformation("Skipping existing item for {Location} @ {Date}", location, itemTime.Date);
                        continue;
                    }

                    // 🔄 Anders enrichen
                    var coordinates = await _geocodingService.GetCoordinatesFromAddressAsync(location);
                    double latitude = coordinates?.lat ?? 51.5719;
                    double longitude = coordinates?.lon ?? 4.7683;

                    double? historicalTemperature = await _historicalWeatherService.GetHistoricalTemperatureAsync(
                        latitude, longitude, itemTime);

                    var enrichedItem = new
                    {
                        id = item.TryGetProperty("id", out var idProp) ? idProp.GetString() : Guid.NewGuid().ToString(),
                        time = itemTime,
                        trashType = trashType,
                        location = location,
                        latitude = latitude,
                        longitude = longitude,
                        confidence = confidence,
                        temperature = historicalTemperature
                    };

                    enrichedLitterData.Add(enrichedItem);

                    var pickup = new TrashPickup
                    {
                        TrashType = trashType,
                        Location = location,
                        Latitude = latitude,
                        Longitude = longitude,
                        Confidence = confidence,
                        Time = itemTime,
                        Temperature = historicalTemperature
                    };

                    _dbContext.LitterModels.Add(pickup);
                }

                await _dbContext.SaveChangesAsync();
            }

       return Ok("Enrichment completed");
        }

        private async Task<string> GetSensorApiToken()
        {
            string? token = _configuration["SensoringToken"];
            if (string.IsNullOrEmpty(token))
                return "SensoringToken is not configured";

            var client = _httpClientFactory.CreateClient();
            var url = "https://bugbusterscontainer.redbay-0ee43133.northeurope.azurecontainerapps.io/api/Auth/token";
            var body = new { clientId = "BitByBit", clientSecret = token };
            var content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");

            var response = await client.PostAsync(url, content);
            if (!response.IsSuccessStatusCode)
                return $"Auth failed: Status {response.StatusCode}, Body: {await response.Content.ReadAsStringAsync()}";

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
            var url = "https://api.open-meteo.com/v1/forecast?latitude=51.571915&longitude=4.768323&current=temperature_2m&timezone=auto";
            var response = await client.GetAsync(url);
            if (!response.IsSuccessStatusCode) return new { error = "Failed to fetch weather data" };
            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<object>(json) ?? new { error = "Empty weather data" };
        }

        [Authorize(Roles = "Beheerder, IT, Gebruiker")]
        [HttpPost]
        public async Task<IActionResult> CreatePickup([FromBody] TrashPickup pickup)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (!pickup.Latitude.HasValue || !pickup.Longitude.HasValue)
            {
                pickup.Latitude = 51.5719;
                pickup.Longitude = 4.7683;
            }

            if (!pickup.Temperature.HasValue && pickup.Latitude.HasValue && pickup.Longitude.HasValue)
            {
                pickup.Temperature = await _historicalWeatherService.GetHistoricalTemperatureAsync(
                    pickup.Latitude.Value,
                    pickup.Longitude.Value,
                    pickup.Time);
            }

            _dbContext.LitterModels.Add(pickup);
            await _dbContext.SaveChangesAsync();

            return CreatedAtAction(nameof(Get), new { id = pickup.Id }, pickup);
        }
    }
}
