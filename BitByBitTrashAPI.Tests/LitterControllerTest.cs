using Xunit;
using Moq;
using Microsoft.AspNetCore.Mvc;
using BitByBitTrashAPI.Controllers;
using Microsoft.Extensions.Configuration;
using System.Threading.Tasks;
using System.Net.Http;
using System.Net;
using System.Threading;
using System;
using Moq.Protected;
using BitByBitTrashAPI.Models;
using BitByBitTrashAPI.Service;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;


namespace LitterControlTest;

public class LitterControllerTest
{
    // Fake IGeocodingService for testing
    public class FakeGeocodingService : IGeocodingService
    {
        public Task<(double lat, double lon)?> GeocodeAsync(string location)
        {
            return Task.FromResult<(double lat, double lon)?>( (51.5719, 4.7683) );
        }
    }

    private LitterController CreateController(HttpResponseMessage tokenResponse, HttpResponseMessage litterResponse, HttpResponseMessage weatherResponse, LitterDbContext dbContext)
    {
        var mockFactory = new Mock<IHttpClientFactory>();
        var callCount = 0;
        mockFactory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(() =>
        {
            callCount++;
            if (callCount == 1) return new HttpClient(new MockHttpMessageHandler(tokenResponse));
            if (callCount == 2) return new HttpClient(new MockHttpMessageHandler(litterResponse));
            return new HttpClient(new MockHttpMessageHandler(weatherResponse));
        });
        var mockConfig = new Mock<IConfiguration>();
        mockConfig.Setup(c => c["SensoringToken"]).Returns("dummy");
        // Use the fake IGeocodingService
        var fakeGeocodingService = new FakeGeocodingService();
        return new LitterController(mockFactory.Object, mockConfig.Object, fakeGeocodingService, dbContext);
    }    public class MockHttpMessageHandler : HttpMessageHandler
    {
        private readonly HttpResponseMessage _response;
        public MockHttpMessageHandler(HttpResponseMessage response) => _response = response;
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) => Task.FromResult(_response);
    }

    [Fact]
    public async Task Get_ReturnsCombinedLitterAndWeather()
    {
        // Arrange
        var tokenResponse = new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("{\"access_token\":\"test-token\"}") };
        var litterJson = "[{\"trashType\":\"plastic\",\"location\":\"loc1\",\"confidence\":0.9}]";
        var weatherJson = "{ \"hourly\": { \"time\": [\"2025-06-06T22:00\"], \"temperature_2m\": [21.5], \"precipitation\": [0.0] } }";
        var litterResponse = new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(litterJson) };
        var weatherResponse = new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(weatherJson) };
        var options = new DbContextOptionsBuilder<LitterDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        using var dbContext = new LitterDbContext(options);
        var controller = CreateController(tokenResponse, litterResponse, weatherResponse, dbContext);

        // Act
        var result = await controller.Get();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var json = System.Text.Json.JsonSerializer.Serialize(okResult.Value);        using var doc = System.Text.Json.JsonDocument.Parse(json);
        var root = doc.RootElement;
        Assert.True(root.TryGetProperty("litter", out _));
        Assert.True(root.TryGetProperty("chartData", out _));
    }

    [Fact]
    public async Task Get_Returns502IfTokenFails()
    {
        // Arrange
        var tokenResponse = new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("{\"access_token\":\"\"}") };
        var litterResponse = new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("{}") };
        var weatherResponse = new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("{}") };
        var options = new DbContextOptionsBuilder<LitterDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        using var dbContext = new LitterDbContext(options);
        var controller = CreateController(tokenResponse, litterResponse, weatherResponse, dbContext);        // Act
        var result = await controller.Get();

        // Assert
        var status = Assert.IsType<ObjectResult>(result);
        Assert.Equal(502, status.StatusCode);
    }

    [Fact]
    public async Task Get_SavesLitterWithTemperatureToDatabase()
    {
        // Arrange
        var tokenResponse = new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("{\"access_token\":\"test-token\"}") };
        var litterJson = "[{\"trashType\":\"plastic\",\"location\":\"loc1\",\"confidence\":0.9},{\"trashType\":\"glass\",\"location\":\"loc2\",\"confidence\":0.8}]";
        var weatherJson = "{ \"hourly\": { \"time\": [\"2025-06-06T22:00\"], \"temperature_2m\": [21.5], \"precipitation\": [0.0] } }";
        var litterResponse = new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(litterJson) };
        var weatherResponse = new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(weatherJson) };
        var options = new DbContextOptionsBuilder<LitterDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        using var dbContext = new LitterDbContext(options);
        var controller = CreateController(tokenResponse, litterResponse, weatherResponse, dbContext);

        // Act
        var result = await controller.Get();

        // Assert
        var addedPickups = dbContext.LitterModels.ToList();
        Assert.Equal(2, addedPickups.Count);
        Assert.All(addedPickups, p => Assert.Equal(21.5, p.Temperature));
        Assert.Contains(addedPickups, p => p.TrashType == "plastic" && p.Location == "loc1");
        Assert.Contains(addedPickups, p => p.TrashType == "glass" && p.Location == "loc2");
        var okResult = Assert.IsType<OkObjectResult>(result);
        var json = System.Text.Json.JsonSerializer.Serialize(okResult.Value);
        using var doc = System.Text.Json.JsonDocument.Parse(json);
        var root = doc.RootElement;
        Assert.True(root.TryGetProperty("litter", out _));
        Assert.True(root.TryGetProperty("chartData", out _));
    }
}
