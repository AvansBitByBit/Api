using Xunit;
using BitByBitTrashAPI.Controllers;
using BitByBitTrashAPI.Models;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using System.Collections.Generic;

namespace DummyTrashControllerTest;
public class DummyTrashControllerTest
{
    [Fact]
    public void GetAll_ReturnsOkAndList()
    {
        // Arrange
        var controller = new DummyTrashController();

        // Act
        var result = controller.GetAll();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.IsAssignableFrom<IEnumerable<TrashPickup>>(okResult.Value);
    }

    [Fact]
    public void GetRandomTrash_InvalidCount_ReturnsBadRequest()
    {
        var controller = new DummyTrashController();

        var result = controller.GetRandomTrash(0);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Equal("Count must be between 1 and 100", badRequest.Value);
    }

    [Fact]
    public void PostTrash_NullTrash_ReturnsBadRequest()
    {
        var controller = new DummyTrashController();

        var result = controller.PostTrash(null);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Equal("Trash pickup data cannot be null", badRequest.Value);
    }

    [Fact]
    public void PostTrash_InvalidConfidence_ReturnsBadRequest()
    {
        var controller = new DummyTrashController();
        var trash = new TrashPickup
        {
            TrashType = "plastic",
            Location = "Breda",
            Confidence = 1.5, // Invalid
            Time = DateTime.Now
        };

        var result = controller.PostTrash(trash);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Equal("Confidence must be between 0.0 and 1.0", badRequest.Value);
    }

    [Fact]
    public void PostAndGetById_Works()
    {
        var controller = new DummyTrashController();
        var trash = new TrashPickup
        {
            TrashType = "plastic",
            Location = "Breda",
            Confidence = 0.8,
            Time = DateTime.Now
        };

        var postResult = controller.PostTrash(trash);
        var created = Assert.IsType<CreatedAtActionResult>(postResult.Result);
        var createdTrash = Assert.IsType<TrashPickup>(created.Value);

        var getResult = controller.GetById(createdTrash.Id.Value);
        var okResult = Assert.IsType<OkObjectResult>(getResult.Result);
        var fetchedTrash = Assert.IsType<TrashPickup>(okResult.Value);

        Assert.Equal(createdTrash.Id, fetchedTrash.Id);
    }

    [Fact]
    public void UpdateTrash_NonExisting_ReturnsNotFound()
    {
        var controller = new DummyTrashController();
        var result = controller.UpdateTrash(Guid.NewGuid(), new TrashPickup { TrashType = "plastic", Confidence = 0.5, Time = DateTime.Now });

        var notFound = Assert.IsType<NotFoundObjectResult>(result.Result);
        Assert.Contains("not found", notFound.Value.ToString());
    }

    [Fact]
    public void DeleteTrash_NonExisting_ReturnsNotFound()
    {
        var controller = new DummyTrashController();
        var result = controller.DeleteTrash(Guid.NewGuid());

        var notFound = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Contains("not found", notFound.Value.ToString());
    }

    [Fact]
    public void SeedData_And_ClearAll_Works()
    {
        var controller = new DummyTrashController();

        var seedResult = controller.SeedData(5);
        var okSeed = Assert.IsType<OkObjectResult>(seedResult);
        Assert.Contains("Seeded 5", okSeed.Value.ToString());

        var clearResult = controller.ClearAll();
        var okClear = Assert.IsType<OkObjectResult>(clearResult);
        Assert.Equal("All dummy data cleared", okClear.Value);
    }

    [Fact]
    public void GetStats_ReturnsOk()
    {
        var controller = new DummyTrashController();
        controller.SeedData(3);

        var result = controller.GetStats();
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }
}
