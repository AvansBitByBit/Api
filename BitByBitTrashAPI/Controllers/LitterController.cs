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
    {        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly IGeocodingService _geocodingService;
        private readonly LitterDbContext _dbContext;

        private static readonly ConcurrentDictionary<string, (double? temperature, double? precipitation)> WeatherCache = new();

        public LitterController(IHttpClientFactory httpClientFactory, IConfiguration configuration, IGeocodingService geocodingService, LitterDbContext dbContext)
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

            var enrichedLitter = new ConcurrentBag<object>();
            var trashEntities = new ConcurrentBag<TrashPickup>();

            var tasks = litterArray.EnumerateArray().Select(async item =>
            {
                try
                {
                    var location = item.GetProperty("location").GetString();
                    var coords = await _geocodingService.GeocodeAsync(location);

                    var trashType = item.GetProperty("trashType").GetString();
                    var time = new DateTime(2025, 6, 6, 22, 0, 0); //alleen om de tijd te testen, en of de openmeteo goeie data teruggeeft gebasseerd op tijd.

                    //var time = item.TryGetProperty("time", out var timeProp) //uigecomment omdat de time die we van de sensoring api krijgen verkeerd is.
                    //    ? DateTime.Parse(timeProp.GetString() ?? "")
                    //    : DateTime.Now;

                    var confidence = item.TryGetProperty("confidence", out var conf) ? conf.GetDouble() : 1.0;

                    if (coords != null)
                    {
                        double latitude = coords.Value.lat / 100000.0;
                        double longitude = coords.Value.lon / 100000.0;

                        var (temperature, precipitation) = await GetWeatherForBredaAtTime(time);

                        trashEntities.Add(new TrashPickup
                        {
                            TrashType = trashType,
                            Location = location,
                            Confidence = confidence,
                            Time = time,
                            Temperature = temperature
                        });

                        enrichedLitter.Add(new
                        {
                            location,
                            latitude,
                            longitude,
                            trashType,
                            confidence,
                            time,
                            temperature,
                            precipitation
                        });
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to process item: {ex.Message}");
                }
            });            await Task.WhenAll(tasks);

            // Save trash pickups to database
            _dbContext.LitterModels.AddRange(trashEntities);
            await _dbContext.SaveChangesAsync();

            var groupedTrash = enrichedLitter
                .GroupBy(x => ((dynamic)x).trashType.ToLower())
                .Select(g => new
                {
                    trashType = g.Key,
                    count = g.Count()
                })
                .ToList();

            return Ok(new
            {
                litter = enrichedLitter,
                chartData = groupedTrash
            });
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

        private async Task<(double? temperature, double? precipitation)> GetWeatherForBredaAtTime(DateTime timestamp)
        {
            double latitude = 51.5719;
            double longitude = 4.7683;
            string date = timestamp.ToString("yyyy-MM-dd");
            string cacheKey = $"{latitude:F4},{longitude:F4}-{timestamp:yyyy-MM-dd-HH}";

            if (WeatherCache.TryGetValue(cacheKey, out var cached))
                return cached;

            var client = _httpClientFactory.CreateClient();
            var url = $"https://archive-api.open-meteo.com/v1/archive?" +
                      $"latitude={latitude}&longitude={longitude}" +
                      $"&start_date={date}&end_date={date}" +
                      $"&hourly=temperature_2m,precipitation" +
                      $"&timezone=Europe%2FAmsterdam";

            var response = await client.GetAsync(url);
            if (!response.IsSuccessStatusCode)
                return (null, null);

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            if (!root.TryGetProperty("hourly", out var hourly))
                return (null, null);

            var timestamps = hourly.GetProperty("time").EnumerateArray().ToList();
            var temperatures = hourly.GetProperty("temperature_2m").EnumerateArray().ToList();
            var precipitations = hourly.GetProperty("precipitation").EnumerateArray().ToList();

            int closestIndex = -1;
            TimeSpan minDiff = TimeSpan.MaxValue;

            for (int i = 0; i < timestamps.Count; i++)
            {
                if (DateTime.TryParse(timestamps[i].GetString(), out DateTime hourlyTime))
                {
                    var diff = (hourlyTime - timestamp).Duration();
                    if (diff < minDiff)
                    {
                        minDiff = diff;
                        closestIndex = i;
                    }
                }
            }

            if (closestIndex >= 0 && closestIndex < temperatures.Count && closestIndex < precipitations.Count)
            {
                var temp = temperatures[closestIndex].GetDouble();
                var precip = precipitations[closestIndex].GetDouble();
                WeatherCache[cacheKey] = (temp, precip);
                return (temp, precip);
            }

            return (null, null);
        }
    }
}
