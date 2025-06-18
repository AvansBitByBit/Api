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


namespace LitterControlTest;

public class LitterControllerTest
{
    private LitterController CreateController(HttpResponseMessage litterResponse, HttpResponseMessage weatherResponse, string token = "test-token")
    {
        // Mock IHttpClientFactory
        var mockFactory = new Mock<IHttpClientFactory>();
        var mockClient = new Mock<HttpMessageHandler>();
        var sequence = new MockSequence();

        // Setup token request
        var tokenClient = new Mock<HttpMessageHandler>();
        tokenClient.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent($"{{\"token\":\"{token}\"}}")
            });
        var litterClient = new Mock<HttpMessageHandler>();
        litterClient.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.Is<HttpRequestMessage>(r => r.RequestUri.ToString().Contains("/api/Litter")), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(litterResponse);
        var weatherClient = new Mock<HttpMessageHandler>();
        weatherClient.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.Is<HttpRequestMessage>(r => r.RequestUri.ToString().Contains("open-meteo.com")), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(weatherResponse);

        // Setup factory to return correct client for each call
        var callCount = 0;
        mockFactory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(() =>
        {
            callCount++;
            if (callCount == 1) return new HttpClient(tokenClient.Object);
            if (callCount == 2) return new HttpClient(litterClient.Object);
            return new HttpClient(weatherClient.Object);
        });

        // Mock IConfiguration
        var mockConfig = new Mock<IConfiguration>();
        mockConfig.Setup(c => c["SensoringToken"]).Returns("dummy");

        return new LitterController(mockFactory.Object, mockConfig.Object);
    }

    [Fact]
    public async Task Get_ReturnsCombinedLitterAndWeather()
    {
        // Arrange
        var litterJson = "{\"testLitter\":123}";
        var weatherJson = "{\"testWeather\":456}";
        var litterResponse = new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(litterJson) };
        var weatherResponse = new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(weatherJson) };
        var controller = CreateController(litterResponse, weatherResponse);

        // Act
        var result = await controller.Get();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var json = System.Text.Json.JsonSerializer.Serialize(okResult.Value);
        using var doc = System.Text.Json.JsonDocument.Parse(json);
        var root = doc.RootElement;
        Assert.True(root.TryGetProperty("litter", out _));
        Assert.True(root.TryGetProperty("weather", out _));
    }

    [Fact]
    public async Task Get_Returns502IfTokenFails()
    {
        // Arrange
        var litterResponse = new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("{}") };
        var weatherResponse = new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("{}") };
        var controller = CreateController(litterResponse, weatherResponse, token: "");

        // Act
        var result = await controller.Get();

        // Assert
        var status = Assert.IsType<ObjectResult>(result);
        Assert.Equal(502, status.StatusCode);
    }
}
