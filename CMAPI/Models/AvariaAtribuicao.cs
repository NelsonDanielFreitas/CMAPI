namespace CMAPI.Models;

public class AvariaAtribuicao
{
    public Guid Id { get; set; }
    public Guid AvariaId { get; set; }
    public Guid AtribuidoPor { get; set; }
    public Guid? TechnicianId { get; set; }
    public DateTime AssignedAt { get; set; }

    public Avaria Avaria { get; set; }
    public User AssignedBy { get; set; }
    public User Technician { get; set; }
    
    public ICollection<Notification> Notifications { get; set; }

}
