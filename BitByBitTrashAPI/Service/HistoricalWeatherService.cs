using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System.Globalization;


namespace BitByBitTrashAPI.Service;

public class HistoricalWeatherService : IHistoricalWeatherService
{
    private readonly HttpClient _httpClient;
    private readonly IMemoryCache _cache;
    private readonly ILogger<HistoricalWeatherService> _logger;
    
    // Rate limiting
    private static readonly SemaphoreSlim _throttle = new SemaphoreSlim(3, 3); // Allow 3 concurrent requests
    // Throttle delay to avoid hitting API limits
    private static readonly TimeSpan _throttleDelay = TimeSpan.FromMilliseconds(400);

    public HistoricalWeatherService(HttpClient httpClient, IMemoryCache cache, ILogger<HistoricalWeatherService> logger)
    {
        _httpClient = httpClient;
        _cache = cache;
        _logger = logger;
    }

    public async Task<double?> GetHistoricalTemperatureAsync(double latitude, double longitude, DateTime date)
    {
        try
        {
            // Create cache key
            var dateString = date.ToString("yyyy-MM-dd");
            var cacheKey = $"weather:{latitude:F6}:{longitude:F6}:{dateString}";

            // Check cache first
            if (_cache.TryGetValue(cacheKey, out double cachedTemperature))
            {
                _logger.LogInformation($"Cache hit for weather data: {cacheKey}");
                return cachedTemperature;
            }

            await _throttle.WaitAsync();
            try
            {
                _logger.LogInformation($"Fetching historical weather for lat:{latitude}, lon:{longitude}, date:{dateString}");

                // Use Open-Meteo historical weather API (free, no API key required)
                // Format: https://archive-api.open-meteo.com/v1/era5?latitude=52.52&longitude=13.41&start_date=2022-01-01&end_date=2022-01-01&daily=temperature_2m_mean&timezone=Europe/Berlin
                var startDate = date.ToString("yyyy-MM-dd");
                var endDate = date.ToString("yyyy-MM-dd");
                var url = $"https://archive-api.open-meteo.com/v1/era5?latitude={latitude.ToString("F6", CultureInfo.InvariantCulture)}&longitude={longitude.ToString("F6", CultureInfo.InvariantCulture)}&start_date={startDate}&end_date={endDate}&daily=temperature_2m_mean&timezone=auto";

                var response = await _httpClient.GetAsync(url);
                
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning($"Historical weather API returned status code: {response.StatusCode}");
                    return null;
                }

                var json = await response.Content.ReadAsStringAsync();
                
                if (string.IsNullOrEmpty(json))
                {
                    _logger.LogWarning("Empty response from historical weather API");
                    return null;
                }

                // Parse the JSON response
                using var document = JsonDocument.Parse(json);
                var root = document.RootElement;

                if (root.TryGetProperty("daily", out var daily) &&
                    daily.TryGetProperty("temperature_2m_mean", out var temperatures) &&
                    temperatures.ValueKind == JsonValueKind.Array &&
                    temperatures.GetArrayLength() > 0)
                {
                    var temperature = temperatures[0].GetDouble();
                    
                    // Cache the result for 7 days (historical data doesn't change)
                    _cache.Set(cacheKey, temperature, TimeSpan.FromDays(7));
                    
                    _logger.LogInformation($"Retrieved historical temperature: {temperature}°C for {dateString}");
                    return temperature;
                }
                else
                {
                    _logger.LogWarning($"No temperature data found in response: {json}");
                    return null;
                }
            }
            finally
            {
                await Task.Delay(_throttleDelay);
                _throttle.Release();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error fetching historical weather data for lat:{latitude}, lon:{longitude}, date:{date:yyyy-MM-dd}");
            return null;
        }
    }
}
