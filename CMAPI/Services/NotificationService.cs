using CMAPI.Data;
using CMAPI.DTO.Notification;
using CMAPI.Enum;
using CMAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace CMAPI.Services;

public class NotificationService
{
    private readonly AppDbContext _context;
    private readonly IConfiguration _config;

    public NotificationService(AppDbContext context, IConfiguration config)
    {
        _context = context;
        _config = config;
    }

    public async Task<IEnumerable<NotificationDTO>> GetNotificationsByUserAsync(string userId)
    {
        // parse the incoming string userId to a Guid
        if (!Guid.TryParse(userId, out var uid))
            return Enumerable.Empty<NotificationDTO>();

        var notifications = await _context.Notifications
            .Where(n =>
                n.UserId == uid && 
                n.ResponseStatus == NotificationResponseStatus.Pending)
            .OrderByDescending(n => n.CreatedAt)
            .Select(n => new NotificationDTO
            {
                Id = n.Id,
                UserId = n.UserId!.Value,
                Message = n.Message,
                CreatedAt = n.CreatedAt,
                AvariaAtribuicaoId = n.AvariaAtribuicaoId ?? Guid.Empty,
                AvariaId = n.AvariaId ?? Guid.Empty,
                ResponseReason = n.ResponseReason ?? string.Empty,
                ResponseStatus = n.ResponseStatus.ToString(),
                Type = n.Type.ToString()
            })
            .ToListAsync();

        return notifications;
    }

    public async Task<bool> UpdateNotificationAsync(NotificationSubmit submit, Guid idNotification)
    {
        var existingNotification = await _context.Notifications.FindAsync(idNotification);

        if (existingNotification == null)
        {
            return false;
        }

        if (submit.action.Equals("ignore") || submit.action.Equals("handleNow"))
        {
            existingNotification.IsRead = true;
            existingNotification.ResponseAt = DateTime.UtcNow;
            existingNotification.ResponseStatus = NotificationResponseStatus.Accepted;
            
            _context.Notifications.Update(existingNotification);
            await _context.SaveChangesAsync();

            return true;
        }
        else if (submit.action.Equals("dismiss"))
        {
            existingNotification.IsRead = true;
            existingNotification.ResponseAt = DateTime.UtcNow;
            existingNotification.ResponseStatus = NotificationResponseStatus.Declined;
            existingNotification.ResponseReason = submit.reason;
            
            _context.Notifications.Update(existingNotification);
            await _context.SaveChangesAsync();
            
            var existingTecnico = await _context.Users.FindAsync(existingNotification.UserId);

            
            string message = $"Uma Avaria foi recusada pelo técnico {existingTecnico.Email}";
            NotificationType notifType = NotificationType.AvariaRecusada;

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
                    AvariaId = existingNotification.AvariaId,
                    ResponseReason = submit.reason,
                    AvariaAtribuicaoId = null
                })
                .ToList();

            _context.Notifications.AddRange(adminNotifications);
            await _context.SaveChangesAsync();

            var avaria = await _context.Avaria.FindAsync(existingNotification.AvariaId);

            avaria.TechnicianId = null;
            avaria.UpdatedAt = DateTime.UtcNow;

            _context.Avaria.Update(avaria);
            await _context.SaveChangesAsync();

            var AvariaAtribuicao = await _context.AvariaAtribuicoes.FindAsync(existingNotification.AvariaAtribuicaoId);
            
            if (AvariaAtribuicao == null)
            {
                return false;
            }

            _context.AvariaAtribuicoes.Remove(AvariaAtribuicao);
            await _context.SaveChangesAsync();

            return true;
        }
        else
        {
            return false;
        }
    }
}