using CMAPI.Data;
using CMAPI.DTO.Asset;
using CMAPI.DTO.Avaria;
using CMAPI.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CMAPI.Services;

public class AvariaService 
{
    private readonly AppDbContext _context;
    private readonly IConfiguration _config;
    private readonly string _storageRoot;
    
    public AvariaService(AppDbContext context, IConfiguration config)
    {
        _context = context;
        _config = config;
        _storageRoot = config.GetValue<string>("PhotoStorage:RootPath")
                       ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
        if (!Directory.Exists(_storageRoot))
            Directory.CreateDirectory(_storageRoot);
    }
    
    public async Task<bool> CreateTipoUrgenciaAsync(CreateTipoUrgenciaDTO urgencia)
    {
        if (await _context.TipoUrgencia.AnyAsync(u => u.Name == urgencia.Name))
        {
            return false;
        }

        var tipoUrgencia = new TipoUrgencia()
        {
            Id = new Guid(),
            Name = urgencia.Name
        };

        _context.TipoUrgencia.Add(tipoUrgencia);
        await _context.SaveChangesAsync();

        return true;
    }
    
    public async Task<IEnumerable<TipoUrgenciaDTO>> GetAllTiposUrgencia()
    {
        var query = _context.TipoUrgencia
            .AsNoTracking()
            .AsQueryable();

        return await query.Select(a => new TipoUrgenciaDTO()
        {
            Id = a.Id,
            Name = a.Name
        }).ToListAsync();
    }
    
