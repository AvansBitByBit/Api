using BitByBitTrashAPI.Models.BitByBitTrashAPI.Models;

namespace BitByBitTrashAPI.Service
{
    public interface IWeatherApiService
    {
        Task<WeatherModel> GetWeatherAsync();
    }

}
