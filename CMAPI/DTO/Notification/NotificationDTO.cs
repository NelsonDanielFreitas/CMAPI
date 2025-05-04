namespace CMAPI.DTO.Notification;

public class NotificationDTO
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Message { get; set; }
    public DateTime CreatedAt { get; set; }
    public Guid AvariaAtribuicaoId { get; set; }
    public Guid AvariaId { get; set; }
    public string ResponseReason { get; set; }
    public string ResponseStatus { get; set; }
    public string Type { get; set; }
}