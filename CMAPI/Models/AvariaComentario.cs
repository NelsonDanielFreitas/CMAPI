namespace CMAPI.Models;

public class AvariaComentario
{
    public Guid Id { get; set; }
    public Guid AvariaId { get; set; }
    public Guid UserId { get; set; }
    public string Comentario { get; set; }
    public DateTime CreatedAt { get; set; }

    public Avaria Avaria { get; set; }
    public User User { get; set; }
}