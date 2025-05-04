using CMAPI.Data;
using CMAPI.DTO.Notification;
using CMAPI.Enum;
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
}