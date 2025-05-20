using CMAPI.Services;
using Microsoft.AspNetCore.Mvc;

namespace CMAPI.controllers;

[Microsoft.AspNetCore.Components.Route("api/users")]
[ApiController]
public class UsersController : ControllerBase
{
    private readonly UsersService _usersService;

    public UsersController(UsersService usersService)
    {
        _usersService = usersService;
    }

    [HttpGet("GetAllUsers")]
    public async Task<IActionResult> GetAllUsers()
    {
        try
        {
            var users = await _usersService.GetAllUsers();
            return Ok(users);
        }
        catch (ArgumentException ex)
        {
            // optional: more granular error handling if you want
            return BadRequest(new { message = ex.Message });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception)
        {
            // don’t expose internal details in production
            return StatusCode(500, "Internal Server Error");
        }
    }
}