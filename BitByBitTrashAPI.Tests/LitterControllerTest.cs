using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using BitByBitTrashAPI.Controllers;
using BitByBitTrashAPI.Models;
using BitByBitTrashAPI.Service;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using Xunit;

namespace BitByBitTrashAPI.Tests.Controllers
{
    public class LitterControllerTests
    {
        private readonly Mock<IHttpClientFactory> _httpClientFactory = new();
        private readonly Mock<IConfiguration> _configuration = new();
        private readonly Mock<IHistoricalWeatherService> _weatherService = new();
        private readonly Mock<ILogger<LitterController>> _logger = new();
        private readonly Mock<IGeocodingService> _geocodingService = new();
        private readonly LitterDbContext _dbContext;

        public LitterControllerTests()
        {
            var options = new DbContextOptionsBuilder<LitterDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _dbContext = new LitterDbContext(options);
        }

        [Fact]
public async Task Get_ReturnsOkWithLocalData()
{
    // Arrange
    _dbContext.LitterModels.Add(new TrashPickup
    {
        Id = Guid.NewGuid(),
        TrashType = "plastic",
        Location = "Teststraat 1",
        Time = DateTime.UtcNow,
        Latitude = 51.5,
        Longitude = 4.7,
        Confidence = 99,
        Temperature = 22.2
    });
    await _dbContext.SaveChangesAsync();

    var mockMessageHandler = new Mock<HttpMessageHandler>();
    mockMessageHandler
        .Protected()
        .Setup<Task<HttpResponseMessage>>(
            "SendAsync",
            ItExpr.IsAny<HttpRequestMessage>(),
            ItExpr.IsAny<CancellationToken>())
        .ReturnsAsync(new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.OK,
            Content = new StringContent("{\"access_token\": \"dummy-token\"}", Encoding.UTF8, "application/json")
        });

    var httpClient = new HttpClient(mockMessageHandler.Object);
    _httpClientFactory.Setup(_ => _.CreateClient(It.IsAny<string>())).Returns(httpClient);
    _configuration.Setup(c => c["SensoringToken"]).Returns("dummy-secret");

    var controller = new LitterController(
        _httpClientFactory.Object,
        _geocodingService.Object,
        _configuration.Object,
        _dbContext,
        _weatherService.Object,
        _logger.Object
    );

    // Act
    var result = await controller.Get();

    // Assert
    var okResult = Assert.IsType<OkObjectResult>(result);
    dynamic response = okResult.Value!;

    var litter = response.GetType().GetProperty("litter")?.GetValue(response) as IEnumerable<object>;
    var weather = response.GetType().GetProperty("weather")?.GetValue(response);

    Assert.NotNull(litter);
    Assert.Single(litter);
    Assert.NotNull(weather);
}


        [Fact]
        public async Task CreatePickup_AddsPickupToDatabase()
        {
            // Arrange
            var controller = new LitterController(
                _httpClientFactory.Object,
                _geocodingService.Object,
                _configuration.Object,
                _dbContext,
                _weatherService.Object,
                _logger.Object
            );

            var pickup = new TrashPickup
            {
                TrashType = "can",
                Location = "Teststraat 2",
                Time = DateTime.UtcNow,
                Latitude = 51.6,
                Longitude = 4.8,
                Confidence = 88
            };

            // Act
            var result = await controller.CreatePickup(pickup);

            // Assert
            var created = Assert.IsType<CreatedAtActionResult>(result);
            var stored = await _dbContext.LitterModels.FirstOrDefaultAsync(x => x.Location == "Teststraat 2");
            Assert.NotNull(stored);
            Assert.Equal("can", stored.TrashType);
        }
    }
}