    public async Task<string> SaveBase64ImageAsync(string base64)
    {
        // Detect and strip data URI prefix (e.g. "data:image/png;base64,...")
        var data = base64;
        string extension = ".png";
        if (base64.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
        {
            var metaEnd = base64.IndexOf(",", StringComparison.Ordinal);
            var meta = base64.Substring(5, metaEnd - 5); // e.g. "image/jpeg;base64"
            var parts = meta.Split(';');
            if (parts.Length > 0 && parts[0].StartsWith("image/", StringComparison.OrdinalIgnoreCase))
            {
                var mime = parts[0].Substring(6); // e.g. "jpeg"
                extension = mime switch
                {
                    "jpeg" => ".jpg",
                    "png"  => ".png",
                    "gif"  => ".gif",
                    _       => $".{mime}"
                };
            }
            data = base64[(metaEnd + 1)..];
        }

        var bytes = Convert.FromBase64String(data);
        var fileName = $"{Guid.NewGuid()}{extension}";
        var fullPath = Path.Combine(_storageRoot, fileName);

        await File.WriteAllBytesAsync(fullPath, bytes);

        // Return relative URL/path
        var relative = Path.Combine("uploads", fileName).Replace("\\", "/");
        return relative;
    }


    public async Task<bool> AddAvariaAsync(CreateAvariaRequestDto avariaDto, string photoPath, string userIdString)
    {
        try
        {
            // Parse userId
            if (!Guid.TryParse(userIdString, out var parsedUserId))
                throw new ArgumentException("Invalid UserId GUID", nameof(userIdString));

            // Handle optional asset
            Guid? assetId = null;
            if (avariaDto.IsProdutoInstitucional == "Sim")
            {
                if (!Guid.TryParse(avariaDto.AssetId, out var parsedAssetId))
                    throw new ArgumentException("Invalid AssetId GUID", nameof(avariaDto.AssetId));
                assetId = parsedAssetId;
            }

            // Lookup TipoUrgencia
            var urgencia = await _context.TipoUrgencia
                .SingleOrDefaultAsync(u => u.Name == avariaDto.IdUrgencia);
            if (urgencia == null)
                throw new KeyNotFoundException($"Urgencia '{avariaDto.IdUrgencia}' not found.");

            // Lookup default status
            var status = await _context.TipoStatusAvaria
                .SingleOrDefaultAsync(s => s.Name == "Pendente");
            if (status == null)
                throw new InvalidOperationException("Default status 'Pendente' not configured.");

            // Decide technician assignment (example: only high urgency auto-assign)
            Guid? technicianId = null;
            if (urgencia.Name.Equals("Alta", StringComparison.OrdinalIgnoreCase))
            {
                // e.g. pick first available tech
                var techUser = await _context.Users
                    .Where(u => /* your criteria for technicians */ true)
                    .FirstOrDefaultAsync();
                technicianId = techUser?.Id;
            }

            var newAvaria = new Avaria
            {
                Id                    = Guid.NewGuid(),
                UserId                = parsedUserId,
                TechnicianId          = technicianId,
                IdUrgencia            = urgencia.Id,          
                IdStatus              = status.Id,
                AssetId               = assetId,
                Descricao             = avariaDto.Descricao,
                Latitude              = 0m,
                Longitude             = 0m,
                Photo                 = photoPath,
                TempoResolverAvaria   = TimeSpan.Zero,
                CreatedAt             = DateTime.UtcNow,
                UpdatedAt             = DateTime.UtcNow,
                Localizacao           = avariaDto.Localizacao
            };

            _context.Avaria.Add(newAvaria);
            await _context.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            // TODO: use your ILogger to log ex
            throw new InvalidOperationException("Failed to add Avaria", ex);
        }
    }


    public async Task<IEnumerable<AvariaDTO>> GetAllAvariaByRoleUserAsync(string userIdString)
    {
        // 1) Parse user ID
        if (!Guid.TryParse(userIdString, out var userId))
            throw new ArgumentException("Invalid UserId GUID", nameof(userIdString));

        // 2) Load user and role
        var user = await _context.Users
            .Include(u => u.Role)
            .SingleOrDefaultAsync(u => u.Id == userId);
        if (user == null)
            throw new KeyNotFoundException("User not found");

        // 3) Build base query depending on role
        IQueryable<Avaria> query = user.Role.RoleName switch
        {
            "ADMIN"      => _context.Avaria,
            "TECNICO" => _context.Avaria.Where(a => a.TechnicianId == userId),
            "CLIENTE"     => _context.Avaria.Where(a => a.UserId       == userId),
            _            => throw new InvalidOperationException($"Unknown role '{user.Role.RoleName}'")
        };

        // 4) Project into DTO
        var dtos = await query
            .Include(a => a.User)
            .Include(a => a.Technician)
            .Include(a => a.Urgencia)
            .Include(a => a.Status)
            .Include(a => a.Asset)
            .Select(a => new AvariaDTO
            {
                Id = a.Id,
                UserId = new User2DTO
                {
                    Id        = a.User.Id,
                    FirstName = a.User.FirstName,
                    LastName  = a.User.LastName
                },
                TechinicianId = a.Technician == null ? null : new TechinicianDTO
                {
                    Id        = a.Technician.Id,
                    FirstName = a.Technician.FirstName,
                    LastName  = a.Technician.LastName
                },
                IdUrgencia = new TipoUrgenciaDTO
                {
                    Id   = a.Urgencia.Id,
                    Name = a.Urgencia.Name
                },
                IdStatus = new StatusAvariaDTO
                {
                    Id   = a.Status.Id,
                    Name = a.Status.Name
                },
                AssetId = a.Asset == null ? null : new AssetDTO
                {
                    Id   = a.Asset.Id,
                    Name = a.Asset.Name,
                },
                Descricao           = a.Descricao,
                Photo               = string.IsNullOrEmpty(a.Photo)
                    ? null
                    : Convert.ToBase64String(
                        File.ReadAllBytes(
                            Path.Combine(_storageRoot, Path.GetFileName(a.Photo))
                        )
                    ),
                TempoResolverAvaria = a.TempoResolverAvaria,
                CreatedAt           = a.CreatedAt,
                Localizacao         = a.Localizacao
            })
            .ToListAsync();

        return dtos;
    }
}