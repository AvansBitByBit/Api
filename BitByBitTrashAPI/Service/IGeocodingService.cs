namespace BitByBitTrashAPI.Service;

public interface IGeocodingService
{
    Task<(double lat, double lon)?> GetCoordinatesFromAddressAsync(string address);
}
