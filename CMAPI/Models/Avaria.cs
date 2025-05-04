namespace CMAPI.Models;

public class Avaria
{
    public Guid Id { get; set; }
    public Guid? UserId { get; set; }
    public Guid? TechnicianId { get; set; }
    public Guid IdUrgencia { get; set; }
    public Guid IdStatus { get; set; }
    public Guid? AssetId { get; set; }
    public string Descricao { get; set; }
    public decimal Latitude { get; set; }
    public decimal Longitude { get; set; }
    public string Photo { get; set; }
    public TimeSpan TempoResolverAvaria { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string Localizacao { get; set; }

    public User User { get; set; }
    public User Technician { get; set; }
    public TipoUrgencia Urgencia { get; set; }
    public TipoStatusAvaria Status { get; set; }
    public Asset Asset { get; set; }
    public ICollection<AvariaComentario> Comentarios { get; set; }
    public ICollection<AvariaAtribuicao> Atribuicoes { get; set; }
    public ICollection<ChatMessage> ChatMessages { get; set; }
    public ICollection<Notification> Notifications { get; set; }
}