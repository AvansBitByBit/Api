using Xunit;
using Moq;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using BitByBitTrashAPI.Controllers;
using BitByBitTrashAPI.Models;

namespace AdminControllerTest;
public class AdminControllerTest
{
    private Mock<UserManager<IdentityUser>> GetMockUserManager()
    {
        var store = new Mock<IUserStore<IdentityUser>>();
        return new Mock<UserManager<IdentityUser>>(
            store.Object, null, null, null, null, null, null, null, null
        );
    }

    [Fact]
    public async Task AddRoleToUser_MissingFields_ReturnsBadRequest()
    {
        var mockUserManager = GetMockUserManager();
        var controller = new AdminController(mockUserManager.Object);

        var result = await controller.AddRoleToUser(new AddRoleRequest { Email = "", Role = "" });

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Email and role are required.", badRequest.Value);
    }

    [Fact]
    public async Task AddRoleToUser_UserNotFound_ReturnsNotFound()
    {
        var mockUserManager = GetMockUserManager();
        mockUserManager.Setup(m => m.FindByEmailAsync(It.IsAny<string>()))
            .ReturnsAsync((IdentityUser)null);

        var controller = new AdminController(mockUserManager.Object);

        var result = await controller.AddRoleToUser(new AddRoleRequest { Email = "test@example.com", Role = "Admin" });

        var notFound = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Contains("not found", notFound.Value.ToString());
    }

    [Fact]
    public async Task AddRoleToUser_AddRoleFails_ReturnsBadRequest()
    {
        var mockUserManager = GetMockUserManager();
        var user = new IdentityUser { Email = "test@example.com" };
        mockUserManager.Setup(m => m.FindByEmailAsync(It.IsAny<string>()))
            .ReturnsAsync(user);
        mockUserManager.Setup(m => m.AddToRoleAsync(user, "Admin"))
            .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "Role error" }));

        var controller = new AdminController(mockUserManager.Object);

        var result = await controller.AddRoleToUser(new AddRoleRequest { Email = "test@example.com", Role = "Admin" });

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.NotNull(badRequest.Value);
    }

    [Fact]
    public async Task AddRoleToUser_Success_ReturnsOk()
    {
        var mockUserManager = GetMockUserManager();
        var user = new IdentityUser { Email = "test@example.com" };
        mockUserManager.Setup(m => m.FindByEmailAsync(It.IsAny<string>()))
            .ReturnsAsync(user);
        mockUserManager.Setup(m => m.AddToRoleAsync(user, "Admin"))
            .ReturnsAsync(IdentityResult.Success);

        var controller = new AdminController(mockUserManager.Object);

        var result = await controller.AddRoleToUser(new AddRoleRequest { Email = "test@example.com", Role = "Admin" });

        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Contains("Role 'Admin' added to user 'test@example.com'.", okResult.Value.ToString());
    }
}
