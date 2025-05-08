using CMAPI.DTO.Avaria;
using CMAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CMAPI.controllers;
[Route("api/avaria")]
[ApiController]
//[Authorize]
public class AvariaController : ControllerBase
{
    private readonly AvariaService _avariaService;
    private readonly JwtService _jwtService;

    public AvariaController(AvariaService avariaService, JwtService jwtService)
    {
        _avariaService = avariaService;
        _jwtService = jwtService;
    }
    
    [HttpPost("CreateTipoUrgencia")]
    public async Task<IActionResult> CreateTipoUrgencia([FromBody] CreateTipoUrgenciaDTO urgencia)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new { message = "Invalid Data" });
        }

        var isCreated = await _avariaService.CreateTipoUrgenciaAsync(urgencia);

        if (!isCreated)
        {
            return BadRequest(new { message = "Já existe o tipo de urgência" });
        }

        return Ok(new { message = "Urgência criada com sucesso" });
    }
    
    [HttpGet("GetAllTiposUrgencia")]
    public async Task<IActionResult> GetAllTiposUrgencia()
    {
        var urgencia = await _avariaService.GetAllTiposUrgencia();
        return Ok(urgencia);
    }

    [HttpGet("GetAllStatusAvaria")]
    public async Task<IActionResult> GetAllStatusAvaria()
    {
        var status = await _avariaService.GetAllStatusAvaria();
        return Ok(status);
    }

    [HttpPost("addAvaria")]
    //[Consumes("multipart/form-data")]
    public async Task<IActionResult> addAvaria([FromBody] CreateAvariaRequestDto avaria)
    {
        if (avaria == null || string.IsNullOrWhiteSpace(avaria.Photo))
            return BadRequest(new { message = "Invalid data"});
        
        var authHeader = Request.Headers["Authorization"].ToString();
        if (!authHeader.StartsWith("Bearer "))
            return BadRequest(new{message = "Missing or malformed Authorization header."});
        
        var token = authHeader.Substring("Bearer ".Length).Trim();
        
        try
        {
            var savedPath = await _avariaService.SaveBase64ImageAsync(avaria.Photo);
            //var savedPath = String.Empty;
            if (savedPath == null)
            {
                return BadRequest(new { message = "Invalid photo" });
            }
            
            var userId = _jwtService.GetUserIdFromToken(token);
            

            var isCreated = await _avariaService.AddAvariaAsync(avaria, savedPath, userId);

            if (isCreated)
                return Ok(new { message = "Avaria adicionada com sucesso" });
            else
                return BadRequest(new { message = "Erro ao adicionar avaria" });
            
        }
        catch (Exception ex)
        {
            //_logger.LogError(ex, "Error saving image");
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpGet("GetAllAvariaByRoleUser")]
    public async Task<IActionResult> GetAllAvariaByRoleUser()
    {
        var authHeader = Request.Headers["Authorization"].ToString();
        if (!authHeader.StartsWith("Bearer "))
            return BadRequest(new { message = "Missing or malformed Authorization header." });

        var token = authHeader["Bearer ".Length..].Trim();

        try
        {
            // 1. Extract the userId string from the token
            var userId = _jwtService.GetUserIdFromToken(token);

            // 2. Await the async service method so that 'avarias' is a real IEnumerable<AvariaDTO>
            var avarias = await _avariaService.GetAllAvariaByRoleUserAsync(userId);

            // 3. Return the DTOs to the caller
            return Ok(avarias);
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
    
    [HttpGet("GetAvariaById/{id}")]
    public async Task<IActionResult> GetAvariaById(Guid id)
    {
        try
        {
            var avaria = await _avariaService.GetAvariaByIdAsync(id);

            return Ok(avaria);
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


    [HttpGet("GetAllTecnicos")]
    public async Task<IActionResult> GetAllTecnicos()
    {
        var tecnicos = await _avariaService.GetAllTecnicos();
        return Ok(tecnicos);
    }
}