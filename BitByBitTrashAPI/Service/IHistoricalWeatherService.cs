namespace BitByBitTrashAPI.Service;

public interface IHistoricalWeatherService
{
    Task<double?> GetHistoricalTemperatureAsync(double latitude, double longitude, DateTime date);
}
