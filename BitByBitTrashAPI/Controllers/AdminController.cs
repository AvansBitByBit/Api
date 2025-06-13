using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using BitByBitTrashAPI.Models;

[ApiController]
[Route("api/[controller]")]
public class AdminController : ControllerBase
{
    private readonly UserManager<IdentityUser> _userManager;

    public AdminController(UserManager<IdentityUser> userManager)
    {
        _userManager = userManager;
    }

    [HttpPost("addrole")]
    public async Task<IActionResult> AddRoleToUser([FromBody] AddRoleRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Role))
            return BadRequest("Email and role are required.");

        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user == null)
            return NotFound($"User with email '{request.Email}' not found.");

        var roleResult = await _userManager.AddToRoleAsync(user, request.Role);
        if (!roleResult.Succeeded)
            return BadRequest(roleResult.Errors);

        return Ok($"Role '{request.Role}' added to user '{request.Email}'.");
    }
}