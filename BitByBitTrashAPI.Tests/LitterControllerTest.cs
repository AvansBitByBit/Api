using Xunit;
using Moq;
using Microsoft.AspNetCore.Mvc;
using BitByBitTrashAPI.Controllers;
using BitByBitTrashAPI.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using BitByBitTrashAPI.Service;

namespace LitterControlTest;
public class LitterControllerTest
{
    private LitterController CreateController()
    {
        // DbContext is niet direct nodig voor huidige implementatie, maar mock voor constructor
        var options = new DbContextOptionsBuilder<LitterDbContext>()
            .UseInMemoryDatabase(databaseName: "TestDb")
            .Options;
        var context = new LitterDbContext(options);
        return new LitterController(context);
    }

    [Fact]
    public void Get_ReturnsFiveTrashPickups()
    {
        // Arrange
        var controller = CreateController();

        // Act
        var result = controller.Get();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(5, result.Count());
        Assert.All(result, item =>
        {
            Assert.NotNull(item.Id);
            Assert.False(string.IsNullOrEmpty(item.TrashType));
            Assert.False(string.IsNullOrEmpty(item.Location));
            Assert.InRange(item.Confidence, 0.0, 1.0);
        });
    }

    [Fact]
    public void Post_NullLitter_ReturnsBadRequest()
    {
        // Arrange
        var controller = CreateController();

        // Act
        var result = controller.Post(null);

        // Assert
        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Litter cannot be null", badRequest.Value);
    }

    [Fact]
    public void Post_ValidLitter_ReturnsOk()
    {
        // Arrange
        var controller = CreateController();
        var litter = new TrashPickup
        {
            Id = System.Guid.NewGuid(),
            TrashType = "plastic",
            Location = "Breda",
            Confidence = 0.8,
            Time = System.DateTime.Now
        };

        // Act
        var result = controller.Post(litter);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal("Het is gelukt", okResult.Value);
    }
}
