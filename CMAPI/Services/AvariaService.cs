using CMAPI.Data;
using CMAPI.DTO.Asset;
using CMAPI.DTO.Avaria;
using CMAPI.Enum;
using CMAPI.Helper;
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
    
    public async Task<IEnumerable<TipoStatusAvariaDTO>> GetAllStatusAvaria()
    {
        var query = _context.TipoStatusAvaria
            .AsNoTracking()
            .AsQueryable();

        return await query.Select(a => new TipoStatusAvariaDTO()
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
                // 1) Get all active technicians
                var techUsers = await _context.Users
                    .Include(u => u.Role)
                    .Where(u => u.Role.RoleName.ToUpper() == "TECNICO" && u.isActive)   
                    .ToListAsync();                                       

                if (techUsers.Count > 0)
                {
                    var rnd = new Random();
                    int idx = rnd.Next(techUsers.Count);
                    technicianId = techUsers[idx].Id;
                }
                
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
            
            //Popular a tabela de Atribuicao Avaria para quando vem um tecnico diferente a null para a urgencia grave
            var newAvariaAtribuicao = new AvariaAtribuicao();
            if (newAvaria.TechnicianId != null)
            {
                Guid AdminAutomaticoId = Guid.NewGuid();
                var AdminUsersAutomatico = await _context.Users
                    .Include(u => u.Role)
                    .Where(u => u.Role.RoleName.ToUpper() == "ADMIN")   
                    .ToListAsync();                                       

                if (AdminUsersAutomatico.Count > 0)
                {
                    var rnd = new Random();
                    int idx = rnd.Next(AdminUsersAutomatico.Count);
                    AdminAutomaticoId = AdminUsersAutomatico[idx].Id;
                }
                newAvariaAtribuicao = new AvariaAtribuicao()
                {
                    Id = Guid.NewGuid(),
                    AvariaId = newAvaria.Id,
                    AtribuidoPor = AdminAutomaticoId,
                    TechnicianId = newAvaria.TechnicianId,
                    AssignedAt = newAvaria.CreatedAt
                };

                _context.AvariaAtribuicoes.Add(newAvariaAtribuicao);
                await _context.SaveChangesAsync();
            }
            
            //Parte para criar a notificação ao criar uma avaria com os diferentes tipos de possiveis notificações
            string message;
            NotificationType notifType;

            if (newAvaria.TechnicianId != null)
            {
                message = "Foi lhe atribuído uma nova avaria automática devido a ter estado de urgência grave. Se não conseguir resolver digite a razão.";
                notifType = NotificationType.AvariaAutomaticaNotificacao;
                var techNotification  = new Notification
                {
                    Id = Guid.NewGuid(),
                    UserId = newAvaria.TechnicianId,
                    Message = message,
                    Type = notifType,
                    CreatedAt = DateTime.UtcNow,
                    IsRead = false,
                    ResponseStatus = NotificationResponseStatus.Pending,
                    AvariaId = newAvaria.Id,
                    AvariaAtribuicaoId = newAvariaAtribuicao.Id,
                    ResponseReason = "Ainda sem nada"
                };
                
                _context.Notifications.Add(techNotification );
            }
            else
            {
                message = "Foi criada uma nova Avaria. O Administrador terá de ir selecionar o Técnico.";
                notifType = NotificationType.NovaAvaria;

                // fetch all admins
                var adminUsers = await _context.Users
                    .Include(u => u.Role)
                    .Where(u => u.Role.RoleName.ToUpper() == "ADMIN")
                    .ToListAsync();

                // create one Notification per admin
                var adminNotifications = adminUsers
                    .Select(admin => new Notification
                    {
                        Id = Guid.NewGuid(),
                        UserId = admin.Id,
                        Message = message,
                        Type = notifType,
                        CreatedAt = DateTime.UtcNow,
                        IsRead = false,
                        ResponseStatus = NotificationResponseStatus.Pending,
                        AvariaId = newAvaria.Id,
                        ResponseReason = "Ainda sem nada",
                        AvariaAtribuicaoId = null
                    })
                    .ToList();

                _context.Notifications.AddRange(adminNotifications);
            }

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

        // 2) Load user and role with optimized query
        var user = await _context.Users
            .AsNoTracking()
            .Include(u => u.Role)
            .Select(u => new { u.Id, u.Role.RoleName })
            .SingleOrDefaultAsync(u => u.Id == userId);
        if (user == null)
            throw new KeyNotFoundException("User not found");

        // 3) Build base query depending on role with optimized includes
        IQueryable<Avaria> query = user.RoleName switch
        {
            "ADMIN" => _context.Avaria,
            "TECNICO" => _context.Avaria.Where(a => a.TechnicianId == userId),
            "CLIENTE" => _context.Avaria.Where(a => a.UserId == userId),
            _ => throw new InvalidOperationException($"Unknown role '{user.RoleName}'")
        };

        // 4) Project into DTO with optimized includes and select only needed fields
        var dtos = await query
            .AsNoTracking()
            .Select(a => new AvariaDTO
            {
                Id = a.Id,
                UserId = new User2DTO
                {
                    Id = a.User.Id,
                    FirstName = a.User.FirstName,
                    LastName = a.User.LastName
                },
                TechinicianId = a.Technician == null ? null : new TechinicianDTO
                {
                    Id = a.Technician.Id,
                    FirstName = a.Technician.FirstName,
                    LastName = a.Technician.LastName
                },
                IdUrgencia = new TipoUrgenciaDTO
                {
                    Id = a.Urgencia.Id,
                    Name = a.Urgencia.Name
                },
                IdStatus = new StatusAvariaDTO
                {
                    Id = a.Status.Id,
                    Name = a.Status.Name
                },
                AssetId = a.Asset == null ? null : new AssetDTO
                {
                    Id = a.Asset.Id,
                    Name = a.Asset.Name,
                },
                Descricao = a.Descricao,
                Photo = a.Photo, // Just return the path, don't convert to base64 here
                TempoResolverAvaria = a.TempoResolverAvaria,
                CreatedAt = a.CreatedAt,
                Localizacao = a.Localizacao
            })
            .ToListAsync();

        return dtos;
    }
    
    
    public async Task<AvariaDTO> GetAvariaByIdAsync(Guid avariaId)
    {
        // 1) Retrieve the Avaria with all its navigation properties
        var avaria = await _context.Avaria
            .Include(a => a.User)
            .Include(a => a.Technician)
            .Include(a => a.Urgencia)
            .Include(a => a.Status)
            .Include(a => a.Asset)
            .SingleOrDefaultAsync(a => a.Id == avariaId);

        if (avaria == null)
            throw new KeyNotFoundException($"Avaria with Id '{avariaId}' not found.");

        // 2) Map to DTO
        return new AvariaDTO
        {
            Id                  = avaria.Id,
            UserId              = new User2DTO
            {
                Id        = avaria.User.Id,
                FirstName = avaria.User.FirstName,
                LastName  = avaria.User.LastName
            },
            TechinicianId       = avaria.Technician == null
                ? null
                : new TechinicianDTO
                {
                    Id        = avaria.Technician.Id,
                    FirstName = avaria.Technician.FirstName,
                    LastName  = avaria.Technician.LastName
                },
            IdUrgencia          = new TipoUrgenciaDTO
            {
                Id   = avaria.Urgencia.Id,
                Name = avaria.Urgencia.Name
            },
            IdStatus            = new StatusAvariaDTO
            {
                Id   = avaria.Status.Id,
                Name = avaria.Status.Name
            },
            AssetId             = avaria.Asset == null
                ? null
                : new AssetDTO
                {
                    Id   = avaria.Asset.Id,
                    Name = avaria.Asset.Name
                },
            Descricao           = avaria.Descricao,
            Photo               = string.IsNullOrEmpty(avaria.Photo)
                ? null
                : Convert.ToBase64String(
                      File.ReadAllBytes(
                          Path.Combine(_storageRoot, Path.GetFileName(avaria.Photo))
                      )
                  ),
            TempoResolverAvaria = avaria.TempoResolverAvaria,
            CreatedAt           = avaria.CreatedAt,
            Localizacao         = avaria.Localizacao
        };
    }

    public async Task<IEnumerable<UserTecnicoDTO>> GetAllTecnicos()
    {
        // assumes Role.Name holds the string "TECNICO"
        return await _context.Users
            .AsNoTracking()
            .Include(u => u.Role)
            .Where(u => u.Role.RoleName == "TECNICO")
            .Select(u => new UserTecnicoDTO
            {
                Id          = u.Id,
                Email       = u.Email,
                FirstName   = u.FirstName,
                LastName    = u.LastName,
                PhoneNumber = u.PhoneNumber
            })
            .ToListAsync();
    }

    public async Task<bool> UpdateAvaria(RequestEditAvariaDTO requestEdit, Guid id, string userId)
    {
        var existingAvaria = await _context.Avaria.FindAsync(id);
        bool newTechinician = false;
        
        if (existingAvaria == null)
            return false;
        
        if (!Guid.TryParse(userId, out var parseduserId))
            throw new ArgumentException("Invalid user id", nameof(userId));
        
        var currentUser = await _context.Users
            .AsNoTracking()
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.Id == parseduserId);

        if (currentUser.Role.RoleName.Equals("ADMIN"))
        {
            if (!Guid.TryParse(requestEdit.TechinicianId, out var parsedTechinicianId))
                throw new ArgumentException("Invalid user id", nameof(requestEdit.TechinicianId));
            existingAvaria.TechnicianId = parsedTechinicianId;
        }

        if (requestEdit.TempoResolver != null)
        {
            if (!string.IsNullOrWhiteSpace(requestEdit.TempoResolver))
            {
                if (TimeSpanParser.TryParsePortuguese(requestEdit.TempoResolver, out var ts))
                {
                    existingAvaria.TempoResolverAvaria = ts;
                    newTechinician = true;
                }
                else
                {
                    // handle parse failure: e.g. return false or throw
                    return false;
                }
            }
        }

        if (requestEdit.Descricao != existingAvaria.Descricao)
        {
            existingAvaria.Descricao = requestEdit.Descricao;
        }
        
        if (!Guid.TryParse(requestEdit.StatusAvaria, out var parsedStatusAvaria))
            throw new ArgumentException("Invalid user id", nameof(requestEdit.StatusAvaria));
        existingAvaria.IdStatus = parsedStatusAvaria;
        
        _context.Avaria.Update(existingAvaria);
        await _context.SaveChangesAsync();
        
        string message = "Uma avaria que você colocou foi atualizada";
        NotificationType notifType = NotificationType.StatusChanged;
        var techNotification  = new Notification
        {
            Id = Guid.NewGuid(),
            UserId = existingAvaria.UserId,
            Message = message,
            Type = notifType,
            CreatedAt = DateTime.UtcNow,
            IsRead = false,
            ResponseStatus = NotificationResponseStatus.Pending,
            AvariaId = existingAvaria.Id,
            AvariaAtribuicaoId = null,
            ResponseReason = "Ainda sem nada"
        };
                
        _context.Notifications.Add(techNotification );
        await _context.SaveChangesAsync();
        
        return true;
    }

    public string GetStorageRoot() => _storageRoot;

    public async Task<TechnicianStatsDTO> GetTechnicianStatsAsync(Guid technicianId, DateTime? startDate = null, DateTime? endDate = null)
    {
        // Set default date range if not provided (last 6 months)
        startDate ??= DateTime.UtcNow.AddMonths(-6);
        endDate ??= DateTime.UtcNow;

        // Get technician info
        var technician = await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == technicianId);

        if (technician == null)
            throw new KeyNotFoundException($"Technician with ID {technicianId} not found");

        // Get all avarias for this technician within date range
        var avarias = await _context.Avaria
            .AsNoTracking()
            .Include(a => a.Urgencia)
            .Include(a => a.Status)
            .Where(a => a.TechnicianId == technicianId && 
                       a.CreatedAt >= startDate && 
                       a.CreatedAt <= endDate)
            .ToListAsync();

        // Calculate basic stats
        var resolvedAvaria = avarias.Where(a => a.Status.Name == "Resolvido").ToList();
        var totalResolved = resolvedAvaria.Count;
        
        // Calculate average resolution time
        var avgResolutionTime = resolvedAvaria.Any() 
            ? resolvedAvaria.Average(a => a.TempoResolverAvaria.TotalHours)
            : 0;

        // Calculate on-time vs delayed resolutions
        var onTimeResolutions = resolvedAvaria.Count(a => 
            a.TempoResolverAvaria <= TimeSpan.FromHours(24)); // Assuming 24h is the target
        var delayedResolutions = totalResolved - onTimeResolutions;

        // Calculate stats by avaria type
        var avariaTypeStats = resolvedAvaria
            .GroupBy(a => a.Urgencia.Name)
            .Select(g => new AvariaTypeStatsDTO
            {
                AvariaType = g.Key,
                Count = g.Count(),
                AverageResolutionTime = g.Average(a => a.TempoResolverAvaria.TotalHours)
            })
            .ToList();

        // Calculate monthly stats
        var monthlyStats = resolvedAvaria
            .GroupBy(a => new { a.CreatedAt.Year, a.CreatedAt.Month })
            .Select(g => new MonthlyStatsDTO
            {
                Year = g.Key.Year,
                Month = g.Key.Month,
                TotalAvariaResolved = g.Count(),
                AverageResolutionTime = g.Average(a => a.TempoResolverAvaria.TotalHours),
                OnTimeResolutions = g.Count(a => a.TempoResolverAvaria <= TimeSpan.FromHours(24)),
                DelayedResolutions = g.Count(a => a.TempoResolverAvaria > TimeSpan.FromHours(24))
            })
            .OrderByDescending(m => m.Year)
            .ThenByDescending(m => m.Month)
            .ToList();

        return new TechnicianStatsDTO
        {
            TechnicianId = technician.Id,
            TechnicianName = $"{technician.FirstName} {technician.LastName}",
            TotalAvariaResolved = totalResolved,
            AverageResolutionTime = avgResolutionTime,
            OnTimeResolutions = onTimeResolutions,
            DelayedResolutions = delayedResolutions,
            AvariaTypeStats = avariaTypeStats,
            MonthlyStats = monthlyStats
        };
    }

    public async Task<IEnumerable<TechnicianStatsDTO>> GetAllTechniciansStatsAsync(DateTime? startDate = null, DateTime? endDate = null)
    {
        // Set default date range if not provided (last 6 months)
        startDate ??= DateTime.UtcNow.AddMonths(-6);
        endDate ??= DateTime.UtcNow;

        // Get all technicians with their avarias in a single query
        var technicians = await _context.Users
            .AsNoTracking()
            .Include(u => u.Role)
            .Where(u => u.Role.RoleName == "TECNICO")
            .Select(u => new
            {
                Technician = u,
                Avaria = _context.Avaria
                    .AsNoTracking()
                    .Include(a => a.Urgencia)
                    .Include(a => a.Status)
                    .Where(a => a.TechnicianId == u.Id && 
                               a.CreatedAt >= startDate && 
                               a.CreatedAt <= endDate)
                    .ToList()
            })
            .ToListAsync();

        var stats = new List<TechnicianStatsDTO>();

        foreach (var tech in technicians)
        {
            var resolvedAvaria = tech.Avaria.Where(a => a.Status.Name == "Concluído").ToList();
            var totalResolved = resolvedAvaria.Count;
            
            var avgResolutionTime = resolvedAvaria.Any() 
                ? resolvedAvaria.Average(a => a.TempoResolverAvaria.TotalHours)
                : 0;

            var onTimeResolutions = resolvedAvaria.Count(a => 
                a.TempoResolverAvaria <= TimeSpan.FromHours(24));
            var delayedResolutions = totalResolved - onTimeResolutions;

            var avariaTypeStats = resolvedAvaria
                .GroupBy(a => a.Urgencia.Name)
                .Select(g => new AvariaTypeStatsDTO
                {
                    AvariaType = g.Key,
                    Count = g.Count(),
                    AverageResolutionTime = g.Average(a => a.TempoResolverAvaria.TotalHours)
                })
                .ToList();

            var monthlyStats = resolvedAvaria
                .GroupBy(a => new { a.CreatedAt.Year, a.CreatedAt.Month })
                .Select(g => new MonthlyStatsDTO
                {
                    Year = g.Key.Year,
                    Month = g.Key.Month,
                    TotalAvariaResolved = g.Count(),
                    AverageResolutionTime = g.Average(a => a.TempoResolverAvaria.TotalHours),
                    OnTimeResolutions = g.Count(a => a.TempoResolverAvaria <= TimeSpan.FromHours(24)),
                    DelayedResolutions = g.Count(a => a.TempoResolverAvaria > TimeSpan.FromHours(24))
                })
                .OrderByDescending(m => m.Year)
                .ThenByDescending(m => m.Month)
                .ToList();

            stats.Add(new TechnicianStatsDTO
            {
                TechnicianId = tech.Technician.Id,
                TechnicianName = $"{tech.Technician.FirstName} {tech.Technician.LastName}",
                TotalAvariaResolved = totalResolved,
                AverageResolutionTime = avgResolutionTime,
                OnTimeResolutions = onTimeResolutions,
                DelayedResolutions = delayedResolutions,
                AvariaTypeStats = avariaTypeStats,
                MonthlyStats = monthlyStats
            });
        }

        return stats;
    }

    public async Task<Dictionary<string, int>> GetAvariaTypeFrequencyAsync(DateTime? startDate = null, DateTime? endDate = null)
    {
        // Set default date range if not provided (last 6 months)
        startDate ??= DateTime.UtcNow.AddMonths(-6);
        endDate ??= DateTime.UtcNow;

        var frequency = await _context.Avaria
            .AsNoTracking()
            .Include(a => a.Urgencia)
            .Where(a => a.CreatedAt >= startDate && a.CreatedAt <= endDate)
            .GroupBy(a => a.Urgencia.Name)
            .Select(g => new { Type = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Type, x => x.Count);

        return frequency;
    }

    public async Task<double> GetGlobalAverageResolutionTimeAsync(DateTime? startDate = null, DateTime? endDate = null)
    {
        // Set default date range if not provided (last 6 months)
        startDate ??= DateTime.UtcNow.AddMonths(-6);
        endDate ??= DateTime.UtcNow;

        var avgTime = await _context.Avaria
            .AsNoTracking()
            .Include(a => a.Status)
            .Where(a => a.Status.Name == "Concluído" && 
                       a.CreatedAt >= startDate && 
                       a.CreatedAt <= endDate)
            .AverageAsync(a => a.TempoResolverAvaria.TotalHours);

        return avgTime;
    }
}