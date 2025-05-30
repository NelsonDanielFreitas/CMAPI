using CMAPI.DTO.Avaria;
using CMAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Collections.Concurrent;

namespace CMAPI.controllers;
[Route("api/avaria")]
[ApiController]
//[Authorize]
public class AvariaController : ControllerBase
{
    private readonly AvariaService _avariaService;
    private readonly JwtService _jwtService;
    private readonly IDistributedCache _cache;
    private const string CACHE_KEY_PREFIX = "avarias_user_";
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = false,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public AvariaController(AvariaService avariaService, JwtService jwtService, IDistributedCache cache)
    {
        _avariaService = avariaService;
        _jwtService = jwtService;
        _cache = cache;
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
            {
                // Invalidate cache for this user
                await _cache.RemoveAsync($"{CACHE_KEY_PREFIX}{userId}");
                return Ok(new { message = "Avaria adicionada com sucesso" });
            }
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
            var userId = _jwtService.GetUserIdFromToken(token);
            var cacheKey = $"{CACHE_KEY_PREFIX}{userId}";

            // Try to get from cache first with optimized serialization
            var cachedData = await _cache.GetStringAsync(cacheKey);
            if (!string.IsNullOrEmpty(cachedData))
            {
                var cachedAvaria = JsonSerializer.Deserialize<List<AvariaDTO>>(cachedData, _jsonOptions);
                return Ok(cachedAvaria);
            }

            // If not in cache, get from database with optimized settings
            var avarias = await _avariaService.GetAllAvariaByRoleUserAsync(userId);

            // Optimize the data before caching
            var optimizedAvaria = avarias.Select(a => new AvariaDTO
            {
                Id = a.Id,
                Descricao = a.Descricao,
                Photo = a.Photo,
                IdStatus = a.IdStatus,
                IdUrgencia = a.IdUrgencia,
                CreatedAt = a.CreatedAt,
                UserId = a.UserId,
                TechinicianId = a.TechinicianId,
                AssetId = a.AssetId,
                Localizacao = a.Localizacao,
                TempoResolverAvaria = a.TempoResolverAvaria
            }).ToList();

            // Cache the results with optimized settings
            var cacheOptions = new DistributedCacheEntryOptions()
                .SetAbsoluteExpiration(TimeSpan.FromMinutes(5))
                .SetSlidingExpiration(TimeSpan.FromMinutes(2));

            var serializedData = JsonSerializer.Serialize(optimizedAvaria, _jsonOptions);
            await _cache.SetStringAsync(cacheKey, serializedData, cacheOptions);

            return Ok(optimizedAvaria);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception)
        {
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
            // don't expose internal details in production
            return StatusCode(500, "Internal Server Error");
        }
    }


    [HttpGet("GetAllTecnicos")]
    public async Task<IActionResult> GetAllTecnicos()
    {
        var tecnicos = await _avariaService.GetAllTecnicos();
        return Ok(tecnicos);
    }

    [HttpPut("UpdateAvaria/{id}")]
    public async Task<IActionResult> UpdateAvaria([FromBody] RequestEditAvariaDTO requestEdit, Guid id)
    {
        var authHeader = Request.Headers["Authorization"].ToString();
        if (!authHeader.StartsWith("Bearer "))
            return BadRequest(new { message = "Missing or malformed Authorization header." });

        var token = authHeader["Bearer ".Length..].Trim();
        try
        {
            var userId = _jwtService.GetUserIdFromToken(token);
            var updatedAvaria = await _avariaService.UpdateAvaria(requestEdit, id, userId);
            
            return Ok(new { message = "Funcionou" });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = "Internal Server Error" });
        }
    }
} 