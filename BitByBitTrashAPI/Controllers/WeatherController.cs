using BitByBitTrashAPI.Models;
using BitByBitTrashAPI.Models.BitByBitTrashAPI.Models;
using BitByBitTrashAPI.Service;
using Microsoft.AspNetCore.Mvc;

namespace BitByBitTrashAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class WeatherController : ControllerBase
    {
        private readonly IWeatherApiService _weatherApiService;

        public WeatherController(IWeatherApiService weatherApiService)
        {
            _weatherApiService = weatherApiService;
        }

        [HttpGet]
        public async Task<ActionResult<WeatherModel>> GetWeather()
        {
            var weather = await _weatherApiService.GetWeatherAsync(null);
            if (weather == null)
                return NotFound();

            return Ok(weather);
        }
    }
}
