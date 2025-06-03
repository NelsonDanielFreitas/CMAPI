using CMAPI.DTO.Users;
using CMAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CMAPI.controllers;

[Route("api/users")]
[ApiController]
[Authorize]
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
            // don't expose internal details in production
            return StatusCode(500, "Internal Server Error");
        }
    }
    
    [HttpGet("GetAllRoles")]
    public async Task<IActionResult> GetAllRoles()
    {
        var roles = await _usersService.GetAllRoles();
        return Ok(roles);
    }

    [HttpPut("UpdateUsers")]
    public async Task<IActionResult> UpdateUsers([FromBody] UpdateUser updateUser)
    {
        if (!ModelState.IsValid)
            return BadRequest(new { message = "Invalid data" });

        try
        {
            var isUpdated = await _usersService.UpdateUsers(updateUser);
            if (!isUpdated)
                return NotFound(new { message = "User not found" });

            return Ok(new { message = "User atualizado com sucesso" });
        }
        catch (DbUpdateException dbEx)
        {
            return StatusCode(500, new { message = "Database error: " + dbEx.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Server error: " + ex.Message });
        }
    }

    [HttpPut("UpdateProfile")]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfile updateProfile){
        if (!ModelState.IsValid)
            return BadRequest(new { message = "Invalid data" });

        try{
            var isUpdated = await _usersService.UpdateProfile(updateProfile);
            if (!isUpdated)
                return NotFound(new { message = "User not found" });

            return Ok(new { message = "Profile updated successfully" });
        }
        catch (DbUpdateException dbEx)
        {
            return StatusCode(500, new { message = "Database error: " + dbEx.Message });
        }
        
    }

    [HttpDelete]
    [Authorize(Roles = "ADMIN")]
    public async Task<IActionResult> DeleteUser([FromQuery] string id)
    {
        try
        {
            var (success, message) = await _usersService.DeleteUser(id);
            
            if (!success)
            {
                return BadRequest(new { message });
            }

            return Ok(new { message });
        }
        catch (FormatException)
        {
            return BadRequest(new { message = "Invalid user ID format" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "An error occurred while processing your request" });
        }
    }
}