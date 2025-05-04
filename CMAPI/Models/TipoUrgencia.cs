namespace CMAPI.Models;

public class TipoUrgencia
{
    public Guid Id { get; set; }
    public string Name { get; set; }

    public ICollection<Avaria> Avaria { get; set; }
}