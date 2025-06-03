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
                isActive = a.isActive,
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
    
    public async Task<IEnumerable<RoleDto>> GetAllRoles()
    {
        var query = _context.Roles
            .AsNoTracking()
            .AsQueryable();

        return await query.Select(a => new RoleDto()
        {
            Id = a.Id,
            RoleName = a.RoleName
        }).ToListAsync();
    }


    public async Task<bool> UpdateUsers(UpdateUser updateUser)
    {
        // Decrypt the incoming ID, which should yield a GUID string
        var decryptedId = _crypto.Decrypt(updateUser.Id);

        // Try to parse it as a Guid
        if (!Guid.TryParse(decryptedId, out var userId))
        {
            // Invalid or corrupted ID
            return false;
        }

        // Look up the user by their Guid primary key
        var existingUser = await _context.Users.FindAsync(userId);
        if (existingUser == null)
        {
            return false;
        }

        // Update properties
        existingUser.FirstName   = updateUser.FirstName;
        existingUser.LastName    = updateUser.LastName;
        existingUser.Email       = updateUser.Email;
        existingUser.PhoneNumber = updateUser.PhoneNumber;
        existingUser.isActive    = updateUser.isActive;
        // If you meant to allow changing roles, uncomment & use this:
        existingUser.IdRole = updateUser.RoleId;

        _context.Users.Update(existingUser);
        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<bool> UpdateProfile(UpdateProfile updateProfile){
        var existingUser = await _context.Users.FindAsync(updateProfile.Id);
        if (existingUser == null)
        {
            return false;
        }

        existingUser.FirstName = updateProfile.FirstName;
        existingUser.LastName = updateProfile.LastName;
        existingUser.PhoneNumber = updateProfile.PhoneNumber;
        if (!string.IsNullOrEmpty(updateProfile.Password))
        {
            existingUser.Password = BCrypt.Net.BCrypt.HashPassword(updateProfile.Password);
        }
        _context.Users.Update(existingUser);
        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<(bool success, string message)> DeleteUser(string encryptedUserId)
    {
        try
        {
            // Replace spaces with plus signs and ensure proper base64 format
            var sanitizedId = encryptedUserId.Replace(" ", "+");
            
            // Decrypt the incoming ID
            var decryptedId = _crypto.Decrypt(sanitizedId);
            Console.WriteLine(decryptedId);

            // Try to parse it as a Guid
            if (!Guid.TryParse(decryptedId, out var userId))
            {
                return (false, "Invalid user ID format");
            }

            // Look up the user by their Guid primary key
            var user = await _context.Users
                .Include(u => u.AvariaReported)
                .Include(u => u.AvariaAssigned)
                .Include(u => u.AvariaComentarios)
                .Include(u => u.ChatMessagesSent)
                .Include(u => u.MessagesRead)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
            {
                return (false, "User not found");
            }

            // Check if user has any activity
            bool hasActivity = user.AvariaReported.Any() || 
                              user.AvariaAssigned.Any() || 
                              user.AvariaComentarios.Any() || 
                              user.ChatMessagesSent.Any() || 
                              user.MessagesRead.Any();

            if (hasActivity)
            {
                // Instead of deleting, mark as inactive
                user.isActive = false;
                await _context.SaveChangesAsync();
                return (true, "User has activity history. Account has been deactivated instead of deleted.");
            }

            // If no activity, proceed with deletion
            _context.Users.Remove(user);
            await _context.SaveChangesAsync();
            return (true, "User successfully deleted");
        }
        catch (Exception ex)
        {
            return (false, "An error occurred while processing the request");
        }
    }
}