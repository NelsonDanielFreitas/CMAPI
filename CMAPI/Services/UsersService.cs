using CMAPI.Data;
using CMAPI.DTO;
using CMAPI.DTO.Users;
using CMAPI.Models;
using CMAPI.Services.Encryption;
using Microsoft.EntityFrameworkCore;

namespace CMAPI.Services;

public class UsersService
{
    private readonly AppDbContext _context;
    private readonly IConfiguration _config;
    private readonly IEncryptionService  _crypto;

    public UsersService(AppDbContext context, IConfiguration config, IEncryptionService crypto)
    {
        _context = context;
        _config = config;
        _crypto = crypto;
    }
    
    public async Task<IEnumerable<UsersDTO>> GetAllUsers()
    {
        IQueryable<User> query = _context.Users;
        var dtos = await query
            .Include(a => a.Role)
            .Select(a => new UsersDTO
            {
                Id = _crypto.Encrypt(a.Id.ToString()),
                Email = a.Email,
                FirstName = a.FirstName,
                LastName = a.LastName,
                PhoneNumber = a.PhoneNumber,
                RoleId = a.Role == null
                    ? null
                    : new RoleDto
                    {
                        Id = a.Role.Id,
                        RoleName = a.Role.RoleName
                    }
            })
            .ToListAsync();

        return dtos;
    }
}