using CMAPI.Enum;

namespace CMAPI.Models;

public class Notification
{
    public Guid Id { get; set; }
    public Guid? UserId { get; set; }

    public NotificationType Type { get; set; }            // Type of notification
    public Guid? AvariaId { get; set; }                   // Optional link to Avaria
    public Avaria Avaria { get; set; }
    public Guid? AvariaAtribuicaoId { get; set; }         // Optional link to assignment
    public AvariaAtribuicao AvariaAtribuicao { get; set; }

    public string Message { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool IsRead { get; set; }

    public NotificationResponseStatus ResponseStatus { get; set; }  // Pending, Accepted, Declined
    public string? ResponseReason { get; set; }                       // If declined, reason why
    public DateTime? ResponseAt { get; set; }                        // When user responded

    public User User { get; set; }
}