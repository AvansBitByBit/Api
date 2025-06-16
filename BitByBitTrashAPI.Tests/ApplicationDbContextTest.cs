using Xunit;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using System.Linq;

namespace ApplicationDbContectTest;
public class ApplicationDbContextTest
{
    [Fact]
    public void OnModelCreating_SetsDefaultSchemaToAuth()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: "TestAuthSchemaDb")
            .Options;

        // Act
        using var context = new ApplicationDbContext(options);
        var model = context.Model;

        // Assert
        // Controleer of het default schema "auth" is voor een Identity-tabel, bijvoorbeeld AspNetUsers
        var userEntity = model.GetEntityTypes()
            .FirstOrDefault(e => e.ClrType == typeof(IdentityUser));
        Assert.NotNull(userEntity);
        Assert.Equal("auth", userEntity.GetSchema());
    }
}
