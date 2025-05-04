using CMAPI.DTO.Asset;
using CMAPI.Models;

namespace CMAPI.DTO.Avaria;

public class AvariaDTO
{
    public Guid Id { get; set; }
    public User2DTO UserId { get; set; }
    public TechinicianDTO TechinicianId { get; set; }
    public TipoUrgenciaDTO IdUrgencia { get; set; }
    public StatusAvariaDTO IdStatus { get; set; }
    public AssetDTO AssetId { get; set; }
    public string Descricao { get; set; }
    public string? Photo { get; set; }
    public TimeSpan TempoResolverAvaria { get; set; }
    public DateTime CreatedAt { get; set; }
    public string Localizacao { get; set; }
}

public class User2DTO
{
    public Guid Id { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
}

public class TechinicianDTO
{
    public Guid Id { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
}

public class StatusAvariaDTO
{
    public Guid Id { get; set; }
    public string Name { get; set; }
}

