using CMAPI.Data;
using CMAPI.DTO.Asset;
using CMAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace CMAPI.Services;

public class AssetService
{
    private readonly AppDbContext _context;
    private readonly IConfiguration _config;

    public AssetService(AppDbContext context, IConfiguration config)
    {
        _context = context;
        _config = config;
    }

    public async Task<bool> CreateAssetStatus(AssetStatusDTO asset)
    {
        if (await _context.AssetStatuses.AnyAsync(u => u.Name == asset.Name))
        {
            return false;
        }

        var assetStatus = new AssetStatus()
        {
            Id = new Guid(),
            Name = asset.Name
        };

        _context.AssetStatuses.Add(assetStatus);
        await _context.SaveChangesAsync();

        return true;
    }
    
    public async Task<bool> CreateAssetType(createAssetTypeDTO asset)
    {
        if (await _context.AssetTypes.AnyAsync(u => u.Name == asset.Name))
        {
            return false;
        }

        var assetType = new AssetType()
        {
            Id = new Guid(),
            Name = asset.Name
        };

        _context.AssetTypes.Add(assetType);
        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<bool> ValidateAssetStatus(Guid AssetStatusId)
    {
        return await _context.AssetStatuses.AnyAsync(u => u.Id == AssetStatusId);
    }

    public async Task<bool> ValidateAssetType(Guid AssetTypeId)
    {
        return await _context.AssetTypes.AnyAsync(u => u.Id == AssetTypeId);
    }

    public async Task<bool> AddAssetAsync(AddAssetDTO assetDto)
    {
        var asset = new Asset()
        {
            Id = Guid.NewGuid(), 
            Name = assetDto.Name,
            Description = assetDto.Description,
            Location = assetDto.Location,
            AssetTypeId = assetDto.AssetTypeId,
            InstalledDate = assetDto.InstalledDate,
            AssetStatusId = assetDto.AssetStatusId
        };

        _context.Assets.Add(asset);
        await _context.SaveChangesAsync();

        return true;
    }
    
    public async Task<IEnumerable<AssetDTO>> GetAssetsAsync(Guid? typeId = null, Guid? statusId = null)
    {
        var query = _context.Assets
            .AsNoTracking()
            .Include(a => a.AssetType)
            .Include(a => a.Status)
            .AsQueryable();

        if (typeId.HasValue)
            query = query.Where(a => a.AssetTypeId == typeId.Value);
        if (statusId.HasValue)
            query = query.Where(a => a.AssetStatusId == statusId.Value);

        return await query
            .Select(a => new AssetDTO
            {
                Id = a.Id,
                Name = a.Name,
                Description = a.Description,
                Location = a.Location,
                InstalledDate = a.InstalledDate,
                AssetTypeId = a.AssetTypeId,
                AssetTypeName = a.AssetType.Name,
                AssetStatusId = a.AssetStatusId,
                AssetStatusName = a.Status.Name
            })
            .ToListAsync();
    }

    public async Task<IEnumerable<AssetStatusDTO>> GetAllAssetStatusAsync()
    {
        var query = _context.AssetStatuses
            .AsNoTracking()
            .AsQueryable();

        return await query.Select(a => new AssetStatusDTO()
        {
            Id = a.Id,
            Name = a.Name
        }).ToListAsync();
    }
    
    public async Task<IEnumerable<AssetTypeDTO>> GetAllAssetTypeAsync()
    {
        var query = _context.AssetTypes
            .AsNoTracking()
            .AsQueryable();

        return await query.Select(a => new AssetTypeDTO()
        {
            Id = a.Id,
            Name = a.Name
        }).ToListAsync();
    }
    
    public async Task DeleteAssetAsync(Guid id)
    {
        var asset = await _context.Assets.FindAsync(id);
        if (asset == null)
        {
            throw new KeyNotFoundException("Asset not found");
        }
        var hasAvaria = await _context.Avaria.AnyAsync(a => a.AssetId == id);
        if (hasAvaria)
        {
            throw new InvalidOperationException("Cannot delete asset because it has associated Avaria");
        }
        _context.Assets.Remove(asset);
        await _context.SaveChangesAsync();
    }

    public async Task<bool> UpdateAssetAsync(AddAssetDTO assetDto, Guid id)
    {
        var existingAsset = await _context.Assets.FindAsync(id);
        if (existingAsset == null)
        {
            return false;
        }

        existingAsset.Name = assetDto.Name;
        existingAsset.Description = assetDto.Description;
        existingAsset.Location = assetDto.Location;
        existingAsset.InstalledDate = assetDto.InstalledDate;
        existingAsset.AssetTypeId = assetDto.AssetTypeId;
        existingAsset.AssetStatusId = assetDto.AssetStatusId;

        _context.Assets.Update(existingAsset);
        await _context.SaveChangesAsync();

        return true;
    }
}