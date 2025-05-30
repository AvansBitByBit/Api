using BitByBitTrashAPI.Models;
using Xunit;

namespace BitByBitTrashAPI.Tests;

public class LitterModelTests
{
    [Fact]
    public void LitterModel_CanBeCreated_WithRequiredProperties()
    {
        // Arrange & Act
        var litter = new LitterModel
        {
            Id = 1,
            Name = "Test Litter",
            Type = "Test Type"
        };

        // Assert
        Assert.Equal(1, litter.Id);
        Assert.Equal("Test Litter", litter.Name);
        Assert.Equal("Test Type", litter.Type);
    }

    [Theory]
    [InlineData("Plastic Bottle", "Plastic")]
    [InlineData("Cigarette Butt", "Tobacco")]
    [InlineData("Food Wrapper", "Paper")]
    public void LitterModel_AcceptsVariousTypes_OfLitter(string name, string type)
    {
        // Arrange & Act
        var litter = new LitterModel
        {
            Id = 1,
            Name = name,
            Type = type
        };

        // Assert
        Assert.Equal(name, litter.Name);
        Assert.Equal(type, litter.Type);
    }
}
