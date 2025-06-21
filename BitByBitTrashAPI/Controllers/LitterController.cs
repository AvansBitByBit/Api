using Microsoft.AspNetCore.Mvc;
using BitByBitTrashAPI.Models;
using Microsoft.AspNetCore.Authorization;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using BitByBitTrashAPI.Service;
using System.Collections.Concurrent;

namespace BitByBitTrashAPI.Controllers
{
    [ApiController]
    [Route("Litter")]
    public class LitterController : ControllerBase
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly GeocodingService _geocodingService;
        private readonly LitterDbContext _dbContext;

        public LitterController(IHttpClientFactory httpClientFactory, IConfiguration configuration, GeocodingService geocodingService, LitterDbContext dbContext)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            _geocodingService = geocodingService;
            _dbContext = dbContext;
        }

        [HttpGet(Name = "GetLitter")]
        public async Task<IActionResult> Get()
        {
            var token = await GetSensorApiToken();
            if (string.IsNullOrEmpty(token) || token.StartsWith("Auth failed") || token.StartsWith("SensoringToken"))
                return StatusCode(502, $"Failed to authenticate with sensor API: {token}");

            var litterRaw = await GetSensorLitterData(token);
            if (litterRaw == null)
                return StatusCode(502, "Failed to fetch litter data");

            using var doc = JsonDocument.Parse((string)litterRaw);
            var litterArray = doc.RootElement;
            if (litterArray.ValueKind != JsonValueKind.Array)
                return StatusCode(500, "Litter data is not a JSON array.");

            var weatherData = await GetWeatherData();
            double? temperature = null;
            try
            {
                var root = weatherData as JsonElement?;
                if (root.HasValue && root.Value.TryGetProperty("current", out var current) && current.TryGetProperty("temperature_2m", out var tempProp))
                    temperature = tempProp.GetDouble();
            }
            catch { }

            var enrichedLitter = new ConcurrentBag<object>();
            var trashEntities = new ConcurrentBag<TrashPickup>();

            var tasks = litterArray.EnumerateArray().Select(async item =>
            {
                try
                {
                    var location = item.GetProperty("location").GetString();
                    var coords = await _geocodingService.GeocodeAsync(location);

                    var trashType = item.GetProperty("trashType").GetString();
                    var time = DateTime.Now;
                    var confidence = item.TryGetProperty("confidence", out var conf) ? conf.GetDouble() : 1.0;

                    trashEntities.Add(new TrashPickup
                    {
                        TrashType = trashType,
                        Location = location,
                        Confidence = confidence,
                        Time = time,
                        Temperature = temperature
                    });

                    if (coords != null)
                    {
                        enrichedLitter.Add(new
                        {
                            location,
                            latitude = coords.Value.lat,
                            longitude = coords.Value.lon,
                            trashType,
                            confidence,
                            time
                        });
                    }
                }
                catch (Exception ex)
                {
                    // Log or handle bad individual items if needed
                    Console.WriteLine($"Failed to process item: {ex.Message}");
                }
            });

            await Task.WhenAll(tasks);

            //_dbContext.LitterModels.AddRange(trashEntities);
            //await _dbContext.SaveChangesAsync();

            var groupedTrash = enrichedLitter
                .GroupBy(x => ((dynamic)x).trashType.ToLower())
                .Select(g => new
                {
                    trashType = g.Key,
                    count = g.Count()
                })
                .ToList();

            return Ok(new { litter = enrichedLitter, weather = weatherData, chartData = groupedTrash });
        }


        private async Task<string> GetSensorApiToken()
        {
            string sensorToken = _configuration["SensoringToken"];
            if (string.IsNullOrEmpty(sensorToken))
                return "SensoringToken is not configured";

            var client = _httpClientFactory.CreateClient();
            var url = "https://bugbusterscontainer.redbay-0ee43133.northeurope.azurecontainerapps.io/api/Auth/token";
            var body = new { clientId = "BitByBit", clientSecret = sensorToken };
            var content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");

            var response = await client.PostAsync(url, content);
            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                return $"Auth failed: Status {response.StatusCode}, Body: {errorBody}";
            }

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            return doc.RootElement.TryGetProperty("access_token", out var tokenProp)
                ? tokenProp.GetString() ?? string.Empty
                : $"Auth succeeded but no access_token in response: {json}";
        }

        private async Task<string?> GetSensorLitterData(string token)
        {
            var client = _httpClientFactory.CreateClient();
            var url = "https://bugbusterscontainer.redbay-0ee43133.northeurope.azurecontainerapps.io/api/Garbage/all";
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            var response = await client.GetAsync(url);
            if (!response.IsSuccessStatusCode) return null;
            return await response.Content.ReadAsStringAsync();
        }

        private async Task<object> GetWeatherData()
        {
            var client = _httpClientFactory.CreateClient();
            var url = "https://api.open-meteo.com/v1/forecast?latitude=51.571915&longitude=4.768323&current=temperature_2m,precipitation,rain,showers,snowfall,cloudcover,windspeed_10m,winddirection_10m,weathercode&timezone=auto";
            var response = await client.GetAsync(url);
            if (!response.IsSuccessStatusCode) return new { error = "Failed to fetch weather data" };
            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<object>(json) ?? new { error = "Empty weather data" };
        }
    }
}
