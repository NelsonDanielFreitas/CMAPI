namespace CMAPI.DTO.Notification;

public class NotificationSubmit
{
    public string idNotification { get; set; }
    public string idTecnico { get; set; }
    public string action { get; set; }
    public string reason { get; set; }
}