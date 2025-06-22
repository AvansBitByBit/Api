namespace BitByBitTrashAPI.Service;

public interface IGeocodingService
{
    Task<(double lat, double lon)?> GeocodeAsync(string location);
}
