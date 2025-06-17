using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using BitByBitTrashAPI.Models;
using BitByBitTrashAPI.Models.BitByBitTrashAPI.Models;

namespace BitByBitTrashAPI.Service
{
    public class WeatherApiService : IWeatherApiService
    {
        private readonly HttpClient _httpClient;

        public WeatherApiService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<WeatherModel> GetWeatherAsync()
        {
            return await _httpClient.GetFromJsonAsync<WeatherModel>("weather");
        }
    }
}
