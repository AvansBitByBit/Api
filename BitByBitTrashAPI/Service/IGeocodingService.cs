namespace BitByBitTrashAPI.Service;

public interface IGeocodingService
{
 public Task<(double lat, double lon)?> GetCoordinatesFromAddressAsync(string address);
}
