using BitByBitTrashAPI.Models;
using Xunit;

namespace BitByBitTrashAPI.Tests;

public class TrashPickupTests
{
    [Fact]
    public void LitterModel_CanBeCreated_WithRequiredProperties()
    {
        // Arrange & Act
        var litter = new TrashPickup
        {
            Id = Guid.NewGuid(),
            Time = DateTime.Now,
            TrashType = new[] { "cola", "blikje", "fles", "plastic", "organisch" }[new Random().Next(0, 5)],
            Location = new[] { "Breda", "Avans", "Lovensdijkstraat", "Hogeschoollaan", "naast de buurvrouw" }[new Random().Next(0, 5)]
        };

        // Assert
        Assert.Equal(Guid.Empty, litter.Id);
        Assert.Equal(DateTime.Now, litter.Time);
        Assert.Equal("Test Type", litter.TrashType);
        Assert.Equal("Test Location", litter.Location);
    }

    [Theory]
    [InlineData("Plastic Bottle", "Plastic")]
    [InlineData("Cigarette Butt", "Tobacco")]
    [InlineData("Food Wrapper", "Paper")]
    public void LitterModel_AcceptsVariousTypes_OfLitter(string name, string type)
    {
        // Arrange & Act
        var litter = new TrashPickup
        {
            Id = Guid.NewGuid(),
            Time = DateTime.Now,
            TrashType = new[] { "cola", "blikje", "fles", "plastic", "organisch" }[new Random().Next(0, 5)],
            Location = new[] { "Breda", "Avans", "Lovensdijkstraat", "Hogeschoollaan", "naast de buurvrouw" }[new Random().Next(0, 5)]
        };

        // Assert
        Assert.Equal(Guid.Empty, litter.Id);
        Assert.Equal(DateTime.Now, litter.Time);
        Assert.Equal("Test Type", litter.TrashType);
        Assert.Equal("Test Location", litter.Location);
    }
}
