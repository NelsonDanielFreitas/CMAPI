using CMAPI.DTO.Asset;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace CMAPI.controllers;
using CMAPI.DTO;
using CMAPI.Models;
using CMAPI.Services;
using Microsoft.AspNetCore.Mvc;
[Route("api/asset")]
[ApiController]
[Authorize]
public class AssetController : ControllerBase
{
    private readonly AssetService _assetService;
    private readonly JwtService _jwtService;

    public AssetController(AssetService assetService, JwtService jwtService)
    {
        _assetService = assetService;
        _jwtService = jwtService;
    }

    [HttpPost("createAssetStatus")]
    public async Task<IActionResult> createAssetStatus([FromBody] AssetStatusDTO asset)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new { message = "Invalid Data" });
        }

        var isCreated = await _assetService.CreateAssetStatus(asset);

        if (!isCreated)
        {
            return BadRequest(new { message = "Já existe o estado introduzido" });
        }

        return Ok(new { message = "Estado criado com sucesso" });
    }
    
    [HttpPost("createAssetType")]
    public async Task<IActionResult> createAssetType([FromBody] createAssetTypeDTO asset)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new { message = "Invalid Data" });
        }

        var isCreated = await _assetService.CreateAssetType(asset);

        if (!isCreated)
        {
            return BadRequest(new { message = "Já existe o tipo de asset introduzido" });
        }

        return Ok(new { message = "Tipo de asset criado com sucesso" });
    }

    [HttpPost("AddAsset")]
    public async Task<IActionResult> AddAsset([FromBody] AddAssetDTO addAsset)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new { message = "Invalid data" });
        }

        var checkAssetStatus = await _assetService.ValidateAssetStatus(addAsset.AssetStatusId);

        if (!checkAssetStatus)
        {
            return BadRequest(new { message = "Asset Status introduzido não existe" });
        }

        var checkAssetTypes = await _assetService.ValidateAssetType(addAsset.AssetTypeId);

        if (!checkAssetTypes)
        {
            return BadRequest(new { message = "Asset Type introduzido não existe" });
        }

        //var isCreated = await _assetService.AddAssetAsync(addAsset);

        /*if (!isCreated)
        {
            return BadRequest(new { message = "Erro ao criar asset" });
        }

        return Ok(new { message = "Asset criado com sucesso" });*/
        
        try
        {
            var isCreated = await _assetService.AddAssetAsync(addAsset);
            if (!isCreated)
                return BadRequest(new { message = "Erro ao criar asset" });

            return Ok(new { message = "Asset criado com sucesso" });
        }
        catch (DbUpdateException dbEx)
        {
            // You might hide dbEx.InnerException.Message in production
            return StatusCode(500, new { message = "Database error: " + dbEx.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Server error: " + ex.Message });
        }
    }
    
    [HttpGet("GetAssets")]
    public async Task<IActionResult> GetAssets([FromQuery] Guid? typeId, [FromQuery] Guid? statusId)
    {
        var authHeader = HttpContext.Request.Headers["Authorization"].ToString();

        // Optional: extract the token if it's in the format "Bearer <token>"
        //var token = authHeader.StartsWith("Bearer ") ? authHeader.Substring("Bearer ".Length).Trim() : authHeader;
        //var see = _jwtService.ValidateAccessToken(token);
        var assets = await _assetService.GetAssetsAsync(typeId, statusId);
        return Ok(assets);
    }

    [HttpGet("GetAllAssetStatus")]
    public async Task<IActionResult> GetAllAssetStatus()
    {
        var assetsStatus = await _assetService.GetAllAssetStatusAsync();
        return Ok(assetsStatus);
    } 
    
    [HttpGet("GetAllAssetType")]
    public async Task<IActionResult> GetAllAssetType()
    {
        var assetsType = await _assetService.GetAllAssetTypeAsync();
        return Ok(assetsType);
    }

    [HttpDelete("DeleteAsset/{id}")]
    public async Task<IActionResult> DeleteAsset(Guid id)
    {
        try
        {
            await _assetService.DeleteAssetAsync(id);
            return Ok(new { message = "Asset eliminado com sucesso"}); 
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { message = "Asset not found" });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "An error occurred while deleting the asset" });
        }
    }

    [HttpPut("UpdateAsset/{id}")]
    public async Task<IActionResult> UpdateAsset([FromBody] AddAssetDTO asset, Guid id)
    {
        if (!ModelState.IsValid)
            return BadRequest(new { message = "Invalid data" });

        // Validate referenced status and type
        if (!await _assetService.ValidateAssetStatus(asset.AssetStatusId))
            return BadRequest(new { message = "Asset Status introduzido não existe" });

        if (!await _assetService.ValidateAssetType(asset.AssetTypeId))
            return BadRequest(new { message = "Asset Type introduzido não existe" });

        try
        {
            var isUpdated = await _assetService.UpdateAssetAsync(asset, id);
            if (!isUpdated)
                return NotFound(new { message = "Asset not found" });

            return Ok(new { message = "Asset editado com sucesso" });
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
}