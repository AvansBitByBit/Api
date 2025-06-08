using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using System.Net.Http.Json;
using BitByBitTrashAPI.Service;
using BitByBitTrashAPI.Models;
using Xunit;

namespace BitByBitTrashAPI.Tests;

public class LitterControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public LitterControllerTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // Remove the existing LitterDbContext
                var descriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(DbContextOptions<LitterDbContext>));
                if (descriptor != null)
                {
                    services.Remove(descriptor);
                }

                // Add in-memory database for testing
                services.AddDbContext<LitterDbContext>(options =>
                {
                    options.UseInMemoryDatabase("TestDatabase");
                });
            });
        });
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task Get_Litter_ReturnsSuccessStatusCode()
    {
        // Act
        var response = await _client.GetAsync("/Litter");

        // Assert
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        Assert.NotEmpty(content);
    }

    [Fact]
    public async Task Post_Litter_WithValidModel_ReturnsOk()
    {
        // Arrange
        var TrashPickup = new TrashPickup
        {
            Id = Guid.NewGuid(),
            Time = DateTime.Now,
            TrashType = new[] { "cola", "blikje", "fles", "plastic", "organisch" }[new Random().Next(0, 5)],
            Location = new[] { "Breda", "Avans", "Lovensdijkstraat", "Hogeschoollaan", "naast de buurvrouw" }[new Random().Next(0, 5)]
        };

        // Act
        var response = await _client.PostAsJsonAsync("/Litter", TrashPickup);

        // Assert
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("Het is gelukt", content);
    }

    [Fact]
    public async Task Post_Litter_WithNullModel_ReturnsBadRequest()
    {
        // Act
        var response = await _client.PostAsJsonAsync("/Litter", (TrashPickup?)null);

        // Assert
        Assert.Equal(System.Net.HttpStatusCode.BadRequest, response.StatusCode);
    }
}
