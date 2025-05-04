using CMAPI.Services;
using Microsoft.AspNetCore.Mvc;

namespace CMAPI.controllers;
[Route("api/notification")]
[ApiController]
public class NotificationController : ControllerBase
{
    private readonly NotificationService _notificationService;
    private readonly JwtService _jwtService;

    public NotificationController(NotificationService notificationService, JwtService jwtService)
    {
        _notificationService = notificationService;
        _jwtService = jwtService;
    }

    [HttpGet("GetNotificationsByUser")]
    public async Task<IActionResult> GetNotificationsByUser()
    {
        var authHeader = Request.Headers["Authorization"].ToString();
        if (!authHeader.StartsWith("Bearer "))
            return BadRequest(new { message = "Missing or malformed Authorization header." });

        var token = authHeader["Bearer ".Length..].Trim();
        
        try
        {
            // 1. Extract the userId string from the token
            var userId = _jwtService.GetUserIdFromToken(token);

            var notifications = await _notificationService.GetNotificationsByUserAsync(userId);
            
            return Ok(notifications);
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