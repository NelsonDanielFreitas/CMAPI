using CMAPI.DTO.Avaria;
using CMAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Collections.Concurrent;
using System.IO;
using System.Threading.Tasks;
using System.Threading;

namespace CMAPI.controllers;
[Route("api/avaria")]
[ApiController]
//[Authorize]
public class AvariaController : ControllerBase
{
    private readonly AvariaService _avariaService;
    private readonly JwtService _jwtService;
    private readonly IDistributedCache _cache;
    private readonly PDFReportService _pdfReportService;
    private const string CACHE_KEY_PREFIX = "avarias_user_";
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = false,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public AvariaController(AvariaService avariaService, JwtService jwtService, IDistributedCache cache, PDFReportService pdfReportService)
    {
        _avariaService = avariaService;
        _jwtService = jwtService;
        _cache = cache;
        _pdfReportService = pdfReportService;
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

            // Try to get from cache first
            var cachedData = await _cache.GetStringAsync(cacheKey);
            if (!string.IsNullOrEmpty(cachedData))
            {
                var cachedAvaria = JsonSerializer.Deserialize<List<AvariaDTO>>(cachedData, _jsonOptions);
                return Ok(cachedAvaria);
            }

            // If not in cache, get from database
            var avarias = await _avariaService.GetAllAvariaByRoleUserAsync(userId);

            // Process images in parallel for better performance
            var optimizedAvaria = await Task.WhenAll(avarias.Select(async a => new AvariaDTO
            {
                Id = a.Id,
                Descricao = a.Descricao,
                Photo = string.IsNullOrEmpty(a.Photo) ? null : 
                    await Task.Run(() => {
                        try {
                            var imagePath = Path.Combine(_avariaService.GetStorageRoot(), Path.GetFileName(a.Photo));
                            if (System.IO.File.Exists(imagePath)) {
                                return Convert.ToBase64String(System.IO.File.ReadAllBytes(imagePath));
                            }
                            return null;
                        }
                        catch {
                            return null;
                        }
                    }),
                IdStatus = a.IdStatus,
                IdUrgencia = a.IdUrgencia,
                CreatedAt = a.CreatedAt,
                UserId = a.UserId,
                TechinicianId = a.TechinicianId,
                AssetId = a.AssetId,
                Localizacao = a.Localizacao,
                TempoResolverAvaria = a.TempoResolverAvaria
            }));

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

    [HttpGet("GetTechnicianStats/{technicianId}")]
    public async Task<IActionResult> GetTechnicianStats(Guid technicianId, [FromQuery] DateTime? startDate = null, [FromQuery] DateTime? endDate = null)
    {
        try
        {
            var stats = await _avariaService.GetTechnicianStatsAsync(technicianId, startDate, endDate);
            return Ok(stats);
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

    [HttpGet("GetAllTechniciansStats")]
    public async Task<IActionResult> GetAllTechniciansStats([FromQuery] DateTime? startDate = null, [FromQuery] DateTime? endDate = null)
    {
        try
        {
            var stats = await _avariaService.GetAllTechniciansStatsAsync(startDate, endDate);
            return Ok(stats);
        }
        catch (Exception)
        {
            return StatusCode(500, "Internal Server Error");
        }
    }

    [HttpGet("GetAvariaTypeFrequency")]
    public async Task<IActionResult> GetAvariaTypeFrequency([FromQuery] DateTime? startDate = null, [FromQuery] DateTime? endDate = null)
    {
        try
        {
            var frequency = await _avariaService.GetAvariaTypeFrequencyAsync(startDate, endDate);
            return Ok(frequency);
        }
        catch (Exception)
        {
            return StatusCode(500, "Internal Server Error");
        }
    }

    [HttpGet("GetGlobalAverageResolutionTime")]
    public async Task<IActionResult> GetGlobalAverageResolutionTime([FromQuery] DateTime? startDate = null, [FromQuery] DateTime? endDate = null)
    {
        try
        {
            var avgTime = await _avariaService.GetGlobalAverageResolutionTimeAsync(startDate, endDate);
            return Ok(new { AverageResolutionTimeHours = avgTime });
        }
        catch (Exception)
        {
            return StatusCode(500, "Internal Server Error");
        }
    }

    [HttpGet("GenerateTechnicianStatsReport")]
    public async Task<IActionResult> GenerateTechnicianStatsReport([FromQuery] string email, [FromQuery] DateTime? startDate = null, [FromQuery] DateTime? endDate = null)
    {
        try
        {
            if (string.IsNullOrEmpty(email))
            {
                return BadRequest(new { message = "Email is required" });
            }

            // Set default date range if not provided (last 6 months)
            startDate ??= DateTime.UtcNow.AddMonths(-6);
            endDate ??= DateTime.UtcNow;

            // Get statistics
            var stats = await _avariaService.GetAllTechniciansStatsAsync(startDate, endDate);

            // Generate PDF report and send via email
            var reportPath = await _pdfReportService.GenerateAndSendTechnicianStatsReportAsync(stats, startDate.Value, endDate.Value, email);

            return Ok(new { message = "Report has been generated and sent to your email" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Error generating report", error = ex.Message });
        }
    }
} 