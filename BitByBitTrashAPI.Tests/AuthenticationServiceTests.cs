using BitByBitTrashAPI.Service;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using Xunit;

namespace BitByBitTrashAPI.Tests;

public class AuthenticationServiceTests
{
    [Fact]
    public void GetCurrentAuthenticatedUserId_WithNoHttpContext_ReturnsNull()
    {
        // Arrange
        var httpContextAccessor = new HttpContextAccessor();
        var authService = new AspNetIdentityAuthenticationService(httpContextAccessor);

        // Act
        var result = authService.GetCurrentAuthenticatedUserId();

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void GetCurrentAuthenticatedUserId_WithAuthenticatedUser_ReturnsUserId()
    {
        // Arrange
        var httpContextAccessor = new HttpContextAccessor();
        var authService = new AspNetIdentityAuthenticationService(httpContextAccessor);
        
        var context = new DefaultHttpContext();
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, "test-user-id")
        };
        var identity = new ClaimsIdentity(claims, "test");
        context.User = new ClaimsPrincipal(identity);
        httpContextAccessor.HttpContext = context;

        // Act
        var result = authService.GetCurrentAuthenticatedUserId();

        // Assert
        Assert.Equal("test-user-id", result);
    }
}
